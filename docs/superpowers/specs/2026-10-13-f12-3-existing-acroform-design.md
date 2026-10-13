# F12.3 — Formularios PDF existentes / AcroForm MVP

Fecha: 2026-10-13  
Base exacta: cierre F12.2 `5fc7064a950b3242ae93eced676f745c9d32ad8c`  
Rama de primer gate: `feat/f12-3-task1-acroform-gate`

## 1. Objetivo

Permitir rellenar campos de formularios AcroForm ya existentes en un PDF local, conservando el original y publicando únicamente una salida validada.

Flujo objetivo:

`PDF AcroForm existente → inspeccionar campos soportados → editar valores → temporal → reabrir/validar valor + apariencia + preservación → fingerprint fuente → Guardar como...`

F12.3 no es un diseñador de formularios.

## 2. Principios obligatorios

1. KISS/YAGNI: demostrar capacidad antes de crear servicio o UI.
2. Ley del Ruido: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. Reusar PDFsharp 6.2.4 ya instalado como candidato de escritura; no añadir otro motor.
4. PDFium sigue siendo lector/render principal e inspector independiente.
5. `Guardar como...`: el original no se sobrescribe.
6. Fail-closed: si valor, apariencia o preservación no pueden demostrarse, no se publica.
7. Reusar `PdfEditPreflightInspector`, `PdfEditSourceFingerprint` y el patrón transaccional existente.
8. No merge a `main` ni a la rama padre sin aprobación explícita.

## 3. Alcance MVP candidato

El capability gate empieza únicamente con un campo de texto AcroForm clásico existente.

Solo después de demostrarlo se caracterizan, uno por uno:

- checkbox;
- radio button;
- combo box;
- list box.

Un tipo que no pase el gate queda fuera del MVP; no se parchea preventivamente.

## 4. Fuera de alcance

- crear formularios desde cero;
- diseñador drag-and-drop;
- XFA;
- JavaScript, cálculos o acciones automáticas;
- campos de firma;
- flatten;
- estilización avanzada;
- servicios web, cuentas o backend;
- segundo motor PDF;
- compatibilidad prometida sin evidencia real.

## 5. Decisión técnica condicionada

### 5.1 Escritura candidata

Usar PDFsharp 6.2.4 ya presente para abrir en modo modificación y cambiar el valor de campos AcroForm existentes.

Esta decisión **no queda aprobada para producción** hasta que Task 1 pruebe que el valor guardado y su apariencia renderizada sobreviven a reapertura sin degradación relevante.

### 5.2 Inspección y validación

PDFium ya expone `FPDF_GetFormType` mediante `PdfDocumentSession.GetOrganizeFormType`. Se reutiliza para confirmar que la salida sigue conteniendo formulario y para renderizar la página.

Si se necesitan primitivas PDFium adicionales después del gate, se añaden únicamente las mínimas y en un partial dedicado; no se introduce FormFill completo salvo un diseño posterior explícito.

### 5.3 Seguridad y preservación

Antes de producción:

- PDF abierto con contraseña: bloqueado;
- PDF con firma criptográfica: bloqueado para modificación;
- fuente cambia durante la operación: no publicar;
- salida no reabre/renderiza: no publicar;
- formulario o campo esperado desaparece: no publicar;
- valor persistido pero apariencia incorrecta/no demostrable: NO-GO del writer candidato.

## 6. Task 1 — capability gate obligatorio

El primer gate es test-only y no crea servicio, UI ni API de producción.

Debe demostrar para un fixture mínimo con campo de texto:

1. snapshot exacto del fuente antes de escribir;
2. PDFsharp modifica solo una copia/destino;
3. fuente conserva bytes exactos;
4. salida abre con `PdfDocumentSession`;
5. page count permanece estable;
6. `GetOrganizeFormType()` sigue indicando formulario;
7. valor de campo cambiado persiste al reabrir;
8. render de salida funciona;
9. la apariencia del campo refleja visualmente el nuevo valor, no solo `/V` semántico;
10. no entra dependencia nueva.

Si el punto 9 no puede probarse limpiamente con PDFsharp 6.2.4, Task 1 cierra **NO-GO** y F12.3 se detiene antes de añadir arquitectura.

## 7. Arquitectura posterior solo si Task 1 = GO

Secuencia máxima:

1. inspector/modelo mínimo de campos soportados;
2. writer/servicio transaccional reutilizando preflight + fingerprint;
3. validador independiente de valor/apariencia/preservación;
4. UI WPF mínima para editar campos existentes;
5. exact-head CI y cierre.

No se construye ninguna de estas capas durante el capability gate.

## 8. Criterios de aceptación de F12.3 MVP

F12.3 solo podrá cerrarse si:

- cada tipo declarado como soportado tiene evidencia RED→GREEN;
- original queda intacto;
- salida conserva formulario y estructuras representativas exigidas;
- valores sobreviven reapertura;
- apariencia renderizada coincide con los valores editados;
- errores/cancelación no publican parcial ni destruyen destino previo;
- exact-head Windows CI pasa con 0 warnings, 0 errors y suite completa;
- tipos no probados siguen explícitamente fuera de alcance.

## 9. Stop conditions

Detener y rediseñar antes de continuar cuando:

- PDFsharp persista `/V` pero no genere/actualice apariencia de forma fiable;
- sea necesario reescribir manualmente sintaxis AcroForm significativa;
- aparezca necesidad de un segundo motor;
- XFA/JavaScript se vuelva requisito para el caso mínimo;
- preservación no pueda verificarse de forma automatizada.

No resolver estas condiciones con parches acumulativos.