# F5 Task 6 checkpoint — selección, drag, comandos y Guardar como

Fecha: 2026-10-09
Rama: `feat/f5-organize`
Base Task 5: `6c56d1ea8b7feb7f7fe491e6ada8c2c8d408a284`
GREEN funcional Task 6: `12b30687d752d510d11a91ce6f21989d4f6638fc`

## Alcance cerrado

Task 6 implementa únicamente F5.2 para la superficie ORGANIZAR:

- selección estilo Windows por `ItemId`;
- click simple, Ctrl+click, Shift+click según el orden actual del plan;
- Ctrl+A, Escape y Delete limitados a ORGANIZAR;
- drag/drop que mueve el bloque seleccionado conservando orden relativo;
- drop dentro del propio bloque seleccionado = no-op;
- rotar izquierda/derecha;
- eliminar, bloqueando eliminación de todas las páginas;
- duplicar y seleccionar los nuevos `ItemId`;
- `Guardar como...` con preflight, confirmación explícita de warnings y writer transaccional existente;
- `_organizeMaterializing` bloquea mutaciones y escrituras concurrentes durante materialización;
- éxito de Guardar como mantiene el PDF activo y el plan en memoria; solo informa la ruta creada.

Task 7 NO fue iniciado. Insertar, Combinar, Extraer y Dividir permanecen deshabilitados.

## TDD — selección pura

### RED

Suite nueva: `tests/SGPdf.App.Tests/OrganizeSelectionTests.cs`.

Hubo dos ajustes test-only para eliminar una ambigüedad de overload de xUnit cuando el tipo de producción todavía no existía. El RED limpio final quedó en:

- SHA: `69c55145714db0c5f0c48e3f04e3364bdb45a4c6`
- CI: `37970285315`
- build de app: PASS
- build de tests: FAIL esperado
- 0 warnings
- únicos 4 errores: `CS0246 OrganizeSelection could not be found`

### GREEN

Producción: `src/SGPdf.App/Features/Organize/OrganizeSelection.cs`.

SHA: `d371ea12db7f4feadbc66214155c362ac4c50e7c`
CI: `37970453392`

Resultado:

- Build PASS
- 0 warnings / 0 errors
- Tests: 480 PASS / 0 FAIL / 0 SKIP

Semántica cubierta:

- selección simple y toggle;
- anchor explícito;
- Shift usa el orden ACTUAL de `OrganizePlan` después de reorder;
- selección stale se reconcilia por `ItemId`;
- SelectAll/Clear.

## TDD — integración de comandos y Save As

### RED

Suite nueva: `tests/SGPdf.App.Tests/MainWindowOrganizeTask6Tests.cs`.

SHA: `9c709bd9e6641ec0ec1cd70599ba9d770d720524`
CI: `37970736832`

Resultado RED:

- Build PASS
- 0 warnings / 0 errors
- Tests: 480 PASS / 6 FAIL / 0 SKIP / 486 total
- los seis fallos correspondieron exclusivamente a contratos Task 6 aún ausentes o botones todavía deshabilitados;
- no hubo regresión en las 480 pruebas existentes.

Contratos RED cubiertos:

1. multiselección y move forward/backward en orden actual;
2. drop dentro de selección como no-op y misma instancia de plan;
3. rotar/eliminar/duplicar y atajos scoped a ORGANIZAR;
4. duplicado selecciona IDs nuevos;
5. solo comandos propiedad de Task 6 quedan habilitados;
6. cancelación de Save As no llama writer;
7. Block preflight no llama writer;
8. Warning exige confirmación explícita y pasa `warningsConfirmed=true`;
9. error del writer conserva sesión/plan;
10. éxito conserva sesión activa y plan, reportando la ruta;
11. durante writer las mutaciones quedan bloqueadas.

### GREEN funcional

Producción: `src/SGPdf.App/MainWindow.Organize.Commands.cs`.

SHA: `12b30687d752d510d11a91ce6f21989d4f6638fc`
CI: `37971138824`

Resultado exacto:

- repository hygiene PASS;
- Labelize 1.7.0 staging PASS;
- restore locked PASS;
- Release build PASS;
- 0 warnings / 0 errors;
- Tests: **486 PASS / 0 FAIL / 0 SKIP**;
- duración tests: 11 s.

## Decisiones de implementación

### Partial separado para comandos

En lugar de aumentar `MainWindow.Organize.cs`, Task 6 se aisló en `MainWindow.Organize.Commands.cs`.

Motivo KISS:

- Task 5 ya tiene el renderer lazy/bounded verificado;
- selección/drag/comandos/Save As tienen responsabilidades distintas;
- el partial nuevo reutiliza los controles ya registrados y no modifica el renderer Task 5;
- reduce riesgo de regresión en miniaturas.

No se añadió arquitectura nueva, DI, servicios, paquetes ni framework de drag/drop.

### Guardar como

La UI hace preflight antes de invocar el writer:

- Block => no writer;
- Warning => confirmación explícita;
- clean => `warningsConfirmed=false`;
- Warning confirmado => `warningsConfirmed=true`.

Después el `PdfOrganizeWriter` vuelve a aplicar sus defensas transaccionales existentes. La UI no sustituye ni debilita el writer.

El éxito NO abre silenciosamente la copia ni reemplaza `_session`.

### Drag/drop

Se usa WPF nativo (`DragDrop.DoDragDrop`) y `OrganizePlanOperations.MoveSelection` como única fuente de verdad del reorder. No hay un segundo modelo de orden en UI.

## Auditoría de diff Task 5 → GREEN Task 6

`6c56d1ea...` → `12b30687...`:

- `src/SGPdf.App/Features/Organize/OrganizeSelection.cs` — nuevo;
- `src/SGPdf.App/MainWindow.Organize.Commands.cs` — nuevo;
- `tests/SGPdf.App.Tests/OrganizeSelectionTests.cs` — nuevo;
- `tests/SGPdf.App.Tests/MainWindowOrganizeTask6Tests.cs` — nuevo.

No se modificaron:

- `PdfOrganizeWriter` / validator;
- bindings PDFium / NativeGate;
- Reader;
- Firma;
- ZPL;
- paquetes/dependencias;
- `MainWindow.Organize.cs` Task 5 y su scheduler de miniaturas.

## QA no ejecutado

No se declara QA físico/manual de Windows. Sigue pendiente validación manual posterior de interacción real con mouse/drag, archivos reales y flujo visual completo.

## Punto exacto de continuación

El siguiente `continua` autoriza **Task 7 — F5.3 Insert + Merge solamente**.

Antes de empezar Task 7:

1. verificar que esta rama siga en el SHA de este checkpoint;
2. exigir CI PASS del SHA exacto del checkpoint;
3. confirmar `main` sin cambios;
4. leer nuevamente la sección Task 7 del plan;
5. ejecutar RED → GREEN en bloque pequeño;
6. no iniciar Task 8 sin otro `continua`;
7. nunca mergear a `main` sin aprobación explícita.
