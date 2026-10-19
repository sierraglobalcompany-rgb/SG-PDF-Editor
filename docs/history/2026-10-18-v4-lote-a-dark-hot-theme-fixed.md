# Lote A — Dark y tema vivo corregidos, 18-10-2026

**Resultado: LOTE_A_CODE_AND_WINDOWS_GREEN para QA-V4-002/003. R4-C/R4-D permanecen QA_PENDING.** Asignación explícita FIX_AND_VALIDATE; rama `feat/f13-task9-t9-1-document-shell-realignment-20261017`. Baseline `4c16a2fa2549a8a60ec85ce1f522f4ca9b82fdc0`, [CI 39159487162 SUCCESS](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39159487162). ZIP conocido preservado; ningún cambio desconocido al iniciar.

## Dos causas y corrección mínima

1. **003: aliases de Color retenidos en brushes Freezable de Application.Resources.** Reproducción aislada con App real y `Application.ThemeMode` Light→Dark→Light en la misma instancia: `ApplicationBackgroundBrush` cambia `#FFFAFAFA → #FF202020`, pero `SgSurfaceBrush` y `Window.Background` siguen `#FFFAFAFA`; Panel/Canvas/Muted también retienen colores. No estaban frozen. Un probe con dictionary local a Window sí actualizaba: el alcance de recursos importa, no basta culpar a StaticResource. Tras corregir, Background sigue `#FFFAFAFA → #FF202020 → #FFFAFAFA`, con aserción in-process contra el brush nativo. Logs/source en evidencia local.
2. **002: foreground sin dueño explícito sobre superficies con contrato distinto.** El encabezado de TabItem no declaraba Foreground; el título heredaba negro en Dark. Las tarjetas blancas de Reader y Organizar heredaban texto claro; OrganizeSurface además tenía RGB 243 fijo bajo comandos Dark. Se define Foreground dinámico nativo en estilo de TabItem, negro sobre papel blanco en ambas numeraciones y recurso de tema en OrganizeSurface.

**KISS / antirruido:** eliminar cinco aliases retenidos y conectar consumidores a brushes Fluent existentes mediante DynamicResource/SetResourceReference. No handler de eventos, ThemeManager, estado paralelo, paquetes, cambios de motores o navegación. App.xaml conserva ThemeMode System. Jerarquía, visibilidad, copy, interacciones e iconos aprobados se conservan; el gate de este lote es legibilidad y feedback coherentes en ambos temas, teclado y 125%.

## Archivos afectados

- `Themes/Modern.xaml`: elimina aliases y actualiza estilos compartidos; Foreground de tab.
- `MainWindow.xaml`, `Shell/HomeDashboard.xaml`, `MainWindow.Documents.cs`, `MainWindow.Shell.cs`: sustitución de claves de recursos; sin cambiar navegación ni selección.
- `MainWindow.ReaderThumbnails.cs`, `MainWindow.Organize.cs`: numeración sobre papel blanco; OrganizeSurface sigue brush nativo.
- `PdfPasswordDialog.xaml`, `SignatureDrawDialog.xaml`, `SignatureLibraryDialog.xaml`, `SignatureNameDialog.xaml`, `SignaturePhotoDialog.xaml`: migración mecánica de referencias a los aliases eliminados para no dejar recursos huérfanos. Sin cambiar contratos/guards de diálogos. Sus flujos completos no se reejecutaron manualmente.
- `UiThemeContractTests.cs`, `DocumentShellRealignmentContractTests.cs`: realinear contratos existentes al recurso nativo; no pruebas cosméticas nuevas ni eliminación del Skip futuro.

No se agregó test xUnit global de Application.ThemeMode: el harness existente usa hilos STA independientes y no tiene Application compartida; introducir ese singleton contaminaría las demás pruebas. La reproducción causal usa un ejecutable temporal aislado, con fuente incluida, más QA de tema System por UI real. Esto evita un test que pruebe solo Window.ThemeMode y dé un falso positivo: dicho caso ya pasaba antes del fix.

## Gates consolidados

- Restore locked PASS; build Release **0 warnings / 0 errors**.
- Focal relevante **166 PASS / 0 FAIL / 1 SKIP**.
- Full final **1290 PASS / 0 FAIL / 1 SKIP / 1291 total**. Una full inicial también pasó; se repitió únicamente porque la QA encontró el mismo defecto de numeración en tarjetas de Organizar y se añadió una línea. No hubo CI/commits por control.
- Smoke final **PASS 30 s**, `SGPdf-startup-0ef6f7cf62b14977b034b0f95e8e8394`. Copia temporal del runner con WindowStyle Hidden; runner del repo intacto.
- Commit final incluye código/checkpoint/matriz/STATE; CI exact-head se verifica después del push y se entrega con SHA/URL. CI no certifica la fidelidad total V4.

## QA Windows real final (125%)

| Caso | Resultado | Evidencia / límites |
| --- | --- | --- |
| QA-V4-002 | **PASS** | Arranque directo en Dark: título de tab claro, números negros sobre papel blanco en Reader y Organizar, toolbar legible; selected/disabled distinguibles y foco de cierre visible. |
| QA-V4-003 | **PASS** | Misma instancia final: Dark→Light, después Light→Dark→Light con PDF abierto, drawer y Organizar; después Home Light→Dark→Light. Se observaron fondos Y textos. Sin superficies retenidas en las vistas ensayadas. |
| W09 | **PASS regresión corta** | A abierto/Marcadores → B homónimo cerrado → A restaura Marcadores; cerrar A conserva B cerrado. No repetir batería completa ni atribuirle validación nueva completa de W01–W15. |
| V01–V04 | **Contraste PASS / composición PARTIAL** | Cuatro capturas finales reales; referencias oficiales comparadas. Marca/tabs/+ compactos, rail/pastilla/encabezado, toggle/drawer, profundidad canvas y footer siguen diferencias P2 del lote B. Barra de comandos DEFERRED/REQUIRES_APPROVAL. Blanco de fixture válido. |
| Teclado/DPI | **PASS focal / PARTIAL global** | Foco de cierre Light/Dark y alternancia real; 125% registrado en cada PNG. Tab-order completo/mínimo 960×640/casos error inducido no certificados. |

Muestras de píxeles de capturas sin alterar, contraste orientativo: tab Dark blanco/#2B2B2B **14,16:1**; comandos Organizar blanco/#383838 **11,73:1**; numeración negro/blanco **21:1**; foco de cierre Dark #9B9B9B/#2B2B2B **5,09:1** y Light #414141/#FEFEFE **10,12:1**. No es una certificación de todos los bordes/estados o de textos disabled.

**P0 observados 0; P1 de este lote pendientes 0.** No generalizar esos conteos a toda la aplicación. R4 sigue QA_PENDING por composición/cobertura. Tema Light restaurado (AppsUseLightTheme=1/SystemUsesLightTheme=1), app cerrada limpia, sin guardar originales.

## Evidencia local y trazabilidad

`C:\Users\felip\AppData\Local\Temp\SGPDF_FIX_LOTE_A_20261018_213212`. Capturas finales `*_FINAL.png`; preliminares conservadas y excluidas del paquete final. Sidecars: timestamp -05:00, escala 125%, tema y hash. Build probado = baseline + `product-final.patch`; el SHA publicado se añade a los sidecars tras commit, sin fingir que el código ya estaba committed al capturar. `product-final-patch-hash.json` identifica el diff exacto; la publicación contiene ese mismo código.

| Captura final | SHA-256 |
| --- | --- |
| V01_LIGHT_CLOSED_FINAL.png | `437982576e61a0f87fa630bbfdbd72a275a02be5304960bd5a3b5de8bc98ed6f` |
| V02_LIGHT_OPEN_FINAL.png | `f30e99ba08dc685c65f1f110b9a4703e46b166f339e83135dc4ed57bce047f33` |
| V03_DARK_CLOSED_FINAL.png | `87bf0506e4057a0a6bf3b4c432ba997cff4842f2387a65e5bfdff8cc34faf781` |
| V04_DARK_OPEN_FINAL.png | `83f438808eab12bdb4ce78fe0c175117ee3744ec9044a99a21dce9ccf9bcb41d` |

Paquete local `SGPDF_FIX_LOTE_A_DARK_HOT_THEME_EVIDENCIAS.zip`, con SHA256SUMS y fuente/log del probe; sin binaries de build ni documentos privados. Capturas crudas solo locales por posibles bordes de escritorio; no subir PNG/ZIP/fixtures a GitHub.

## NEXT único

Asignar lote B para las diferencias de composición V4, conservando este cierre focal y la cobertura pendiente para el gate C. Sin iniciar B por iniciativa propia, sin bloque 3, R5/T9.2/F14–F18 ni merge a main.
