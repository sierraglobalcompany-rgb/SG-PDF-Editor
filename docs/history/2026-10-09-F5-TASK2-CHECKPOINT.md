# F5 Task 2 — Checkpoint final

Fecha: 2026-10-09  
Rama: `feat/f5-organize`

## Estado

Task 2 — **F5.1 OrganizePlan + Intra-Document Operations** completado mediante TDD. Task 3 no ha sido iniciado.

## Evidencia RED

- Commit RED: `8f06817cea8780ab7df4a09974c836e6967b8fef`
- CI RED: `37955886508`
- Build: PASS, 0 warnings / 0 errors.
- Suite: 426 pruebas existentes PASS + 16 pruebas nuevas FAIL.
- Los 16 fallos fueron por ausencia de los tipos `SGPdf.App.Features.Organize.*`, que era la causa esperada antes de implementar el modelo.

## Implementación

Archivos creados:

- `src/SGPdf.App/Features/Organize/OrganizeSource.cs`
- `src/SGPdf.App/Features/Organize/OrganizePage.cs`
- `src/SGPdf.App/Features/Organize/OrganizePlan.cs`
- `src/SGPdf.App/Features/Organize/OrganizePlanOperations.cs`

Pruebas creadas en el RED:

- `tests/SGPdf.App.Tests/OrganizePlanTests.cs`
- `tests/SGPdf.App.Tests/OrganizePlanOperationsTests.cs`

Contratos cubiertos:

- captura de fuente con ruta normalizada + tamaño + `LastWriteTimeUtc`;
- detección de fuente desaparecida o modificada;
- páginas lógicas con IDs únicos y rotación normalizada 0..3;
- validación de fuente desconocida, página fuera de rango e ID duplicado;
- move de selección conservando orden visual, incluida selección no contigua hacia adelante/atrás;
- drop dentro del span seleccionado como no-op;
- rotate modulo 4;
- delete con bloqueo de documento de cero páginas;
- duplicate con nuevos `ItemId`;
- insert de páginas al inicio/medio/final;
- IDs e índices inválidos rechazados;
- plan/state sin PDFium, WPF ni dependencias nuevas.

## Incidencia GREEN

- Implementación WIP inicial: `330ad90322f08a249cf28388fd9949a91375f726`.
- CI WIP: `37956253142` — FAIL en build por falta de `using System.IO;` en `OrganizeSource.cs`; tests no llegaron a ejecutarse.
- Checkpoint WIP: `3594e5481b56acc4594a0021d882f92e48a61b88`.
- Fix mínimo: `0aefa34562f37b49286b118c9159950a7e3254c1`.

## Evidencia GREEN funcional

CI exact-head funcional: `37956579499` sobre `0aefa34562f37b49286b118c9159950a7e3254c1`.

- higiene: PASS;
- Labelize pinneado: PASS;
- restore locked: PASS;
- build Release: PASS, 0 warnings / 0 errors;
- suite completa: **442/442 PASS**, 0 failed, 0 skipped.

## Auditoría de alcance

Diff desde el commit RED hasta el GREEN funcional contiene únicamente:

- los cuatro archivos puros de `Features/Organize`;
- el checkpoint WIP documental.

No se modificaron PDFium, UI, Firma, Reader, ZPL, csproj, lockfiles ni dependencias.

## Punto exacto de reanudación

Siguiente tarea: **Task 3 — F5.1 Structural Preflight + Frozen Policies**.

No ejecutar Task 3 hasta una nueva instrucción `continua` del usuario.
No hacer merge a `main` sin autorización explícita.
