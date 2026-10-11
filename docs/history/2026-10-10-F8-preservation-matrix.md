# F8 — Matriz de preservación COMENTAR V1

Fecha de medición: 2026-10-11  
Rama: `feat/f8-comments-v1`  
Writer medido: `PdfCommentWriter`  
Prueba: `tests/SGPdf.App.Tests/PdfCommentPreservationF8Tests.cs`

## Regla de evidencia

Esta matriz se midió de nuevo con el writer real de F8. No se copiaron resultados de F7. Se reutilizaron únicamente técnicas de construcción/inspección de fixtures ya probadas en el repositorio.

La fixture F8 usa marcadores propios y contiene simultáneamente estructuras de documento, una anotación `TEXT` soportada por COMENTAR V1 y una anotación `STAMP` deliberadamente no soportada por V1.

## Matriz medida

| Estructura / dato | Evidencia F8 fresca | Resultado |
|---|---|---|
| AcroForm + valor | `GetOrganizeFormType() != 0` y `/V (F8 Filled Value)` después de Save As | PROVEN PRESERVED |
| Marcadores | bookmark `F8 Root` reaparece apuntando a página 0 | PROVEN PRESERVED |
| Destinos nombrados | `HasOrganizeNamedDestinations()` y marcador `F8Chapter` | PROVEN PRESERVED |
| Enlace interno | link de página 0 conserva destino a página 1 | PROVEN PRESERVED |
| Estructura etiquetada | `IsOrganizeTagged()` y `/StructTreeRoot` presentes | PROVEN PRESERVED |
| Etiquetas de página | `HasOrganizePageLabels()` y prefijo `F8-` | PROVEN PRESERVED |
| Adjunto | `f8-note.bin` conservado con payload exacto `f8-exact-payload-2026` | PROVEN PRESERVED |
| Metadatos | título, autor y `CustomMarker (F8 Task11 fresh metadata)` conservados | PROVEN PRESERVED |
| Rotación | página 2 conserva quarter-turn `1` (`/Rotate 90`) | PROVEN PRESERVED |
| Anotación soportada existente | `TEXT` con contenido `Existing supported F8` reaparece tras Save As | PROVEN PRESERVED |
| Nueva anotación F8 | nueva `TEXT` Unicode `Nueva nota F8 niño café` reaparece tras Save As | PROVEN PRESERVED |
| Anotación no soportada representativa | `STAMP` + contenido `Unsupported F8 stamp` permanecen en el PDF; discovery sigue tratándola fuera del alcance editable V1 | PROVEN PRESERVED / READ-ONLY V1 |

## Lifecycle completo

Se ejecutó con archivos físicos distintos y reapertura real entre cada etapa:

1. source F8 → crear nota `Lifecycle creada F8 niño café` → Save As;
2. reabrir → descubrir + reconstruir identidad/baseline → editar a `Lifecycle editada F8 Bogotá áé` → Save As;
3. reabrir → descubrir + reconstruir identidad/baseline → eliminar la nota → Save As;
4. reabrir salida final → la nota lifecycle ya no existe;
5. la anotación `TEXT` original y la anotación unsupported representativa siguen presentes.

Resultado: **PROVEN** para `create → save → reopen → edit → save → reopen → delete → save → reopen`.

## RED / GREEN de Task 11

### Primer gate

Commit: `20ee97f1a748d48966c95173b670c6ad2d304d35`  
Workflow: `38103391271`  
Job: `114363775914`

- build: 0 warnings / 0 errors;
- tests: 847 PASS / 1 FAIL / 0 skipped;
- fallo único: la prueba exigía texto PDF literal `/Subtype /Stamp`;
- evidencia del archivo reserializado: PDFium puede escribir válidamente `/Subtype/Stamp` sin espacio;
- no se detectó pérdida del `STAMP`; el fallo era de la aserción textual.

### Corrección mínima y GREEN

Commit: `63785d450a293de98f0ae898ebd875e8003438a3`  
Workflow: `38103509065`  
Job: `114364109894`

Cambio único: hacer la aserción tolerante a serialización PDF equivalente mediante `/Subtype\s*/Stamp`.

Resultado exacto:

- build: **0 warnings / 0 errors**;
- tests: **848 PASS / 0 FAIL / 0 skipped**;
- producción modificada por Task 11 hasta este punto: **ninguna**.

## Implicaciones

- F8 dispone ahora de evidencia propia para la preservación de las estructuras representativas exigidas por el plan.
- La evidencia de `STAMP` significa preservación, no soporte de edición. COMENTAR V1 no debe promover ese subtipo a editable.
- La política/preflight puede consumir esta evidencia en el hardening/cierre posterior; Task 11 no cambia esa política por sí sola.
- Esta matriz no sustituye QA manual en lectores externos ni prueba compatibilidad visual completa en Adobe/Foxit/Edge; ese QA sigue pendiente.
