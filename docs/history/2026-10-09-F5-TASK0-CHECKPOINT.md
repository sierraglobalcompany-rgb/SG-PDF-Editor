# F5 Organizar — Checkpoint Task 0

Fecha: 2026-10-09
Rama: `feat/f5-organize`

## Propósito

Checkpoint de continuidad antes de iniciar F5.1. Este archivo permite retomar el trabajo sin reconstruir el contexto del chat.

## Estado general

- F4 Full Reader permanece cerrado en su rama previa; no se ha fusionado a `main`.
- `main` permanece intacto en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- F5 tiene especificación y plan aprobados en `feat/f5-organize`.
- Task 0 del plan F5 quedó implementado y verificado.
- No se ha iniciado Task 1 / F5.1.
- No hay autorización para hacer merge a `main`.

## Base documental F5

- Spec: `docs/superpowers/specs/2026-10-09-f5-organize-design.md`
- Plan: `docs/superpowers/plans/2026-10-09-f5-organize.md`
- Head del plan antes de implementación: `24f926d39cc91fa37148158fc71560fbb6276c62`
- CI del plan: `37951468121` — PASS.

## Task 0 — problema encontrado

La auditoría previa a F5 detectó que `PdfDocumentSession.Navigation.cs` utilizaba:

```csharp
lock (PdfiumRuntime.NativeGate)
```

mientras `PdfiumRuntime.NativeGate` es un `SemaphoreSlim` y el resto del acceso a PDFium usa `Wait()/Release()`.

`lock` y `SemaphoreSlim.Wait()` no sincronizan entre sí, por lo que navegación podía ejecutar llamadas nativas simultáneamente con render/text/session.

## TDD RED

Se agregó `tests/SGPdf.App.Tests/PdfiumNativeGateTests.cs` con la prueba conductual:

`NavigationCalls_WaitForGlobalNativeSemaphore`

La prueba toma `NativeGate`, inicia `GetBookmarks()` y `GetPageLinks()` desde otro task y exige que no completen hasta liberar el gate.

Commit RED:

`9070458c8c079a334015f5b13852bc6e94cd6523`

CI RED:

`37952146683`

Resultado esperado y confirmado:

- Build: PASS, 0 warnings, 0 errors.
- Tests: 417 PASS / 1 FAIL / 418 total.
- Único fallo: `PdfiumNativeGateTests.NavigationCalls_WaitForGlobalNativeSemaphore`.
- Mensaje: `La operación de navegación atravesó PDFium mientras NativeGate seguía ocupado.`

Esto confirmó que el defecto era real y que la prueba fallaba por la razón correcta.

## Primer GREEN y segundo bug descubierto

Se sustituyeron los `lock` de `GetBookmarks()` y `GetPageLinks()` por:

```csharp
PdfiumRuntime.NativeGate.Wait(cancellationToken);
try
{
    // trabajo nativo existente
}
finally
{
    PdfiumRuntime.NativeGate.Release();
}
```

Commit:

`67a680efddeb8b9c6c78f9759a02b9cebb1cbc75`

Al ejecutar la suite apareció un auto-deadlock oculto: `ReadDestinationPageIndex()` llamaba a `PageCount`, y `PageCount` intentaba adquirir nuevamente el mismo `SemaphoreSlim` mientras navegación ya lo poseía.

La demora anormal del CI fue la señal que permitió encontrar esta segunda condición.

## Ajuste de prueba

La prueba del gate fue aislada de paralelización de xUnit mediante una collection con `DisableParallelization = true`, porque toma intencionalmente un recurso nativo global.

Commit:

`ca06a255bb548c6c33e240d378dda6f863cc78b8`

Esto no modifica el criterio funcional de la prueba; únicamente evita interferencia con otras clases durante la comprobación del recurso global.

## Fix final Task 0

`ReadDestinationPageIndex()` ya no llama a la propiedad `PageCount` dentro de una región que posee `NativeGate`.

Ahora consulta directamente:

```csharp
var pageIndex = PdfiumNative.FPDFDest_GetDestPageIndex(_document, destination);
var pageCount = PdfiumNative.FPDF_GetPageCount(_document);
```

y valida el destino usando ese conteo mientras el gate ya está adquirido.

Commit funcional final de Task 0:

`6963696283ae8d21fc49acff0544220a061aafd2`

Archivos de producto/prueba implicados:

- `src/SGPdf.App/Pdf/PdfDocumentSession.Navigation.cs`
- `tests/SGPdf.App.Tests/PdfiumNativeGateTests.cs`

## Evidencia final Task 0

CI exacto del commit funcional final:

`37953079615`

Resultado:

- Repository hygiene: PASS.
- Labelize 1.7.0 staging: PASS.
- Locked restore: PASS.
- Release build: PASS.
- Build: 0 warnings / 0 errors.
- Tests: 418 PASS / 0 FAIL / 0 SKIP.
- Suite completa: PASS.

Task 0 se considera AUTOMATED PASS.

## Estado actual al crear este checkpoint

Antes de este commit documental:

- Rama `feat/f5-organize`: `6963696283ae8d21fc49acff0544220a061aafd2`.
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No merge realizado.
- No Task 1 iniciado.

El commit que contiene este archivo será un checkpoint documental posterior al commit funcional `6963696...`; no cambia código de producto.

## Siguiente paso exacto

Retomar en **Task 1 — F5.1 PDFium Capability Gate + Protected-Session Marker** del plan.

Orden recomendado al continuar:

1. Verificar que `feat/f5-organize` parte de este checkpoint y que `main` sigue intacto.
2. Leer Task 1 del plan `docs/superpowers/plans/2026-10-09-f5-organize.md`.
3. Ejecutar primero las pruebas del capability gate contra el `pdfium.dll` pinneado.
4. Si falta cualquier export crítico (`FPDF_CreateNewDocument`, `FPDF_ImportPagesByIndex`, `FPDFPage_GetRotation`, `FPDFPage_SetRotation`, `FPDF_SaveAsCopy`), detener implementación y volver a revisión de diseño.
5. Si el capability gate pasa, continuar TDD RED→GREEN para `OpenedWithPassword` y rotación.
6. No avanzar más allá del checkpoint F5.1 sin CI Windows exact-head PASS.
7. No hacer merge a `main` sin aprobación explícita del usuario.

## Estado de QA

Este checkpoint solo certifica automatización de Task 0. No cambia el estado de QA físico/manual previo: continúa NOT RUN donde ya estaba marcado como tal.
