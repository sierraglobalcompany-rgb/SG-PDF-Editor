# Checkpoint — F13 Task 2 GREEN implemented / CI pending

**Fecha:** 2026-10-15

## Stable baseline

- UI-4 GREEN / closed.
- Release-truth baseline: `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`.
- `main` remains read-only and untouched.

## F13 Task 1 — GREEN

Branch: `feat/f13-shell-primitives`  
Head: `5a8e13f88175fe7469c18a1769fe86a96e001f10`  
PR: `#34` draft  
CI run: `38686210087` SUCCESS  
Release build: 0 warnings / 0 errors  
Tests: `1218/1218 PASS`

Task 1 adds only approved native WPF shell primitives and their contract tests.

## F13 Task 2 — RED proven

Branch: `feat/f13-tool-catalog`  
Base: Task 1 head  
RED head: `b92e29dc07b3f7d42952e0214e336237f25d661b`  
RED CI: `38686564150`

Expected RED:
- build PASS;
- existing 1218 tests PASS;
- exactly 3 new tests fail because `SGPdf.App.Shell.ToolCatalog` does not yet exist.

This proves the contract before implementation.

## F13 Task 2 — GREEN implementation

Head: `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`  
PR: `#35` draft

Production files added only:
- `src/SGPdf.App/Shell/ToolDescriptor.cs`
- `src/SGPdf.App/Shell/ToolCatalog.cs`

No routing, no MainWindow references, no delegates, no DI, no command bus, no framework or package changes.

Capability honesty:
- Document executable in F13: Read/Edit/Comment/Sign/Organize only.
- Forms/OCR/Redact/Compare/Convert/Protect remain disabled until F14 wiring.
- Home: `PDF a PNG` honest label.
- `Imagen a PDF` explicitly `Próximamente`.
- only ZPL is executable from Home in F13.

CI run `38686853745` was still running at checkpoint creation.

## Exact resume

1. Inspect CI `38686853745` for head `8c40cc98e102e75a3bd550f8ef3e9e58c599df32`.
2. If GREEN, close Task 2.
3. Start Task 3 HomeDashboard only in a new stacked branch and with RED first.
4. No merge to main without explicit user approval.
