# F7 — Task 7 Conservative Text Policy + Workspace — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 7 — política conservadora + `TextEditWorkspace`  
**Estado:** **FUNCTIONAL_GREEN / FINAL_CI_PENDING**  
**Rama:** `feat/f7-text-v1`  
**Base Task 7:** `91108bd8f39a498f6f2db384bd1c533ba583d4b0`  
**RED:** `5d6f44a1717488f8faf34b044cd7ad466a249c1d`  
**Head funcional:** `36db4d74da675cad3c5269157ae2dfa76e5a9a1e`

## Alcance entregado

Task 7 añade únicamente política y estado managed para edición conservadora de objetos de texto ya descubiertos por F7.

### Política

`TextEditPolicy`:

- solo `TextRenderMode == Fill` es editable en Texto V1; otros modos son read-only;
- texto vacío o solo espacios se rechaza;
- tamaño debe ser finito, > 0 y <= 1000 pt;
- la decisión de cobertura usa `System.Text.Rune`, no `char`;
- reordenar runes ya presentes y añadir whitespace mantiene `OriginalFont`;
- cualquier rune no-whitespace nuevo fuerza `FallbackTtf`;
- mientras `FPDFTextObj_SetFontSize` no esté promovido como capacidad segura, cualquier cambio de tamaño fuerza `FallbackTtf`;
- la ruta fallback exige cobertura real de `FallbackFontAsset`; un rune sin glyph bloquea el candidato.

### Workspace

`TextEditWorkspace`:

- captura `PdfEditSourceFingerprint` al abrir;
- mantiene estado actual, original y baseline guardado;
- `PrepareCandidate(...)` no muta;
- `CommitCandidate(...)` aplica solo un candidato válido y no obsoleto;
- candidato inválido/read-only no ensucia el workspace;
- no-op permanece limpio;
- `MarkSavedBaseline()` limpia dirty después de una futura publicación exitosa;
- rechaza candidatos stale mediante `ExpectedState`;
- expone `EditedStates` ordenados por página/objeto;
- no persiste `IntPtr`/handles nativos;
- no añade undo global.

## RED

Commit: `5d6f44a1717488f8faf34b044cd7ad466a249c1d`  
Workflow: `38076903191`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **705 PASS / 16 FAIL / 721 total**;
- los 16 fallos pertenecían exclusivamente a `TextEditWorkspaceTests`;
- causa esperada: aún no existían `TextEditPolicy`, `TextEditWorkspace`, `TextEditState` y `TextEditCandidate`.

## GREEN y diagnóstico de harness

Commits de producto:

- `0fb0bb10b83698ec02f75bd7c1c3b1de10b1aa38` — estado/candidato managed;
- `27cd018330f854cdde9ef858f08d77893662c7ea` — política conservadora;
- `1beb945134d3fbf428eebb36e1e21900ba6ba981` — workspace candidate-first.

El primer run GREEN compiló pero expuso un fallo del harness: la reflexión de tests buscaba solo propiedades públicas, mientras las propiedades de producto son `internal`. Se corrigió únicamente el helper de tests en `9028c05d15a178f4d4ab76a164f8dec92d8fff24`.

Después quedó un único fallo de fixture: el test asumía que DejaVu Sans 2.37 no cubría U+1F600, pero la fuente sí tiene glyph. Se reemplazó únicamente el fixture por CJK U+4E00 en `36db4d74da675cad3c5269157ae2dfa76e5a9a1e`; producto no cambió.

## GREEN funcional verificado

Workflow: `38077268635`, **attempt 2**, mismo SHA `36db4d74da675cad3c5269157ae2dfa76e5a9a1e`.

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **721 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

Attempt 1 del mismo SHA tuvo **720 PASS / 1 FAIL** por un flake heredado de `MainWindowOrganizeThumbnailTests.RebuildAfterReorder_ReusesBitmapWhenSourceAndRotationAreUnchanged`; ningún test Task 7 falló. Se repitió el mismo SHA sin cambiar código y quedó limpio.

## Auditoría KISS / scope

Diff neto desde Task 6 cerrada hasta el head funcional: exactamente cuatro archivos:

1. `src/SGPdf.App/Features/Edit/Text/TextEditState.cs`;
2. `src/SGPdf.App/Features/Edit/Text/TextEditPolicy.cs`;
3. `src/SGPdf.App/Features/Edit/Text/TextEditWorkspace.cs`;
4. `tests/SGPdf.App.Tests/TextEditWorkspaceTests.cs`.

No se implementó en Task 7:

- UI/overlay/panel de propiedades;
- writer/materialización PDF;
- Save As de texto;
- P/Invoke nuevo;
- undo/redo global;
- reflow/OCR;
- Task 8+.

## QA manual

**NOT RUN.** Task 7 es estado/política managed sin UI nueva. El PASS automatizado no se presenta como validación manual Windows.

## Gate final

CI exacto sobre el commit de este checkpoint: **PENDING**.

No iniciar Task 8 hasta que el checkpoint tenga CI exacto PASS y quede sellado CLOSED.
