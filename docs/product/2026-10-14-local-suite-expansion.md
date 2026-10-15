# SG PDF Editor — Local Suite Expansion

**Date:** 2026-10-14
**Status:** approved product direction / implementation blocked by current UI-4 P1 defects
**Inspiration:** iLovePDF tool discoverability + Acrobat-style document workspace, adapted to SG PDF and kept local-first.

## Product rules

### 1. KISS
Simple, efficient, optimized, effective. Prefer A→B over generalized architecture.

### 2. Anti-noise
Before ADD: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

### 3. Anti-patch
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Non-negotiable product constraints

- Windows desktop first.
- Local/offline normal operation.
- No mandatory account, cloud, API or backend.
- No AI features in baseline.
- No advertising in baseline.
- Preserve originals; publication flows default to explicit Save As/copy semantics.
- Reuse PDFium/qpdf/Tesseract/Labelize/current writers before considering new production dependencies.
- No second general PDF engine without a demonstrated capability gap and explicit approval.
- No merge to `main` without explicit user approval.

## Desired experience

### Home / Dashboard

A local dashboard should expose the suite as clear tool cards instead of making users discover features through menus.

Core elements:
- official SG PDF app icon/logo;
- `Abrir PDF` primary action;
- drag/drop;
- recent local documents;
- grouped tool cards with icon + short label + one-line explanation;
- no login, advertising or remote content.

Initial categories:
- Organize;
- Edit;
- Convert;
- Optimize;
- Security;
- Sign;
- Forms;
- OCR;
- Utilities;
- ZPL / professional tools.

### Open-document workspace

Use one coherent shell:
- compact top bar for file/document actions;
- left primary tool rail with icon + text;
- PDF canvas in the center;
- contextual properties on the right;
- contextual floating object toolbar only when useful.

Primary left tools:
- Read;
- Edit;
- Comment;
- Sign;
- Organize;
- Forms;
- OCR;
- Redact;
- Compare;
- Convert/Export;
- Protect;
- More tools.

Avoid duplicating the same actions across multiple global toolbars.

## Existing capabilities to productize first

The F0–F12 codebase already contains substantial functionality that should be surfaced before adding new engines:
- reader/navigation/zoom/search/selection/print;
- organize/reorder/rotate/delete/duplicate/insert/merge/extract/split;
- image editing;
- conservative text editing + reflow;
- comments;
- watermark text + Bates/page numbering;
- AES-256 protection;
- OCR with local Tesseract;
- safe redaction;
- visual signature/photo/draw/local library;
- cryptographic PFX/P12 signature;
- existing AcroForm `/Tx` editing;
- PDF compare core + minimal UI;
- batch TextWatermark;
- QR PNG;
- PDF→PNG.

Rule: **productize and improve UX before adding a parallel implementation.**

## New local capabilities by value

### High priority
1. Image/JPG/PNG → PDF.
2. PDF → JPG as an extension of existing PDF→PNG.
3. Visual crop PDF.
4. Authorized unlock with known password using existing qpdf capability where appropriate.
5. Expose the already-characterized safe optimization path as a user command if the capability gate remains valid.
6. Complete visual-signature UX: typed signature/initials/date only if it reuses the existing placement pipeline cleanly.
7. Compare V2: side-by-side view, synchronized navigation/zoom and overlay where technically safe.
8. Edit V3 UX and compatibility improvements.

### Medium priority
- image watermark after writer capability validation;
- additional AcroForm types `/Btn` and `/Ch`;
- optional simple form designer after existing-form support is reliable;
- local scanner integration after hardware capability gate.

### Optional integrations
- Office→PDF through a detected local LibreOffice installation, not as a mandatory runtime dependency;
- local HTML→PDF only after a separate renderer/dependency capability decision.

## Explicit deferrals

Do not promise these without a new capability gate:
- high-fidelity PDF→Word/Excel/PowerPoint;
- universal PDF repair;
- PDF/A conformance without proper standards validation;
- XFA/complex JavaScript/calculation forms;
- third-party signature-request service;
- AI summarization/translation/chat;
- cloud workflows.

## Edit V3 product requirement

Editing is a dedicated product block because the current conservative engine is difficult to understand in real use.

Required UX:
- explicit `Edit mode` state;
- clear hover/selection affordance for editable objects;
- right properties panel by selected object type;
- visible status for `Editable`, `Editable with limitations` or `Read-only`;
- Spanish reason when an object cannot be edited safely;
- visible Undo/Redo where supported;
- `Guardar como...` visible throughout Edit mode;
- save-state indicator: no changes / pending / saving / saved / error;
- never silently overwrite the original;
- scanned-document guidance: offer OCR instead of pretending text is editable.

## Official application icon

Release requirement:
- source must be the user-approved visual asset;
- generate a multi-resolution `.ico` from that source only;
- apply to window, executable, Alt+Tab, taskbar, shortcut and installer;
- do not invent or substitute a logo from a textual description.

## Sequencing rule

This expansion does **not** bypass current release blockers.

Required order:
1. close current reproduced UI-4 P1 defects;
2. capture real UI-4 evidence;
3. implement F13 shell/dashboard;
4. productize existing capabilities in F14;
5. execute Edit V3 in F15;
6. add local utilities in small capability-gated slices in F16;
7. add professional UX/forms/compare improvements in F17;
8. optional local integrations only in F18.
