# SG PDF — Autoridad visual V4 aprobada (2026-10-17)

**Decisión:** el usuario aprobó expresamente el diseño V4 del shell/lector en modos claro y oscuro, navegación abierta y cerrada. Es la **referencia visual obligatoria** para los próximos cambios de T9.1, subordinada a las leyes SG, seguridad y verificaciones de funcionamiento. Aprobación de un mockup **no es aprobación de implementación ni QA de Windows**.

**Repositorio:** https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor  
**Rama de trabajo:** \`feat/f13-task9-t9-1-document-shell-realignment-20261017\`  
**Ancla de código previa a V4:** \`5ff21171abb4b467e8796ba18f54e0d337ab2418\`  
**CI verificado de ese código:** https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39069863859 — SUCCESS; 1286 PASS, 0 FAIL, 1 SKIP; build 0/0; smoke 30 s. **No certifica V4**.

## Entregables V4 aprobados y huellas SHA-256

Las imágenes originales fueron adjuntadas en el chat/proyecto del 17-10-2026. **Por ahora estos PNG de mockup no están incorporados como blobs a GitHub; no inferir lo contrario.** Localizarlos por sus nombres y comprobar las huellas antes de copiar al repositorio. Hasta que se suban, el bloqueo \`V4_ASSETS_NOT_IN_GITHUB\` permanece.

| Referencia | SHA-256 |
| --- | --- |
| \`SG_PDF_MOCKUP_V4_CLARO_CERRADO_2026-10-17.png\` | \`d7b12f909ae893d722f3d3cc95e602a39c614cfeb3adf27a4478218b2d51eeb3\` |
| \`SG_PDF_MOCKUP_V4_CLARO_NAVEGACION_2026-10-17.png\` | \`c55657965551945c5a0c22db52b2eb29fca760e26886fb6ddcffc0fb7dca90a3\` |
| \`SG_PDF_MOCKUP_V4_OSCURO_CERRADO_2026-10-17.png\` | \`f8958a5e0a9b9f46463b16b6990df128e19775149ccfe7a5e7026d6fb6a93aa8\` |
| \`SG_PDF_MOCKUP_V4_OSCURO_NAVEGACION_2026-10-17.png\` | \`d5ac3c0aac6940ea13adc134f65a0c9034beffc967154be75897c03c449a846f\` |
| \`SG_PDF_INVENTARIO_ICONOS_APROBADOS_V4_2026-10-17.png\` | \`35f2aae43e8922ca7d1ddcc3f2ebec2b85742964c4032c7c26af9b9161c7e23d\` |

Documentos del paquete del chat: \`SG_PDF_MOCKUP_V4_IDENTIDAD_OFICIAL_ESPECIFICACION_2026-10-17.md\`, \`SG_PDF_MOCKUP_SHELL_OFICIAL_V4_2026-10-17.html\`, \`SG_PDF_MOCKUP_V4_IDENTIDAD_OFICIAL_PACK_2026-10-17.zip\`.

**Acción para completar continuidad:** subir únicamente los cinco PNG aprobados en \`docs/design/references/sgpdf-v4/\` con sus nombres exactos, verificar SHA-256 y actualizar esta sección con enlaces relativos permanentes. El HTML es prototipo no productivo y puede conservarse solo si su peso/documentación aportan valor; no incorporar capturas de documentos privados ni recortes como assets productivos.

## Identidad de tres tipos — sin sustitutos

1. **Icono de la app:** \`src/SGPdf.App/Assets/SGPdf.App.ico\` (ventana, EXE, taskbar). No usar el icono PDF por error.
2. **Documento asociado:** \`src/SGPdf.App/Assets/SGPdf.Document.ico\` / \`SGPdf.Document.png\` (asociación, Recientes, tabs PDF). No usar el icono de la app en cada documento.
3. **Utilidades:** \`src/SGPdf.App/Assets/HomeTools/*.png\`, ruta canónica del \`ToolDescriptor.DocumentIconPath\` para \`edit, sign, organize, ocr, redact, compare, convert, protect\`. \`read, comment, forms\` conservan geometrías semánticas existentes en \`src/SGPdf.App/Themes/ShellIcons.xaml\`. No redibujar iconos ni usar los recortes de los mockups en producción.

## Defectos observados y dueño técnico

| Hallazgo | Evidencia / dueño a corregir |
| --- | --- |
| Texto \`SGPdf.App.Shell.PdfDocumentWorkspace\` visible bajo las pestañas | Captura Windows 17-10; \`src/SGPdf.App/MainWindow.Documents.cs\`: \`TabControl\` enlazado sin un visual de contenido apropiado; eliminar área de contenido ajena a la tira, no aplicar ocultación cosmética de texto |
| Tab activo poco reconocible, título/truncamiento/cierre desordenados | \`MainWindow.Documents.cs\` + estilos SG; indicador de selección perceptible con color **y** geometría/peso, foco y UIA |
| Barra de herramientas izquierda desalineada, mezcla de selected/disabled y espaciados | \`MainWindow.Shell.cs\`, \`ToolCatalog.cs\`, estilos existentes; un único estado activo, 11 posiciones con iconos oficiales cuando existen, affordance disabled |
| Cabecera, menú, zoom y navegación incoherentes | \`MainWindow.Documents.cs\`, \`MainWindow.xaml\`; evitar nuevas filas/duplicación de comandos |
| Toggle / pestañas Páginas-Marcadores y estados de feedback visualmente débiles | \`MainWindow.ReaderThumbnails.cs\`, reutilizando \`MainWindow.ReaderLinks.cs\`; preservar navegación, cargas diferidas, cancelación y aislamiento por PDF |

## R4-D — slice visual aprobado, aún NO implementado

**Orden:** preflight exact-head y rama limpia → test RED para la fuga de tipo/selección de tabs → mínima reparación del dueño de las pestañas → focal GREEN → pausa/checkpoint → rail/menú/drawer por microtareas → build completo/tests/smoke → capturas de Windows *reales* Light/Dark, escala 125%, navegación cerrada/abierta, teclado/foco, varias tabs, Home, ZPL → CI exact-head después del último commit → checkpoint. **No cerrar R4/C/D ni declarar fidelidad visual por test automático.**

**Aceptación V4:**
- 0 textos internos de tipos/clases expuestos.
- 0 iconos sustitutos nuevos, 0 motores/routers/owners duplicados.
- tab PDF activo inequívoco; icono de documento asociado; cerrar sigue guard dirty R1B; agregar/activar conserva multi-documento.
- un único modo seleccionado en rail; botones no implementados deshabilitados de forma coherente, con tooltip accesible.
- menú/nav/zoom sin clipping a 960px mínimos y DPI 125%; Light/Dark, foco y contraste.
- drawer cerrado devuelve ancho de trabajo; Marcadores y Páginas siguen funcionando, sin resultados atrasados entre PDFs.
- screenshot de la app real contra las cuatro imágenes aprobadas; incidencias clasificadas P0/P1/P2.
- build 0 warnings/0 errors, focal/full tests sin fallos, smoke ≥30s, CI SUCCESS en SHA final.

## Estado y límites

- **R4-B:** GREEN cerrado (no reabrir).
- **R4-C:** código en \`5ff2117\`, CI PASS, **QA_PENDING**; Windows W01–W15 sin cierre. Checkpoint original fuera de GitHub: \`R4C_CHECKPOINT.md\` (debe sincronizarse como evidencia, sin inventar QA).
- **R4-D:** diseño V4 aprobado; **IMPLEMENTATION_NOT_STARTED** en esta referencia.
- **R5/T9.2:** NOT STARTED; **NO MERGE**.
- \`.planning/STATE.md\` y \`.planning/ROADMAP.md\` deben reflejar los estados actuales y enlazar esta autoridad.
- Estilo SG: KISS + antirruido + anti-parches + ley diseño. Aceptación del diseño **no sustituye** aprobación para modificar arquitectura ni exime pruebas.

## Recuperación de chat

Leer GitHub HEAD y CI → \`.planning/STATE.md\` → esta referencia → \`.planning/ROADMAP.md\` → \`docs/MASTER_CONTEXT.md\` → \`AGENTS.md\`. Verificar los cinco SHA-256 antes de interpretar imágenes como V4.
