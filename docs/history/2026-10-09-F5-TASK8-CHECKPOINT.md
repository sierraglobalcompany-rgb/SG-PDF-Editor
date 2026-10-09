# F5 Task 8 Checkpoint — Extraer + Dividir PDF

Fecha: 2026-10-09
Rama: `feat/f5-organize`
Estado: **AUTO PASS / Task 8 cerrada**

## Base

- Checkpoint Task 7: `65e6713f232c65de1053af539d12bb2536fad2ce`
- `main` esperado e intacto durante Task 8: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`
- Sin merge a `main`.

## Alcance de Task 8

F5.4 añade únicamente:

- Extraer páginas seleccionadas a un PDF derivado.
- Dividir el plan actual cada N páginas.
- Dividir por grupos/rangos explícitos no superpuestos.
- Nombres deterministas para las partes.
- Publicación secuencial por lote con preflight de colisiones completo antes del primer write.
- Integración WPF mínima de los comandos Extraer y Dividir.

No se inició Task 9 / F5.5.

## TDD RED

Commit RED:

`3b1a33045c6331c67659cc4acb2c6cd4094d8d8a`

Mensaje:

`test(organize): define extract split and batch publication`

CI RED:

- Run: `37975103275`
- Build: PASS, 0 warnings, 0 errores.
- Tests: 531 total; 513 PASS; 18 FAIL; 0 skipped.
- Los 513 tests existentes siguieron verdes.
- Los 18 fallos nuevos fueron exactamente los contratos Task 8 aún inexistentes: planner, naming, batch publisher y comandos WPF.

Suites añadidas:

- `OrganizeSplitPlannerTests.cs`
- `OrganizeOutputNamingTests.cs`
- `OrganizeBatchPublisherTests.cs`
- `MainWindowOrganizeExtractSplitTests.cs`

Review-focus cubierto:

`SplitPublish_AnyExistingTarget_BlocksBeforeFirstWrite`

## GREEN funcional

Head funcional verificado:

`3bc0b68b04f8fbe97e1400a9b81454d447503d37`

CI exacto:

- Run: `37976252372`
- Re-run del mismo SHA por flake de carga de `pdfium.dll` en el primer intento.
- Re-run job: `113975720317`
- Build Release: PASS, 0 warnings, 0 errores.
- Tests: **531/531 PASS**, 0 FAIL, 0 skipped.

El primer intento del mismo SHA tuvo 528 PASS / 3 FAIL únicamente en tests de disponibilidad PDFium porque el runner no pudo cargar `pdfium.dll`; no falló ningún test Task 8. El re-run exacto eliminó el ruido sin cambios de código.

## Implementación congelada

### `OrganizeSplitPlanner`

- `ExtractSelected(plan, selectedIds)` conserva el orden lógico actual del plan.
- No modifica el workspace original.
- Selección vacía o IDs ajenos al plan se rechazan.
- `SplitEvery(plan, N)` genera grupos contiguos y permite un último grupo corto.
- `N <= 0` se rechaza.
- `SplitExplicitRanges(plan, expression)` trabaja en numeración humana 1-based y la convierte al orden lógico actual.
- Sintaxis aprobada: grupos separados por coma, por ejemplo `1-3, 4-7, 8-10`.
- Se rechazan grupos vacíos, rangos descendentes, páginas fuera de rango y solapamientos entre grupos.

### `OrganizeOutputNaming`

Formato congelado:

- `reporte_parte-001_p1-3.pdf`
- `reporte_parte-002_p4.pdf`

El nombre conserva directorio y nombre base del destino elegido y añade ordinal de tres dígitos + rango lógico.

### `OrganizeBatchPublisher`

Antes del primer write valida **todo el lote**:

- ninguna ruta destino duplicada;
- ningún destino existente;
- ningún destino igual a un PDF de origen;
- directorios destino existentes;
- cancelación.

Después publica secuencialmente mediante el mismo `PdfOrganizeWriter` transaccional de F5.

Semántica de fallo congelada:

- si falla el primer writer, no queda salida publicada por Task 8;
- si falla un writer posterior, las salidas anteriores permanecen y el lote se detiene;
- no existe rollback distribuido multiarchivo;
- cancelación se comprueba antes de cada writer.

### Preflight / advertencias

- Extraer reutiliza el writer existente.
- Dividir hace una confirmación de warnings para el lote planificado.
- Esa autorización se propaga a cada writer.
- Cada `PdfOrganizeWriter` sigue revalidando fingerprints, preflight y Block de forma independiente.
- Firma criptográfica y fuente protegida siguen siendo Block sin override.
- No se almacenan ni reutilizan contraseñas.

### WPF

- `Dividir...` queda habilitado al entrar en ORGANIZAR.
- `Extraer...` requiere al menos una página seleccionada.
- Extraer/Dividir no sustituyen la sesión PDF activa ni el `OrganizePlan` del workspace.
- Mientras materializan, las mutaciones quedan bloqueadas por `_organizeMaterializing`.
- Split ofrece:
  - Cada N páginas.
  - Rangos explícitos.

## Hallazgos durante GREEN

1. El primer GREEN no compiló por faltar `using System.IO;` en dos archivos nuevos. Se corrigió sin cambio de comportamiento.
2. Dos E2E inicialmente devolvían `false` porque las fixtures creadas por PDFsharp contienen metadatos (`Creator/Producer`), que el preflight real clasifica correctamente como Warning. Las pruebas se corrigieron para confirmar dicha advertencia en vez de simular un preflight limpio incompatible con el writer real.
3. Dos tests previos aún congelaban la frontera Task 7 (Extract/Split deshabilitados). Se actualizaron únicamente para la nueva frontera Task 8: Split habilitado; Extract condicionado a selección.
4. Un intento de CI del head funcional mostró un flake conocido de carga de `pdfium.dll`; el re-run del mismo SHA pasó 531/531 sin modificación.

## Auditoría de diff Task 7 → Task 8

Base: `65e6713f232c65de1053af539d12bb2536fad2ce`
Head funcional: `3bc0b68b04f8fbe97e1400a9b81454d447503d37`

Archivos del alcance:

### Producción
- `src/SGPdf.App/Features/Organize/OrganizeBatchPublisher.cs`
- `src/SGPdf.App/Features/Organize/OrganizeOutputNaming.cs`
- `src/SGPdf.App/Features/Organize/OrganizeSplitPlanner.cs`
- `src/SGPdf.App/MainWindow.Organize.ExtractSplit.cs`

### Tests
- `tests/SGPdf.App.Tests/OrganizeBatchPublisherTests.cs`
- `tests/SGPdf.App.Tests/OrganizeOutputNamingTests.cs`
- `tests/SGPdf.App.Tests/OrganizeSplitPlannerTests.cs`
- `tests/SGPdf.App.Tests/MainWindowOrganizeExtractSplitTests.cs`
- `tests/SGPdf.App.Tests/MainWindowOrganizeTask6Tests.cs` — solo frontera de comandos Task 8.
- `tests/SGPdf.App.Tests/MainWindowOrganizeInsertTests.cs` — solo frontera de comandos Task 8.

No hubo cambios en:

- paquetes/dependencias;
- `.csproj` / lockfiles;
- bindings nativos PDFium;
- LEER;
- FIRMAR;
- ZPL/Labelize;
- `main`.

## Próximo gate

**Task 9 — F5.5 Preservation hardening**, solo después de un nuevo `continua` del usuario.

Task 9 no se inició en este checkpoint.
