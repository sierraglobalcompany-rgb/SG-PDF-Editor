# SG PDF — F13 / Task9 / T9.1 — R3 FINAL GREEN CHECKPOINT

**Fecha:** 2026-10-17

## Autoridad

```yaml
branch: feat/f13-task9-t9-1-document-shell-realignment-20261017
r3_product_commit: 394bd320f8df03c0b00b5a517c00869a6c3a74e2
r3_test_alignment_commit: 4050ef37422fc2cdb98b79f4bd4c215534fc553e
r3_docs_closure_commit: cb48b2dc9cf694b211b6916c3dadd4d0423061e2
merge: NO
main_changed: NO
t9_2_started: NO
```

## R2

`R2 TOP_SHELL_AND_REAL_DOCUMENT_TABS = GREEN / CLOSED`

Technical anchor:

`cf816557adab80f0e6e2e4ecb2f6628dc92eba90`

Exact-head CI run:

`39043770373` — SUCCESS.

## R3

`R3 DOCUMENT_TOOL_RAIL = GREEN / CLOSED technically`

Implementation:

- rail documental conserva 96 px;
- orden de 11 herramientas preservado;
- accesibilidad/focus/selected state preservados;
- 8 herramientas usan arte oficial aprobado como `Image` de 22 px;
- `read`, `comment`, `forms` mantienen glyph `Path` como fallback temporal aprobado;
- labels 10.5 px, `NoWrap`, `CharacterEllipsis`;
- no se rediseñaron los assets oficiales.

Mapping oficial:

```text
edit       -> edit-pdf.png
sign       -> sign.png
organize   -> organize-pages.png
ocr        -> ocr.png
redact     -> redact.png
compare    -> compare-pdf.png
convert    -> pdf-to-png.png
protect    -> protect.png

read       -> fallback glyph
comment    -> fallback glyph
forms      -> fallback glyph
```

## TDD / CI R3

RED:

`1f6aeba80245589ffd66986a3d0e545f966cadae`

Producto:

`394bd320f8df03c0b00b5a517c00869a6c3a74e2`

Test alignment:

`4050ef37422fc2cdb98b79f4bd4c215534fc553e`

Exact-head CI de `4050ef...`:

```yaml
run: 39046256870
job: 117435463253
build: PASS
warnings: 0
errors: 0
startup_smoke: PASS_30S
tests: 1279_PASS_0_FAIL_2_SKIP_1281_TOTAL
overall: SUCCESS
```

Los dos skips son intencionales y reservan:

- R4 `ReaderNavigation_DeclaresCollapsibleDrawerAndToggleOwner`
- R5 `PdfWorkspace_DeclaresContextMenuOwner`

No eliminarlos.

## Cierre documental y flake observado

El cierre documental `cb48b2dc...` tuvo un primer intento con un único fallo en:

`ChildProcessRunnerTests.RunAsync_WhenTimeoutExpires_KillsProcessAndThrowsTimeout`

Build y smoke pasaron. El mismo job se volvió a ejecutar sobre el mismo SHA sin cambio de código.

Rerun exact-head:

```yaml
run: 39047234828
job: 117439080896
sha: cb48b2dc9cf694b211b6916c3dadd4d0423061e2
build: PASS
warnings: 0
errors: 0
startup_smoke: PASS_30S
tests: 1279_PASS_0_FAIL_2_SKIP_1281_TOTAL
overall: SUCCESS
```

Clasificación del primer fallo:

`TRANSIENT CI TIMING FLAKE / NON-REPRODUCED`

No se modificó timeout ni código de proceso.

## Documentación de autoridad

Ya actualizada para reflejar:

```text
R1   GREEN / CLOSED
R1B  GREEN / CLOSED
R2   GREEN / CLOSED
R3   GREEN / CLOSED
R4   NEXT / NOT STARTED
R5   NOT STARTED
R6   NOT STARTED
R7   NOT STARTED
T9.2 NOT STARTED
```

Archivos actualizados antes de este checkpoint:

- `.planning/STATE.md`
- `.planning/ROADMAP.md`
- `README.md`
- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/history/2026-10-17-f13-t9-1-r3-green-r4-next-checkpoint.md`

## Próximo bloque

`T9.1-R4 — PAGES_BOOKMARKS_DRAWER`

Estado:

`NEXT / NOT STARTED`

Antes de empezar R4:

1. leer la Ley SG de Diseño;
2. auditar owner actual de thumbnails + bookmarks;
3. definir micro-task R4-A RED;
4. implementar drawer cerrado por defecto;
5. checkpoint por micro-task;
6. no iniciar R5 en el mismo bloque.

## Guardrails

- NO merge sin aprobación explícita.
- NO `main`.
- NO T9.2.
- NO framework nuevo.
- NO duplicar renderer/thumbnails.
- NO declarar bookmarks funcionales sin evidencia real.
- KISS / anti-ruido / anti-parches / Ley SG de Diseño siguen obligatorios.
