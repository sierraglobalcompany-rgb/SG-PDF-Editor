# SG PDF — R4-C / R4-D: matriz de QA Windows real contra V4

**Estado:** `NOT_RUN / PARTIAL`; este documento es protocolo, NO certificación. Fecha 2026-10-17.  
**Rama:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`. **Baseline exact-head:** `0427cfff84e500884759d5287fd53054738c2fb9`, CI [39076821081](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39076821081) SUCCESS; 1.288 PASS, 0 FAIL, 1 SKIP; build 0/0; smoke 30 s. El commit documental que agrega esta matriz debe tener CI nuevo para citarlo como HEAD final.

## Entorno y controles

En Windows x64, comprobar `git fetch --all --prune`, rama, SHA, tree limpio y ejecutable WPF realmente compilado. Registrar monitor/resolución, zoom DPI 100% y 125%, tema del sistema, hora y SHA exacto de la app. No probar el prototipo HTML ni reutilizar capturas antiguas. Usar dos PDFs sintéticos no sensibles (A con jerarquía de bookmarks, B sin outlines), más un ZPL seguro. Si un diálogo Windows bloquea la herramienta, registrar `BLOCKED_ENVIRONMENT` sin tocar el producto para “arreglar” el agente. No versionar archivos sensibles.

## Cuatro capturas WPF obligatorias

| Evidencia real | Diseño aprobado | Estado |
| --- | --- | --- |
| `R4D_V01_LIGHT_CLOSED_REAL.png` | [Claro, navegación cerrada](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_CLARO_CERRADO_2026-10-17.png) | NOT_RUN |
| `R4D_V02_LIGHT_OPEN_REAL.png` | [Claro, navegación abierta](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_CLARO_NAVEGACION_2026-10-17.png) | NOT_RUN |
| `R4D_V03_DARK_CLOSED_REAL.png` | [Oscuro, navegación cerrada](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_OSCURO_CERRADO_2026-10-17.png) | NOT_RUN |
| `R4D_V04_DARK_OPEN_REAL.png` | [Oscuro, navegación abierta](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_OSCURO_NAVEGACION_2026-10-17.png) | NOT_RUN |

Cada captura debe mostrar la aplicación real sin teclado virtual ocultando elementos. Registrar identidad de app, icono de PDF asociado en cada tab, tab activo/cierre, iconos aprobados de utilidades o fallback `read/comment/forms`, 11 tools y estados disabled, menu/zoom, toggle/drawer, foco/contraste y ausencia de texto `SGPdf.App.Shell.PdfDocumentWorkspace` o clipping. Diferencias cosméticas P2 y funcionales P1/P0; no exigir coincidencia pixel a pixel del PDF ilustrativo.

## W01–W15: contratos originales R4-C

| ID | Paso / expectativa | Estado inicial |
| --- | --- | --- |
| W01 | Abrir PDF A, drawer cerrado por defecto, lector a ancho completo | NOT_RUN |
| W02 | Toggle abre/cierra, recupera ancho, tooltip/AutomationName correctos | NOT_RUN |
| W03 | Páginas: miniaturas reales, selección/salto coherentes | NOT_RUN |
| W04 | Marcadores: árbol padre/hijo real, orden/títulos, expansión | NOT_RUN |
| W05 | Clic y Enter al destino: número/scroll/render correctos | NOT_RUN |
| W06 | Padre sin destino no navega, hijo conserva destino válido | NOT_RUN |
| W07 | B sin outlines: «Este PDF no tiene marcadores.»; sin árbol de A | NOT_RUN |
| W08 | A↔B en carga: sin datos stale/excepciones | NOT_RUN |
| W09 | Guardar/restaurar drawer y pestaña de navegación por PDF | NOT_RUN |
| W10 | Cierre del activo/último: limpiar UI/nav | NOT_RUN |
| W11 | Home y ZPL sin toggle/bookmarks residuales | PARTIAL (Home visto; ZPL NOT_RUN) |
| W12 | Light/Dark: estados/empty/error legibles sin color fijo erróneo | NOT_RUN (Home Light visto solamente) |
| W13 | Windows DPI 125%: sin clipping, foco visible, sin scroll horizontal accidental | NOT_RUN |
| W14 | Teclado Tab/flechas/Enter y accesibilidad/nombres, sin activación inadvertida | NOT_RUN |
| W15 | Error de carga inducido de forma segura si viable: mensaje de error distinto a empty, app estable | NOT_RUN |

## T01–T08: contrato visual R4-D

| ID | Paso / expectativa | Estado |
| --- | --- | --- |
| T01 | Abrir A+B, alternar tabs, título largo elipsis, selected consistente, sin tipo interno | NOT_RUN |
| T02 | Cerrar inactivo/activo, dirty guard R1B con Sí/No y persistencia correcta | NOT_RUN |
| T03 | Foco de teclado y active tab visibles en Light/Dark, no solo color | NOT_RUN |
| T04 | Rail Leer→Editar→Comentar→Firmar→Organizar→Leer, guard correcto y 1 selected | NOT_RUN |
| T05 | 11 tools, forms/OCR/censurar/comparar/convertir/proteger disabled y sin acción falsa | NOT_RUN |
| T06 | Iconos oficiales sin deformación, label centrada, tooltip y accesibilidad | NOT_RUN |
| T07 | Menú/zoom/página alineados Light/Dark, documentar deuda para bloque 3 sin implementarla | NOT_RUN |
| T08 | Home y ZPL sin rail PDF ni tab/documento fantasma | NOT_RUN |

## Evidencia y gate

Por fila: `PASS/FAIL/NOT_RUN/PARTIAL/BLOCKED_ENVIRONMENT`, timestamp, SHA/entorno, ruta local de captura o log, observaciones y severidad P0/P1/P2. `NOT_RUN`, `PARTIAL`, `BLOCKED` **no** son PASS. No subir capturas privadas ni PDFs de prueba. 
- P0: crash, pérdida/corrupción de trabajo, acción peligrosa.
- P1: tab activa errónea, fuga de clase, iconos sustituidos, dirty guard/navegación rota, clipping que impide el uso, herramienta engañosamente habilitada.
- P2: espaciado y retoques sin riesgo/obstáculo de uso.

**R4-C/R4-D siguen QA_PENDING** hasta capturas WPF reales V01–V04, W01–W15 y T01–T08 con cobertura suficiente y P0=P1=0, más CI exact-head final. Si el agente no puede operar Windows, reportar bloqueo con pasos reproducibles. **No empezar bloque 3, R5, T9.2 ni hacer merge.**