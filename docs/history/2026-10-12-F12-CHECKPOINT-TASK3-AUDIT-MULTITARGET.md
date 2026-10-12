# F12.1 — CHECKPOINT POST TASK 3 + AUDITORÍA MULTI-TARGET

Fecha: 2026-10-12  
Rama: `feat/f12-professional`  
Base F11 cerrada: `7a890b7c8ece75869730c95c275a1ee8202efe93`  
HEAD funcional verificado antes de este checkpoint: `580e49d76a911d70dae8cee5cfa0af3482964fcc`  
`main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7` — SIN CAMBIOS / SIN MERGE

## 1. Estado ejecutivo

F12.1 Redacción real segura continúa en curso.

Estado por tareas del plan:

- Task 1 — capability gate PDFium: **CLOSED / GREEN**.
- Task 2 — política fail-closed + plan inmutable: **CLOSED / GREEN**.
- Task 3 — workspace + materialización transaccional: **AUTOMATED GREEN** y auditada con endurecimientos adicionales.
- Task 4 — UI WPF mínima: **NO INICIADA**.
- Task 5 — hardening/cierre F12.1: **NO INICIADA**.

No se autoriza merge a `main` y no se inicia F12.2 desde este checkpoint.

## 2. Evidencia Task 1 — capability gate

Se verificó contra el PDFium pinneado que `FPDFPageObj_CreateNewRect` existe y ejecuta realmente.

- RED: `f6e188cc…`
  - fallo limpio por miembro interop inexistente.
- GREEN: `46349ca4…`
  - Release: 0 warnings / 0 errors.
  - 1086/1086 tests PASS.
  - sin `EntryPointNotFoundException`.

No se añadió otro motor PDF.

## 3. Evidencia Task 2 — RedactionPolicy

Política pura y fail-closed implementada sin I/O, WPF ni dependencias nuevas.

Cubre al menos:

- texto visible horizontal simple;
- varios objetos compatibles;
- rechazo OCR/invisible;
- rechazo rotación/skew;
- rechazo bounds inválidos;
- rechazo solapamiento inseguro;
- rechazo preflight bloqueado;
- rechazo selección vacía/duplicada/inconsistente.

HEAD funcional de cierre conocido de Task 2: `2f6c2af0…`.

Evidencia: Release 0 warnings/errors y 1097/1097 tests PASS.

## 4. Evidencia Task 3 — redacción real

La implementación usa:

`RedactionWorkspace → PdfEditWriter existente a staging → PdfRedactionMaterializer → PdfRedactionOutputValidator → publicación atómica`

Propiedades demostradas:

1. el objeto textual subyacente se elimina realmente;
2. el texto redactado deja de ser extraíble en su región original;
3. el contenido vecino no seleccionado permanece;
4. se crea un PATH negro opaco con fill `WINDING` y sin stroke;
5. la página reabre y renderiza;
6. el PDF fuente queda byte-a-byte intacto;
7. snapshot cambiado falla cerrado;
8. PDF firmado continúa bloqueado;
9. cancelación preexistente no publica destino parcial;
10. la política se vuelve a ejecutar en la frontera transaccional para impedir bypass mediante planes construidos manualmente.

Hitos principales:

- `153d90eefa985d49216f85367c030fb18a1b234a`
  - materialización real funcional;
  - CI `38269371320`;
  - 1100/1100 PASS;
  - Release 0 warnings / 0 errors.
- `4c377c3fbc3630e85cda1bf244514bcbb9719718`
  - inspección nativa PATH negro/bounds/fill/draw mode;
  - CI `38269775117`;
  - 1100/1100 PASS;
  - Release 0 warnings / 0 errors.
- RED de bypass rotado: `e6c6d50a4dd03bbab73154ecd48cd387ee9684d4`
  - 1 fallo esperado / 1101 PASS;
  - demostraba que un plan manual podía saltarse la política.
- GREEN del bypass: `91c971a5d299948050aff30c8af35b69842dabf0`
  - CI `38270644214`;
  - 1102/1102 PASS;
  - Release 0 warnings / 0 errors.

## 5. Microbloque de esta sesión — multi-target misma página

Hueco de cobertura encontrado durante auditoría:

El alcance de F12.1 permite uno o varios objetos completos, pero la integración transaccional estaba demostrada solo con un objetivo aunque la política ya permitía varios.

Se añadió únicamente:

- `tests/SGPdf.App.Tests/PdfEditWriterRedactionMultipleTargetsTests.cs`

Commit:

- `580e49d76a911d70dae8cee5cfa0af3482964fcc`
- `test(f12): verify same-page multi-target redaction`

La prueba demuestra con dos objetos seguros en la misma página:

- ambos se eliminan;
- el destino se publica correctamente;
- el fuente permanece byte-a-byte intacto;
- el PDF reabre;
- no quedan objetos de texto objetivo;
- la página renderiza.

Evidencia exacta:

- workflow: `38273323442`
- job Windows: `114892670497`
- Build Release: **0 Warning(s), 0 Error(s)**
- Test: **Failed 0, Passed 1103, Skipped 0, Total 1103**

No fue necesario cambiar producción: el comportamiento ya era correcto; se cerró un hueco de evidencia/regresión.

## 6. Auditoría de alcance

Comparación `7a890b7c…` → `580e49d7…`:

- rama ahead de F11;
- cambios confinados a spec/plan F12.1, `Features/Edit/Redaction`, interop/materialización/validación PDF y pruebas F12;
- sin cambios WPF;
- sin dependencia runtime nueva;
- sin segundo motor PDF;
- sin cambios a `main`;
- sin PR F12 creado todavía.

`main` fue revalidado en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.

## 7. Huecos auditados aún abiertos

Estos puntos NO invalidan el GREEN automatizado actual de Tasks 1–3, pero deben reconciliarse antes del cierre de F12.1:

### A. Validación de marca visual

`PdfRedactionOutputValidator` valida reapertura, conteo/texto por región y render, pero no inspecciona internamente el PATH negro. La presencia/forma/color del PATH sí está demostrada por la prueba de integración nativa.

Antes de cerrar F12.1 decidir si conviene absorber esa comprobación dentro del validador sin duplicar lógica ni inflar arquitectura.

### B. Orden exacto de mutación vs spec

La spec describe como orden obligatorio por página:

`resolver todos → eliminar textos → crear rectángulos → insertar rectángulos → GenerateContent una vez`.

La implementación actual:

`resolver todos → preparar rectángulos → por objetivo remove+insert → GenerateContent una vez`.

El comportamiento multi-target quedó GREEN y no se observó corrupción ni pérdida, pero existe una diferencia documental/estructural que debe reconciliarse antes del cierre: o se ajusta la implementación con evidencia TDD o se corrige la spec si el orden actual se demuestra equivalente y más seguro/simple.

### C. Trabajo aún no iniciado

- Task 4 UI mínima.
- Task 5 cierre/hardening documental.
- QA manual con PDFs privados/reales y lectores externos: no ejecutada.
- PR draft F12: no creado.
- F12.2: no iniciada.

## 8. Próximo paso autorizado al reanudar

Retomar desde este checkpoint y, antes de abrir alcance nuevo:

1. verificar HEAD de la rama y este checkpoint;
2. no tocar `main`;
3. decidir el microbloque TDD más pequeño para uno de los dos huecos A/B o, si se posponen justificadamente a Task 5, abrir Task 4 RED;
4. UI Task 4 debe reutilizar `MainWindow.Edit.Text.cs`, selección `TextObjectKey` y panel existente;
5. no crear sidebar profesional nuevo;
6. no abrir regex, batch, redact-all, imágenes/escaneos ni F12.2.

## 9. STOP

Checkpoint creado por solicitud del usuario después de un único microbloque auditado.

**PARAR AQUÍ.**
