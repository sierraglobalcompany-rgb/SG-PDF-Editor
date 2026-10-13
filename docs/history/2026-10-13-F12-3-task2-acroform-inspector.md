# F12.3 Task 2 — AcroForm inspector

Fecha: 2026-10-13

Estado: **CLOSED / GREEN**

Rama: `feat/f12-3-task2-inspector`

Base: `5f7703da6ab717abe12ca3981be6fb8bc5c651ea`

HEAD funcional validado: `6799b23512e874b1ae373f370c769e0f589fdd67`

## Alcance probado

- campo texto raíz;
- campo texto jerárquico mediante `/Kids`;
- nombre compuesto `Parent.Child`;
- herencia de `/FT`;
- solo terminales `/Tx`;
- `/Btn` se ignora;
- no se devuelve el nodo intermedio.

Implementación limitada a `PdfAcroFormInspector`, `PdfAcroFormTextFieldInfo` y pruebas focales. No se añadieron FormFill, P/Invoke, paquetes, segundo motor PDF ni UI.

## RED

SHA `367cd78b7e813b68392af62610afe072a26a8900`

Workflow `38359823934`, job `115171119288`.

Build PASS, 0 warnings, 0 errors, 1152 pruebas previas PASS y 1 fallo intencional nuevo que demostró la falta de recorrido jerárquico.

## GREEN

SHA `6799b23512e874b1ae373f370c769e0f589fdd67`

Workflow `38360052031`, job `115171840150`.

Repository hygiene PASS, locked restore PASS, Release build PASS, 0 warnings, 0 errors, `1153/1153 PASS`, 0 skipped.

Checkbox, radio, combo y list siguen NO PROBADOS.

Task 3 queda fuera de este cierre y no se inicia aquí.
