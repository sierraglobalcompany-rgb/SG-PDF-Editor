# F12.4 — Compare UI mínima sobre la shell existente

Fecha: 2026-10-14  
Estado del diseño: **DESIGN APPROVED IN CHAT / SPEC WRITTEN / WRITTEN SPEC REVIEW PENDING / IMPLEMENTATION NOT STARTED**  
Rama de contexto: `feat/f12-4-task2-compare-core`  
Base documental previa: `b3729dbc7063e918572e1259cd8d09d0609191ed`

## 1. Objetivo

Añadir una UI mínima y útil para consumir el núcleo `PdfCompareService` ya cerrado, sin crear una segunda shell, un segundo visor, un coordinador genérico de modos ni una arquitectura paralela.

El usuario debe poder:

1. abrir un PDF A en SG PDF;
2. elegir **Archivo → Comparar con otro PDF...**;
3. seleccionar un PDF B;
4. ejecutar la comparación local/offline existente;
5. ver un resumen claro en el panel derecho;
6. cerrar la comparación y volver al estado normal.

La UI no ampliará la semántica del núcleo. Solo mostrará con fidelidad el contrato ya probado:

- `LeftPageCount`;
- `RightPageCount`;
- `TextChanged`;
- `VisualChanged`.

## 2. Decisión de diseño

### Opción elegida — visor actual + panel derecho reutilizado

El PDF actualmente abierto permanece como **PDF A** y continúa visible en el visor central. El usuario selecciona un segundo archivo como **PDF B**. El resultado se muestra en el panel derecho existente.

Flujo:

`PDF A abierto → Comparar con otro PDF → seleccionar PDF B → PdfCompareService.Compare(A,B) → publicar resumen en panel derecho`

### Alternativas rechazadas en este slice

#### Alternar A/B en un solo visor

No se implementa todavía porque requiere mantener una segunda sesión/documento visible, coordinación adicional de navegación y estado que el núcleo actual no necesita.

#### Dos visores sincronizados lado a lado

No se implementa porque obliga a duplicar render, zoom, scroll, navegación y sesión. Para el alcance actual sería arquitectura prematura y ruido.

## 3. Punto de entrada

Agregar un único comando:

**Archivo → Comparar con otro PDF...**

Condiciones:

- deshabilitado o no operativo si no hay PDF A abierto;
- no debe aceptar el mismo flujo ZPL como fuente;
- el selector de archivo B usa `OpenFileDialog` con filtro PDF;
- cancelar el selector no cambia estado ni UI.

No se agrega una pestaña, ribbon, toolbar nueva ni modo global permanente.

## 4. Panel de resultados

La UI debe reutilizar el host del panel derecho donde hoy conviven propiedades, firma y formularios.

Contenido mínimo:

- título: **Comparar PDF**;
- nombre del PDF A;
- nombre del PDF B;
- páginas A;
- páginas B;
- texto: **Sin cambios** / **Cambios detectados**;
- visual independiente del texto: **Sin cambios** / **Cambios detectados**;
- estructura: **Igual cantidad de páginas** / **Cantidad de páginas diferente**;
- conclusión:
  - **Sin diferencias detectadas** cuando conteos iguales y ambos flags son `false`;
  - **Se encontraron diferencias** en cualquier otro caso;
- botón **Cerrar comparación**.

`VisualChanged` debe presentarse con una etiqueta equivalente a:

> **Cambio visual independiente del texto**

No debe mostrarse como “cambio visual total”, porque el núcleo deliberadamente no separa un posible cambio gráfico adicional cuando ya existe cambio textual.

## 5. Semántica y límites

La UI refleja exactamente el núcleo ya cerrado.

### Idéntico

- conteos iguales;
- `TextChanged=false`;
- `VisualChanged=false`;
- conclusión: sin diferencias detectadas.

### Texto-only

- `TextChanged=true`;
- `VisualChanged=false`;
- conclusión: se encontraron diferencias.

### Visual-only

- `TextChanged=false`;
- `VisualChanged=true`;
- conclusión: se encontraron diferencias.

### Diferencia estructural por cantidad de páginas

- `LeftPageCount != RightPageCount`;
- no convertir automáticamente la diferencia estructural en falso cambio textual o visual-only;
- conclusión: se encontraron diferencias.

## 6. Arquitectura KISS

Producción prevista inicialmente:

- `src/SGPdf.App/MainWindow.Compare.cs`

Ese partial será propietario de:

- creación/inyección del `MenuItem`;
- creación/inyección del panel derecho de Compare;
- selección de PDF B;
- llamada al núcleo `PdfCompareService`;
- publicación del resumen;
- `ResetCompareUi()`;
- manejo local de errores/estado busy.

Integraciones lifecycle permitidas, solo si son necesarias:

- abrir otro PDF;
- entrar en Firma;
- entrar en Editar;
- entrar en Organizar;
- abrir Formularios.

Cada integración debe ser focal, idealmente una línea llamando `ResetCompareUi()` o equivalente. No se crea un `ModeManager`, state machine global, controller paralelo ni nueva shell.

## 7. Exclusividad con herramientas existentes

Antes de iniciar Compare se deben respetar los guards ya existentes:

- Editar: `TryLeaveEditModeWithGuard()`;
- Organizar: `TryLeaveOrganizeModeWithGuard()`;
- Firma: resolver firmas visuales pendientes antes de abandonar el contexto.

Al entrar en Compare:

- ocultar paneles incompatibles de Formularios/Firma/etiquetas si están visibles;
- mantener el PDF A en el visor central;
- no destruir ni modificar el PDF A;
- no crear estado persistente fuera de la sesión UI actual.

Al salir de Compare:

- ocultar su panel;
- limpiar referencia/ruta de PDF B y resultado UI;
- restaurar `PropertiesPlaceholderText` cuando corresponda;
- no reabrir ni recargar PDF A innecesariamente.

## 8. Contrato mínimo de error

La comparación debe fallar de forma controlada:

- archivo B no válido/no legible → mensaje/status claro, sin dejar panel con resultado parcial;
- excepción del núcleo → no reemplazar el PDF A ni alterar su sesión;
- cancelar selector → no-op;
- PDF protegido con contraseña → **fuera del alcance inicial** del flujo Compare; se debe mostrar error controlado, sin introducir manejo nuevo de credenciales en este slice.

No se añadirá cache, persistencia, base de datos ni historial de comparaciones.

## 9. Fuera de alcance explícito

- dos visores lado a lado;
- alternancia A/B en el visor;
- navegación entre diferencias;
- resultados por página;
- overlay/heatmap;
- diff semántico tipo Word;
- OCR automático;
- matching/reordenamiento inteligente de páginas;
- comparación profunda de firmas o metadatos;
- exportar reporte PDF;
- guardar comparación;
- backend, nube, IA o telemetría;
- OpenCV, ImageMagick o segundo motor PDF;
- nuevas dependencias salvo bloqueo demostrado y aprobado.

## 10. Criterios de aceptación del MVP UI

1. Con PDF A abierto, existe **Archivo → Comparar con otro PDF...**.
2. Cancelar la selección de B deja SG PDF sin cambios.
3. Seleccionar B llama al núcleo existente con rutas A/B.
4. El panel derecho muestra nombres, conteos y las dos señales con semántica correcta.
5. Una comparación idéntica muestra **Sin diferencias detectadas**.
6. Cualquier cambio textual, visual-only o estructural muestra **Se encontraron diferencias**.
7. **Cerrar comparación** limpia el panel y mantiene A abierto.
8. Abrir/cambiar de documento o entrar en otra herramienta incompatible resetea Compare sin estado huérfano.
9. Un fallo de B no destruye ni reemplaza A.
10. No se modifica `MainWindow.xaml` salvo que un RED posterior demuestre que la inyección focal existente no alcanza; la preferencia es reutilizar el patrón runtime ya usado por Reader/Formularios/Firma.

## 11. Estrategia TDD prevista

La implementación se dividirá en micro-gates estrictos:

### Task 3A — RED UI contract

Un único test debe definir el primer contrato observable mínimo sin implementar producción en el mismo gate.

Contrato sugerido inicial:

- el partial/flujo Compare puede publicar una presentación derivada de `PdfCompareResult` para el caso idéntico;
- no exigir todavía interacción real WPF si el repo ya ofrece un patrón de test más simple para lógica de presentación.

### Task 3B — GREEN mínimo

Máximo 1–2 archivos productivos donde sea posible. Hacer pasar solo el RED anterior.

### Task 3C+ — incremento uno por uno

Agregar después, cada uno con RED/GREEN separado si hace falta:

- comando de menú;
- selección de B;
- panel derecho;
- reset/lifecycle;
- errores controlados.

No agrupar todo el flujo en un único lote.

## 12. KISS / Ley del Ruido — guardrails obligatorios

Antes de cada GREEN comprobar:

- ¿puede reutilizarse la shell existente? Si sí, no crear otra;
- ¿puede reutilizarse el panel derecho? Si sí, no crear sidebar nueva;
- ¿puede mantenerse A en la sesión actual? Si sí, no crear sesión visual paralela;
- ¿el nuevo tipo/abstracción tiene una necesidad demostrada por RED? Si no, no añadirlo;
- ¿hay lógica duplicada de guards/reset? Mejorar o unir solo cuando exista duplicación real, no por anticipación;
- ¿el cambio toca XAML global sin necesidad? Evitarlo;
- ¿aparece paquete nuevo? Bloquear y justificar antes de continuar.

Resultado esperado del diseño: **KISS PASS / Ley del Ruido PASS**.

## 13. Definition of Done del slice UI mínimo

El slice UI no se considera cerrado hasta cumplir:

- todos los criterios de aceptación automatizables cubiertos;
- build Windows exact-head: 0 warnings / 0 errors de SG PDF;
- suite completa PASS;
- no regresiones en Reader, Firma, Formularios, Editar u Organizar;
- auditoría KISS/Ley del Ruido;
- documento de cierre exact-head;
- F12.4 sigue declarando límites honestos, sin prometer diff universal.

## 14. Próximo paso después de aprobar este spec

Solo después de revisión explícita de este documento se debe crear el plan de implementación con `writing-plans`.

Ese plan debe respetar la regla del proyecto:

> **1 continua = 1 micro-gate**

No se inicia código productivo mientras este spec no haya sido revisado y aprobado como artefacto escrito.
