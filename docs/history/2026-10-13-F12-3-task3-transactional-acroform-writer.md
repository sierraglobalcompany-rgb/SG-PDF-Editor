# F12.3 Task 3 — Transactional AcroForm writer/service

Fecha: 2026-10-13  
Rama: `feat/f12-3-task3-transactional-writer`  
Base exacta de Task 3: `9f7e3ee423c54b07043b3880c5e2ca840f6500a6`  
HEAD funcional verificado: `37158b25e6d957c421eada7d23604af49c23efc9`

## Resultado

**CLOSED / GREEN** para campos de texto AcroForm clásicos existentes, incluidos campos jerárquicos con `/FT /Tx` heredado.

Task 3 implementa el writer/servicio transaccional. El validador independiente y la preservación estructural amplia siguen siendo Task 4 y no forman parte de este cierre.

## Contrato implementado

`preflight → supported-field gate → fingerprint → same-directory temp → write /V + /AP/N → reopen/value validation → fingerprint recheck → atomic publish → cleanup`

Garantías demostradas:

- el source no se sobrescribe;
- `source == destination` se rechaza;
- solo nombres reconocidos por `PdfAcroFormInspector` como `/Tx` soportados pueden escribirse;
- el temporal se crea en el directorio de destino;
- `/V` persiste tras reapertura;
- `/AP/N` se regenera con el nuevo texto;
- el formulario sigue presente al reabrir el temporal;
- los valores esperados se validan antes de publicar;
- el fingerprint del source se revalida antes de publicar;
- destination existente se reemplaza solo al final;
- cualquier fallo pre-publicación conserva destination existente;
- el temporal se limpia en éxito y fallo;
- cancelación antes de publicar no deja salida parcial;
- source firmado se bloquea fail-closed;
- source realmente protegido por contraseña se bloquea antes de temp/publicación;
- campos jerárquicos como `customer.name` funcionan con `/FT /Tx` heredado.

## Implementación KISS

Archivo de producción:

- `src/SGPdf.App/Features/Forms/PdfAcroFormSaveService.cs`

Pruebas:

- `tests/SGPdf.App.Tests/PdfAcroFormSaveServiceTests.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormSaveServicePreflightTests.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormSaveServiceHierarchyTests.cs`

No se añadió:

- segundo motor PDF;
- paquete NuGet adicional;
- FormFill/PInvoke nuevo;
- UI;
- backend;
- reescritura manual general de `/AP`;
- arquitectura genérica para tipos de campo no probados.

La apariencia usa el `PdfTextField.Text` de PDFsharp, que escribe el valor y ejecuta su regeneración de apariencia. Para terminales jerárquicos con `/FT` heredado, el recorrido raw ya conoce que el campo efectivo es `/Tx` y materializa sobre ese mismo `PdfDictionary` el `PdfTextField(PdfDictionary)` interno de PDFsharp mediante una reflexión estrecha. No se inyecta un `/FT` local sintético ni se modifica la estructura jerárquica para acomodar la librería.

## Línea RED → GREEN

### Transacción base

- RED: `d328c93cfa3b96ee02924737772dba12cdf65dbc`
- GREEN: `98d9594570cce73aa63e7f37913aa309f8d0ab03`

Probó same-directory temp, publicación solo después de validación, source intacto y cleanup.

### Apariencia

- RED: `f4acbb737d27876ee0400bdee2e16201537f5521`
- GREEN: `c1ff671e67cbcff920f270814e8a79db1d0ccd8a`

Probó que cambiar solo `/V` dejaba `/AP/N` stale y luego que el setter tipado de PDFsharp regeneraba la apariencia persistida.

### Guardrails transaccionales caracterizados

- destino existente preservado en fallo pre-publicación: `af7f10b8797436e50cdecb5c411d49e8169ff400`
- source fingerprint drift: `efbe2cee1a12f5db8d5708c5a1b9e187362c510d`
- cancelación antes de publicación: `50b9be8a277227f5a89c59f9425df4ef35bdd852`
- source firmado bloqueado: `5e24731be958bacd6b29627aa2733b34c66165d4`
- source protegido real bloqueado: `ecd42c734cffaab5779ebbe9ea62af77992f7570`

El fixture protegido se crea con qpdf 12.4.2 ya fijado por el repositorio; no se añadió dependencia.

### Hueco jerárquico detectado en auditoría final

RED: `1988f9fb2e3db1178462e1d965877d12cfcda71e`

Workflow: `38425728873`  
Job: `115394499911`

Resultado RED:

- build: PASS;
- 0 warnings / 0 errors;
- 1160 tests PASS;
- 1 fallo intencional nuevo;
- fallo: `customer.name` fue materializado por PDFsharp como `PdfGenericField` sin setter `Text`.

Causa raíz: PDFsharp 6.2.4 determina el subtipo de un `PdfAcroField` usando el `/FT` local del diccionario. Un widget terminal válido cuyo `/FT /Tx` se hereda del padre no se materializa automáticamente como `PdfTextField`.

GREEN: `37158b25e6d957c421eada7d23604af49c23efc9`

Workflow: `38426417849`  
Job: `115396878669`

Corrección mínima:

- conservar el recorrido raw existente que ya resuelve `/Kids`, nombre completo e `/FT` heredado;
- sobre el diccionario terminal efectivo `/Tx`, reutilizar `PdfTextField(PdfDictionary)` de PDFsharp mediante reflexión estrecha;
- establecer `Text` allí mismo;
- eliminar el segundo recorrido tipado por nombre que causaba la discrepancia.

Resultado GREEN exact-head:

- repository hygiene: PASS;
- Labelize 1.7.0: PASS;
- qpdf 12.4.2: PASS;
- Tesseract 5.5.3: PASS;
- locked restore: PASS;
- Release build: PASS;
- warnings: 0;
- errors: 0;
- tests: **1161/1161 PASS**;
- skipped: 0.

El único warning externo del workflow es la deprecación de Node.js 20 de acciones GitHub, forzadas por la plataforma a Node.js 24; no pertenece al código SG PDF.

## Scope probado

AUTO PASS dentro de Task 3:

- classic text field plano;
- classic text field jerárquico;
- `/FT /Tx` heredado;
- nombres compuestos `Parent.Child`;
- persistencia de `/V`;
- regeneración persistida de `/AP/N`;
- transacción Save As con temp + publicación atómica;
- cleanup;
- fingerprint drift;
- cancelación;
- source firmado fail-closed;
- source protegido por contraseña fail-closed.

## No probado / fuera de Task 3

Permanece NOT RUN o fuera de alcance:

- checkbox;
- radio button;
- combo box;
- list box;
- XFA;
- JavaScript/cálculos/acciones;
- campos de firma;
- flatten;
- estilización avanzada;
- UI WPF;
- validación independiente completa de preservación estructural representativa;
- validación visual mediante FormFill/`FPDF_FFLDraw`;
- interoperabilidad manual con Acrobat/Foxit/Edge.

La preservación independiente de Task 4 debe validar page count, valores, apariencia persistida y estructuras representativas sin ampliar el writer.

## Decisión

Task 3 queda **CLOSED / GREEN** sobre el HEAD funcional `37158b25e6d957c421eada7d23604af49c23efc9`, sujeto a una última ejecución exact-head después de este cierre documental.

Task 4 queda preparado para un microbloque posterior. No se inicia en este cierre.

No merge realizado.
