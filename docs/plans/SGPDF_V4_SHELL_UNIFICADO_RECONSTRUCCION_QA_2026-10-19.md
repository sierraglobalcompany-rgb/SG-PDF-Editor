# SG PDF — Reconstrucción visual integral V4 · Checkpoint y QA Windows

**Fecha:** 19 de octubre de 2026. **Proyecto:** F13 / Task 9 / T9.1, R4-C y R4-D.  
**Repositorio:** `sierraglobalcompany-rgb/SG-PDF-Editor`  
**Rama:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
**Autoridad visual:** `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md` y cuatro PNG en `docs/design/references/sgpdf-v4/`.  
**Autorización nueva:** el usuario aprobó el 19-10-2026 **reconstruir la composición visual completa del shell, incluida la apariencia de la barra superior**, conservando la lógica PDF/ZPL. **No** aprobó la fase funcional de la barra de comandos, R5, T9.2, F14–F18 ni merge a `main`.

## 1. Cambio de método (KISS / antirruido / antiparches / diseño)

Se suspende la corrección cosmética por microbloques y el prompt B anterior. Se adopta un único **lote coherente de composición**: una sola fuente de geometría de cabecera, UI real en el resto de regiones con propietarios funcionales ya existentes, y una **QA Windows integral** al terminar. No insertar `ThemeManager`, router, DOM paralelo, caché de estado ni nuevo motor PDF; reutilizar `MainWindow`, `PdfDocumentWorkspace`, Toolbar, `ToolCatalog` y la navegación actual.

## 2. Implementación del shell sobre la rama existente

- `MainWindow.xaml`: `DocumentTopShell` es ahora el **dueño declarativo** de marca oficial, pestañas PDF, botón `+` inmediatamente después de las tabs, menú y fila de controles reales de navegación/zoom. Se conserva la barra ZPL por separado, el Workspace PDF, Home, los nombres WPF y las acciones existentes. Pie compacto y lienzo con un contraste mayor respecto del papel.
- `MainWindow.Documents.cs`: no extrae ni recoloca `Menu`/`NavigationBar`; solo conecta `OpenDocuments`, selección, apertura/cierre y visualización. Conserva dirty guard y el mismo `BuildDocumentTabHeaderTemplate`/icono original PDF.
- `MainWindow.Shell.cs` y `Modern.xaml`: rail más estrecho, encabezado `DOCUMENTO`, filas compactas, selección con contenedor redondeado; los 11 ítems del catálogo siguen siendo los mismos y las funciones no listas permanecen deshabilitadas. Sin reemplazar PNG originales ni glyphs semánticos.
- `MainWindow.ReaderThumbnails.cs`: **un solo** drawer; encabezado `Navegación`, botón cerrar con el mismo `ToggleReaderNavigation`, y affordance más visible para abrirlo. La visibilidad y selección continúan bajo estado por documento; W09 debe seguir PASS.
- Contratos WPF actualizados para comprobar la nueva ubicación de controles, altura/ancho del rail y recursos de lienzo. No se eliminan los tests de dirty guard, multiservicio ni tema en vivo.

**Importante:** código publicado no equivale a pantalla certificada. La barra de impresión o selección desplegable no debe fingir funcionalidades no existentes: las acciones reales de página/zoom siguen siendo las originales. Las diferencias que requieran *nueva funcionalidad* se marcan como fuera de alcance; las de pura apariencia están autorizadas en esta iteración.

## 3. Inspección del agente Codex Windows — ÚNICA intervención visual integral

**Modo obligatorio:** `QA_ONLY` sobre el **último SHA de GitHub** que tenga CI green; no cambiar producto antes de devolver un dictamen, salvo que se asigne `FIX_AND_VALIDATE` expresamente después. No ejecutar el prompt obsoleto del lote B ni volver a hacer las pruebas del lote A como proyecto nuevo.

**Preflight:** `git fetch --all --prune` → `git status --short` → `git branch --show-current` → `git rev-parse HEAD`; si el árbol tiene cambios ajenos, preservar y reportar, nunca reset/clean destructivo. No subir PDFs, ZIP de evidencia, capturas privadas o assets recortados al repo.

**Matriz final en una sesión Windows 125 %:**

| Código | Qué capturar y evaluar | Resultado inicial |
|---|---|---|
| V01 | Light, drawer cerrado, 2 PDF: marca oficial, tabs y `+` adyacente, rail, canvas/footer | NOT_RUN tras refactor |
| V02 | Light, drawer abierto: `Navegación`, Páginas/Marcadores, miniaturas, toggle/cierre, lienzo | NOT_RUN |
| V03 | Dark, drawer cerrado: contraste, seleccionado/foco, tema aplicado, iconos | NOT_RUN |
| V04 | Dark, drawer abierto: estados de miniaturas/bookmarks, cabeceras, accesibilidad | NOT_RUN |
| W09 | Dos PDFs homónimos: restauración independiente de drawer y tab Páginas/Marcadores | REGRESSION_REQUIRED |
| HOME/ZPL | Home y ZPL sin tabs/rail/drawer PDF residuales; abrir/cerrar/reabrir | REGRESSION_REQUIRED |
| GUARDS | Alternar tabs y cerrar con cambios pendientes sin perder confirmaciones R1B | REGRESSION_REQUIRED |
| THEME | Light→Dark→Light **sin reiniciar**, con PDF y drawer, recursos sin colores retenidos | REGRESSION_REQUIRED |
| DPI/KEYBOARD | 125%, ventana mínima 960×640 DIP, Tab/Enter/Space, foco, clipping | NOT_RUN |
| RESTO | W01–W15 y T01–T08: completar pendientes con evidencia sin recrear lo ya demostrado | PENDING |

**Referencias:** los cuatro mockups son 1600×950 px conceptuales; no comparar el contenido ficticio de PDF con el fixture real blanco. Se compara jerarquía, posición relativa, proporciones, selección, color, identidad, affordances y funcionalidad; registrar P0/P1/P2 con imagen y descripción concreta por **región**, no 20 tickets por detalle. La QA debe devolver cuatro capturas reales con SHA256SUMS y una sola matriz PASS/FAIL/PARTIAL/NOT_RUN.

## 4. Criterio de parada y continuidad

- **STOP** por crash, pérdida de datos, dirty guard roto, drawer de documento equivocado, cambio de tema con texto ilegible, control engañoso activo, o nueva arquitectura no autorizada.
- Diferencias visuales de composición P2 pueden agruparse para **una única corrección conjunta** del shell y repetir la comparación al final, no CI/checkpoint por cada margen.
- Cierre de R4 solo tras QA suficiente, P0/P1=0, evaluación humana de fidelidad V4, full suite/build/smoke CI del SHA exacto; ni CI ni una captura aislada bastan.
- Si la composición aún es insuficiente, mantener `QA_PENDING`, producir un solo diff regional de ajustes y ejecutar otra QA **consolidada**.
- Prohibido iniciar bloque 3 **funcional**, R5, T9.2, F14–F18, habilitar IA o merge a `main`.

## 5. Formato breve de entrega

`BASE/HEAD` · `ARCHIVOS/CAMBIOS` · `CI/BUILD/FULL/SMOKE` · `V01–V04` · `W09/HOME/ZPL/GUARDS/THEME/DPI` · `DIFERENCIAS P0/P1/P2 POR REGIÓN` · `RUTAS LOCALES Y SHA-256` · `DECISIÓN: R4 QA_PENDING o elegible para cierre` · `NEXT ÚNICO`.

El prompt no autoriza fingir un resultado ni sustituir la captura WPF por HTML, mockup u otra aplicación.