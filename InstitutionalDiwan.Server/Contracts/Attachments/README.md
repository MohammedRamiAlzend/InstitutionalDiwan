# Presentation / Contracts / Attachments — Attachment request bodies

> **Status:** Implemented

## Purpose

This folder contains HTTP request records specific to document attachments. Controllers use these
records to keep multipart and OnlyOffice callback payloads separate from Application and Domain
types.

## Contents

| File | Purpose |
| --- | --- |
| `AttachmentOnlyOfficeRequests.cs` | Carries the OnlyOffice save callback fields used to create an immutable attachment version. |

## Implementation status

| Item | State |
| --- | --- |
| OnlyOffice callback request | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-23 | Copilot | Added the signed OnlyOffice callback request contract for document attachments. |
