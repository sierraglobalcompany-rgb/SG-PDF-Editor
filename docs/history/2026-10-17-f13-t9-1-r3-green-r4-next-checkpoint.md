# SG PDF — F13 / Task9 / T9.1 — R3 GREEN → R4 NEXT

**Fecha:** 2026-10-17

## Autoridad

```yaml
repo: sierraglobalcompany-rgb/SG-PDF-Editor
branch: feat/f13-task9-t9-1-document-shell-realignment-20261017
r3_product_commit: 394bd320f8df03c0b00b5a517c00869a6c3a74e2
r3_exact_head_green: 4050ef37422fc2cdb98b79f4bd4c215534fc553e
merge: NO
main_changed: NO
t9_2_started: NO
```

## R2 — CLOSED / GREEN

Anchor:
`cf816557adab80f0e6e2e4ecb2f6628dc92eba90`

CI:
`39043770373` — SUCCESS.

R2 delivered real document tabs/top shell while reusing `OpenDocuments`, `ActiveDocument`, `PdfDocumentWorkspace`, activation and dirty-close owners. No parallel document registry was introduced.

## R3 — CLOSED TECHNICALLY / GREEN

### RED

Commit:
`1f6aeba80245589ffd66986a3d0e545f966cadae`

Contract:
- official artwork for 8 mapped document tools;
- fallback glyph only for `read`, `comment`, `forms`;
- Image/Path split explicit;
- 22 px icons;
- labels without wrapping.

### Product implementation

Commit:
`394bd320f8df03c0b00b5a517c00869a6c3a74e2`

Implemented:
- `DocumentIconPath` mapping;
- Image for mapped official artwork;
- Path fallback for unmapped tools;
- 22 px icon size;
- 10.5 label font;
- `TextWrapping.NoWrap`;
- ellipsis trimming.

### Test-contract realignment

Commit:
`4050ef37422fc2cdb98b79f4bd4c215534fc553e`

Reason:
A historical test still required a `Path` for all 11 rail buttons. That contradicted the new approved R3 authority. The test was updated to assert:
- `Image` for 8 mapped tools;
- `Path` only for Leer/Comentar/Formularios.

No invisible glyph/path workaround was added.

### Exact-head CI

```yaml
run: 39046256870
job: 117435463253
sha: 4050ef37422fc2cdb98b79f4bd4c215534fc553e
build:
  result: PASS
  warnings: 0
  errors: 0
startup_smoke:
  result: PASS
  duration: 30s
tests:
  passed: 1279
  failed: 0
  skipped: 2
  total: 1281
overall: SUCCESS
```

The two remaining skips are future R4/R5 contracts:
- collapsible Pages/Bookmarks drawer;
- PDF workspace context menu.

### Tesseract incident

Previous run `39044638036` had one transient process timeout in `TesseractProcessRecognizerTests.Process_runner_captures_stdout_and_stderr` plus the expected stale rail assertion.

On the fresh exact-head run `39046256870`, Tesseract passed and the whole suite was green.

Ruling:
`TRANSIENT CI PROCESS TIMEOUT / NON-REPRODUCED`.

No OCR timeout or product code was changed.

## Next block — R4

`T9.1-R4 — PAGES_BOOKMARKS_COLLAPSIBLE_DRAWER`

Status:
`NEXT / NOT STARTED`.

Goals:
1. drawer collapsed by default;
2. explicit expand/collapse;
3. reclaim workspace width when collapsed;
4. reuse existing thumbnail renderer/scheduler;
5. audit actual bookmark/outline data owner before calling Bookmarks functional;
6. no second navigation architecture;
7. Design Law before implementation;
8. focused RED→GREEN;
9. build + smoke + full suite + exact-head CI;
10. checkpoint before R5.

## Guardrails

- KISS.
- Anti-noise.
- Ley SG de Diseño.
- Anti-patch root-cause workflow.
- No merge to main.
- No T9.2.
- No R5 before R4 GREEN.
- No new UI framework.
- No duplicate navigation/document/session owners.

## Recovery

When resuming:
1. verify live branch HEAD and exact-head CI;
2. read `.planning/STATE.md`;
3. read `.planning/ROADMAP.md`;
4. read this checkpoint;
5. start R4 only if GitHub live does not show newer authority.
