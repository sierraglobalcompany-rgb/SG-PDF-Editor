# SG PDF Editor — F15 Edit V3 Design

**Fecha:** 2026-10-15  
**Estado:** DESIGN APPROVED / SPEC WRITTEN / IMPLEMENTATION NOT STARTED  
**Base conceptual:** `feat/release-ui-modernization` @ `abd52d05279e3e656723ad4ff5a21d36de91508e`  
**Rama de continuidad:** `plan/post-ui4-f13-f18-expansion-20261014`

## 1. Objetivo

Convertir el modo `EDITAR` actual en una experiencia clara, directa y profesional sin reemplazar los motores existentes ni añadir una arquitectura paralela.

La meta de F15 no es “editar cualquier PDF del mundo”. La meta es que SG PDF:

- deje claro qué objeto está seleccionado;
- deje claro qué puede modificarse y qué no;
- exponga correctamente capacidades que ya existen;
- permita editar texto e imágenes de forma predecible;
- mantenga censura segura integrada;
- mantenga `Guardar como...` visible y confiable;
- conserve el original;
- explique limitaciones en vez de fallar silenciosamente;
- mantenga el writer transaccional y los workspaces existentes.

## 2. Reglas obligatorias

### Regla 1 — KISS

`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → A→B`

No crear un “editor universal” nuevo, un modelo abstracto de documento ni una segunda jerarquía de objetos si el motor actual ya resuelve el caso.

### Regla 2 — RUIDO

`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

Antes de agregar lógica, reutilizar los workspaces y writers actuales.

### Regla 3 — ANTI-PARCHE

`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI`

La nueva UI no puede esconder fallos funcionales de Editar/Guardar todavía abiertos en UI-4.

## 3. Decisión arquitectónica aprobada

Se adopta **Opción A — UX unificada sobre el motor actual**.

Se conservan como owners funcionales:

- `ImageEditWorkspace`
- `TextEditWorkspace`
- `RedactionWorkspace`
- `PdfEditWriter`
- preflight/guardas existentes
- selección y hit-testing existentes
- extracción/reemplazo de imágenes existentes
- reflow seguro existente

F15 cambia principalmente:

- navegación;
- descubribilidad;
- selección visual;
- panel contextual;
- toolbar contextual;
- estados de edición;
- feedback de guardado;
- comunicación de limitaciones.

No cambia el motor PDF principal ni crea otro writer.

## 4. Problemas actuales a resolver

### 4.1 Editar no es suficientemente descubrible

El modo existe, pero el usuario no recibe una guía clara sobre:

- qué puede seleccionar;
- dónde hacer clic;
- cuándo un objeto es editable;
- por qué un objeto no puede modificarse;
- cuándo una edición quedó aplicada;
- cuándo existe un cambio pendiente;
- cuándo puede guardar.

### 4.2 `Guardar como...` está demasiado escondido

Hoy aparece como control contextual inyectado en la franja de modos.

En V3 debe ser una acción permanente mientras `EDITAR` esté activo.

### 4.3 Texto e imagen se sienten como subsistemas separados

El usuario no debería escoger primero “editar texto” o “editar imagen”. Debe seleccionar el objeto directamente y SG PDF debe resolver el tipo de objeto.

### 4.4 Las limitaciones de texto no se explican bien

Algunos objetos deben permanecer solo lectura por seguridad/fidelidad. Esa limitación debe mostrarse explícitamente en UI.

### 4.5 Undo/Redo no es suficientemente visible

Existe para imágenes y capacidades internas relacionadas, pero debe convertirse en affordance visible del flujo de edición.

## 5. Experiencia objetivo

### Entrada

Al activar `EDITAR`:

- el rail izquierdo marca `Editar` como activo;
- aparece una toolbar contextual compacta;
- el canvas muestra affordance de selección;
- el panel derecho muestra inicialmente:
  - `Selecciona un texto o imagen para editarlo`;
- `Guardar como...` queda visible desde el inicio del modo;
- si no hay cambios, aparece deshabilitado o con estado `Sin cambios` según el patrón final validado en Windows.

### Selección

Flujo:

`EDITAR → clic en objeto → detectar tipo → seleccionar → mostrar propiedades aplicables`

No se introduce un selector de “tipo de edición”.

## 6. Estados visuales de selección

### Texto

Al pasar/seleccionar un objeto de texto compatible:

- hover sutil si es viable sin coste de render excesivo;
- selección visible inequívoca;
- borde/overlay sin alterar el PDF;
- panel derecho cambia a propiedades de texto.

### Imagen

Al seleccionar imagen:

- borde claro;
- handles de resize existentes, mejor presentados;
- cursor adecuado durante mover/resize;
- panel derecho cambia a propiedades de imagen.

### Sin selección

Panel derecho vuelve a ayuda contextual de Editar.

## 7. Panel derecho — Texto

Debe mostrar solo controles útiles.

### Campos

- `Estado`
- `Contenido`
- `Fuente`
- `Tamaño`
- `Color`
- `Aplicar`
- sección `Reflow seguro` solo cuando aplique
- sección `Censura` como acción sobre selección compatible

### Estado del objeto

Uno de:

- **Editable**
- **Editable con limitaciones**
- **Solo lectura**

Cuando sea solo lectura, mostrar motivo breve en español.

Ejemplos:

- modo de render no soportado;
- fuente/estructura no representable de forma segura;
- restricción del preflight;
- objeto no compatible con la estrategia actual.

No habilitar controles que no pueden ejecutar una acción segura.

## 8. Panel derecho — Imagen

Acciones mínimas:

- Reemplazar
- Extraer
- Opacidad
- Rotar izquierda/derecha
- Eliminar
- Enviar atrás
- Enviar hacia atrás
- Traer hacia delante
- Traer al frente

Interacción directa en canvas:

- mover;
- redimensionar;
- preservar relación de aspecto por defecto;
- modifier existente para comportamiento alternativo si ya está soportado.

No duplicar estas operaciones en otro modelo.

## 9. Toolbar contextual de Editar

Contenido recomendado:

- Seleccionar
- Deshacer
- Rehacer
- Guardar como...

Opcional solo si el layout lo requiere y sin duplicar ruido:

- indicador `Cambios pendientes`

No convertir la toolbar en otra navegación global.

## 10. Guardado

### Principios

- `Guardar como...` siempre visible mientras Editar está activo.
- Nunca sobrescribir silenciosamente el original.
- Mantener preflight existente.
- Mantener confirmaciones actuales para casos de riesgo.
- Mantener `PdfEditWriter` como único owner de materialización.

### Estados UI

- Sin cambios
- Cambios pendientes
- Guardando
- Guardado
- Error

El error debe mostrar la causa útil disponible sin logs técnicos extensos.

### Éxito

Tras guardar:

- marcar baseline correctamente;
- mantener el workspace coherente;
- no perder selección/estado por accidente;
- mostrar nombre de archivo destino.

La decisión de reabrir automáticamente el archivo guardado debe mantenerse igual al comportamiento más seguro existente, salvo que una prueba específica justifique cambiarlo.

## 11. Undo / Redo

F15 debe exponer Undo/Redo de forma coherente.

Regla:

- no crear un historial universal nuevo si los workspaces actuales ya tienen historial propio;
- primero caracterizar qué operaciones ya soportan undo/redo;
- crear una capa de routing mínima solo si es necesaria para que los botones globales actúen sobre el workspace activo.

Si texto/reflow/censura no tienen paridad suficiente con imágenes, no simular soporte: documentar y ampliar solo cuando exista un modelo seguro.

## 12. Censura segura

La censura permanece dentro del owner de Editar.

Flujo:

`seleccionar texto → verificar elegibilidad → preparar censura → Guardar como...`

No crear:

- otro writer;
- otro workspace de censura;
- una ruta visual que omita `RedactionPolicy`;
- un botón que simplemente dibuje un rectángulo negro.

El UI debe distinguir claramente:

- censura preparada;
- censura no elegible;
- motivo de no elegibilidad.

## 13. Reflow seguro

Se mantiene como capacidad avanzada del texto seleccionado.

No debe mostrarse como opción principal para todo objeto.

Debe aparecer cuando el análisis de layout/preflight lo habilite.

Si el párrafo no puede reflowarse de forma segura, mostrar razón y mantenerlo sin cambios.

## 14. PDFs escaneados

Si la página/documento no presenta texto seleccionable suficiente:

Mostrar sugerencia:

> Este PDF parece escaneado. Ejecuta OCR para obtener texto seleccionable.

No ejecutar OCR automáticamente.

La acción puede enrutar al owner OCR existente.

## 15. Guardas de cambio de modo

Editar V3 debe preservar las guardas actuales al:

- abrir otro PDF;
- abrir ZPL;
- cambiar a Leer;
- cambiar a Firmar;
- cambiar a Organizar;
- cambiar a Comentar;
- cerrar ventana.

El nuevo shell/rail no puede saltarse esas guardas.

## 16. Integración con F13 Shell

F15 asume que F13 proveerá:

- rail izquierdo;
- patrón visual de tool seleccionado;
- topbar/toolbar consistente;
- iconografía reutilizable;
- panel derecho contextual.

F15 no debe recrear esas piezas.

Si F13 todavía no está integrado cuando se implemente F15, F15 se mantiene bloqueado o se ejecuta sobre una rama compatible sin duplicar shell temporal.

## 17. Accesibilidad y teclado

Requisitos:

- Tab/Shift+Tab predecible;
- foco visible;
- tooltips en iconos;
- nombres accesibles para botones solo-icono;
- Delete conserva eliminación de imagen seleccionada donde ya aplica;
- Ctrl+Z / Ctrl+Y mantienen semántica correcta;
- Enter no debe aplicar accidentalmente cambios en un campo distinto al enfocado.

## 18. Tema / visual

- Light/Dark mediante recursos SG existentes.
- No hard-coded `White`, `LightGray`, `DimGray` nuevos en navegación/estado si existe equivalente temático.
- Iconos vectoriales propios o símbolos Windows compatibles.
- No librería UI nueva.

## 19. No objetivos de F15

F15 NO incluye:

- editor PDF universal;
- reconstrucción automática estilo Word;
- OCR automático;
- PDF→Word;
- edición arbitraria de paths/vectores;
- JavaScript PDF;
- edición XFA;
- cambio de motor PDF;
- nueva librería gráfica;
- nuevo writer;
- cloud/backend/API;
- IA.

## 20. Arquitectura mínima esperada

Cambios permitidos, si realmente son necesarios:

- un pequeño `EditCommandRouter` o método equivalente dentro del owner UI actual para dirigir Undo/Redo/Save al workspace correcto;
- helpers de estado visual;
- XAML/recursos de propiedades;
- tests de contrato e integración.

Cambios NO permitidos:

- `DocumentObjectModel` nuevo para representar todo el PDF;
- command bus;
- event aggregator;
- DI framework nuevo;
- plugin system;
- segundo motor/writer.

## 21. Tests requeridos

### Selección

- seleccionar texto enruta a panel texto;
- seleccionar imagen enruta a panel imagen;
- clic vacío limpia selección;
- objeto eliminado no permanece seleccionado.

### Texto

- editable habilita controles;
- read-only deshabilita controles y explica razón;
- aplicar modifica workspace, no archivo fuente;
- reflow solo se habilita si elegible.

### Imagen

- mover;
- resize;
- rotate;
- delete;
- opacity;
- z-order;
- replace;
- extract;
- undo/redo según soporte actual.

### Censura

- no bypass de `RedactionPolicy`;
- estado `prepared` visible;
- save materializa a copia.

### Guardado

- visible durante Editar;
- disabled/sin cambios según decisión final;
- dirty → save habilitado;
- cancel no modifica fuente;
- failure no marca baseline;
- success marca baseline;
- output reabre/valida según comportamiento aprobado.

### Guards

- cambiar de modo con cambios pendientes conserva la decisión existente;
- abrir otro documento no pierde cambios sin confirmación.

### Theme/UI contract

- no hard-coded light-only resources nuevos en los controles F15;
- focusable controls y accessible names presentes.

## 22. QA real Windows

Después de CI GREEN:

1. PDF con texto editable normal.
2. PDF con texto read-only por render mode.
3. PDF con imagen.
4. PDF mixto texto+imagen.
5. edición + Guardar como + reapertura.
6. Undo/Redo visible.
7. censura segura + guardar + reapertura.
8. escaneado → sugerencia OCR.
9. light/dark.
10. 1366×768 @100%.
11. perfil escalado.
12. teclado.

## 23. Gate de aceptación F15

F15 solo puede cerrar si:

- usuario puede entender cómo empezar a editar sin explicación externa;
- selección es visualmente inequívoca;
- texto e imagen comparten una experiencia coherente;
- limitaciones se explican;
- `Guardar como...` es visible y fiable;
- original queda protegido;
- no se creó segundo writer/motor;
- full suite GREEN;
- exact-head CI GREEN;
- Windows QA GREEN;
- no hay P0/P1 abiertos.

## 24. Dependencias / orden

F15 no se implementa antes de:

1. resolver P1 actuales de UI-4;
2. cerrar/rerun UI-4 sin P0/P1;
3. integrar F13 shell suficiente para no crear navegación temporal duplicada;
4. productizar entrypoints existentes F14 donde corresponda.

## 25. Resumen de decisión

F15 es una **mejora profunda de experiencia**, no un reemplazo del motor.

Ruta:

`selección directa → propiedades contextuales → cambios en workspace → feedback claro → Guardar como transaccional`

Esto maximiza valor usando el código ya probado y cumple las tres reglas del proyecto.
