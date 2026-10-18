# SG PDF Editor — Auditoría funcional (31 herramientas sin IA) y brecha visual V4

**Fecha:** 2026-10-17 (Colombia) / 2026-10-18 (UTC)  
**Repositorio:** https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor  
**Rama auditada:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
**HEAD contrastado en la auditoría:** `bd8b88b4f0f2ce34d73e60bd5063264481c77006`  
**Fuente visual:** [V4 claro cerrado](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_CLARO_CERRADO_2026-10-17.png) y [V4 claro abierto](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_CLARO_NAVEGACION_2026-10-17.png); V4 oscuro cerrado/abierto también aprobado.  
**Captura real de base:** `R4D_V01_LIGHT_CLOSED_REAL.png` del QA Windows (app `26db4b1`, Windows 11, 125 %), subida por usuario. Es **anterior** a la corrección automática del icono (`3f5d0c9`) y por ello no constituye una comparación posfix.

> **Alcance:** lectura y contraste documental/de código de la rama, sin repetir QA manual. `Implementada` = existe flujo de producto y evidencia automatizada dentro de sus límites; NO equivale a validación visual universal. `Parcial` = implementación/capacidad disponible pero con alcance acotado, formato diferente, o acceso aún no conectado. `Falta` = no se identificó flujo de producto aprobado en esta rama.

## A. Inventario de la imagen: 31 herramientas sin IA

La referencia tiene 33: 6 Ordenar, 3 Optimizar, 5 Convertir a PDF, 5 Convertir desde PDF, 6 Editar, 5 Seguridad y 3 PDF Intelligence. Se **excluyen** `Resumir con IA` y `Traducir PDF`. `PDF a Markdown` sigue inventariado por ser posible sin IA, aunque no está implementado. No agregar motores de IA, traducción, cloud o cuentas.

| N.º | Grupo | Herramienta exacta de referencia | Estado | Evidencia y precisión |
|---:|---|---|---|---|
| 01 | Ordenar | Unir PDF | **Implementada** | `MainWindow.Organize.Insert.cs` (`TryMergeOrganizePdfCandidate`), plan y materialización F5. Flujo por `Organizar`, no botón Home directo. |
| 02 | Ordenar | Dividir PDF | **Implementada** | `MainWindow.Organize.ExtractSplit.cs`: dividir por cantidad de páginas o rangos; salida en varios PDF. |
| 03 | Ordenar | Eliminar páginas | **Implementada** | `OrganizePlanOperations.Delete` + UI Organizar. No permite salida con cero páginas. |
| 04 | Ordenar | Extraer páginas | **Implementada** | `TryExtractOrganizeSelectionTo`; guarda PDF derivado, originales protegidos por preflight. |
| 05 | Ordenar | Ordenar PDF | **Implementada** | `MoveSelection`, `Duplicate`, edición de plan de páginas y guardado. |
| 06 | Ordenar | Escanea a PDF | **Falta** | No se identificó captura directa desde escáner Windows/WIA/TWAIN a nuevo PDF. No confundir OCR de un PDF ya existente con escaneo físico. |
| 07 | Optimizar | Comprimir PDF | **Parcial** | `PdfOptimizeRepairCapabilityAdapter.OptimizeCopyAsync` con qpdf 12.4.2, caracterizado como capacidad GO; **sin comando WPF de producto**. La recompresión no garantiza reducir todos los PDF. |
| 08 | Optimizar | Reparar PDF | **Falta / NO-GO** | Existe `RepairCopyAsync` de capacidad, pero `RepairProductDecision.NoGo`; no vender ni habilitar reparación genérica sin validez probada. |
| 09 | Optimizar | OCR PDF | **Implementada** | `MainWindow.Ocr.cs`, Tesseract local 5.5.3, PDF buscable con texto invisible; alcance conservador y QA físico real pendiente. Acceso desde menú Herramientas, no rail F14. |
| 10 | A PDF | JPG a PDF | **Falta** | `image-to-pdf` figura `Próximamente` en `ToolCatalog.Home`; no escritor JPG/PNG→PDF aprobado. |
| 11 | A PDF | WORD a PDF | **Falta** | Sin motor local Office→PDF integrado. |
| 12 | A PDF | POWERPOINT a PDF | **Falta** | Ídem. |
| 13 | A PDF | EXCEL a PDF | **Falta** | Ídem. |
| 14 | A PDF | HTML a PDF | **Falta** | Sin pipeline aprobado HTML→PDF ni renderer de navegador. |
| 15 | Desde PDF | PDF a JPG | **Parcial (otro formato)** | Existe **PDF a PNG**, no JPG, en `MainWindow.Conversions.cs` + `PdfToPngExporter`. 144 DPI fijo, todas las páginas; no llamar JPG al PNG. |
| 16 | Desde PDF | PDF a WORD | **Falta** | Sin exportador PDF→DOCX. |
| 17 | Desde PDF | PDF a POWERPOINT | **Falta** | Sin exportador PDF→PPTX. |
| 18 | Desde PDF | PDF a EXCEL | **Falta** | Sin exportador PDF→XLSX. |
| 19 | Desde PDF | PDF a PDF/A | **Falta** | Sin conversor/validador de conformidad PDF/A certificado. |
| 20 | Editar | Rotar PDF | **Implementada** | `OrganizePlanOperations.Rotate`; rotación de páginas por cuartos de vuelta en el editor. |
| 21 | Editar | Insertar números de página | **Implementada** | `MainWindow.Utilities.cs`, `PageNumberingDialog`, `PdfTextOverlayWriter` (incluye Bates). |
| 22 | Editar | Insertar marca de agua | **Parcial** | Marca de agua **de texto** vía `WatermarkDialog`; imagen como marca de agua no autorizada como flujo productivo. Lote solo TextWatermark. |
| 23 | Editar | Recortar PDF | **Falta** | No se identificó herramienta de recorte de páginas/MediaBox/CropBox. Editar una imagen interna no equivale a recortar páginas. |
| 24 | Editar | Editar PDF | **Parcial** | Edición real de texto e imágenes en subconjuntos compatibles (`MainWindow.Edit*`, F6/F7/F11), con restricciones conservadoras; no editor universal de todos los PDF. |
| 25 | Editar | Formularios PDF | **Parcial** | Puede inspeccionar/editar/guardar campos existentes AcroForm `/Tx`; no crear formularios nuevos ni soportar botones, XFA, flattening, diseñador. Menú/contexto; rail F14 deshabilitado. |
| 26 | Seguridad | Desbloquear PDF | **Falta** | Apertura autorizada de PDFs con contraseña y protección de copia no equivalen a herramienta explícita para retirar cifrado con contraseña conocida. No incluir bypass de derechos/contraseñas desconocidas. |
| 27 | Seguridad | Proteger PDF | **Implementada** | `MainWindow.Utilities.cs`, `ProtectionDialog`, copia con AES-256 y contraseña usando qpdf. Alcance autorizado. |
| 28 | Seguridad | Firmar PDF | **Implementada** | Firma visual (PNG/foto/dibujo/biblioteca) y firma criptográfica local PFX/P12 en flujo independiente. No prometer TSA/HSM/validez legal certificada. |
| 29 | Seguridad | Censurar PDF | **Parcial** | Eliminación segura subyacente en objetos de texto simple horizontal compatibles, política fail-closed; no cobertura general de todo tipo de PDF/tags/imágenes. |
| 30 | Seguridad | Comparar PDF | **Parcial** | `MainWindow.Compare.cs`, señales de texto, visual independientes y páginas; **sin** visor de dos documentos lado a lado, overlay o heatmap. |
| 31 | PDF Intelligence sin IA | PDF a Markdown | **Falta** | No hay exportador Markdown local. Sería una conversión heurística con PDFium y límites de tablas/columnas, separada de IA. |

**Totales por clasificación:** **10 implementadas, 7 parciales, 14 faltantes/no habilitadas**. Son categorías auditadas del código y alcance — **no** 10/31 manualmente certificadas en Windows.

## B. Brecha entre funciones existentes y acceso real

1. `ToolCatalog.Home`: 15 accesos visibles pero solo `Etiquetas ZPL` tiene `ActionId = open-zpl`; las utilidades PDF muestran `ActionId=null` y razón «Disponible con un PDF abierto; acceso desde Home se conectará en F14». **No equivalen a funciones ausentes**; muchas tienen dueño operativo en menús del PDF.
2. `ToolCatalog.Document`: 11 modos; solo los 5 primeros (`Leer`, `Editar`, `Comentar`, `Firmar`, `Organizar`) tienen un `ActionId` ejecutable en el rail. Los 6 restantes (`Formularios`, `OCR`, `Censurar`, `Comparar`, `Convertir`, `Proteger`) están visualmente deshabilitados y apuntan a F14, aunque varios ya tienen flujo funcional en menús.
3. **Prioridad de UX tras R4:** conectar acciones existentes reutilizando el owner actual y las precondiciones. No escribir una segunda implementación, enrutador universal, motor PDF nuevo ni una plataforma de plugins.
4. Las 31 operaciones de esta referencia son **una lista de objetivos**, no el alcance aprobado de F13 ni una obligación de entregar todas antes del QA visual. F14–F18 siguen congeladas hasta autorización.

## C. Auditoría visual V4: lo que NO coincide aún

Comparación V01 **real** (`R4D_V01_LIGHT_CLOSED_REAL.png`, ejecutable `26db4b1`) contra `SG_PDF_MOCKUP_V4_CLARO_CERRADO_2026-10-17.png`. Son tamaños de referencia diferentes (WPF al 125 %, mockup 1600×950): no exigir igualdad píxel a píxel. Diferenciar defecto real de dato de fixture.

| Área | Diseño V4 aprobado | V01 real observada | Clasificación / próximo owner |
|---|---|---|---|
| Identidad superior | Logo/app visual aprobado en cabecera y símbolo documental por pestaña | Windows OS title icon correcto; banda superior solo texto «SG PDF», pestaña con hueco sin icono | **Brecha visual significativa**. Icono de tab QA-V4-001 P1: fix en `3f5d0c9` + test CI GREEN, aún **sin V01 real posterior**. Cabecera de marca sigue pendiente. `MainWindow.Documents.cs`. |
| Geometría de cabecera | Marca, tabs y `+` compactos; control seleccionado integrado | Tabs grandes y altas, amplitud excesiva con mucho blanco; botón `+` en extremo derecho | **Visual P2/P1 por jerarquía**. `MainWindow.Documents.cs`; ajustar solo tras gate y respetar elipsis/dirty guard. |
| Rail izquierdo | ~90 px en mockup, ritmo vertical compacto, encabezado «DOCUMENTO», seleccionado con pastilla azul y borde de acento | ~120 px en captura al 125 %, sin encabezado, marcador activo es franja lateral fina y filas muy espaciadas | **Desviación visual manifiesta**. `MainWindow.Shell.cs` + `Themes/Modern.xaml`. Comparar DIPs a 125 %; no modificar todos los tamaños arbitrariamente. |
| Scroll del rail | Iconos alineados, 11 tools, disabled sutiles, sin scrollbar intrusiva | Scrollbar gris interna muy visible y rompe composición; iconos PNG pequeños y varios pálidos | **P2 / problema de jerarquía**. Revisar altura, disposición/responsividad y scroll sin ocultar herramientas. |
| Toggle de navegación | Botón cuadrado delimitado con icono reconocible junto al lienzo | Signo `›` suelto, diminuto y de bajo contraste | **P1/P2 de descubribilidad**; owner `MainWindow.ReaderThumbnails.cs` / estilos; test teclado/UIA. |
| Canvas / lector | Lienzo gris claro, página con sombra/borde y márgenes amplios, rótulo de vista | Fondo casi blanco, poca separación cromática del papel; página finamente delimitada | **P2 de superficie/jerarquía**. Reader / tema; la página **blanca y sin texto** viene del fixture PDF sintético, no demuestra fallo de render. |
| Menú / toolbar | Fila compacta, controles agrupados, zoom/«Ajustar al ancho» en dropdown, imprimir visible | Fila más alta y dispersa; `Página` y `Ancho` son dos botones; falta acción de imprimir expuesta como en V4 | **Brecha de diseño conocida para bloque 3**, NO implementarla hasta pasar gate R4. `MainWindow.Documents.cs`, `MainWindow.xaml`; reutilizar comandos actuales. |
| Estado activo | Pastilla rellena azul suave, icono/etiqueta azul, foco perceptible | Marca lateral cian sin fondo de selección equivalente | **P1 visual si impide identificar claramente modo; de momento diferencia de fidelidad V4**. `SgRailItemSelectedStyle`. |
| Estado/footer | Indicador «Listo», vista del documento y alineación discreta | Pie muestra archivo/página en banda más gruesa | **P2**. Reevaluar sin perder información útil del lector. |
| Tema oscuro / drawer abierto | V02–V04 aprobados con contraste, miniaturas, dos estados de navegación | **No existen capturas reales**; Codex detuvo QA por P1 en V01 | **NOT_RUN**. No declarar coincidencia ni falla de Dark/drawer a partir de la V01 Light cerrada. |
| Soporte multiplataña | Dos documentos ilustrativos en V4 | Una pestaña en V01 porque había un solo PDF de prueba | **No es defecto demostrable**. Requiere T01 (dos PDFs) y dirty guard. |

**Causa sistémica probable:** se ha implementado la funcionalidad y unos estilos básicos de shell, no la composición visual completa de V4. Los tests automáticos actuales demuestran existencia/función de controles, no semejanza visual. **No copiar el HTML ni introducir un segundo framework visual en WPF.**

## D. Plan por gates; no inventar cierre

**G0 — P1 (bloqueo activo):** repetir V01 Light cerrado en WPF real tras `3f5d0c9` / checkpoint GitHub; comprobar PNG correcto junto a título, elipsis, cierre y foco. El test automático real del bitmap pasó (`1.289 PASS/0 FAIL/1 SKIP`, CI `39082539638` en `bd8b88b`). Captura V01 antigua sigue FAIL histórico. Si sigue sin imagen, STOP de nuevo.

**G1 — R4-C/R4-D visual antes de bloque 3:** V01–V04 reales, W01–W15 + T01–T08 a 125 %, claro/oscuro, 2+ PDFs, ZPL y drawer Páginas/Marcadores. Clasificar P0/P1/P2. Si el mockup sigue lejos, dejar deuda visual estructurada y acordar microbloques R4-D: (a) marca y tabs; (b) rail + seleccionado/scroll; (c) toggle+superficie+footer; (d) command bar solo **tras autorización/desbloqueo del bloque 3**. No confundir «pasó tests» con «se ve como mockup».

**G2 — Conectar capacidades ya existentes (previsto F14, NO aprobado para ejecución aquí):** Home y rail → acciones reales Organizar, OCR, Formularios `/Tx`, Censurar, Comparar, PDF→PNG, Proteger, Marcas de agua/Numeración. Un router/owner existente, sin duplicar código. Hacer disabled honesto en opciones futuras.

**G3 — Funciones nuevas, previa priorización (secuencia propuesta, NO autorizada):**
1. Fácil/reutilizable: PNG/JPG→PDF, PDF→JPG reutilizando raster de PDF→PNG, comprimir conectado de forma honesta, recorte CropBox seguro y desbloqueo **con contraseña conocida** si superan preservation-gate.
2. Siguientes: escanear→PDF (dependencia Windows hardware), PDF→Markdown offline con límites, HTML→PDF con decisión previa de motor.
3. Avanzadas/costosas: Office↔PDF (calidad/compatibilidad y dependencia externa), PDF/A (perfiles y validadores), reparación de PDFs dañados (mantener NO-GO hasta demostrar seguridad).
4. No implementar Resumir con IA, Traducir mediante IA ni introducción encubierta de servicios de IA.

**Leyes de ejecución:** KISS, DELETE→SIMPLIFY→REUSE→MERGE→EXTEND→ADD, REPRODUCE→ROOT CAUSE→OWNER→MINIMAL CHANGE→CAUSAL TEST→FULL SUITE→EXACT-HEAD CI, diseño V4 y accesibilidad. Sin nueva arquitectura compleja, no merge, no R5/T9.2, no F14–F18 sin autorización.

## E. Evidencia contrastada (GitHub)

- [ToolCatalog](../../src/SGPdf.App/Shell/ToolCatalog.cs): lista de 15 Home y 11 Document; `ActionId` habilitados; bloqueos F14.
- [Requisitos trazables](../../.planning/REQUIREMENTS.md): F5 Organize, F9 Utilities, F10 OCR, F12.1–F12.7; límites explícitos sobre Office y conversiones.
- [MainWindow.Organize.cs](../../src/SGPdf.App/MainWindow.Organize.cs), [ExtractSplit](../../src/SGPdf.App/MainWindow.Organize.ExtractSplit.cs), [Insert](../../src/SGPdf.App/MainWindow.Organize.Insert.cs), [PlanOperations](../../src/SGPdf.App/Features/Organize/OrganizePlanOperations.cs).
- [MainWindow.Utilities.cs](../../src/SGPdf.App/MainWindow.Utilities.cs) + [OptimizeRepairCapabilityAdapter](../../src/SGPdf.App/Features/Utilities/PdfOptimizeRepairCapabilityAdapter.cs) + [Conversiones](../../src/SGPdf.App/MainWindow.Conversions.cs).
- [MainWindow.Forms.cs](../../src/SGPdf.App/MainWindow.Forms.cs), [OCR](../../src/SGPdf.App/MainWindow.Ocr.cs), [Comparar](../../src/SGPdf.App/MainWindow.Compare.cs), [Sign](../../src/SGPdf.App/MainWindow.Sign.cs), [Signing](../../src/SGPdf.App/MainWindow.Signing.cs).
- [V4 aprobado](../design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md), [checkpoint QA P1](../history/2026-10-17-r4c-r4d-windows-qa-p1-tab-icon.md), [RED/GREEN icono](../history/2026-10-18-qa-v4-001-tab-icon-red-green-windows-retest-pending.md), [matriz QA](../qa/2026-10-17-r4c-r4d-v4-windows-matrix.md).
- [CI exact-head `bd8b88b`](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39082539638): 1289 PASS, 0 FAIL, 1 SKIP, build 0/0, smoke PASS; **no certifica visualmente V4**.

**Conclusión:** completar V4 como diseño WPF real y conectar propietarios existentes tiene mayor valor y menor riesgo inmediato que añadir una docena de herramientas nuevas. Los 14 faltantes son backlog, no blocker directo de R4. Ninguna decisión de este informe autoriza implementar G2/G3 antes de cerrar el gate presente.