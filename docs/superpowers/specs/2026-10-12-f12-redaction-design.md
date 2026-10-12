# F12.1 — Redacción real segura

Fecha: 2026-10-12  
Base funcional: `feat/f11-text-v2` @ `7a890b7c8ece75869730c95c275a1ee8202efe93`  
Rama: `feat/f12-professional`

## 1. Objetivo

Añadir una primera capacidad profesional de redacción que elimine contenido textual real del PDF, en lugar de limitarse a cubrirlo visualmente.

Flujo objetivo:

`selección de texto real → política fail-closed → plan de redacción → resolver todos los objetivos → preparar recursos sin mutar → eliminar texto e insertar rectángulos de forma determinista → regenerar contenido una vez → Guardar como → reabrir → validar`

La primera versión debe reutilizar PDFium y la transacción existente de EDITAR. No se introduce un segundo motor PDF ni un segundo writer general.

## 2. Por qué F12 empieza por redacción

El repositorio ya dispone de las primitivas necesarias para mutar objetos de página y guardar de forma transaccional: resolución de objetos, `FPDFPage_RemoveObject`, inserción por índice, `FPDFPage_GenerateContent`, fingerprint, preflight, archivo temporal, validación y reemplazo/movimiento atómico.

La firma criptográfica queda fuera de esta primera slice porque la integración actual solo inspecciona firmas y bloquea la edición de documentos firmados. Formularios avanzados también quedan fuera porque requieren un ciclo de vida y callbacks adicionales que todavía no forman parte de SG PDF.

## 3. Definición de redacción real

Una operación solo se considera redacción si:

1. el objeto textual objetivo se elimina del contenido del PDF;
2. el texto redaccionado deja de ser extraíble/buscable al reabrir el PDF;
3. se inserta una marca visual opaca sobre el área eliminada;
4. el contenido vecino no seleccionado permanece;
5. la salida vuelve a abrir y renderizar correctamente.

Un rectángulo negro superpuesto sin eliminación del contenido subyacente NO es redacción válida.

## 4. Alcance inicial

Se admite únicamente:

- texto PDF real y seleccionable;
- uno o varios objetos de texto completos;
- geometría horizontal simple;
- límites finitos y no degenerados;
- renderizado visible normal compatible con edición segura;
- rectángulo negro opaco por objeto o región segura equivalente;
- página concreta, bajo demanda;
- `Guardar como...` con el original intacto;
- reapertura y validación automatizada de la salida.

## 5. Fail-closed

La operación se rechaza antes de mutar cuando exista cualquiera de estas condiciones:

- redacción parcial de caracteres dentro de un único objeto;
- texto rotado o transformación no compatible;
- geometría ambigua o degenerada;
- solapamiento que impida demostrar qué contenido se elimina;
- texto invisible de OCR sobre una imagen;
- contenido textual incrustado en una imagen;
- objeto original no resoluble o distinto del snapshot;
- PDF firmado criptográficamente;
- PDF con estructura etiquetada (`TaggedStructure`) en F12.1, porque una representación semántica como `/ActualText` puede conservar contenido sensible aunque desaparezca el texto visible;
- fuente abierta con contraseña o preflight bloqueante;
- cualquier caso donde no pueda demostrarse que la mutación es segura.

No se intenta adivinar.

## 6. Fuera de alcance de F12.1

- redacción automática por expresiones regulares;
- “redactar todas las coincidencias”;
- selección arbitraria que mezcle texto, imágenes y vectores;
- eliminación de píxeles dentro de fotografías o escaneos;
- batch de documentos;
- firma criptográfica/PAdES;
- edición de formularios;
- recuperación de texto redaccionado;
- arquitectura de plugins o proveedor abstracto de redacción.

## 7. Arquitectura mínima

### 7.1 Capability gate

Antes de modelar la feature se prueba contra el DLL PDFium pinneado que `FPDFPageObj_CreateNewRect` está realmente exportado y puede crear/destruir un rectángulo nativo.

Si el binario no soporta la API, F12.1 queda en NO-GO técnico y no se añade automáticamente otro motor.

### 7.2 Política y plan puros

Se incorporan modelos pequeños de redacción, sin WPF ni I/O:

- `RedactionPolicy`: decide si una selección puede redactarse de forma segura;
- `RedactionPlan`: snapshot inmutable de los objetos y áreas que se deben eliminar/cubrir.

La política reutiliza la información de `PdfTextObjectInfo`, el preflight existente y la geometría ya disponible. No duplica el analizador de F11.

### 7.3 Materialización

La materialización extiende la transacción existente de EDITAR en vez de crear un writer general paralelo.

Orden obligatorio por página:

1. resolver y validar todos los handles objetivo antes de la primera mutación;
2. preparar los rectángulos negros opacos sin mutar la página;
3. eliminar cada objeto aprobado e insertar su rectángulo de forma determinista;
4. regenerar contenido una sola vez por página;
5. guardar en temporal;
6. validar la salida;
7. publicar atómicamente el destino.

Este orden prioriza la propiedad transaccional relevante: no iniciar mutaciones hasta haber resuelto todos los objetivos y preparado los recursos que pueden fallar de forma determinista.

Ante fallo, el original permanece intacto y el temporal se elimina.

### 7.4 Validación

La validación debe comprobar al menos:

- reapertura por PDFium;
- render válido de la página;
- texto redaccionado ausente de extracción/búsqueda;
- objetos no seleccionados relevantes conservados contra el staging ya editado;
- una marca visual negra opaca válida por objetivo;
- número de páginas estable;
- archivo fuente sin cambios.

Cuando sea determinista y útil, se puede reforzar con las herramientas locales ya presentes, sin convertir qpdf en un nuevo motor de edición.

### 7.5 UI mínima

La UI se añade únicamente después de demostrar política y writer.

Se reutiliza la selección de texto existente en EDITAR. La UI muestra:

- estado `Seguro para redactar` o rechazo conservador en español;
- comando mínimo `Redactar`;
- estado de borrador hasta `Guardar como...`.

No se crea un panel profesional independiente en esta primera slice.

## 8. Seguridad de firmas y preservación

Los PDF con firma criptográfica continúan bloqueados para cualquier materialización que invalide la firma. F12.1 no relaja el `PdfEditPreflightInspector` ni el chequeo de `PdfEditWriter`.

La presencia de formularios, marcadores, destinos, enlaces, etiquetas de página, rotaciones, adjuntos o metadatos mantiene las reglas de preservación ya probadas por EDITAR; F12.1 no redefine esas políticas salvo cuando la seguridad de redacción exige un bloqueo más conservador.

En particular, `TaggedStructure` puede seguir siendo preservable para edición normal, pero F12.1 la bloquea específicamente porque preservar la estructura no demuestra sanear representaciones semánticas alternativas como `/ActualText`.

## 9. Dependencias

No se añade ninguna dependencia runtime para F12.1.

- PDFium continúa como motor PDF propietario de la mutación.
- PDFsharp no se amplía preventivamente.
- qpdf, si se usa para verificación secundaria, no edita el documento.
- no hay red, APIs, SaaS ni claves.

## 10. Criterios de aceptación

F12.1 puede cerrarse automatizadamente solo si:

1. el capability gate del PDFium pinneado es GREEN;
2. la política fail-closed está cubierta por pruebas RED→GREEN;
3. la materialización elimina realmente el texto subyacente;
4. la salida se reabre, extrae y renderiza correctamente;
5. se demuestra preservación del contenido vecino y del archivo fuente;
6. cancelación/fallo no publican una salida parcial;
7. documentos firmados siguen bloqueados;
8. Release build y suite completa pasan en Windows CI sobre el HEAD exacto;
9. la UI mínima no introduce un segundo flujo de edición;
10. no se mezcla F12.2 ni se hace merge a `main`.
