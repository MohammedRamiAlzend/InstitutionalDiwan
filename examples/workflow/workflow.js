/* ==========================================================================
   Institutional Diwan — workflow example.
   A tiny in-memory simulation of the document lifecycle. No server, no deps.
   Rules mirror institutionaldiwan.front/knowledge.md (simplified).
   ========================================================================== */
(() => {
  'use strict';

  const $ = (sel, root = document) => root.querySelector(sel);

  /* ------------------------------------------------------------------ data */

  const DEPTS = {
    A: 'الديوان المركزي',
    B: 'مديرية الشؤون القانونية',
    C: 'مديرية المالية',
  };
  const DEPT_ORDER = ['A', 'B', 'C'];

  const ALL_PERMS = [
    'DOCUMENT_VIEW', 'DOCUMENT_CREATE', 'DOCUMENT_EDIT', 'DOCUMENT_DELETE', 'DOCUMENT_APPROVE',
    'DOCUMENT_ARCHIVE', 'NUMBERING_VIEW', 'NUMBERING_MANAGE', 'DEPARTMENT_MANAGE', 'ROLE_MANAGE',
    'USER_MANAGE', 'EMPLOYEE_MANAGE', 'ACCESS_GRANT_MANAGE', 'REPORT_VIEW', 'LOOKUP_MANAGE',
    'AUDIT_VIEW', 'AUDIT_MANAGE',
  ];
  const CLERK_PERMS = ['DOCUMENT_VIEW', 'DOCUMENT_CREATE', 'DOCUMENT_EDIT', 'NUMBERING_VIEW', 'NUMBERING_MANAGE'];

  const ROLES = {
    admin:  { label: 'مدير النظام',               scope: ['A', 'B', 'C'], perms: ALL_PERMS, note: 'نطاق النظام: كل الأقسام وكل الصلاحيات (مثل الحساب admin).' },
    clerkA: { label: 'موظف الديوان المركزي',      scope: ['A'],           perms: CLERK_PERMS, note: 'نطاق القسم: الديوان المركزي فقط، بلا صلاحية الأرشفة.' },
    clerkB: { label: 'موظف الشؤون القانونية',     scope: ['B'],           perms: CLERK_PERMS, note: 'نطاق القسم: مديرية الشؤون القانونية فقط.' },
    clerkC: { label: 'موظف المالية',              scope: ['C'],           perms: CLERK_PERMS, note: 'نطاق القسم: مديرية المالية فقط.' },
    viewer: { label: 'مطّلع (قراءة فقط)',         scope: ['A', 'B', 'C'], perms: ['DOCUMENT_VIEW', 'AUDIT_VIEW'], note: 'يرى كل شيء ولا يغيّر شيئاً.' },
  };

  const NOTIFICATION_TYPES = {
    1: 'إنشاء وثيقة', 2: 'تسجيل وثيقة', 3: 'إرسال حركة', 4: 'استلام حركة', 5: 'إكمال حركة', 6: 'أرشفة وثيقة',
    7: 'رفض حركة', 8: 'إعادة حركة', 9: 'إعادة توجيه', 10: 'إغلاق حركة', 11: 'إلغاء حركة',
  };

  const MODULES = [
    { n: '01', t: 'الهيكل التنظيمي', d: 'الأقسام وشجرتها، إعدادات المؤسسة، والموظفون. تحدد من هو «القسم» الذي يرسل ويستلم.', w: 'الخريطة أعلاه' },
    { n: '02', t: 'الهوية والصلاحيات', d: 'المستخدمون والأدوار و١٧ صلاحية ونطاق القسم ومنح الوصول لكل وثيقة.', w: 'قائمة «الدور الحالي»' },
    { n: '03', t: 'المراسلات', d: 'الوثيقة نفسها: مسودة، تسجيل، مشاركون (مرسل/مستلم)، إلغاء، سجل حالات وخط زمني.', w: 'أزرار الإنشاء والتسجيل' },
    { n: '04', t: 'الترقيم', d: 'سجلات الترقيم، الرقم الرسمي المركزي، ودفتر تسجيل كل قسم.', w: 'الرقم الرسمي ورقم القسم' },
    { n: '05', t: 'الحركات', d: 'مسار الوثيقة بين الأقسام: فتح، إرسال، استلام، إكمال، رفض، إعادة، توجيه، إغلاق، إلغاء.', w: 'مؤشر الحركة والخريطة' },
    { n: '06', t: 'المرفقات', d: 'ملفات بنسخ متعددة، كل نسخة بتجزئة SHA-256، مع أرشفة ناعمة وتحقق من السلامة.', w: 'لوحة المرفقات' },
    { n: '07', t: 'الأرشفة', d: 'أرشفة الوثيقة المسجلة في موقع محدد وتجميدها، بحث محفوظ، وسجل أرشفة.', w: 'زر الأرشفة' },
    { n: '08', t: 'الروابط', d: 'ربط وثيقة بأخرى (رد على، ذات صلة، تلغي، مكررة) مع منع الدورات وإخفاء ما لا يحق للمستخدم رؤيته.', w: 'تبويب «الروابط» في الواجهة' },
    { n: '09', t: 'التدقيق', d: 'سلسلة تجزئة لكل عملية، نقاط فحص، وتقارير سلامة مجدولة.', w: 'سجل التدقيق' },
    { n: '10', t: 'الإشعارات', d: 'إشعارات داخل التطبيق للمستخدمين المعنيين عبر SignalR وصندوق صادر مع إعادة محاولة.', w: 'لوحة الإشعارات' },
  ];

  /* ----------------------------------------------------------------- state */

  const BASE_TIME = Date.UTC(2026, 10, 4, 9, 0);
  let S = fresh('admin');
  let busy = false;
  let lastError = null;

  function fresh(role) {
    return {
      role,
      doc: { status: 'none', number: null, archived: false, title: 'تعميم بشأن مواعيد الدوام' },
      mv: { status: 'none', from: 'A', to: 'B', custody: 'A' },
      deptReg: {},
      participants: [],
      files: [],
      notes: [],
      audit: [],
      clock: 0,
      seq: 0,
      counters: { official: 0, dept: 0 },
    };
  }

  /* --------------------------------------------------------------- helpers */

  /** Small 64-bit-looking FNV-style hash. Illustrative only — the real chain uses SHA-256. */
  function hash(str) {
    let a = 0x811c9dc5, b = 0x9e3779b1;
    for (let i = 0; i < str.length; i++) {
      const c = str.charCodeAt(i);
      a = Math.imul(a ^ c, 16777619) >>> 0;
      b = Math.imul(b ^ c, 2246822519) >>> 0;
      b = (b ^ (b >>> 13)) >>> 0;
    }
    return a.toString(16).padStart(8, '0') + b.toString(16).padStart(8, '0');
  }

  async function sha256(text) {
    try {
      if (window.crypto && crypto.subtle) {
        const buf = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
        return [...new Uint8Array(buf)].map((x) => x.toString(16).padStart(2, '0')).join('');
      }
    } catch { /* fall through to the illustrative hash */ }
    return hash(text) + hash(text + '1') + hash(text + '2') + hash(text + '3');
  }

  function time() {
    S.clock += 2 + (S.seq % 5);
    const d = new Date(BASE_TIME + S.clock * 60000);
    return d.toISOString().slice(11, 16);
  }

  function toast(msg, isErr) {
    const t = $('#toast');
    t.textContent = msg;
    t.className = 'show' + (isErr ? ' err' : '');
    clearTimeout(toast.h);
    toast.h = setTimeout(() => { t.className = ''; }, 2600);
  }

  const deptNames = (list) => list.map((d) => DEPTS[d]).join(' أو ');

  /* ------------------------------------------------------------- audit log */

  function pushAudit({ ok, text, api, module, action, entity }) {
    const role = ROLES[S.role].label;
    const id = ++S.seq;
    const payload = [id, role, module, action, entity, ok ? 'OK' : 'FAIL'].join('|');
    const prev = S.audit.length ? S.audit[S.audit.length - 1].hash : '0'.repeat(16);
    S.audit.push({
      id, ok, text, api, role, at: time(), module, action, entity,
      payload, prev, hash: hash(prev + payload), tampered: false, flagged: false,
    });
  }

  function notify(typeId, to, text) {
    S.notes.unshift({ typeId, to, text, at: S.audit.length ? S.audit[S.audit.length - 1].at : '09:00' });
  }

  /* --------------------------------------------------------------- actions */

  const err = (status, title, detail) => ({ error: { status, title, detail } });
  const docIs = (...s) => s.includes(S.doc.status);
  const mvIs = (...s) => s.includes(S.mv.status);
  const mvActive = () => mvIs('draft', 'sent', 'received');

  /**
   * Each action:
   *  - group/label/api: what the user sees
   *  - perm: permission code that is required
   *  - side(): departments any of which must be inside the caller's scope
   *  - visible(): is this step meaningful in the current state?
   *  - run(): mutates S and returns a descriptor (or an `error`)
   */
  const ACTIONS = [
    {
      id: 'create', group: 'الإنشاء والتسجيل', label: 'إنشاء مسودة وثيقة', api: 'POST /api/documents/drafts',
      perm: 'DOCUMENT_CREATE', side: () => ['A'], visible: () => docIs('none'),
      run() {
        S.doc.status = 'draft';
        return { text: `أُنشئت المسودة «${S.doc.title}» في ${DEPTS.A}.`, module: 'Correspondence', action: 'Create', entity: 'Document' };
      },
    },
    {
      id: 'recipient', group: 'الإنشاء والتسجيل', label: 'إضافة المستلم الأساسي', api: 'POST /api/documents/{id}/participants',
      perm: 'DOCUMENT_EDIT', side: () => ['A'], visible: () => docIs('draft', 'registered') && !S.doc.archived && !S.participants.length,
      run() {
        S.participants.push(`${DEPTS.B} (مستلم أساسي)`);
        return { text: `أُضيف ${DEPTS.B} كمستلم أساسي.`, module: 'Correspondence', action: 'AddParticipant', entity: 'Document' };
      },
    },
    {
      id: 'attach', group: 'الإنشاء والتسجيل', label: 'إرفاق ملف (PDF)', api: 'POST /api/documents/{id}/files',
      perm: 'DOCUMENT_EDIT', side: () => ['A'], visible: () => docIs('draft', 'registered'),
      async run() {
        if (S.doc.archived) return err(409, 'Correspondence.DocumentArchived', 'The document is archived. Unarchive it before changing attachments.');
        const n = S.files.length + 1;
        const name = n === 1 ? 'نص-التعميم.pdf' : `ملحق-${n - 1}.pdf`;
        const sum = await sha256(`${S.doc.title}#${name}#${n}`);
        S.files.push({ name, version: 1, sum });
        return { text: `رُفع الملف «${name}» (نسخة ١) مع حساب SHA-256.`, module: 'Attachments', action: 'Upload', entity: 'DocumentFile' };
      },
    },
    {
      id: 'register', group: 'الإنشاء والتسجيل', label: 'تسجيل الوثيقة وإصدار الرقم الرسمي', api: 'POST /api/documents/{id}/register',
      perm: 'DOCUMENT_CREATE', side: () => ['A'], visible: () => docIs('draft'),
      run() {
        S.doc.status = 'registered';
        S.doc.number = `2026/${String(++S.counters.official).padStart(6, '0')}`;
        return {
          text: `سُجّلت الوثيقة وصدر الرقم الرسمي ${S.doc.number}. (لا إشعار: المنفِّذ هو المنشئ.)`,
          module: 'Numbering', action: 'Issue', entity: 'DocumentNumber',
        };
      },
    },

    {
      id: 'openMv', group: 'الحركة بين الأقسام', label: `فتح حركة إلى ${DEPTS.B}`, api: 'POST /api/movements',
      perm: 'DOCUMENT_EDIT', side: () => ['A'], visible: () => docIs('registered') && !S.doc.archived && !mvActive(),
      run() {
        S.mv = { status: 'draft', from: 'A', to: 'B', custody: 'A' };
        return { text: `فُتحت حركة (مسودة) من ${DEPTS.A} إلى ${DEPTS.B}.`, module: 'Movements', action: 'Open', entity: 'Movement' };
      },
    },
    {
      id: 'send', group: 'الحركة بين الأقسام', label: 'إرسال الحركة', api: 'POST /api/movements/{id}/send',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.from], visible: () => mvIs('draft'),
      run() {
        S.mv.status = 'sent';
        notify(3, `مستخدمو ${DEPTS[S.mv.to]}`, `وصلت وثيقة جديدة برقم ${S.doc.number} من ${DEPTS[S.mv.from]}.`);
        return { text: `أُرسلت الحركة إلى ${DEPTS[S.mv.to]} (الوثيقة في الطريق).`, module: 'Movements', action: 'Send', entity: 'Movement' };
      },
    },
    {
      id: 'deptReg', group: 'الحركة بين الأقسام', label: 'تسجيل الوثيقة في سجل القسم المستلم', api: 'POST /api/documents/{id}/numbers/department-registration',
      perm: 'NUMBERING_MANAGE', side: () => [S.mv.to], visible: () => mvIs('draft', 'sent') && !S.deptReg[S.mv.to],
      run() {
        const code = `DEP-2026-${String(++S.counters.dept).padStart(3, '0')}`;
        S.deptReg[S.mv.to] = code;
        return { text: `سُجّلت الوثيقة في دفتر ${DEPTS[S.mv.to]} برقم ${code}.`, module: 'Numbering', action: 'DepartmentRegister', entity: 'DepartmentRegisterEntry' };
      },
    },
    {
      id: 'receive', group: 'الحركة بين الأقسام', label: 'استلام الحركة', api: 'POST /api/movements/{id}/receive',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.to], visible: () => mvIs('sent'),
      run() {
        if (!S.deptReg[S.mv.to]) {
          return err(409, 'Movements.ReceiverRegistrationRequired',
            `The document must be registered in the receiving department's register (${DEPTS[S.mv.to]}) before it can be received.`);
        }
        S.mv.status = 'received';
        S.mv.custody = S.mv.to;
        notify(4, `مستخدمو ${DEPTS[S.mv.from]} + منشئ الوثيقة`, `استلم ${DEPTS[S.mv.to]} الوثيقة ${S.doc.number}.`);
        return { text: `استلم ${DEPTS[S.mv.to]} الوثيقة وأصبحت في عهدته.`, module: 'Movements', action: 'Receive', entity: 'Movement' };
      },
    },
    {
      id: 'complete', group: 'الحركة بين الأقسام', label: 'إكمال المعالجة', api: 'POST /api/movements/{id}/complete',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.to], visible: () => mvIs('received'),
      run() {
        S.mv.status = 'completed';
        notify(5, `مستخدمو ${DEPTS[S.mv.from]} + منشئ الوثيقة`, `أكمل ${DEPTS[S.mv.to]} معالجة الوثيقة ${S.doc.number}.`);
        return { text: `أكمل ${DEPTS[S.mv.to]} المعالجة.`, module: 'Movements', action: 'Complete', entity: 'Movement' };
      },
    },
    {
      id: 'reject', group: 'الحركة بين الأقسام', label: 'رفض الحركة', api: 'POST /api/movements/{id}/reject',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.to], visible: () => mvIs('received'),
      run() {
        S.mv.status = 'rejected';
        S.mv.custody = S.mv.from;
        notify(7, `مستخدمو ${DEPTS[S.mv.from]} + منشئ الوثيقة`, `رفض ${DEPTS[S.mv.to]} الوثيقة ${S.doc.number}.`);
        return { text: `رُفضت الحركة وعادت العهدة إلى ${DEPTS[S.mv.from]}.`, module: 'Movements', action: 'Reject', entity: 'Movement' };
      },
    },
    {
      id: 'return', group: 'الحركة بين الأقسام', label: 'إعادة الحركة للمرسل', api: 'POST /api/movements/{id}/return',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.to], visible: () => mvIs('received'),
      run() {
        S.mv.status = 'returned';
        S.mv.custody = S.mv.from;
        notify(8, `مستخدمو ${DEPTS[S.mv.from]} + منشئ الوثيقة`, `أعاد ${DEPTS[S.mv.to]} الوثيقة ${S.doc.number} للاستكمال.`);
        return { text: `أُعيدت الحركة إلى ${DEPTS[S.mv.from]}.`, module: 'Movements', action: 'Return', entity: 'Movement' };
      },
    },
    {
      id: 'redirect', group: 'الحركة بين الأقسام', label: `إعادة توجيه إلى ${DEPTS.C}`, api: 'POST /api/movements/{id}/redirect',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.to], visible: () => mvIs('received') && S.mv.to !== 'C',
      run() {
        const was = S.mv.to;
        S.mv = { status: 'sent', from: was, to: 'C', custody: was };
        notify(9, `مستخدمو ${DEPTS.C}`, `وُجّهت إليكم الوثيقة ${S.doc.number} من ${DEPTS[was]}.`);
        return { text: `وُجّهت الحركة من ${DEPTS[was]} إلى ${DEPTS.C}؛ يحتاج القسم الجديد تسجيلاً قبل الاستلام.`, module: 'Movements', action: 'Redirect', entity: 'Movement' };
      },
    },
    {
      id: 'close', group: 'الحركة بين الأقسام', label: 'إغلاق الحركة', api: 'POST /api/movements/{id}/close',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.from, S.mv.to], visible: () => mvIs('completed', 'rejected', 'returned'),
      run() {
        S.mv.status = 'closed';
        notify(10, `الطرف الآخر من الحركة`, `أُغلقت الحركة الخاصة بالوثيقة ${S.doc.number}.`);
        return { text: 'أُغلقت الحركة (يمكن فتح حركة جديدة أو أرشفة الوثيقة).', module: 'Movements', action: 'Close', entity: 'Movement' };
      },
    },
    {
      id: 'cancelMv', group: 'الحركة بين الأقسام', label: 'إلغاء الحركة', api: 'POST /api/movements/{id}/cancel',
      perm: 'DOCUMENT_EDIT', side: () => [S.mv.from], visible: () => mvIs('draft', 'sent'),
      run() {
        S.mv.status = 'cancelled';
        notify(11, `مستخدمو ${DEPTS[S.mv.to]}`, `أُلغيت الحركة المتعلقة بالوثيقة ${S.doc.number}.`);
        return { text: 'ألغى المرسل الحركة قبل الاستلام.', module: 'Movements', action: 'Cancel', entity: 'Movement' };
      },
    },

    {
      id: 'archive', group: 'الأرشفة والإلغاء', label: 'أرشفة الوثيقة', api: 'POST /api/documents/{id}/archive',
      perm: 'DOCUMENT_ARCHIVE', side: () => ['A'], visible: () => docIs('registered') && !S.doc.archived && !mvActive(),
      run() {
        S.doc.archived = true;
        notify(6, 'منشئ الوثيقة', `أُرشفت الوثيقة ${S.doc.number}.`);
        return { text: 'أُرشفت الوثيقة وأصبحت مجمّدة (لا مرفقات ولا مشاركين جدد).', module: 'Archiving', action: 'Archive', entity: 'DocumentArchiveEvent' };
      },
    },
    {
      id: 'unarchive', group: 'الأرشفة والإلغاء', label: 'فك الأرشفة (بسبب مطلوب)', api: 'POST /api/documents/{id}/unarchive',
      perm: 'DOCUMENT_ARCHIVE', side: () => ['A'], visible: () => S.doc.archived,
      run() {
        S.doc.archived = false;
        return { text: 'فُكّت الأرشفة بسبب: «تصحيح موقع الأرشفة».', module: 'Archiving', action: 'Unarchive', entity: 'DocumentArchiveEvent' };
      },
    },
    {
      id: 'cancelDoc', group: 'الأرشفة والإلغاء', label: 'إلغاء الوثيقة (بسبب)', api: 'POST /api/documents/{id}/cancel',
      perm: 'DOCUMENT_EDIT', side: () => ['A'], visible: () => docIs('draft', 'registered') && !S.doc.archived && !mvActive(),
      run() {
        S.doc.status = 'cancelled';
        return { text: 'أُلغيت الوثيقة، ويبقى سجلها وتاريخها محفوظين.', module: 'Correspondence', action: 'Cancel', entity: 'Document' };
      },
    },
  ];

  /** Returns a Arabic reason string when the current role may not run this action, otherwise null. */
  function gateOf(a) {
    const role = ROLES[S.role];
    if (!role.perms.includes(a.perm)) return `يتطلب الصلاحية ${a.perm}`;
    if (!a.side().some((d) => role.scope.includes(d))) return `خارج نطاقك — يتطلب موظفاً في ${deptNames(a.side())}`;
    return null;
  }

  async function perform(id) {
    if (busy && !perform.auto) return;
    const a = ACTIONS.find((x) => x.id === id);
    if (!a || !a.visible()) return;
    if (gateOf(a)) return;

    const res = await a.run();
    if (res.error) {
      lastError = res.error;
      pushAudit({ ok: false, text: `فشل: ${a.label}`, api: a.api, module: 'Operation', action: 'Failed', entity: res.error.title });
      S.audit[S.audit.length - 1].errTitle = res.error.title;
      toast(arabicError(res.error.title), true);
    } else {
      pushAudit({ ok: true, text: res.text, api: a.api, module: res.module, action: res.action, entity: res.entity });
      toast(res.text);
    }
    render();
  }

  function arabicError(title) {
    return ({
      'Correspondence.DocumentArchived': 'الوثيقة مؤرشفة؛ فُكّ الأرشفة أولاً قبل تعديل المرفقات.',
      'Movements.ReceiverRegistrationRequired': 'سجّل الوثيقة في سجل القسم المستلم قبل الاستلام.',
    })[title] || 'تعذّر تنفيذ الإجراء.';
  }

  /* ---------------------------------------------------------------- render */

  function stepper(el, steps, current, badLabel) {
    el.innerHTML = '';
    steps.forEach((label, i) => {
      const li = document.createElement('li');
      li.textContent = label;
      if (badLabel && i === current) { li.className = 'bad'; li.textContent = badLabel; }
      else if (i < current) li.className = 'done';
      else if (i === current) li.className = 'current';
      el.appendChild(li);
    });
  }

  function renderSteppers() {
    const docSteps = ['لا وثيقة', 'مسودة', 'مسجّلة برقم رسمي', 'مؤرشفة'];
    let di = 0, dBad = null;
    if (S.doc.status === 'draft') di = 1;
    if (S.doc.status === 'registered') di = S.doc.archived ? 3 : 2;
    if (S.doc.status === 'cancelled') { di = 2; dBad = 'مُلغاة'; }
    stepper($('#docStepper'), docSteps, di, dBad);

    const mvSteps = ['لا حركة', 'مسودة', 'مرسلة', 'مستلمة', 'مكتملة', 'مغلقة'];
    const map = { none: 0, draft: 1, sent: 2, received: 3, completed: 4, rejected: 4, returned: 4, closed: 5, cancelled: 2 };
    const bad = { rejected: 'مرفوضة', returned: 'معادة', cancelled: 'مُلغاة' }[S.mv.status] || null;
    stepper($('#mvStepper'), mvSteps, map[S.mv.status], bad);
  }

  function renderMap() {
    const m = $('#map');
    const exists = S.doc.status !== 'none';
    const f = DEPT_ORDER.indexOf(S.mv.from), t = DEPT_ORDER.indexOf(S.mv.to), c = DEPT_ORDER.indexOf(S.mv.custody);
    const inTransit = S.mv.status === 'sent';
    const pos = inTransit ? (f + t) / 2 : c;
    const nodes = DEPT_ORDER.map((d) => {
      const has = exists && !inTransit && S.mv.custody === d;
      const reg = S.deptReg[d];
      return `<div class="node${has ? ' has' : ''}${reg ? ' reg' : ''}" ${reg ? `data-reg="${reg}"` : ''}><b>${DEPTS[d]}</b>${has ? 'الوثيقة هنا' : ''}</div>`;
    }).join('');
    const cls = !exists ? 'hidden' : S.doc.status === 'cancelled' ? 'cancelled' : S.doc.archived ? 'archived' : inTransit ? 'transit' : '';
    const icon = S.doc.archived ? '🗄️' : S.doc.status === 'cancelled' ? '✖' : inTransit ? '✉️' : '📄';
    m.innerHTML = `${nodes}<div class="packet ${cls}" style="--pos:${pos}" title="الوثيقة">${icon}</div>`;
  }

  function renderFacts() {
    const items = [
      ['العنوان', S.doc.status === 'none' ? '—' : S.doc.title],
      ['الرقم الرسمي', S.doc.number ? `<span class="ltr mono">${S.doc.number}</span>` : '—'],
      ['أرقام الأقسام', Object.keys(S.deptReg).length ? Object.entries(S.deptReg).map(([d, n]) => `<span class="ltr mono">${n}</span>`).join('<br>') : '—'],
      ['المشاركون', S.participants.length ? S.participants.join('، ') : '—'],
      ['العهدة الحالية', S.doc.status === 'none' ? '—' : (S.mv.status === 'sent' ? 'في الطريق' : DEPTS[S.mv.custody])],
      ['المرفقات', String(S.files.length)],
    ];
    $('#facts').innerHTML = items.map(([k, v]) => `<div><dt>${k}</dt><dd>${v}</dd></div>`).join('');
  }

  function renderActions() {
    const box = $('#actions');
    const visible = ACTIONS.filter((a) => a.visible());
    if (!visible.length) {
      box.innerHTML = '<p class="empty">لا إجراءات متاحة في هذه الحالة (الوثيقة ملغاة أو لم تُنشأ بعد).</p>';
      return;
    }
    box.innerHTML = '';
    let group = null, grid = null;
    visible.forEach((a) => {
      if (a.group !== group) {
        group = a.group;
        const t = document.createElement('div');
        t.className = 'group-title';
        t.textContent = group;
        box.appendChild(t);
        grid = document.createElement('div');
        grid.className = 'actions';
        box.appendChild(grid);
      }
      const gate = gateOf(a);
      const b = document.createElement('button');
      b.type = 'button';
      b.className = 'action';
      b.dataset.id = a.id;
      b.disabled = !!gate || (busy && !perform.auto);
      b.innerHTML = `<span class="t">${a.label}</span><span class="api ltr">${a.api}</span>${gate ? `<span class="why">🔒 ${gate}</span>` : ''}`;
      b.addEventListener('click', () => perform(a.id));
      grid.appendChild(b);
    });
  }

  function renderTimeline() {
    const ul = $('#timeline');
    if (!S.audit.length) { ul.innerHTML = '<li class="empty">لا أحداث بعد.</li>'; return; }
    ul.innerHTML = [...S.audit].reverse().map((e) => `
      <li class="ev ${e.ok ? '' : 'err'}${e.flagged ? ' bad' : ''}">
        <div class="msg">${e.tampered ? '⚠️ ' : ''}${e.text}</div>
        <div class="row"><span>${e.role} · <span class="ltr mono">${e.at}</span></span>
          <span class="chips"><span class="chip">${e.module}.${e.action}</span><span class="chip ${e.ok ? 'ok' : 'err'}">${e.ok ? 'OK' : 'FAILED'}</span></span></div>
        <div class="hash">#${e.id} prev:${e.prev.slice(0, 8)} → hash:${e.hash.slice(0, 8)}</div>
      </li>`).join('');
  }

  function renderNotes() {
    const ul = $('#notes');
    ul.innerHTML = S.notes.length
      ? S.notes.map((n) => `<li><span class="dot"></span><div><strong>${NOTIFICATION_TYPES[n.typeId]}</strong> ← ${n.to}<br><span class="muted">${n.text}</span></div></li>`).join('')
      : '<li class="empty">لا إشعارات بعد. تُرسل عند الإرسال والاستلام والإكمال وغيرها (ولا يُشعَر المنفِّذ).</li>';
  }

  function renderFiles() {
    $('#files').innerHTML = S.files.length
      ? S.files.map((f) => `<li><strong>${f.name}</strong> — نسخة ${f.version}<br><span class="mono ltr">${f.sum.slice(0, 32)}…</span></li>`).join('')
      : '<li class="empty">لا مرفقات.</li>';
  }

  function renderProblem() {
    $('#problem').textContent = lastError
      ? JSON.stringify({ type: 'about:blank', title: lastError.title, status: lastError.status, detail: lastError.detail }, null, 2)
      : '// لم يحدث خطأ بعد — جرّب الاستلام قبل التسجيل في سجل القسم، أو إضافة مرفق بعد الأرشفة.';
  }

  function renderRole() {
    const r = ROLES[S.role];
    $('#roleInfo').textContent = `${r.note} الصلاحيات: ${r.perms.length === ALL_PERMS.length ? 'الكل (١٧)' : r.perms.join('، ')}.`;
  }

  function render() {
    renderRole();
    renderSteppers();
    renderMap();
    renderFacts();
    renderActions();
    renderTimeline();
    renderNotes();
    renderFiles();
    renderProblem();
  }

  /* ----------------------------------------------------- audit integrity */

  function checkIntegrity() {
    S.audit.forEach((e) => { e.flagged = false; });
    let prev = '0'.repeat(16);
    for (const e of S.audit) {
      const expected = hash(prev + e.payload);
      if (e.prev !== prev || e.hash !== expected) {
        e.flagged = true;
        return { isValid: false, first: e.id };
      }
      prev = e.hash;
    }
    return { isValid: true };
  }

  function showVerdict(r) {
    const v = $('#verdict');
    v.hidden = false;
    v.className = 'verdict ' + (r.isValid ? 'ok' : 'err');
    v.textContent = r.isValid
      ? '✓ السلسلة سليمة (HTTP 200، isValid: true)'
      : `✗ السلسلة مكسورة عند السجل #${r.first} (HTTP 200، isValid: false)`;
    render();
  }

  function tamper() {
    if (S.audit.length < 2) { toast('نفّذ بضع خطوات أولاً ليكون هناك سجل قديم للتلاعب به.', true); return; }
    const target = S.audit[Math.floor(S.audit.length / 2) - (S.audit.length % 2 ? 0 : 1)] || S.audit[0];
    target.payload += '|EDITED';
    target.tampered = true;
    toast(`عُدّل السجل #${target.id} مباشرةً في قاعدة البيانات… اضغط «فحص السلامة».`, true);
    renderTimeline();
  }

  /* ------------------------------------------------------------- autoplay */

  const SCRIPT = [
    ['clerkA', 'create'], ['clerkA', 'recipient'], ['clerkA', 'attach'], ['clerkA', 'register'],
    ['clerkA', 'openMv'], ['clerkA', 'send'],
    ['clerkB', 'receive'],   // fails on purpose: no department registration yet
    ['clerkB', 'deptReg'], ['clerkB', 'receive'], ['clerkB', 'complete'],
    ['clerkA', 'close'], ['admin', 'archive'],
  ];

  const wait = (ms) => new Promise((r) => setTimeout(r, ms));

  async function play() {
    if (busy) return;
    reset();
    busy = true;
    perform.auto = true;
    $('#playBtn').disabled = true;
    for (const [role, id] of SCRIPT) {
      S.role = role;
      $('#roleSel').value = role;
      render();
      const btn = $(`.action[data-id="${id}"]`);
      if (btn) btn.classList.add('pulse');
      await wait(900);
      await perform(id);
      await wait(700);
    }
    perform.auto = false;
    busy = false;
    $('#playBtn').disabled = false;
    toast('انتهى السيناريو. جرّب الآن بنفسك: فكّ الأرشفة، أو تلاعب بالسجل ثم افحص السلامة.');
    render();
  }

  function reset() {
    if (busy && !perform.auto) return;
    const role = S.role;
    S = fresh(role);
    lastError = null;
    $('#verdict').hidden = true;
    render();
  }

  /* ----------------------------------------------------------- static bits */

  function buildRoleSelect() {
    const sel = $('#roleSel');
    sel.innerHTML = Object.entries(ROLES).map(([k, r]) => `<option value="${k}">${r.label}</option>`).join('');
    sel.value = S.role;
    sel.addEventListener('change', () => { S.role = sel.value; render(); });
  }

  function buildModules() {
    $('#moduleGrid').innerHTML = MODULES.map((m) => `
      <article class="card module">
        <h3><span class="num">${m.n}</span> ${m.t}</h3>
        <p>${m.d}</p>
        <p class="where chip">يظهر في: ${m.w}</p>
      </article>`).join('');
  }

  function buildMatrix() {
    const shown = ['DOCUMENT_VIEW', 'DOCUMENT_CREATE', 'DOCUMENT_EDIT', 'NUMBERING_MANAGE', 'DOCUMENT_ARCHIVE', 'AUDIT_VIEW'];
    const head = `<tr><th>الدور</th><th>النطاق</th>${shown.map((p) => `<th><code>${p}</code></th>`).join('')}</tr>`;
    const rows = Object.values(ROLES).map((r) => `<tr><td>${r.label}</td><td>${r.scope.length === 3 ? 'كل الأقسام' : deptNames(r.scope)}</td>${
      shown.map((p) => (r.perms.includes(p) ? '<td class="yes">✓</td>' : '<td class="no">—</td>')).join('')}</tr>`).join('');
    $('#matrix').innerHTML = head + rows;
  }

  function wireTheme() {
    $('#themeBtn').addEventListener('click', () => {
      const root = document.documentElement;
      const dark = root.dataset.theme
        ? root.dataset.theme === 'dark'
        : window.matchMedia('(prefers-color-scheme: dark)').matches;
      const next = dark ? 'light' : 'dark';
      root.dataset.theme = next;
      try { localStorage.setItem('diwan.example.theme', next); } catch { /* storage may be blocked */ }
    });
  }

  document.addEventListener('DOMContentLoaded', () => {
    buildRoleSelect();
    buildModules();
    buildMatrix();
    wireTheme();
    $('#resetBtn').addEventListener('click', reset);
    $('#playBtn').addEventListener('click', play);
    $('#checkBtn').addEventListener('click', () => showVerdict(checkIntegrity()));
    $('#tamperBtn').addEventListener('click', tamper);
    render();
  });
})();
