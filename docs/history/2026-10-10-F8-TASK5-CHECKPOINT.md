# F8 — Task 5 checkpoint — text markup geometry + writer/validator

Fecha: 2026-10-10
Rama: `feat/f8-comments-v1`
Base Task 4: `6768224c7a7e98ffdf785f7e41cc886f8a2fd509`

## Estado

Task 5 implementada y verificada automáticamente. Manual Windows/stylus/touch/external-viewer QA: **NOT RUN**.

## Contrato implementado

- `TextMarkupQuadBuilder` convierte `PdfTextRect` autoritativos de LEER a quadpoints PDFium para HIGHLIGHT / UNDERLINE / STRIKEOUT.
- La selección multilinea de una misma página conserva un quad por rectángulo y calcula un bounding rect global.
- Geometría vacía, degenerada o no finita falla explícitamente.
- `CommentWorkspace` puede asociar un baseline existente con su `CommentIdentity` estable; el writer falla cerrado si una mutación existente carece de esa identidad.
- `PdfCommentWriter` materializa exclusivamente Resaltar/Subrayar/Tachado en este slice.
- Resolución de comentarios existentes se hace contra una sesión fresca antes de cualquier mutación nativa.
- El `AnnotationIndex` solo se usa como locator request-scoped después de resolver la identidad estructural; no se almacena como autoridad durable.
- Orden seguro por página: modificaciones existentes → eliminaciones en índice descendente → creaciones nuevas.
- En Task 5, un markup existente solo admite cambio de color/alpha o eliminación; cambios de geometría fallan cerrado y se difieren.
- Nuevos markups escriben subtype + rect + RGBA + quadpoints reales.
- Source == destination bloqueado; source fingerprint stale bloqueado; password-opened bloqueado; firma criptográfica bloqueada.
- Guardado: source fresco → temporal sibling → cerrar handles → reopen/validate → publicación atómica → cleanup `finally`.
- `PdfCommentOutputValidator` reconstruye el conjunto esperado desde source + workspace, reabre temporal, compara anotaciones en páginas tocadas y ejecuta smoke render antes de publicar.
- El writer no llama `MarkSaved()`; el baseline solo podrá limpiarse después del flujo UI/reopen posterior.

## TDD evidence

### RED

Head RED: `61bb2b1ce7ff326354337f656ee4da397b4406e1`
Windows CI: workflow `38096028578`, job `114341992615`.
Resultado esperado:
- build compiló;
- tests previos: **801 PASS**;
- nuevos Task 5: **7 FAIL** por ausencia de `TextMarkupQuadBuilder`, `PdfCommentWriter`, `PdfCommentOutputValidator` y overload de identidad en workspace;
- total: 808 tests;
- el RED reveló 2 warnings xUnit2031 en tests nuevos; se corrigieron antes del GREEN final.

### GREEN funcional

Head funcional antes de este checkpoint: `b2738e97ba0231019a53a8194e5433756d1156c7`
Windows CI: workflow `38096259213`, job `114342665683`.
Resultado:
- Release build: **0 warnings / 0 errors**;
- Tests: **808 PASS / 0 FAIL / 0 skipped**.

Cobertura funcional del slice:
- crear HIGHLIGHT + UNDERLINE + STRIKEOUT en un source sin anotaciones;
- source original permanece intacto;
- editar RGBA de un highlight existente mediante identidad estable;
- eliminar underline existente;
- preservar strikeout, note, ink, square y circle no objetivo en la misma página;
- mutación existente sin identidad estable falla cerrado y no publica destino;
- validator rechaza un PDF que no contiene el markup esperado.

## Scope audit

Diff Task 4 checkpoint → GREEN Task 5: **7 commits ahead / 0 behind**.
Archivos cambiados únicamente:
- `src/SGPdf.App/Features/Comments/CommentWorkspace.cs`
- `src/SGPdf.App/Features/Comments/TextMarkupQuadBuilder.cs`
- `src/SGPdf.App/Pdf/PdfCommentWriter.cs`
- `src/SGPdf.App/Pdf/PdfCommentOutputValidator.cs`
- `tests/SGPdf.App.Tests/TextMarkupQuadBuilderTests.cs`
- `tests/SGPdf.App.Tests/PdfCommentWriterTextMarkupTests.cs`

No se añadió:
- UI/XAML;
- soporte de Nota/Ink/Square/Circle en el writer;
- nuevo motor PDF;
- paquete/NuGet;
- networking/cloud;
- merge a `main`.

## Gate

Task 6 no se inicia dentro de este checkpoint. Siguiente tarea del plan: **Sticky note slice (TEXT)**.
