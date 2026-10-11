# Phase 9 — F8 Comentarios V1

**Status:** AUTOMATED CLOSURE PASS; manual Windows/stylus/touch/external-reader QA **NOT RUN**.  
**Branch:** `feat/f8-comments-v1`  
**Base:** `feat/f7-text-v1` @ `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`

## Goal

Implementar comentarios PDF reales, conservadores y offline en un modo `COMENTAR` independiente, manteniendo PDFium como único motor de edición y preservando estructuras no objetivo.

## Approved V1 scope

- Select.
- Highlight.
- Underline.
- Strikeout.
- Note.
- Ink.
- Rectangle.
- Ellipse.

Native annotation families proven against pinned PDFium `bblanchon.PDFium.Win32 156.0.8076`:

- `TEXT`
- `SQUARE`
- `CIRCLE`
- `HIGHLIGHT`
- `UNDERLINE`
- `STRIKEOUT`
- `INK`

Out of V1: lines/arrows/polygons/free text/stamps/attachments/audio/replies/authors/resolved/mentions/cloud/flatten/pressure/cross-page selection.

## Architecture invariants

- `COMENTAR` is independent from `EDITAR`.
- Real PDF annotations; no rasterization into page content.
- PDFium only; no second PDF editor engine.
- Durable comment state is managed-only; native handles are request-scoped.
- Raw annotation index is observed/session data, never durable identity authority.
- Existing annotations re-resolve through stable structural identity and fail closed on mismatch.
- Save As materializes from a fresh source and publishes only after reopen/validation.
- Unsupported annotations are preserved/read-only rather than reconstructed.
- Cryptographically signed and password-opened sources remain blocked for comment output.
- No runtime network/cloud/account/API-key/service layer.

## Execution record

1. **Capability gate — PASS.** Exact PDFium annotation APIs and seven required native families characterized create→save→reopen→read→render.
2. **Managed workspace/history — PASS.** Candidate-first immutable states, undo/redo, dirty/baseline semantics.
3. **Discovery — PASS.** Page-bounded existing-annotation discovery with request-scoped native handles.
4. **Stable identity + hit-testing — PASS.** Structural fingerprint + duplicate ordinal/count; deterministic geometry including rotation.
5. **Text-markup geometry/writer/validator — PASS.** Highlight/underline/strikeout real quadpoints and save/reopen validation.
6. **Sticky note writer — PASS.** Unicode `TEXT` creation/edit/delete.
7. **Ink writer — PASS.** Managed strokes persisted as native `INK`.
8. **Square/Circle writer — PASS.** Rectangle/ellipse annotations with geometry/border semantics.
9. **Transaction/preflight hardening — PASS.** Temp→reopen/validate→atomic publish; warning/block/cancellation/source-race cleanup paths.
10. **COMENTAR UI — PASS automated.** V1 tools integrated with local dirty/save semantics; manual interaction QA pending.
11. **Preservation/lifecycle — PASS.** Fresh F8 fixture + create→edit→delete across independent reopen cycles.
12. **Hardening/regression/closure — PASS automated.** Managed-handle invariant, page-bounded discovery, cancellation recovery and full regression suite.

## Preservation evidence

`docs/history/2026-10-10-F8-preservation-matrix.md`

Fresh F8 writer measurement proves representative preservation of:

- forms + value;
- bookmarks;
- named destinations;
- internal links;
- tagged structure marker;
- page labels;
- attachment name + exact payload;
- metadata title/author/custom marker;
- page rotation;
- supported comments;
- unsupported `STAMP` as preserved/read-only.

## Automated closure evidence before final docs gate

Task-12 hardening commit:
`2359dc793667f59266062b64eb12e0e0f4efe08c`

Windows CI:

- workflow `38104169562`;
- job `114366028415`;
- hygiene PASS;
- locked restore PASS;
- Release build **0 warnings / 0 errors**;
- tests **851 passed / 0 failed / 0 skipped**.

The full assembly run keeps LEER, FIRMAR, ORGANIZAR, EDITAR and ZPL regression suites green.

## Manual gate — still open

**NOT RUN:** real Windows comment UX, mouse/stylus/touch, zoom/rotation/overlap behavior, dirty guards/dialogs, heavy PDFs, network-disabled physical smoke and manual reopen/interoperability in Adobe Acrobat Reader, Foxit and Edge.

Automated PASS does not imply this manual gate passed.

## Closure rule

F8 stops after:

1. final planning/history reconciliation;
2. final branch scope audit from F7 closure;
3. exact-head Windows CI PASS;
4. stacked draft PR `feat/f7-text-v1` → `feat/f8-comments-v1` created and verified open/draft/unmerged;
5. `main` verified unchanged.

No merge without explicit user approval. No F9 work inside the F8 closure gate.
