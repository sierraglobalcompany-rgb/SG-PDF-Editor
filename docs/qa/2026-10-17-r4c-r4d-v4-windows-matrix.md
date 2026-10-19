# SG PDF — R4-C / R4-D: matriz de QA Windows real contra V4

**Estado actual (2026-10-18, FIX_AND_VALIDATE lote A):** **002/003 PASS Windows**, W09 PASS y regresión corta PASS, icono PDF PASS. Cuatro capturas finales al 125%; [checkpoint único del fix](../history/2026-10-18-v4-lote-a-dark-hot-theme-fixed.md) contiene causas, hashes y límites. **R4-C/R4-D QA_PENDING** por composición V4 y cobertura restante. Filas no reejecutadas conservan evidencia previa; ningún PASS procede solo de CI. Los protocolos/STOP históricos inferiores no sustituyen este resultado.
**Rama:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`. **Baseline exact-head:** `0427cfff84e500884759d5287fd53054738c2fb9`, CI [39076821081](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39076821081) SUCCESS; 1.288 PASS, 0 FAIL, 1 SKIP; build 0/0; smoke 30 s. El commit documental que agrega esta matriz debe tener CI nuevo para citarlo como HEAD final.

## Entorno y controles

En Windows x64, comprobar `git fetch --all --prune`, rama, SHA, tree limpio y ejecutable WPF realmente compilado. Registrar monitor/resolución, zoom DPI 100% y 125%, tema del sistema, hora y SHA exacto de la app. No probar el prototipo HTML ni reutilizar capturas antiguas. Usar dos PDFs sintéticos no sensibles (A con jerarquía de bookmarks, B sin outlines), más un ZPL seguro. Si un diálogo Windows bloquea la herramienta, registrar `BLOCKED_ENVIRONMENT` sin tocar el producto para “arreglar” el agente. No versionar archivos sensibles.

## Cuatro capturas WPF obligatorias

| Evidencia real | Diseño aprobado | Estado |
| --- | --- | --- |
| `V01_LIGHT_CLOSED_FINAL.png` | [Claro, navegación cerrada](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_CLARO_CERRADO_2026-10-17.png) | PARTIAL composición; icon/contraste PASS |
| `V02_LIGHT_OPEN_FINAL.png` | [Claro, navegación abierta](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_CLARO_NAVEGACION_2026-10-17.png) | PARTIAL composición; contraste PASS |
| `V03_DARK_CLOSED_FINAL.png` | [Oscuro, navegación cerrada](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_OSCURO_CERRADO_2026-10-17.png) | PARTIAL composición; título Dark PASS corregido |
| `V04_DARK_OPEN_FINAL.png` | [Oscuro, navegación abierta](../design/references/sgpdf-v4/SG_PDF_MOCKUP_V4_OSCURO_NAVEGACION_2026-10-17.png) | PARTIAL composición; numeración Dark PASS corregida |

Cada captura debe mostrar la aplicación real sin teclado virtual ocultando elementos. Registrar identidad de app, icono de PDF asociado en cada tab, tab activo/cierre, iconos aprobados de utilidades o fallback `read/comment/forms`, 11 tools y estados disabled, menu/zoom, toggle/drawer, foco/contraste y ausencia de texto `SGPdf.App.Shell.PdfDocumentWorkspace` o clipping. Diferencias cosméticas P2 y funcionales P1/P0; no exigir coincidencia pixel a pixel del PDF ilustrativo.

## W01–W15: contratos originales R4-C

| ID | Paso / expectativa | Resultado 2026-10-18 |
| --- | --- | --- |
| W01 | Abrir PDF A, drawer cerrado por defecto, lector a ancho completo | PASS — V01 posfix, mismo A y drawer cerrado |
| W02 | Toggle abre/cierra, recupera ancho, tooltip/AutomationName correctos | PASS — toggle y Space, ancho recuperado; W09 aparte |
| W03 | Páginas: miniaturas reales, selección/salto coherentes | PASS navegación Light — miniatura 2 actualiza lector; contraste Dark en W12 |
| W04 | Marcadores: árbol padre/hijo real, orden/títulos, expansión | PASS — Root/Child y Unsupported/Reachable child, expansión real |
| W05 | Clic y Enter al destino: número/scroll/render correctos | PARTIAL — clicks navegan; Enter comprobado ya en destino |
| W06 | Padre sin destino no navega, hijo conserva destino válido | PASS — padre null no navega; hijo sí |
| W07 | B sin outlines: «Este PDF no tiene marcadores.»; sin árbol de A | PASS — B vacío sin árbol de A |
| W08 | A↔B en carga: sin datos stale/excepciones | PARTIAL — A↔B sin mezcla; no carga lenta forzada |
| W09 | Guardar/restaurar drawer y pestaña de navegación por PDF | PASS Windows lote A `8d6db5e` — A abierto/Marcadores y B cerrado; después A cerrado y B abierto/Páginas; alternancia y reapertura A conserva Marcadores |
| W10 | Cierre del activo/último: limpiar UI/nav | PASS — cierre activo/último e inactivo observados |
| W11 | Home y ZPL sin toggle/bookmarks residuales | PASS — Home y ZPL real sin rail/drawer PDF |
| W12 | Light/Dark: estados/empty/error legibles sin color fijo erróneo | PARTIAL — 002/003 contraste/tema PASS real; estados de error/carga inducidos no reejecutados |
| W13 | Windows DPI 125%: sin clipping, foco visible, sin scroll horizontal accidental | PARTIAL — 125% y scroll real; mínimo 960x640 NOT_RUN |
| W14 | Teclado Tab/flechas/Enter y accesibilidad/nombres, sin activación inadvertida | PARTIAL — Right/Enter/Space; Tab-order completo no certificado |
| W15 | Error de carga inducido de forma segura si viable: mensaje de error distinto a empty, app estable | NOT_RUN — no error de lectura inducido de forma segura sin tocar producto |

## T01–T08: contrato visual R4-D

| ID | Paso / expectativa | Estado |
| --- | --- | --- |
| T01 | Abrir A+B, alternar tabs, título largo elipsis, selected consistente, sin tipo interno | PASS funcional — A+B, alternancia, elipsis/cierre visibles; Dark en T03 |
| T02 | Cerrar inactivo/activo, dirty guard R1B con Sí/No y persistencia correcta | PASS — guard No conserva/Sí cierra; cierre inactivo conserva B |
| T03 | Foco de teclado y active tab visibles en Light/Dark, no solo color | PARTIAL — título y foco de cierre PASS Light/Dark; recorrido completo no certificado |
| T04 | Rail Leer→Editar→Comentar→Firmar→Organizar→Leer, guard correcto y 1 selected | PARTIAL — cinco modos responden; retorno directo/guard de modo no cubierto |
| T05 | 11 tools, forms/OCR/censurar/comparar/convertir/proteger disabled y sin acción falsa | PASS — 11 tools, seis disabled, scroll y click Proteger sin acción |
| T06 | Iconos oficiales sin deformación, label centrada, tooltip y accesibilidad | PASS iconografía — PNG documental reparado y arte/glyphs oficiales observados |
| T07 | Menú/zoom/página alineados Light/Dark, documentar deuda para bloque 3 sin implementarla | PARTIAL — fila real Light/Dark, deuda bloque 3 documentada |
| T08 | Home y ZPL sin rail PDF ni tab/documento fantasma | PASS — Home y etiqueta ZPL renderizada sin rail/tabs PDF |

## Evidencia y gate

Por fila: `PASS/FAIL/NOT_RUN/PARTIAL/BLOCKED_ENVIRONMENT`, timestamp, SHA/entorno, ruta local de captura o log, observaciones y severidad P0/P1/P2. `NOT_RUN`, `PARTIAL`, `BLOCKED` **no** son PASS. No subir capturas privadas ni PDFs de prueba. 
- P0: crash, pérdida/corrupción de trabajo, acción peligrosa.
- P1: tab activa errónea, fuga de clase, iconos sustituidos, dirty guard/navegación rota, clipping que impide el uso, herramienta engañosamente habilitada.
- P2: espaciado y retoques sin riesgo/obstáculo de uso.

**R4-C/R4-D siguen QA_PENDING** hasta capturas WPF reales V01–V04, W01–W15 y T01–T08 con cobertura suficiente y P0=P1=0, más CI exact-head final. Si el agente no puede operar Windows, reportar bloqueo con pasos reproducibles. **No empezar bloque 3, R5, T9.2 ni hacer merge.**
## Cobertura automatizada nueva (independiente del QA manual)

Dos refuerzos se aprobaron en CI sobre `e8001a0ea354a22f5b9868d1c792148e92d11e49`: prueba XAML semántica header-only en `f3aecac` y prueba STA de `DocumentTabs.SelectedItem == ActiveDocument` tras abrir un PDF en `e8001a0`. [CI 39080291059](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39080291059) SUCCESS; 1.288 PASS, 0 FAIL, 1 SKIP; build 0/0, smoke 30 s. **El gate W01–W15, V01–V04 y T01–T08 NO cambia de NOT_RUN/PARTIAL a PASS por esta evidencia automatizada.** Este checkpoint documental crea nuevo SHA que requerirá su propio CI.

## Histórico anterior al retest — corrección automatizada (2026-10-18)

- Captura V01 original (app `26db4b1`) **FAIL/P1** por imagen no dibujada; queda como evidencia histórica, sin sobrescribir.
- Nueva prueba sobre pestaña generada real por WPF: `ad69280517201d3a1804a82d15ca809179ea7708`; [CI RED 39082039007](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39082039007) falla por `Image.Source == null`, 1 fallo esperado + 1 SKIP. Primer prototipo de test `af1137a` falló por un problema del harness de template no sellado, y **no es evidencia causal**.
- Corrección mínima al URI del mismo icono oficial en `BuildDocumentTabHeaderTemplate()`: `3f5d0c9b43c925ab5b740d69b7d8c5b17fb47077` (pack URI absoluta, sin reemplazo de asset).
- [CI GREEN 39082254808](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39082254808): **1.289 PASS / 0 FAIL / 1 SKIP**, build 0/0, smoke 30 s PASS. El test ahora ve `BitmapImage` con `UriSource` pack absoluta y píxeles decodificados.
- **Protocolo de ejecución inmediata para Codex Windows:** preflight de rama limpia y último HEAD; compilar; lanzar app real; abrir el mismo PDF sintético y capturar **primero V01 Light Closed**, con icono oficial visible junto al título; si falta, `STOP_P1` sin avanzar. Si V01 pasa, ejecutar V02–V04 y resto W01–W15/T01–T08 (Dark, DPI 125%, 2 PDFs, bookmarks y ZPL); registrar resultados y diferencias sin fingir GREEN.
- Estado histórico antes del retest: V01/T06 RECHECK_REQUIRED y V02–V04 NOT_RUN. El retest de la tabla superior lo supersede: icono PASS, cuatro capturas reales, otros P1 abiertos. Sin merge, bloque 3, R5 ni T9.2.
