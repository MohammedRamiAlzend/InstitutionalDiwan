#!/usr/bin/env node
// Adds [EndpointSummary]/[EndpointDescription] attributes and XML <param> documentation
// (with concrete examples) to every endpoint in InstitutionalDiwan.Server/Controllers.
// Idempotent: skips endpoints/parameters that already carry the docs.
import fs from "node:fs";
import path from "node:path";

const CTRL_DIR = path.join("InstitutionalDiwan.Server", "Controllers");
const CONTRACT_ROOTS = [path.join("InstitutionalDiwan.Server", "Contracts")];

/* ---------------------------------- text utils ---------------------------------- */
const collapse = (s) => s.replace(/\s+/g, " ").trim();
const stripXml = (s) =>
  collapse(
    s
      .replace(/<\/?c>/g, "")
      .replace(/<see[^>]*>([\s\S]*?)<\/see>/g, "$1")
      .replace(/<see[^>]*\/>/g, "")
      .replace(/<\/?[a-zA-Z][^>]*>/g, "")
      .replace(/&lt;/g, "<")
      .replace(/&gt;/g, ">")
      .replace(/&quot;/g, '"')
      .replace(/&amp;/g, "&"),
  );
const escAttr = (s) => s.replace(/\\/g, "\\\\").replace(/"/g, '\\"');

const humanize = (name) => {
  const words = name
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1 $2")
    .split(/[\s_]+/)
    .filter(Boolean);
  return words
    .map((w, i) => (i === 0 ? w.charAt(0).toUpperCase() + w.slice(1) : w.toLowerCase()))
    .join(" ");
};

/* ---------------------------------- sample values ---------------------------------- */
const GUID_EXAMPLES = {
  employeeid: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  userid: "5f8b32a7-c941-4e62-bd07-2c86ea754921",
  documentid: "7c9e3b42-8a61-4d52-9f17-2ab85c6e7934",
  departmentid: "e7a62f43-9b82-4d01-8c2e-7ab91e506d42",
  parentdepartmentid: "8b4d1f70-6c93-42a5-b18e-4d7a3c5f9e12",
  primarydepartmentid: "e7a62f43-9b82-4d01-8c2e-7ab91e506d42",
  internaldepartmentid: "8b4d1f70-6c93-42a5-b18e-4d7a3c5f9e12",
  centraldiwandepartmentid: "1c3a5e80-2f47-4b96-a5d1-6e0c8b3f7a25",
  roleid: "9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72",
  registryid: "b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33",
  movementid: "1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84",
  grantid: "c8e5a2f7-6d31-4b90-a4e8-3f7b1d9c6e05",
  assignmentid: "d4b7f9c2-8e15-4a63-b2d7-5c0e9f8a1b46",
  documentnumberid: "e5c8a3d9-4f72-4b16-9a3d-8e1b6c7f0a92",
  externalorganizationid: "f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53",
  organizationtypeid: "3c5e9a81-7d42-4b6f-a0c9-5e8b2f6d7a31",
  documenttypeid: "4d6f1b92-8e53-4c7a-b1da-6f9c3a7e8b42",
  directionid: "5e7a2c03-9f64-4d8b-8e2d-7a0d4b8f9c53",
  confidentialitylevelid: "6f8b3d14-a075-4e9c-93fc-8b1e5c9a0d64",
  prioritylevelid: "7a9c4e25-b186-4afd-b40e-9c2f6d0b1e75",
  assignmenttypeid: "2b6e4f18-d935-470c-a713-5e9f2a4b8c60",
  accesslevelid: "8b5c2d90-c174-4e0a-95fa-4a3d8e1b6c7f",
  responsibleemployeeid: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
};
const STRING_EXAMPLES = {
  username: "ahmed.khalil",
  password: "Str0ngP@ss!",
  oldpassword: "OldP@ssw0rd",
  newpassword: "NewP@ssw0rd123",
  fullnamear: "أحمد محمد علي",
  fullnameen: "Ahmed Mohammed Ali",
  employeenumber: "EMP-2024-001",
  jobtitlear: "مطور برمجيات",
  jobtitleen: "Software Engineer",
  email: "ahmed.ali@example.com",
  phone: "+966501234567",
  code: "CODE-01",
  namear: "الإدارة المالية",
  nameen: "Finance Department",
  desc: "Finance Department",
  descar: "الإدارة المالية",
  lookup: "document-types",
  permissioncode: "DOCUMENT_VIEW",
  title: "طلب تصريح زيارة لوفدة",
  subjectsummary: "طلب تصريح زيارة لوفدة خارجية",
  reason: "Duplicate entry; replaced by a corrected copy.",
  notes: "Reviewed and forwarded to the responsible department.",
  personname: "أحمد محمد علي",
  contactinfo: "info@example.org | +966112345678",
  formatpattern: "DOC-{YEAR}-{SEQ:0000}",
  officialnumbertext: "1447/2026",
  registrynumbertext: "FIN-001/2026",
  externalreferencenumber: "EXT-2026-114",
  logopath: "/assets/logo.png",
  defaulttimezone: "Asia/Riyadh",
  institutionnamear: "الديوان المركزي",
  institutionnameen: "Central Diwan",
  centraldiwantransferreason: "Reassignment of the Central Diwan designation",
};
const INT_EXAMPLES = { numberingyear: "2026", year: "2026", id: "101", scopetypeid: "1" };
const DATE_EXAMPLES = {
  startdate: "2026-01-01",
  enddate: "2026-12-31",
  documentdate: "2026-01-15",
  registrydate: "2026-01-15",
  expiresat: "2026-12-31",
  externaldocumentdate: "2026-01-10",
};
const BOOL_EXAMPLES = {
  mustchangepassword: "true",
  includeinactive: "true",
  includesubtree: "true",
  isprimary: "true",
  cansendexternal: "true",
  canreceiveexternal: "true",
};

function exampleFor(name, type) {
  const n = name.toLowerCase();
  const base = type.replace(/\?$/, "");
  if (base === "Guid") return GUID_EXAMPLES[n] || "3fa85f64-5717-4562-b3fc-2c963f66afa6";
  if (base === "bool") return BOOL_EXAMPLES[n] || "true";
  if (base === "int") return INT_EXAMPLES[n] || "1";
  if (base === "DateTime") return DATE_EXAMPLES[n] || "2026-01-15";
  if (base === "string") return STRING_EXAMPLES[n] || "Sample value";
  if (base === "decimal" || base === "double" || base === "float") return "1.0";
  return "Example value";
}

const LABELS = {
  username: "Username",
  password: "Password",
  oldpassword: "Current password",
  newpassword: "New password",
  fullnamear: "Full name in Arabic",
  fullnameen: "Full name in English",
  employeenumber: "Employee number",
  jobtitlear: "Job title in Arabic",
  jobtitleen: "Job title in English",
  email: "Email address",
  phone: "Phone number",
  code: "Code",
  namear: "Name in Arabic",
  nameen: "Name in English",
  desc: "English label",
  descar: "Arabic label",
  lookup: "Lookup catalog key",
  permissioncode: "Permission code",
  id: "Catalog entry id",
  includeinactive: "Include inactive records",
  numberingyear: "Numbering year",
  personname: "Person name",
  contactinfo: "Contact information",
  formatpattern: "Format pattern",
  notes: "Notes",
  reason: "Reason",
  title: "Document title",
  subjectsummary: "Subject summary",
  officialnumbertext: "Official number text",
  registrynumbertext: "Department register number text",
  externalreferencenumber: "External reference number",
  logopath: "Logo file path",
  defaulttimezone: "Default time zone",
  institutionnamear: "Institution name in Arabic",
  institutionnameen: "Institution name in English",
  centraldiwantransferreason: "Central Diwan transfer reason",
  numberingmodeid: "Numbering mode id",
  numberingresetpolicyid: "Numbering reset policy id",
  resetpolicyid: "Reset policy id",
};

function friendlyType(t) {
  const base = t.replace(/\?$/, "");
  if (base === "Guid") return "GUID";
  if (base === "bool") return "boolean";
  if (base === "int") return "integer";
  if (base === "DateTime") return "date";
  if (base === "string") return "string";
  if (base === "decimal" || base === "double" || base === "float") return "number";
  return base;
}

function labelFor(name, type) {
  const key = name.toLowerCase();
  if (LABELS[key]) return LABELS[key];
  const base = type.replace(/\?$/, "");
  if (base === "Guid" && /id$/i.test(name)) {
    return humanize(name.replace(/id$/i, "")) + " unique identifier";
  }
  return humanize(name);
}

/* ---------------------------------- C# snippet parsing ---------------------------------- */
function splitTop(text) {
  const out = [];
  let depth = 0,
    start = 0,
    inStr = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inStr) {
      if (c === "\\") i++;
      else if (c === '"') inStr = false;
      continue;
    }
    if (c === '"') inStr = true;
    else if (c === "(" || c === "[" || c === "{" || c === "<") depth++;
    else if (c === ")" || c === "]" || c === "}" || c === ">") depth--;
    else if (c === "," && depth === 0) {
      out.push(text.slice(start, i));
      start = i + 1;
    }
  }
  const last = text.slice(start);
  if (last.trim()) out.push(last);
  return out;
}

function stripAttrs(chunk) {
  let s = chunk;
  const attrs = [];
  for (;;) {
    const t = s.trimStart();
    if (!t.startsWith("[")) break;
    let depth = 0,
      inStr = false,
      end = -1;
    for (let i = 0; i < t.length; i++) {
      const c = t[i];
      if (inStr) {
        if (c === "\\") i++;
        else if (c === '"') inStr = false;
        continue;
      }
      if (c === '"') inStr = true;
      else if (c === "[") depth++;
      else if (c === "]") {
        depth--;
        if (depth === 0) {
          end = i;
          break;
        }
      }
    }
    if (end === -1) break;
    attrs.push(t.slice(0, end + 1));
    s = t.slice(end + 1);
  }
  return { attrs, rest: s.trim() };
}

function findTopEq(s) {
  let depth = 0,
    inStr = false;
  for (let i = 0; i < s.length; i++) {
    const c = s[i];
    if (inStr) {
      if (c === "\\") i++;
      else if (c === '"') inStr = false;
      continue;
    }
    if (c === '"') inStr = true;
    else if ("([{<".includes(c)) depth++;
    else if (")]}>" .includes(c)) depth--;
    else if (c === "=" && depth === 0 && s[i + 1] !== ">" && s[i + 1] !== "=" && s[i - 1] !== "=")
      return i;
  }
  return -1;
}

function parseParams(rawList) {
  return splitTop(rawList).map((chunk0) => {
    const chunk = chunk0.trim();
    const { attrs, rest } = stripAttrs(chunk);
    let decl = rest,
      def;
    const eq = findTopEq(rest);
    if (eq !== -1) {
      decl = rest.slice(0, eq).trim();
      def = rest.slice(eq + 1).trim();
    }
    const m = decl.match(/(\w+)\s*$/);
    const name = m ? m[1] : decl;
    const type = decl.slice(0, decl.length - name.length).trim();
    return { chunk, attrs, type, name, defaultVal: def, hasDefault: def !== undefined };
  });
}

function extractParen(text, openIdx) {
  let depth = 0,
    inStr = false;
  for (let i = openIdx; i < text.length; i++) {
    const c = text[i];
    if (inStr) {
      if (c === "\\") i++;
      else if (c === '"') inStr = false;
      continue;
    }
    if (c === '"') inStr = true;
    else if (c === "(") depth++;
    else if (c === ")") {
      depth--;
      if (depth === 0) return { inner: text.slice(openIdx + 1, i), end: i };
    }
  }
  return null;
}

const attrDescription = (attrs) => {
  for (const a of attrs) {
    const m = a.match(/\[property:\s*Description\("((?:[^"\\]|\\.)*)"\)\]/);
    if (m) return m[1].replace(/\\"/g, '"').replace(/\\\\/g, "\\");
  }
  return null;
};

function walkCs(dir, out = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walkCs(p, out);
    else if (e.name.endsWith(".cs")) out.push(p);
  }
  return out;
}

function buildContractIndex() {
  const idx = new Map();
  for (const root of CONTRACT_ROOTS) {
    if (!fs.existsSync(root)) continue;
    for (const f of walkCs(root)) {
      const src = fs.readFileSync(f, "utf8");
      const re = /\brecord\s+(\w+)\s*\(/g;
      let m;
      while ((m = re.exec(src))) {
        const openIdx = m.index + m[0].length - 1;
        const bal = extractParen(src, openIdx);
        if (!bal) continue;
        idx.set(m[1], { fields: parseParams(bal.inner), file: f });
      }
    }
  }
  return idx;
}

/* ---------------------------------- doc/attribute builders ---------------------------------- */
function parseDoc(docLines) {
  const text = docLines.map((l) => l.replace(/^\s*\/\/\/ ?/, "")).join("\n");
  const summary = (text.match(/<summary>([\s\S]*?)<\/summary>/) || [])[1] || "";
  const remarks = (text.match(/<remarks>([\s\S]*?)<\/remarks>/) || [])[1] || "";
  const responses = [];
  const rre = /<response code="(\d+)">([\s\S]*?)<\/response>/g;
  let m;
  while ((m = rre.exec(text))) responses.push({ code: m[1], text: stripXml(m[2]) });
  const existingParams = new Set();
  const pre = /<param name="([^"]+)"/g;
  while ((m = pre.exec(text))) existingParams.add(m[1]);
  return { summary, remarks, responses, existingParams };
}

function authSentence(actionAttrs, classAttrs) {
  if (/\[AllowAnonymous\]/.test(actionAttrs)) return "";
  const pol = (s) => (s.match(/\[Authorize\(Policy\s*=\s*Permissions\.(\w+)\)/) || [])[1];
  const p = pol(actionAttrs) || pol(classAttrs);
  if (p) return `Requires the ${p.replace(/([a-z0-9])([A-Z])/g, "$1_$2").toUpperCase()} permission.`;
  if (/\[Authorize(\(|\])/.test(actionAttrs) || /\[Authorize(\(|\])/.test(classAttrs))
    return "Requires a valid access token.";
  return "";
}

function buildSummary(doc, methodName) {
  const s = stripXml(doc.summary);
  return s || humanize(methodName) + ".";
}

function buildDescription(doc, auth, methodName) {
  const parts = [];
  const s = stripXml(doc.summary);
  if (s) parts.push(s);
  const r = stripXml(doc.remarks);
  if (r) parts.push(r);
  if (!parts.length) parts.push(humanize(methodName) + ".");
  const combined = parts.join(" ");
  if (auth) {
    const perm = auth.match(/Requires the ([A-Z_]+) permission/);
    if (perm) {
      if (!combined.includes(perm[1])) parts.push(auth);
    } else if (!combined.toLowerCase().includes("access token")) parts.push(auth);
  }
  const taken = [];
  for (const x of doc.responses) {
    const entry = `${x.code} (${x.text})`;
    const candidate = "Responses: " + taken.concat(entry).join(", ");
    if (taken.length && parts.join(" ").length + candidate.length > 700) break;
    taken.push(entry);
  }
  if (taken.length) parts.push("Responses: " + taken.join(", "));
  let desc = parts.join(" ");
  if (desc.length > 740) desc = desc.slice(0, 737).replace(/\s+\S*$/, "") + "…";
  return desc;
}

function describeSimple(p, loc, xml) {
  const bits = [friendlyType(p.type)];
  const optional = p.type.endsWith("?") || p.hasDefault;
  if (optional) bits.push("optional");
  bits.push(loc);
  if (p.hasDefault && p.defaultVal !== "null")
    bits.push(xml ? `default <c>${p.defaultVal}</c>` : `default ${p.defaultVal}`);
  const ex = exampleFor(p.name, p.type);
  const exPart = xml ? `Example: <c>${ex}</c>.` : `Example: ${ex}.`;
  return `${labelFor(p.name, p.type)} (${bits.join(", ")}). ${exPart}`;
}

function fieldText(f) {
  const bits = [friendlyType(f.type)];
  const optional = f.type.endsWith("?") || f.hasDefault;
  bits.push(optional ? "optional" : "required");
  if (f.hasDefault && f.defaultVal !== "null") bits.push(`default <c>${f.defaultVal}</c>`);
  const ex = exampleFor(f.name, f.type);
  return `${labelFor(f.name, f.type)} (${bits.join(", ")}). Example: <c>${ex}</c>.`;
}

function bodyParamLines(p, contracts, indent) {
  const lines = [`${indent}/// <param name="${p.name}">`];
  const rec = contracts.get(p.type);
  if (!rec) {
    lines.push(`${indent}/// The request payload (fields defined by <c>${p.type}</c>).`);
    lines.push(`${indent}/// </param>`);
    return { lines, found: false };
  }
  lines.push(`${indent}/// The request payload with the following fields:`);
  lines.push(`${indent}/// <list type="bullet">`);
  for (const f of rec.fields) {
    const existing = attrDescription(f.attrs);
    const text = existing || fieldText(f);
    lines.push(`${indent}/// <item><term>${f.name}</term><description>${text}</description></item>`);
  }
  lines.push(`${indent}/// </list>`);
  lines.push(`${indent}/// </param>`);
  return { lines, found: true };
}

/* ---------------------------------- main ---------------------------------- */
const contracts = buildContractIndex();
const files = fs.readdirSync(CTRL_DIR).filter((f) => f.endsWith(".cs")).sort();
const warnings = [];
const totals = { endpoints: 0, summaries: 0, descriptions: 0, paramDocs: 0, paramDescs: 0 };
const perFile = [];

for (const file of files) {
  const full = path.join(CTRL_DIR, file);
  const content = fs.readFileSync(full, "utf8");
  const eol = content.includes("\r\n") ? "\r\n" : "\n";
  let lines = content.split(/\r?\n/);

  // class-level attributes (for Route template, Authorize policy)
  const classIdx = lines.findIndex((l) => /\bclass\s+\w+/.test(l) && !/interface|record|enum/.test(l));
  let classStart = classIdx;
  while (classStart > 0 && /^\s*\[/.test(lines[classStart - 1])) classStart--;
  const classAttrs = lines.slice(classStart, classIdx).join("\n");
  const classRoute = (classAttrs.match(/\[Route\("([^"]*)"\)\]/) || [])[1] || "";

  const httpRe = /^\s*\[Http(?:Get|Post|Put|Delete|Patch)(?:\(|\])/;
  const httpIdxs = [];
  lines.forEach((l, i) => {
    if (httpRe.test(l)) httpIdxs.push(i);
  });

  const stats = { endpoints: httpIdxs.length, summaries: 0, descriptions: 0, paramDocs: 0, paramDescs: 0 };

  // Parse all endpoints first (indices valid because we edit bottom-up)
  const eps = httpIdxs.map((httpIdx) => {
    let attrStart = httpIdx;
    while (attrStart > 0 && /^\s*\[/.test(lines[attrStart - 1])) attrStart--;
    let attrEnd = httpIdx + 1;
    while (attrEnd < lines.length && /^\s*\[/.test(lines[attrEnd])) attrEnd++;
    let docStart = attrStart;
    while (docStart > 0 && /^\s*\/\/\/\s*/.test(lines[docStart - 1])) docStart--;
    const docLines = lines.slice(docStart, attrStart);
    let bodyIdx = -1;
    for (let i = attrEnd; i < lines.length; i++) {
      if (lines[i].includes("{")) {
        bodyIdx = i;
        break;
      }
    }
    const httpLine = lines[httpIdx];
    const methodRoute = (httpLine.match(/\[Http\w+\("([^"]*)"\)/) || [])[1] || "";
    const method = (httpLine.match(/\[Http\w+(?:\("([^"]*)"\))?\]/) || [])[0] || "";
    const actionAttrs = lines.slice(attrStart, attrEnd).join("\n");
    return { httpIdx, attrStart, attrEnd, docStart, docLines, bodyIdx, classRoute, methodRoute, actionAttrs, method };
  });

  for (const ep of [...eps].reverse()) {
    if (ep.bodyIdx === -1) {
      warnings.push(`${file}: could not find body of endpoint at line ${ep.httpIdx + 1}`);
      continue;
    }
    const doc = parseDoc(ep.docLines);

    // method name + raw parameter list
    const region = lines.slice(ep.attrEnd, ep.bodyIdx + 1).join(eol);
    const cutAt = region.indexOf("{");
    const decl = region.slice(0, cutAt === -1 ? region.length : cutAt);
    const openIdx = decl.indexOf("(");
    const methodName = openIdx > 0 ? (decl.slice(0, openIdx).match(/(\w+)\s*$/) || [])[1] || "Operation" : "Operation";
    const bal = openIdx >= 0 ? extractParen(decl, openIdx) : null;
    const params = bal ? parseParams(bal.inner) : [];

    const fullRoute = [ep.classRoute, ep.methodRoute].filter(Boolean).join("/");
    const inRoute = (name) => new RegExp("\\{" + name + "(?::[^}]+)?\\}").test(fullRoute);
    const fromBody = (p) => p.attrs.some((a) => /\[FromBody/.test(a));
    const fromQuery = (p) => p.attrs.some((a) => /\[FromQuery/.test(a));
    const hasDesc = (p) => p.attrs.some((a) => /Description\(/.test(a));

    /* 1) [Description] on route/query parameters missing one */
    const descEdits = [];
    for (const p of params) {
      if (p.type === "CancellationToken") continue;
      if (fromBody(p) || hasDesc(p)) continue;
      const text = describeSimple(p, inRoute(p.name) ? "route" : "query parameter", false);
      descEdits.push({ find: p.chunk, replace: `[Description("${escAttr(text)}")] ${p.chunk}` });
    }
    if (descEdits.length) {
      let reg = region;
      let cursor = 0;
      let changed = false;
      for (const e of descEdits) {
        const at = reg.indexOf(e.find, cursor);
        if (at === -1) continue;
        reg = reg.slice(0, at) + e.replace + reg.slice(at + e.find.length);
        cursor = at + e.replace.length;
        changed = true;
      }
      if (changed) {
        const newLines = reg.split(/\r?\n/);
        const oldCount = ep.bodyIdx + 1 - ep.attrEnd;
        if (newLines.length === oldCount) {
          lines.splice(ep.attrEnd, oldCount, ...newLines);
          stats.paramDescs += descEdits.length;
          totals.paramDescs += descEdits.length;
        } else {
          warnings.push(`${file}: line-count mismatch adding [Description] near line ${ep.httpIdx + 1}`);
        }
      }
    }

    /* 2) missing <param> docs (+ summary block when the doc comment is absent) */
    const indent = ep.docLines.length ? (ep.docLines[0].match(/^(\s*)/) || ["    "])[1] : (lines[ep.attrStart].match(/^(\s*)/) || ["    "])[1];
    const newDocLines = [];
    if (!ep.docLines.length) newDocLines.push(`${indent}/// <summary>${buildSummary(doc, methodName)}</summary>`);
    for (const p of params) {
      if (doc.existingParams.has(p.name)) continue;
      if (p.type === "CancellationToken") {
        newDocLines.push(`${indent}/// <param name="${p.name}">Request cancellation token (optional).</param>`);
        continue;
      }
      if (fromBody(p)) {
        const { lines: bl, found } = bodyParamLines(p, contracts, indent);
        if (!found) warnings.push(`${file}: contract record ${p.type} not found (line ${ep.httpIdx + 1})`);
        newDocLines.push(...bl);
        continue;
      }
      const text = describeSimple(p, inRoute(p.name) ? "route" : "query parameter", true);
      newDocLines.push(`${indent}/// <param name="${p.name}">${text}</param>`);
    }
    let inserted = 0;
    if (newDocLines.length) {
      const firstResp = ep.docLines.findIndex((l) => /^\s*\/\/\/\s*<response\b/.test(l));
      const at = firstResp !== -1 ? ep.docStart + firstResp : ep.attrStart;
      lines.splice(at, 0, ...newDocLines);
      inserted = newDocLines.length;
      const counted = ep.docLines.length ? newDocLines.length : newDocLines.length - 1;
      stats.paramDocs += counted;
      totals.paramDocs += counted;
    }

    /* 3) [EndpointSummary] / [EndpointDescription] */
    const attrsToInsert = [];
    if (!/EndpointSummary/.test(ep.actionAttrs)) {
      attrsToInsert.push(`${indent}[EndpointSummary("${escAttr(buildSummary(doc, methodName))}")]`);
      stats.summaries++;
      totals.summaries++;
    }
    if (!/EndpointDescription/.test(ep.actionAttrs)) {
      attrsToInsert.push(`${indent}[EndpointDescription("${escAttr(buildDescription(doc, authSentence(ep.actionAttrs, classAttrs), methodName))}")]`);
      stats.descriptions++;
      totals.descriptions++;
    }
    if (attrsToInsert.length) lines.splice(ep.httpIdx + 1 + inserted, 0, ...attrsToInsert);

    totals.endpoints++;
  }

  perFile.push({ file, ...stats });

  // ensure `using System.ComponentModel;` when [Description] attributes were added
  if (stats.paramDescs > 0 && !lines.some((l) => /^using System\.ComponentModel;/.test(l))) {
    const firstSystem = lines.findIndex((l) => /^using System\./.test(l));
    if (firstSystem !== -1) {
      lines.splice(firstSystem, 0, "using System.ComponentModel;");
    } else {
      let lastUsing = -1;
      lines.forEach((l, i) => {
        if (/^using /.test(l)) lastUsing = i;
      });
      lines.splice(lastUsing + 1, 0, "using System.ComponentModel;");
    }
  }

  fs.writeFileSync(full, lines.join(eol), "utf8");
}

console.log("File".padEnd(42) + "endpoints  +summary  +desc  +paramDocs  +[Description]");
for (const f of perFile) {
  console.log(
    f.file.padEnd(42) +
      String(f.endpoints).padStart(9) +
      String(f.summaries).padStart(9) +
      String(f.descriptions).padStart(7) +
      String(f.paramDocs).padStart(11) +
      String(f.paramDescs).padStart(14),
  );
}
console.log("-".repeat(92));
console.log(
  "TOTAL".padEnd(42) +
    String(totals.endpoints).padStart(9) +
    String(totals.summaries).padStart(9) +
    String(totals.descriptions).padStart(7) +
    String(totals.paramDocs).padStart(11) +
    String(totals.paramDescs).padStart(14),
);
if (warnings.length) {
  console.log("\nWarnings:");
  for (const w of warnings) console.log("  - " + w);
}
