# F8 — COMENTAR V1 — Diseño aprobado

**Fecha:** 2026-10-10  
**Proyecto:** SG PDF Editor  
**Rama:** `feat/f8-comments-v1`  
**Base:** cierre automatizado F7 `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`  
**Estado:** diseño consolidado; implementación aún no iniciada  
**Principios:** KISS, offline, PDFium como autoridad, sin segundo motor PDF, sin merge a `main` sin aprobación explícita.

## 1. Objetivo

Agregar un modo independiente **COMENTAR** para crear, editar y eliminar anotaciones PDF reales compatibles con lectores comerciales, sin rasterizar comentarios dentro del contenido de página ni mezclar su modelo con EDITAR.

F8 debe conservar el enfoque local/offline del proyecto, proteger el original mediante Save As transaccional y medir de nuevo la preservación estructural con el writer real de comentarios.

## 2. Decisión arquitectónica principal

`COMENTAR` será un modo independiente de `EDITAR`.

```text
LEER
FIRMAR
ORGANIZAR
EDITAR      -> contenido real de página: imágenes + texto
COMENTAR    -> anotaciones PDF reales
```

Motivos:

- las anotaciones PDF son estructuras distintas de los page objects de F6/F7;
- evita contaminar `ImageEditWorkspace` y `TextEditWorkspace`;
- permite un writer y validator especializados;
- reduce riesgo de regresión sobre el writer combinado F7;
- coincide con el patrón de editores comerciales que separan edición de contenido y comentarios/revisión.

No se creará un writer genérico universal ni un segundo motor PDF.

## 3. Alcance funcional V1

Herramientas incluidas:

1. **Resaltar** (`HIGHLIGHT`).
2. **Subrayar** (`UNDERLINE`).
3. **Tachado** (`STRIKEOUT`).
4. **Nota adhesiva** (`TEXT`).
5. **Lápiz** (`INK`).
6. **Rectángulo** (`SQUARE`).
7. **Óvalo** (`CIRCLE`).

Herramienta neutra adicional:

- **Seleccionar** para seleccionar, mover, redimensionar cuando aplique, modificar propiedades y eliminar anotaciones existentes soportadas.

Fuera de F8 V1:

- línea/flecha;
- polígono/polilínea;
- texto libre/caja de texto;
- sellos;
- archivos adjuntos como nuevas anotaciones;
- audio;
- respuestas/hilos;
- autores/perfiles;
- estado resuelto;
- menciones;
- nube/sincronización;
- flatten de comentarios;
- reconocimiento de presión complejo;
- selección continua entre páginas.

## 4. Modelo de estado

Nuevo namespace previsto:

`src/SGPdf.App/Features/Comments/`

Unidades conceptuales:

- `CommentWorkspace`: estado editable managed de F8.
- `CommentState`: snapshot estable por anotación.
- `CommentTool`: herramienta activa.
- `CommentHitTester`: selección determinista.
- modelos managed por geometría/propiedades.

Reglas:

- ningún `IntPtr` o handle PDFium persiste dentro del workspace;
- todos los handles nativos son request-scoped;
- cada anotación existente se representa mediante identidad managed estable suficiente para resolverla de nuevo sobre una copia fresca del PDF fuente;
- nuevos comentarios, modificaciones y eliminaciones viven en memoria hasta Save As;
- `CommentWorkspace.IsDirty` se activa con cualquier cambio efectivo;
- `MarkSaved()` solo ocurre después de publicación validada exitosa.

Para anotaciones preexistentes se conservará información suficiente como:

- `PageIndex`;
- índice/identidad original resoluble;
- subtipo;
- rect/quadpoints originales;
- propiedades soportadas.

No se asumirá que un índice nativo permanece válido entre aperturas; la estrategia concreta de resolución debe quedar probada por tests antes de usarse como autoridad.

## 5. Descubrimiento de anotaciones

Se añadirá una superficie tipo `PdfDocumentSession.Comments.cs` para enumerar anotaciones soportadas de forma managed.

Requisitos:

- descubrimiento por página, preferiblemente activa/visible según el flujo UI;
- lectura conservadora de subtipo, rect, quadpoints, color, contenido, ink strokes y propiedades disponibles;
- anotaciones no soportadas pueden representarse como read-only o ignorarse para edición, pero nunca deben ser eliminadas/recreadas incidentalmente;
- la enumeración no debe mantener handles abiertos después de devolver los snapshots.

Antes de implementar cada familia se hará un **capability gate** contra la versión exacta de PDFium pinneada por el proyecto. No se asumirá que una API presente hoy en `main` de PDFium existe o se comporta igual en el runtime instalado.

## 6. Text markup

### 6.1 Resaltar

Flujo:

1. activar Resaltar;
2. seleccionar texto dentro de una página;
3. reutilizar la infraestructura existente del lector para obtener rango y rectángulos PDF;
4. convertir geometría a quadpoints válidos para `HIGHLIGHT`;
5. registrar el comentario en `CommentWorkspace`;
6. mantener la herramienta activa para marcas sucesivas.

Defaults:

- amarillo;
- opacidad moderada.

Editable después de crear:

- color;
- opacidad cuando el gate runtime demuestre round-trip estable;
- eliminar.

No se editarán manualmente sus quadpoints con handles en V1.

### 6.2 Subrayar

Mismo flujo que Resaltar con subtipo `UNDERLINE`.

Editable:

- color;
- opacidad si queda demostrada;
- eliminar.

### 6.3 Tachado

Mismo flujo con subtipo `STRIKEOUT`.

Editable:

- color;
- opacidad si queda demostrada;
- eliminar.

### 6.4 Restricciones comunes

- selección multi-línea dentro de una página: sí;
- selección continua entre páginas: no;
- al soltar la selección se crea el comentario; no hay botón Aplicar;
- `Esc` vuelve a Seleccionar;
- crear una marca debe ser una unidad de undo.

## 7. Nota adhesiva

Subtipo PDF: `TEXT`.

Flujo:

1. activar Nota;
2. clic en una posición de página;
3. crear preview/icono local;
4. abrir editor pequeño junto a la nota;
5. `Ctrl+Enter` o pérdida de foco confirma;
6. `Esc` cancela una nota nueva todavía vacía.

Nota existente:

- clic simple: selecciona;
- doble clic: abre contenido;
- contenido Unicode;
- posición movible;
- color básico seleccionable;
- eliminar.

No se usarán ventanas modales por nota.

## 8. Lápiz / Ink

Subtipo PDF: `INK`.

Entrada:

- mouse: requerido;
- stylus: soportado cuando WPF lo entregue;
- touch: soportado cuando el pipeline WPF lo entregue correctamente;
- presión variable: fuera de V1.

Flujo:

- pointer down inicia stroke;
- movimiento actualiza preview WPF;
- pointer up cierra el gesto y registra una unidad de comentario/undo;
- los puntos se mantienen managed en coordenadas PDF para el writer.

Propiedades:

- color;
- grosor básico;
- opacidad solo si round-trip queda demostrado estable.

No se reutilizará el modelo de firma; solo patrones técnicos de input/historial cuando sirvan. Firma visual e Ink PDF siguen siendo subsistemas separados.

## 9. Rectángulo y óvalo

Subtipos:

- Rectángulo -> `SQUARE`;
- Óvalo -> `CIRCLE`.

Gesto:

1. pointer down;
2. drag;
3. preview WPF;
4. pointer up;
5. registrar anotación;
6. dejarla seleccionada.

Edición:

- mover;
- resize con handles;
- color de borde;
- grosor;
- eliminar;
- relleno/opacidad solo si el gate PDFium demuestra comportamiento estable y round-trip verificable.

No se añadirá relleno preventivamente si la API exacta no demuestra consistencia.

## 10. Undo / redo

`CommentWorkspace` tendrá historial propio e independiente.

Operaciones mínimas:

- crear;
- modificar propiedades;
- mover;
- redimensionar;
- editar texto de nota;
- eliminar.

Atajos:

- `Ctrl+Z` -> undo de comentario cuando COMENTAR posee el foco de interacción y ningún TextBox debe consumir su propio undo;
- `Ctrl+Y` -> redo;
- `Delete` -> eliminar seleccionada;
- `Esc` -> cancelar gesto/deseleccionar/volver a Seleccionar según contexto.

No se fusionará este historial con imágenes, texto ni firma.

## 11. UI/UX

La UI actual de tres columnas se conserva:

- izquierda: páginas;
- centro: documento;
- derecha: propiedades.

Al entrar en COMENTAR aparece una barra contextual:

```text
Seleccionar | Resaltar | Subrayar | Tachado | Nota | Lápiz | Rectángulo | Óvalo
Deshacer | Rehacer | Guardar como
```

El menú existente `Anotar` puede exponer comandos equivalentes, pero no será el lugar principal de propiedades.

### 11.1 Seleccionar

Herramienta predeterminada/neutra.

Permite:

- seleccionar anotación soportada;
- mover Nota/Rectángulo/Óvalo;
- resize de Rectángulo/Óvalo;
- modificar propiedades;
- eliminar.

Highlights/underlines/strikeouts no mostrarán handles de resize.

### 11.2 Panel derecho

Sin selección:

- herramienta activa;
- defaults para nuevos comentarios compatibles con esa herramienta.

Con selección:

- tipo;
- color;
- opacidad cuando aplique;
- grosor para Ink/Square/Circle;
- contenido para Nota;
- eliminar.

Paleta V1 recomendada:

- amarillo;
- verde;
- azul;
- rojo;
- rosa;
- naranja;
- negro.

No se añadirá dependencia nueva solo para selector de color. Un selector personalizado puede incorporarse únicamente si se resuelve con infraestructura WPF existente sin ensanchar el alcance.

Las preferencias de estilo pueden mantenerse solo durante la sesión en V1.

## 12. Ownership de interacción

Solo un subsistema posee la interacción del visor a la vez.

Mutuamente excluyentes:

- selección LEER;
- colocación FIRMAR;
- ORGANIZAR;
- selección/edición de objetos EDITAR;
- herramientas COMENTAR.

Cambiar de modo con dirty state debe pasar por el guard correspondiente.

Ejemplo:

- `COMENTAR dirty -> LEER/FIRMAR/ORGANIZAR/EDITAR`: pedir descartar/cancelar según patrón existente;
- `EDITAR dirty -> COMENTAR`: usar el guard de EDITAR.

No habrá autosave.

## 13. Writer

Nuevo writer especializado:

`PdfCommentWriter`

Responsabilidad:

- materializar únicamente el plan de anotaciones;
- no editar page objects de F6/F7;
- trabajar sobre una copia fresca del PDF fuente;
- guardar a temporal;
- cerrar handles;
- reabrir temporal;
- validar;
- publicar atómicamente al destino;
- limpiar temporales en éxito, cancelación y fallo.

Ruta:

```text
CommentWorkspace
  -> plan final managed
  -> abrir fuente fresca
  -> aplicar modificaciones/eliminaciones/creaciones objetivo
  -> temporal
  -> cerrar
  -> reabrir
  -> PdfCommentOutputValidator
  -> publicación atómica
```

`PdfEditWriter` continúa especializado en contenido de página. No se crea un `MegaPdfWriter`.

## 14. Orden de materialización

Para anotaciones existentes:

1. resolver el snapshot original sobre la copia fresca;
2. aplicar modificaciones soportadas;
3. ejecutar eliminaciones de manera que los cambios de índices no corrompan el resto;
4. crear nuevas anotaciones después;
5. cerrar todos los handles antes de guardar/publicar.

La estrategia exacta de identidad/resolución debe quedar demostrada por tests de round-trip y por escenarios con varias anotaciones del mismo subtipo.

Anotaciones no soportadas no se recrean y no se tocan.

## 15. Fingerprint, preflight y protección del original

Antes de publicar se reutiliza el patrón de protección ya probado:

- comprobar que el documento fuente no cambió externamente;
- PDF abierto con contraseña -> Block;
- firma criptográfica o imposibilidad de verificar preservación segura -> Block;
- Save As como ruta normal;
- no sobrescribir silenciosamente el original;
- stale/publication failure no marca baseline limpio.

La reutilización será por componentes/patrones concretos, no mediante una abstracción genérica preventiva.

## 16. Validator

Nuevo:

`PdfCommentOutputValidator`

Debe reabrir el archivo temporal antes de publicación y comprobar el resultado esperado.

### 16.1 Text markup

- subtipo;
- página;
- número/geometría de quadpoints con tolerancia controlada;
- color;
- opacidad cuando forme parte del contrato demostrado.

### 16.2 Nota

- subtipo `TEXT`;
- página/posición;
- contenido Unicode exacto;
- color soportado.

### 16.3 Ink

- subtipo `INK`;
- cantidad de strokes;
- puntos/coordenadas con tolerancia;
- color;
- grosor cuando pueda medirse de forma confiable.

### 16.4 Square/Circle

- subtipo;
- rect;
- color;
- borde/grosor;
- relleno/opacidad solo si se incluyeron finalmente en el contrato.

### 16.5 Render smoke

Las páginas tocadas deben volver a renderizar correctamente después del save -> reopen.

Si falla cualquier validación:

- no publicar;
- no marcar workspace como limpio;
- no reemplazar un destino válido previo;
- eliminar temporal;
- conservar dirty state y permitir reintento.

## 17. Preservación F8

F8 no hereda automáticamente la conclusión de F7. Debe ejecutar su propia matriz con el writer real `PdfCommentWriter`.

Estructuras mínimas a volver a medir:

- AcroForm + valor;
- bookmarks/outlines;
- named destinations;
- internal links;
- tagged structure / `StructTreeRoot`;
- page labels;
- embedded attachments;
- metadata representativa;
- page rotation.

Además, F8 añade una obligación específica:

### Anotaciones preexistentes no objetivo

Fixture representativa con, como mínimo:

- highlight existente;
- nota existente;
- una anotación no soportada cuando sea viable generar la fixture;
- formulario;
- bookmark;
- metadata.

Acción: crear o modificar una anotación objetivo distinta.

Resultado esperado: solo cambia la anotación objetivo y todas las demás estructuras permanecen.

## 18. Ciclo completo de comentarios existentes

Antes de cerrar F8 debe quedar probado al menos un ciclo:

```text
crear
-> guardar
-> reabrir
-> editar
-> guardar
-> reabrir
-> eliminar
-> guardar
-> reabrir
```

Este ciclo debe cubrir más que creación nueva y demostrar que el resolvedor de anotaciones existentes no depende de handles persistentes.

## 19. Capability gate PDFium

Primer gate técnico de implementación:

- verificar exports/entry points realmente disponibles en el PDFium exacto pinneado;
- verificar comportamiento mínimo de create/get/set para los subtipos previstos;
- comprobar quadpoints para text markup;
- comprobar Ink list/strokes;
- comprobar Square/Circle rect/border/color;
- comprobar Unicode Contents para TEXT;
- clasificar APIs como utilizables, utilizables con restricciones o no aptas para V1.

Si una propiedad secundaria como fill/opacity no demuestra round-trip estable, se recorta esa propiedad sin bloquear el subtipo completo.

Si un subtipo completo no puede materializarse/validarse de forma segura con el runtime exacto, se debe detener esa herramienta y registrar la evidencia antes de considerar una dependencia o enfoque alternativo.

No se añade segundo motor PDF como atajo.

## 20. Pruebas y TDD

Cada bloque de implementación seguirá:

1. RED específico y observable;
2. implementación mínima GREEN;
3. suite completa;
4. Windows CI exact-head;
5. scope audit;
6. checkpoint durable.

Cobertura mínima por categorías:

### Dominio/workspace

- create/update/delete;
- undo/redo;
- dirty/baseline;
- no-op no ensucia;
- historial se invalida correctamente tras nueva rama de edición.

### Geometría

- device <-> PDF mapping;
- páginas rotadas;
- zoom;
- hit testing determinista;
- resize/move;
- quadpoint normalization.

### Writer

- creación por subtipo;
- modificación de existentes;
- eliminación;
- orden seguro con múltiples anotaciones;
- cleanup en excepción;
- stale source;
- source/output collision cuando aplique.

### Validator

- éxito real;
- detección de subtipo incorrecto;
- geometría incorrecta;
- contenido Unicode incorrecto;
- ausencia de anotación esperada;
- render failure.

### UI

- entrada/salida de modo;
- toolbar contextual;
- herramienta activa;
- selección única autoritativa;
- guards dirty;
- shortcuts;
- coexistencia sin interferencia con modos existentes.

### Regresión

- LEER;
- FIRMAR;
- ORGANIZAR;
- EDITAR imágenes;
- EDITAR texto;
- Save As existente;
- tests completos previos.

## 21. Criterios de aceptación F8 V1

F8 solo puede declararse **AUTOMATED CLOSURE PASS** si:

1. las siete herramientas incluidas pasan el capability gate aplicable o cualquier recorte queda explícitamente documentado antes de cierre;
2. comentarios se guardan como anotaciones PDF reales y reaparecen tras reopen;
3. create/edit/delete round-trip está demostrado;
4. validator bloquea publicación inválida;
5. temp cleanup y dirty-baseline semantics están demostrados;
6. anotaciones no objetivo permanecen;
7. matriz de preservación F8 queda medida con el writer F8;
8. no se añadió segundo motor PDF;
9. no se añadió dependencia sin justificación aprobada;
10. suite completa Windows CI exact-head queda verde;
11. `main` permanece intacto salvo autorización explícita;
12. cualquier QA manual no ejecutado se etiqueta **NOT RUN**, nunca como PASS implícito.

## 22. QA manual posterior

Automated PASS no equivale a QA físico/manual.

La checklist manual de Windows deberá incluir cuando se ejecute:

- mouse;
- stylus si hay hardware disponible;
- touch si hay hardware disponible;
- zoom y páginas rotadas;
- apertura del PDF resultante en al menos un lector externo comercial disponible;
- aspecto visual de text markup, Ink, notas y formas;
- Save As y reapertura real.

Si no se ejecuta, el estado será **NOT RUN**.

## 23. Límites de arquitectura

No hacer durante F8:

- refactor general de MainWindow no requerido por F8;
- introducir MVVM/framework nuevo;
- base de datos;
- nube;
- colaboración;
- generic annotation engine para todos los subtipos PDF;
- abstracción genérica de todos los writers;
- OCR;
- Text V2/reflow;
- formularios avanzados;
- firmas criptográficas;
- redacción real;
- F9+.

## 24. Secuencia técnica recomendada

El plan de implementación detallado deberá mantener lotes pequeños, aproximadamente:

1. capability gate PDFium;
2. dominio `CommentWorkspace` + historial;
3. descubrimiento managed de anotaciones existentes;
4. text markup writer/validator;
5. Nota writer/validator;
6. Ink writer/validator;
7. Square/Circle writer/validator;
8. writer transaccional completo + cleanup/stale guards;
9. UI `COMENTAR` + propiedades + mode ownership;
10. preservación F8;
11. hardening/regresiones;
12. cierre documental/CI/draft PR.

La granularidad definitiva corresponde al implementation plan; cada tarea debe ser lo bastante pequeña para TDD y checkpoint independiente.

## 25. Estado al terminar esta spec

- F7 permanece cerrado automatizadamente en `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`;
- F8 usa rama propia `feat/f8-comments-v1`;
- esta spec consolida las decisiones de arquitectura, alcance, persistencia, UI y pruebas;
- todavía no se ha escrito código de producto F8;
- todavía no se ha escrito el implementation plan F8;
- no hay merge a `main`.
