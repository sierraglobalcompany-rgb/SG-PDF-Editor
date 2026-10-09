# SG PDF — F5 ORGANIZAR — Task 4 Checkpoint Final

**Fecha:** 2026-10-09  
**Rama:** `feat/f5-organize`  
**Estado:** Task 4 — F5.2 Transactional Writer + Output Validator — **COMPLETO / GREEN**  
**No iniciar Task 5 sin nuevo `continua` del usuario.**

## Baseline

- `main` permanece intacto en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- Checkpoint final Task 3: `a462a43e9c2b494053f766cf4b00d397c1f006e8`.
- RED inicial Task 4: `0e8d557597aa3bed746186a94c124e99c8007fa8`.
- RED corregido y válido: `2485ccd0435743a47a066b44cbefb888df68f212`.
- GREEN inicial: `2856ee3187b255e842c35d556890e2a4a79f4eab`.
- Fix semántica tamaño/rotación: `5946e89f5ec248f55ebe1e31998341845c52cfa7`.
- GREEN funcional final: `4e96d980ca70cbf79c1baf78c0123ae3d6f1fb37`.

## TDD evidence

### RED inicial

CI `37965194963` sobre `0e8d557597aa3bed746186a94c124e99c8007fa8` confirmó ausencia de los contratos Task 4, pero también detectó errores de las propias pruebas:

- ambigüedad de overload de `Assert.Throws` con la versión actual de xUnit;
- un `Select` con method-group ambiguo.

No se escribió código de producción hasta corregir esos defectos de test.

### RED válido

Tras corregir exclusivamente los tests, CI `37965399270` sobre `2485ccd0435743a47a066b44cbefb888df68f212`:

- restore PASS;
- el proyecto de producto compiló;
- el proyecto de tests falló con **26 errores**, todos por tipos Task 4 todavía inexistentes:
  - `OrganizeExpectedPage`;
  - `OrganizeOutputValidator`;
  - `PdfOrganizeWriter`;
- los errores accidentales de xUnit/method-group ya no estaban presentes.

Este es el RED válido de Task 4.

### GREEN inicial y hallazgo semántico

CI `37965643858` sobre `2856ee3187b255e842c35d556890e2a4a79f4eab`:

- Release build PASS;
- 0 warnings / 0 errors;
- 464 tests total;
- 462 PASS;
- 2 FAIL.

Los dos fallos fueron de validación de tamaño de página con rotación. El fallo demostró que `PdfDocumentSession.GetPageSize` / `FPDF_GetPageSizeByIndexF` entrega el tamaño visible considerando la rotación actual.

### Ruling: tamaño visible y rotación relativa

El writer recibe desde PDFium el tamaño visible del origen bajo su rotación existente. Por lo tanto:

- delta relativo par (`0` o `2`) conserva los ejes visibles;
- delta relativo impar (`1` o `3`) intercambia ancho/alto visibles;
- la rotación absoluta esperada sigue siendo `(sourceRotation + delta) & 3`.

Se corrigió la expectativa generada por el writer; el validador mantuvo su responsabilidad de comparar el descriptor esperado con el PDF real.

### GREEN final

CI exact-head `37965905332` sobre `4e96d980ca70cbf79c1baf78c0123ae3d6f1fb37`:

- repository hygiene PASS;
- Labelize 1.7.0 staging PASS;
- locked restore PASS;
- Release build PASS;
- **0 warnings / 0 errors**;
- **464/464 tests PASS**;
- 0 failed / 0 skipped.

El restore del runner tardó aproximadamente 2.16 minutos, pero terminó correctamente; no fue un defecto de código.

## Implementado

### `src/SGPdf.App/Pdf/OrganizeOutputValidator.cs`

Contratos:

```csharp
internal readonly record struct OrganizeExpectedPage(
    PdfPageSize PageSize,
    int RotationQuarterTurns);

internal sealed class OrganizeOutputValidator
{
    internal void Validate(
        string outputPath,
        IReadOnlyList<OrganizeExpectedPage> expectedPages,
        CancellationToken cancellationToken = default);
}
```

Validación:

- reabre el archivo temporal mediante `PdfDocumentSession`;
- verifica cantidad exacta de páginas;
- verifica tamaño por página con tolerancia estrecha de `0.05` puntos;
- verifica rotación absoluta exacta `0..3`;
- renderiza **cada página**, secuencialmente, a **36 DPI**;
- soporta cancelación;
- no publica ningún archivo: solo valida.

La prueba incluye una costura interna opcional para observar el orden/DPI de render sin alterar la ruta real de `RenderPage`.

### `src/SGPdf.App/Pdf/PdfOrganizeWriter.cs`

`SaveAsCopy(...)` implementa el materializador transaccional F5:

1. valida plan, ruta activa y destino;
2. rechaza destino igual al PDF activo o a cualquier PDF fuente;
3. valida directorio de destino;
4. verifica fingerprint de todos los orígenes antes de crear temporal;
5. reabre cada origen y ejecuta preflight antes de crear temporal;
6. `Block` siempre detiene;
7. `Warning` exige `warningsConfirmed=true`;
8. PDF que requiere contraseña se bloquea sin persistir credenciales;
9. entra una sola vez a `PdfiumRuntime.NativeGate` para la fase nativa;
10. abre fuentes con password `null`;
11. crea un PDF nuevo con `FPDF_CreateNewDocument`;
12. importa **una página lógica a la vez** con `FPDF_ImportPagesByIndex`;
13. conserva reorder/delete/duplicate mediante el orden del `OrganizePlan`;
14. calcula rotación absoluta `(sourceRotation + delta) & 3`;
15. aplica `FPDFPage_SetRotation` a cada página importada;
16. calcula `OrganizeExpectedPage` mientras las fuentes nativas están abiertas;
17. guarda a temporal en el mismo directorio del destino mediante `FPDF_SaveAsCopy`;
18. cierra handles y libera `NativeGate` antes de usar APIs públicas de sesión;
19. valida el temporal completo con `OrganizeOutputValidator`;
20. publica atómicamente con `File.Replace` o `File.Move`;
21. limpia el temporal en `finally`.

## Cobertura Task 4

### `OrganizeOutputValidatorTests`

- count/tamaños/rotaciones correctos;
- render de todas las páginas a 36 DPI en orden secuencial;
- count incorrecto falla;
- tamaño incorrecto falla;
- rotación incorrecta falla;
- cancelación precancelada aborta.

### `PdfOrganizeWriterTests`

- materializa reorder + duplicate + delete + rotaciones relativas;
- confirma el contenido mediante marcadores de texto `THREE`, `ONE`, `ONE`;
- verifica rotaciones absolutas `3, 0, 1`;
- destino igual a origen se rechaza antes de escribir;
- `SaveAsCopy_SourceChangedOrMissingBeforeMaterialization_PreservesDestination` cubre origen modificado y eliminado;
- fallo nativo preserva destino existente y limpia temp;
- fallo de validación preserva destino existente y limpia temp;
- preflight Block no crea temp ni reemplaza destino;
- PDF protegido se bloquea sin temp;
- Warning requiere confirmación explícita;
- con confirmación, un PDF con metadata puede materializarse.

Fixtures Task 4 son sintéticas y públicas; no se añadió información privada ni fixtures de Mercado Libre/clientes.

## Self-review Task 3 → Task 4

Comparación `a462a43e9c2b494053f766cf4b00d397c1f006e8` → `4e96d980ca70cbf79c1baf78c0123ae3d6f1fb37`:

- ahead 8 / behind 0;
- exactamente 4 archivos modificados antes de este checkpoint:
  - `src/SGPdf.App/Pdf/OrganizeOutputValidator.cs` — nuevo;
  - `src/SGPdf.App/Pdf/PdfOrganizeWriter.cs` — nuevo;
  - `tests/SGPdf.App.Tests/OrganizeOutputValidatorTests.cs` — nuevo;
  - `tests/SGPdf.App.Tests/PdfOrganizeWriterTests.cs` — nuevo.

Sin cambios en:

- WPF/UI;
- Reader;
- Firma;
- ZPL/Labelize;
- dependencias o lockfiles;
- `PdfDocumentSession.Organize.cs` (no fue necesario ampliarlo para Task 4);
- Task 5;
- `main`.

## Siguiente paso exacto

**Task 5 — F5.2 Organize Surface + Lazy/Bounded Thumbnails.**

Al recibir un nuevo `continua`:

1. releer el brief exacto Task 5;
2. empezar por RED de lazy/bounded thumbnail realization, stale publication y aislamiento de render;
3. implementar solo la superficie/thumbnail lifecycle definida por el plan;
4. suite completa + CI exact-head;
5. checkpoint final;
6. detenerse antes de Task 6.

**No mergear `main` sin aprobación explícita del usuario.**
