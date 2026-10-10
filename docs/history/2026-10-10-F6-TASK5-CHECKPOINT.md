# F6 Task 5 checkpoint — extracción PNG y reemplazo PNG/JPEG

Fecha: 2026-10-10
Rama: `feat/f6-images`
Base Task 4: `1f578b58d835b3d3c15700fffdcb8401fda3a320`

## Estado

F6 Task 5 queda GREEN. Task 6 no está iniciada.

## RED

Commit RED: `ae261166ef4459b3a2f7ce80830c83b057a31ad0`
Workflow: `38017441206`

Se añadieron únicamente:
- `tests/SGPdf.App.Tests/ImageReplacementAssetLoaderTests.cs`
- `tests/SGPdf.App.Tests/PdfImageExtractionTests.cs`
- `tests/SGPdf.App.Tests/MainWindowEditImageExtractReplaceTests.cs`

Primer intento: 593 PASS + 16 FAIL. Quince fallos correspondían a los contratos nuevos ausentes; apareció además un fallo heredado de `MainWindowOrganizeSecondaryThumbnailTests.InsertedSecondaryPage_RendersThumbnailFromItsOwnSourceSession`.

Se repitió el mismo job sobre el mismo SHA. El rerun produjo 594 PASS + 15 FAIL, exactamente los 15 contratos nuevos. El fallo de thumbnail desapareció sin cambio de código y quedó clasificado como flaky heredada.

## Implementación

Commits funcionales:
- `b17144746c9f77d6b19e8d226bad9c376cb29608` — implementación inicial.
- `7d14a7be9bab52e55bb27734e1254e4cd3ff6a3b` — corrección mínima de manejo controlado de candidato inválido.

Cambios de producción:
- `ImageReplacementAsset.cs`: payload in-memory con formato, bytes codificados, BGRA, dimensiones, stride y alpha real.
- `ImageReplacementAssetLoader.cs`: acepta PNG/JPG/JPEG; valida extensión + firma; decodifica completamente con WPF; captura los bytes antes de retornar; no conserva dependencia de la ruta fuente.
- `PdfDocumentSession.Images.cs`: `preferRendered` usa `FPDFImageObj_GetRenderedBitmap` y cae a `FPDFImageObj_GetBitmap` si no está disponible; `GetImagePng` codifica localmente una representación visual como PNG.
- `MainWindow.EditImages.ExtractReplace.cs`: reemplazo candidate-first y extracción a PNG mediante seams testeables, sin escribir/modificar el PDF fuente.
- `MainWindow.EditImages.cs`: acciones contextuales `Reemplazar...` y `Extraer PNG`.
- `ImageEditState.cs`: reemplaza el forward-record vacío por el asset real.

## Contratos cerrados

- PNG/JPG/JPEG válidos se cargan en memoria; contenido corrupto o extensión no soportada se rechazan antes de modificar el workspace.
- Borrar el archivo de reemplazo después de seleccionarlo no invalida el asset: bytes codificados y BGRA ya quedaron capturados.
- Reemplazar conserva exactamente `CurrentMatrix` y crea una operación `Replace` undoable.
- Cancelar o fallar un reemplazo no cambia estado ni historial.
- Extraer cancela sin escribir ni invocar extracción; el éxito no ensucia el workspace ni crea undo.
- La extracción genera PNG real; nunca bytes crudos del stream PDF con extensión `.png`.
- Se probaron imagen opaca, rotada/escalada y transparencia representativa.

## Estado de transparencia

Task 5 prueba dos cosas distintas y ambas pasan:
1. El loader detecta transparencia real de un PNG por los valores alpha de los píxeles BGRA, no solo por el tipo de contenedor.
2. La ruta PDFium rendered-bitmap → PNG conserva alpha representativo en el fixture transparente de extracción.

Esto **no** autoriza todavía a afirmar que un reemplazo PNG transparente se guarda correctamente dentro de un PDF. Esa afirmación queda bloqueada hasta Task 6, donde debe demostrarse con escritura transaccional + reopen + render/validación. No existe fallback de flatten a blanco.

## GREEN exacto

SHA funcional final: `7d14a7be9bab52e55bb27734e1254e4cd3ff6a3b`
Workflow: `38017919605`
- checkout exacto: PASS
- repository hygiene: PASS
- restore locked: PASS
- build Release: PASS
- warnings: 0
- errors: 0
- tests: **609 PASS / 0 FAIL / 0 SKIP**

El intento anterior `b1714474...` quedó en 608/609 por una única excepción `InvalidDataException` no absorbida en la frontera WPF de reemplazo; se corrigió únicamente ese manejo, sin cambiar tests ni ruta PDFium.

## Auditoría de alcance

Comparación `1f578b58...` → `7d14a7be...`: 9 archivos de Task 5, exactamente 3 tests y 6 archivos de producción. Los cambios adicionales a la lista primaria del plan son modificaciones estrechas a `PdfDocumentSession.Images.cs` para activar la ruta rendered ya caracterizada en Task 1 y a `MainWindow.EditImages.cs` para exponer las dos acciones contextuales.

No se añadió writer, `Guardar como...`, mutación nativa de objetos, opacity, z-order, flatten ni segundo motor PDF.

`main` sigue en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
No hay PR ni merge.
Manual Windows UX QA: NOT RUN.

## Próximo paso

F6 Task 6: writer transaccional + validación save/reopen/render. Debe empezar con RED y probar, entre otras cosas, la preservación real de transparencia antes de permitir guardarla.
