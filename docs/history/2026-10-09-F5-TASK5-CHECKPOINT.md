# SG PDF Editor — F5 Task 5 Checkpoint

Fecha: 2026-10-09
Rama: `feat/f5-organize`
Estado: **Task 5 — F5.2 Organize Surface + Lazy/Bounded Thumbnails — AUTOMATED PASS**

## Punto de partida

- Checkpoint Task 4: `91ac9a102a5029c50caad5e6ad48c59b4af89374`
- `main` se mantuvo sin cambios en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No se hizo merge.

## Alcance ejecutado

Task 5 implementa únicamente la superficie base de `ORGANIZAR` y sus miniaturas lazy/bounded. No implementa todavía selección Windows completa, drag/drop, mutaciones de página ni Guardar como desde la UI; esas acciones permanecen deshabilitadas para Task 6 y tareas posteriores.

### Archivos agregados desde Task 4

- `src/SGPdf.App/Features/Organize/OrganizePageItem.cs`
- `src/SGPdf.App/MainWindow.Organize.cs`
- `tests/SGPdf.App.Tests/OrganizePageItemTests.cs`
- `tests/SGPdf.App.Tests/MainWindowOrganizeTests.cs`
- `tests/SGPdf.App.Tests/MainWindowOrganizeThumbnailTests.cs`

No se modificaron writers PDF, lector, firma, ZPL, paquetes ni workflow CI.

## TDD — RED 1

Pruebas de contrato agregadas en:

- commit `1b74693b4e9c0a0d79a4da42ff2e18aaf51736e2`
- commit `b19908e36fd4ba29684514e78b21e7faa5bca3b2`
- commit `4dfbf19ff7fb8c0ea0c5b7187f471b49e9e085e4`

CI exacto de `4dfbf19ff7fb8c0ea0c5b7187f471b49e9e085e4`:

- run `37967653809`
- Build: FAIL esperado
- 0 warnings
- 3 errores, todos exclusivamente por `OrganizePageItem` todavía inexistente.

Esto confirmó el primer RED sin ruido de fixtures ni sintaxis ajena al contrato.

## GREEN parcial — tile lógico

Se agregó `OrganizePageItem`:

- commit `7c8cd59a1e6d99bcfba03b1b2582eff86f5e3dd8`

Contrato implementado:

- identidad lógica inmutable: `ItemId`, `SourceId`, `SourcePageIndex`;
- numeración mutable según orden del plan;
- rotación relativa planificada;
- ancho de miniatura convencional de 132 px;
- `Bitmap`, loading/error state;
- publicación/liberación explícita de bitmap;
- renumerar no invalida bitmap;
- cambiar rotación sí invalida bitmap;
- DPI calculado para que una rotación impar conserve 132 px de ancho visual.

CI exacto `37967804877` sobre `7c8cd59...`:

- Build: PASS, 0 warnings / 0 errors
- Tests: 467 PASS / 9 FAIL / 476 total
- Los 9 fallos restantes correspondieron exclusivamente a la superficie/scheduler ORGANIZAR aún inexistentes (`TryEnterOrganizeMode`, controles y seams lazy).

Esto confirmó el segundo RED limpio.

## GREEN — superficie ORGANIZAR

Se agregó `MainWindow.Organize.cs`:

- commit funcional inicial `69a76e6d76e9bf1817f30cc358d5b31fc7a0b52e`

Características:

1. Botón de modo `ORGANIZAR` agregado dinámicamente a la barra `LEER | FIRMAR`.
2. `OrganizeSurface` independiente del lector y de la superficie de firma/ZPL.
3. Toolbar con controles nombrados:
   - `OrganizeRotateLeftButton`
   - `OrganizeRotateRightButton`
   - `OrganizeDeleteButton`
   - `OrganizeDuplicateButton`
   - `OrganizeInsertButton`
   - `OrganizeMergeButton`
   - `OrganizeExtractButton`
   - `OrganizeSplitButton`
   - `OrganizeSaveAsButton`
4. Todos esos comandos permanecen intencionalmente deshabilitados en Task 5.
5. `OrganizePageList` usa virtualización WPF estándar:
   - virtualization enabled;
   - recycling;
   - pixel scrolling;
   - `CanContentScroll=true`.
6. Entrada a ORGANIZAR:
   - requiere sesión PDF activa;
   - resuelve primero firmas visuales pendientes;
   - `Cancel` conserva el modo actual y bloquea la transición;
   - preflight Block impide la entrada;
   - PDF abierto con contraseña queda bloqueado por el preflight existente;
   - crea `OrganizeSource` + `OrganizePlan` del PDF activo;
   - oculta LEER/FIRMAR/ZPL y muestra la superficie ORGANIZAR.
7. Salida de ORGANIZAR:
   - cancela render pendiente;
   - libera miniaturas;
   - descarta únicamente el plan en memoria;
   - restaura LEER para PDF activo.
8. `ApplyOrganizePlanSnapshot(...)` conserva el mismo `OrganizePageItem`/bitmap cuando `ItemId + SourceId + SourcePageIndex + rotación` siguen siendo equivalentes, aunque cambie la posición.

### Integración con LEER/FIRMAR

El plan original permitía una modificación estrecha de `MainWindow.Sign.cs`. Se encontró una opción más KISS y menos acoplada: `MainWindow.Organize.cs` registra su propia inicialización al `Loaded` del `MainWindow` y escucha `PreviewMouseLeftButtonDown` de los botones LEER/FIRMAR para abandonar ORGANIZAR antes de entrar al otro modo.

Por eso **no fue necesario modificar `MainWindow.Sign.cs`**. El comportamiento requerido queda cubierto sin mezclar lógica ORGANIZAR dentro del módulo FIRMAR.

## Miniaturas lazy/bounded

Se agregó un scheduler separado:

`_organizeThumbnailRenderScheduler`

Reglas implementadas y probadas:

- solo se retienen bitmaps del rango realizado + 1 vecino a cada lado;
- prioridad: páginas visibles primero, vecinos después;
- trabajo secuencial, un render a la vez;
- scroll rápido invalida la request anterior;
- una request obsoleta no puede publicar bitmap después de una request nueva;
- fallo de un tile marca únicamente ese tile y continúa con los demás;
- la sesión del lector permanece utilizable tras un fallo de miniatura;
- rotación planificada se aplica visualmente al bitmap;
- reconstrucción/reordenamiento conserva bitmap si la identidad física y rotación no cambiaron;
- bitmaps fuera de ventana se liberan.

Prueba de revisión crítica cubierta:

`LargeDocument_OnlyRealizedPlusNeighborThumbnailsHoldBitmaps`

Con 100 páginas y rango realizado 50..52, únicamente 49..53 conservan bitmap.

## Incidencia GREEN y corrección

El primer build del workspace detectó un solo error de compilación:

- run del commit `69a76e6d...`
- `MainWindow.Organize.cs`: se usó `sizes.Count` sobre un arreglo.
- C# interpretó `Count` como method-group.

Root cause: tipo concreto `PdfPageSize[]` tras `ToArray()`.

Corrección mínima:

- `sizes.Count` → `sizes.Length`
- commit `75efea6f0c498bd4d15401943f88fd700de51d2f`

No se cambió ninguna otra semántica.

## Verificación final funcional

CI exacto:

- SHA: `75efea6f0c498bd4d15401943f88fd700de51d2f`
- run: `37968561524`
- repository hygiene: PASS
- Labelize staging: PASS
- locked restore: PASS
- Release build: PASS
- warnings: 0
- errors: 0
- tests: **476 PASS / 0 FAIL / 0 SKIP**

## Auditoría de alcance Task 4 → Task 5

Comparación `91ac9a10...` → `75efea6f...`:

- ahead by 6 commits;
- 5 archivos cambiados;
- todos pertenecen al tile/superficie/tests de ORGANIZAR;
- sin cambios a writers PDF;
- sin cambios a `MainWindow.Sign.cs`;
- sin cambios a lector/ZPL;
- sin paquetes nuevos;
- sin Task 6.

## Estado de `main`

Reverificado después del GREEN:

`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

Sin merge.

## QA manual

No ejecutado todavía:

- QA físico/visual en Windows real;
- scroll manual de documentos grandes;
- apariencia final de tiles;
- transición manual LEER/FIRMAR/ORGANIZAR;
- rendimiento con PDFs reales pesados.

El estado de Task 5 es **AUTOMATED PASS**, no QA físico final.

## Siguiente paso exacto

**Task 6 — F5.2 Selection + Drag/Drop + Save As wiring.**

No iniciar hasta recibir el siguiente `continua` del usuario.

Task 6 debe montar sobre la infraestructura de este checkpoint e implementar, entre otras cosas:

- selección Windows consistente;
- Ctrl/Shift/Ctrl+A/Escape/Delete;
- selección por `ItemId`;
- drag/drop de bloque seleccionado conservando orden relativo;
- no-op al soltar dentro del propio bloque;
- wiring de rotate/delete/duplicate;
- Save As usando `PdfOrganizeWriter` y confirmación explícita de warnings;
- no sustituir silenciosamente la sesión activa tras guardar.

No hacer merge a `main` sin aprobación explícita.
