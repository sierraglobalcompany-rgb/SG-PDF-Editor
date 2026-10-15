# SG PDF Editor — F15 Edit V3 Design

**Fecha:** 2026-10-15  
**Estado:** DESIGN APPROVED / SPEC WRITTEN / IMPLEMENTATION NOT STARTED  
**Base conceptual:** `feat/release-ui-modernization` @ `abd52d05279e3e656723ad4ff5a21d36de91508e`  
**Rama de continuidad:** `plan/post-ui4-f13-f18-expansion-20261014`

## 1. Objetivo

Convertir `EDITAR` en una experiencia clara, directa y profesional **sin reemplazar motores existentes ni añadir una arquitectura paralela**.

F15 debe lograr que el usuario:

- sepa qué objeto está seleccionado;
- sepa qué puede modificar y qué no;
- edite texto e imágenes de forma predecible;
- use censura segura dentro del mismo flujo;
- tenga `Guardar como...` siempre visible en Editar;
- preserve siempre el original;
- reciba una explicación cuando algo no sea editable.

## 2. Tres reglas obligatorias

### Regla 1 — KISS
`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → A→B`

No crear un editor universal, un segundo modelo de documento ni una jerarquía paralela de objetos.

### Regla 2 — RUIDO
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

Reutilizar primero los workspaces, hit-testing, preflight y writers existentes.

### Regla 3 — ANTI-PARCHE
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI`

La nueva UI no puede ocultar fallos funcionales abiertos de Editar/Guardar.

## 3. Decisión arquitectónica aprobada

Se adopta **Opción A — UX unificada sobre el motor actual**.

Se conservan como owners funcionales:

- `ImageEditWorkspace`
- `TextEditWorkspace`
- `RedactionWorkspace`
- `PdfEditWriter`
- preflight/guardas actuales
- hit-testing/selección actuales
- extracción/reemplazo de imágenes
- reflow seguro

F15 cambia principalmente presentación, selección, propiedades, comandos visibles y feedback; **no cambia el motor PDF ni crea otro writer**.

## 4. Problemas actuales

1. Editar no explica qué puede seleccionarse.
2. `Guardar como...` está escondido en la franja dinámica de modos.
3. Texto e imagen se perciben como subsistemas distintos.
4. Las limitaciones de texto no se explican claramente.
5. Undo/Redo existe para imágenes, pero no tiene paridad en texto/censura.
6. El estado dirty/guardado no es suficientemente visible.

## 5. Experiencia objetivo

Al activar `EDITAR`:

- el rail izquierdo marca Editar como activo;
- aparece una toolbar contextual compacta;
- el canvas permite selección directa;
- el panel derecho muestra inicialmente `Selecciona un texto o imagen para editarlo`;
- `Guardar como...` queda **siempre visible**;
- sin cambios, `Guardar como...` está **deshabilitado** y el estado indica `Sin cambios`;
- con cambios, se habilita y el estado indica `Cambios pendientes`.

Flujo principal:

`EDITAR → clic en objeto → detectar tipo → seleccionar → mostrar propiedades → modificar workspace → Guardar como...`

No existe selector previo “editar texto / editar imagen”.

## 6. Selección visual

### Texto
- selección inequívoca;
- overlay/borde sin modificar el PDF;
- hover sutil solo si no penaliza render;
- panel derecho de texto.

### Imagen
- borde claro;
- handles visibles;
- cursor adecuado durante mover/resize;
- panel derecho de imagen.

### Sin selección
- limpiar selección;
- volver a ayuda contextual.

## 7. Panel derecho — Texto

Mostrar solo controles útiles:

- Estado
- Contenido
- Fuente
- Tamaño
- Color
- Aplicar
- Reflow seguro cuando aplique
- Censura cuando sea elegible

Estado del objeto:

- **Editable**
- **Editable con limitaciones**
- **Solo lectura**

Cuando sea solo lectura, mostrar una razón breve en español. No habilitar controles que no pueden ejecutar una edición segura.

## 8. Panel derecho — Imagen

Acciones mínimas reutilizando el core existente:

- Reemplazar
- Extraer
- Opacidad
- Rotar izquierda/derecha
- Eliminar
- Enviar atrás
- Enviar hacia atrás
- Traer hacia delante
- Traer al frente

Canvas:

- mover;
- redimensionar;
- preservar relación de aspecto por defecto;
- mantener modifiers existentes.

No duplicar estas operaciones en otro modelo.

## 9. Toolbar contextual

Contenido inicial:

- Seleccionar
- Deshacer
- Rehacer
- Guardar como...
- indicador corto de estado (`Sin cambios`, `Cambios pendientes`, `Guardando`, `Guardado`, `Error`)

No es otra navegación global.

## 10. Contrato de Guardar

- `Guardar como...` siempre visible en Editar.
- Deshabilitado mientras no haya cambios.
- Habilitado si **cualquiera** de los workspaces editables está dirty.
- Nunca sobrescribir silenciosamente el original.
- Mantener preflight y confirmaciones de riesgo existentes.
- `PdfEditWriter` sigue siendo el único owner de materialización.
- Guardar crea una **copia nueva**.
- Tras éxito, marcar baseline de los workspaces que correspondan.
- **No reabrir automáticamente la copia guardada en F15.** El documento activo sigue siendo el original hasta que el usuario abra explícitamente la copia.
- Mostrar nombre/ruta resumida del destino guardado.
- En fallo, no marcar baseline y mantener los cambios pendientes.

## 11. Undo / Redo

Estado real auditado:

- imágenes: tienen historial Undo/Redo;
- texto: no tiene historial Undo/Redo propio;
- censura: no tiene historial Undo/Redo propio.

Decisión KISS:

1. No crear historial universal.
2. La toolbar enruta Undo/Redo solo al owner que realmente lo soporte.
3. Si la selección/última operación activa no tiene historial compatible, el comando queda deshabilitado.
4. Antes de añadir Undo/Redo a texto o censura, hacer una caracterización aislada.
5. Solo extender cada workspace con historial propio si puede hacerse con cambio pequeño, determinista y sin duplicar el writer.
6. La ausencia de Undo/Redo para un tipo debe ser visible/consistente; nunca simular éxito.

La paridad total de historial **no es requisito para arrancar F15**, pero cualquier extensión aprobada debe quedar dentro del owner correspondiente.

## 12. Censura segura

Permanece dentro de Editar:

`seleccionar texto → verificar elegibilidad → preparar censura → Guardar como...`

Prohibido:

- segundo writer;
- segundo workspace de censura;
- bypass de `RedactionPolicy`;
- “censura” basada solo en dibujar un rectángulo negro.

UI debe distinguir:

- censura preparada;
- no elegible;
- motivo de no elegibilidad.

## 13. Reflow seguro

Capacidad avanzada del texto seleccionado.

Solo aparece/habilita cuando layout/preflight lo permite. Si no es seguro, mostrar razón y no modificar el objeto.

## 14. PDFs escaneados

Si no hay texto seleccionable suficiente:

> Este PDF parece escaneado. Ejecuta OCR para obtener texto seleccionable.

No ejecutar OCR automáticamente. La acción reutiliza el owner OCR existente.

## 15. Guardas de salida

Editar V3 debe preservar las guardas actuales al:

- abrir otro PDF;
- abrir ZPL;
- cambiar a Leer;
- cambiar a Firmar;
- cambiar a Organizar;
- cambiar a Comentar;
- cerrar ventana.

El nuevo shell nunca puede saltarse esas guardas.

## 16. Integración con F13/F14

F15 consume del shell:

- rail izquierdo;
- estado de herramienta seleccionada;
- topbar/toolbar consistente;
- iconografía reutilizable;
- panel derecho contextual.

F15 no recrea shell temporal ni duplica navegación.

F14 ya habrá mapeado las herramientas a owners existentes. F15 mejora el owner de Editar, no crea un segundo entrypoint funcional.

## 17. Accesibilidad / teclado

- Tab/Shift+Tab predecible;
- foco visible;
- tooltips;
- nombre accesible en controles solo-icono;
- Delete conserva semántica de imagen cuando corresponde;
- Ctrl+Z/Ctrl+Y solo habilitados cuando el owner activo los soporte;
- Enter no debe aplicar cambios al control equivocado.

## 18. Tema / iconos

- Light/Dark con recursos SG existentes.
- No introducir colores light-only hard-coded en controles F15.
- Iconos vectoriales propios o símbolos Windows compatibles.
- Sin librería UI nueva.

## 19. No objetivos

F15 NO incluye:

- editor PDF universal;
- reconstrucción tipo Word;
- OCR automático;
- PDF→Word;
- edición arbitraria de vectores/paths;
- JavaScript PDF;
- XFA;
- cambio de motor PDF;
- nueva librería gráfica;
- nuevo writer;
- cloud/backend/API;
- IA.

## 20. Arquitectura mínima permitida

Permitido si una prueba demuestra necesidad:

- método/router pequeño dentro del owner UI para `Save/Undo/Redo`;
- helpers de estado visual;
- recursos XAML;
- extensiones de historial **dentro del workspace propietario**, no globales;
- tests de contrato/integración.

Prohibido:

- `DocumentObjectModel` universal;
- command bus;
- event aggregator;
- DI framework nuevo;
- plugin system;
- segundo motor/writer.

## 21. Tests requeridos

### Selección
- texto → panel texto;
- imagen → panel imagen;
- clic vacío limpia selección;
- objeto eliminado no queda seleccionado.

### Texto
- editable habilita controles;
- read-only deshabilita y explica razón;
- aplicar modifica workspace, no archivo fuente;
- reflow solo si es elegible.

### Imagen
- mover;
- resize;
- rotate;
- delete;
- opacity;
- z-order;
- replace;
- extract;
- undo/redo existente.

### Censura
- no bypass de `RedactionPolicy`;
- estado prepared visible;
- materialización solo con Guardar como.

### Guardado
- visible siempre durante Editar;
- sin dirty → disabled;
- cualquier dirty → enabled;
- cancel no modifica fuente;
- failure no marca baseline;
- success marca baseline;
- output es copia;
- original continúa como documento activo;
- reopen manual de copia confirma materialización.

### Guards
- cambio de modo con dirty conserva decisiones actuales;
- abrir otro documento no pierde cambios sin confirmación.

### Tema/accesibilidad
- recursos temáticos;
- focus visible;
- accessible names.

## 22. QA real Windows

Después de CI GREEN:

1. PDF con texto editable.
2. PDF con texto read-only por render mode.
3. PDF con imagen.
4. PDF mixto.
5. editar + Guardar como + abrir manualmente la copia.
6. Undo/Redo de imagen.
7. censura + Guardar como + abrir copia.
8. escaneado → sugerencia OCR.
9. light/dark.
10. 1366×768 @100%.
11. perfil escalado.
12. teclado.

## 23. Gate de aceptación

F15 solo cierra si:

- el usuario entiende cómo empezar a editar sin explicación externa;
- selección es inequívoca;
- texto e imagen comparten una experiencia coherente;
- limitaciones se explican;
- `Guardar como...` es visible y fiable;
- original queda protegido;
- no hay segundo writer/motor;
- full suite GREEN;
- exact-head CI GREEN;
- Windows QA GREEN;
- no hay P0/P1 abiertos.

## 24. Orden / dependencias

No implementar F15 antes de:

1. resolver P1 actuales de UI-4;
2. rerun UI-4 sin P0/P1;
3. integrar F13 shell suficiente;
4. completar F14 productización necesaria.

## 25. Decisión final

F15 es una mejora profunda de experiencia, no un reemplazo del motor.

Ruta:

`selección directa → propiedades contextuales → cambio en workspace → feedback claro → Guardar como transaccional`

Esto maximiza el código ya probado y cumple KISS, Ruido y Anti-parche.
