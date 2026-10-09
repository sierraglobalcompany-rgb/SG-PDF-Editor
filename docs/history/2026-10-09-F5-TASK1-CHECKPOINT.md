# F5 Organizar — Checkpoint Task 1

Fecha: 2026-10-09
Rama: `feat/f5-organize`

## Propósito

Checkpoint de continuidad después de completar Task 1 / F5.1 capability gate. Permite retomar sin reconstruir el contexto del chat.

## Estado general

- `main` permanece intacto en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No se ha realizado ningún merge.
- F4 Full Reader continúa cerrado en su rama previa y sin merge a `main`.
- F5 spec y plan permanecen aprobados.
- Task 0: AUTOMATED PASS.
- Task 1: AUTOMATED PASS en su commit funcional.
- No se ha iniciado Task 2.

## Base anterior

Checkpoint Task 0:

`09f157a1a1dee50555f7680881d6d51ebb9f349a`

CI del checkpoint Task 0:

`37953422253` — PASS, 418/418 tests, build 0 warnings / 0 errors.

## Task 1 — objetivo

Implementar el capability gate de PDFium necesario para F5 y agregar al `PdfDocumentSession` el contrato mínimo para:

- identificar una sesión abierta con contraseña sin persistir la contraseña;
- consultar la rotación nativa de una página como entero `0..3`;
- confirmar que el `pdfium.dll` pinneado exporta las funciones críticas de organización.

## TDD RED

Commit RED:

`608560afd96c4e089a51ea1550986afab211d296`

Mensaje:

`test(organize): define F5 PDFium session contracts`

Archivos de prueba:

- `tests/SGPdf.App.Tests/PdfiumOrganizeApiAvailabilityTests.cs`
- `tests/SGPdf.App.Tests/PdfOrganizeSessionTests.cs`
- `tests/SGPdf.App.Tests/PdfPasswordTests.cs`

CI RED:

`37954395049`

Resultado:

- Build: PASS, 0 warnings / 0 errors.
- Total: 426 tests.
- PASS: 421.
- FAIL: 5.
- Los cinco fallos correspondieron únicamente a `OpenedWithPassword` y `GetPageRotation` todavía inexistentes.
- Los tests de capability gate pasaron, por lo que el `pdfium.dll` pinneado sí exporta todas las funciones críticas requeridas.

Exports críticos confirmados:

- `FPDF_CreateNewDocument`
- `FPDF_ImportPagesByIndex`
- `FPDFPage_GetRotation`
- `FPDFPage_SetRotation`
- `FPDF_SaveAsCopy`

Al estar presentes todos los exports críticos, no fue necesario detener F5 para revisión de diseño.

## GREEN mínimo

Commit funcional:

`14cf4e5a7e92eb40b82dea8758edf8d0f0b936d0`

Mensaje:

`feat(organize): add PDFium capability gate`

Cambios de producto limitados a:

- `src/SGPdf.App/Pdf/PdfiumNative.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.Organize.cs`

No se modificaron Reader, Firma, ZPL, UI, proyectos ni dependencias.

### PDFium

Se añadieron únicamente los bindings críticos ya demostrados en el DLL pinneado:

- `FPDF_CreateNewDocument`
- `FPDF_ImportPagesByIndex`
- `FPDFPage_GetRotation`
- `FPDFPage_SetRotation`

`FPDF_SaveAsCopy` ya existía.

No se añadieron bindings de detectores opcionales en este Task; faltas opcionales no justifican otro motor PDF.

### Sesión protegida

`PdfDocumentSession` ahora expone:

```csharp
public bool OpenedWithPassword { get; }
```

La contraseña no se guarda en la sesión. Solo se conserva el booleano. Un `Open` exitoso con contraseña no vacía marca la sesión como `OpenedWithPassword = true`; apertura normal queda `false`.

Este criterio es conservador: una sesión abierta proporcionando contraseña se trata como protegida para F5, evitando falsos negativos y sin retener el secreto.

### Rotación

Se agregó:

```csharp
public int GetPageRotation(int pageIndex, CancellationToken cancellationToken = default);
```

Contrato:

- valida índice;
- respeta cancelación;
- usa `PdfiumRuntime.NativeGate` mediante `Wait/Release`;
- carga y cierra la página dentro del gate;
- acepta únicamente rotaciones nativas `0..3`.

Fixture sintético validó páginas con `/Rotate 0`, `/Rotate 90`, `/Rotate 180` y `/Rotate 270` como `0`, `1`, `2`, `3`.

## Evidencia GREEN

CI exacto del commit funcional:

`37954902295`

Resultado:

- repository hygiene: PASS;
- Labelize 1.7.0 staging: PASS;
- locked restore: PASS;
- Release build: PASS;
- build: 0 warnings / 0 errors;
- tests: 426 PASS / 0 FAIL / 0 SKIP;
- suite completa: PASS.

Task 1 se considera AUTOMATED PASS.

## Estado de seguridad/alcance

- PDFium continúa siendo el único motor PDF.
- No hay paquete runtime nuevo.
- No se persistió ninguna contraseña.
- No se implementó todavía materialización/escritura F5.
- No se inició `OrganizePlan`.
- No se tocó `main`.
- No hay autorización para merge a `main`.
- QA físico/manual previo continúa NOT RUN donde ya estaba marcado.

## Siguiente paso exacto

Retomar en **Task 2 del plan F5**.

Antes de ejecutarlo:

1. verificar CI del presente checkpoint documental;
2. confirmar que `main` sigue en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
3. leer Task 2 en `docs/superpowers/plans/2026-10-09-f5-organize.md`;
4. ejecutar Task 2 con TDD RED → GREEN;
5. no avanzar a Task 3 en el mismo bloque sin el siguiente `continua` del usuario;
6. no hacer merge a `main` sin autorización explícita.
