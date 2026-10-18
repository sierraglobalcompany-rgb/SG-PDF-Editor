# V4 lote A — QA_ONLY Windows, 18-10-2026

## Baseline y alcance

- Rama `feat/f13-task9-t9-1-document-shell-realignment-20261017`; app ensayada **`8d6db5e983048d2d1814ec0b2efdb1465e1d493f`**, avance directo desde `68a27c4`; [CI de entrada 39157586452](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39157586452) SUCCESS.
- Modo **QA_ONLY / lote A** por defecto del prompt entregado. Cambios de producto/tests/assets: **ninguno**. ZIP conocido preservado sin versionar; ningún otro cambio al iniciar.
- Windows 11, entorno .NET SDK 10.0.402, escala real **125%** medida por monitor en cada captura. Light inicial y final (AppsUseLightTheme=1 / SystemUsesLightTheme=1). Dos sesiones de arranque integradas en el mismo lote: Light para W09/ciclos vivos, después arranque Dark para contraste independiente.
- Fixtures sintéticos `source.pdf` en carpetas distintas: A `sgpdf-reader-ui-c099e64e924440a49d4de4f9579c7de1` (dos páginas blancas y bookmarks Root/Unsupported); B `sgpdf-reader-ui-455fb12e1e064084ab5ff3ad1f00a525` (sin outlines). Sin guardar/modificar originales. Blanco del fixture esperado, no fallo de render.

## Gates ejecutados

`dotnet restore SGPdf.slnx --locked-mode` PASS; Release build **0 warnings / 0 errors**; una suite completa `--no-build`: **1290 PASS / 0 FAIL / 1 SKIP / 1291 total**, incluye test causal W09 ya existente. No suite focal duplicada ni tests añadidos.

Smoke **PASS 30 s**, evidencia `SGPdf-startup-6c49ab284ba44c06aa00e23e6d437405`. Se usó copia temporal del runner existente añadiendo `-WindowStyle Hidden` para cumplir la instrucción del host; runner del repo intacto. Logs en la carpeta local indicada abajo. CI del commit documental posterior es independiente y se entrega con su SHA/URL al terminar; este baseline no equivale a aceptación visual.

## Resultado real del lote

| Caso | Resultado | Evidencia / límite |
| --- | --- | --- |
| W09 / QA-V4-004 | **PASS Windows** | A abierto/Marcadores → abrir B cerrado → A restaura bookmarks; cerrar A drawer → B abierto/Páginas → A sigue cerrado → B restaura Páginas → reabrir A conserva Marcadores. Dos capturas W09. Fix recibido `cc5d58d` validado sin editar producto. |
| Dark al iniciar / QA-V4-002 | **FAIL P1 OPEN** | Tab seleccionado negro sobre gris oscuro; números de miniatura blancos sobre blanco; Organizar con comandos blancos sobre superficie clara. Capturas V03/V04 y DARK_ORGANIZE_CONTRAST_FAIL. Disabled se distinguen de activos, sin certificar ratio de contraste numérico. |
| Tema vivo / QA-V4-003 | **FAIL P1 OPEN** | Misma instancia Light→Dark→Light en PDF y luego Home: Dark actualiza texto pero retiene superficies claras; retorno Light recupera legibilidad. Arranque Dark→Light adicional en PDF: texto negro sobre superficies Dark retenidas. Capturas antes/después. |
| V01/V02 Light | **PARTIAL** | Capturas nuevas reales; icono documental correcto, drawer abre y devuelve espacio; desviaciones por región abajo. |
| V03/V04 Dark | **FAIL** | Capturas nuevas reales; P1 de tab y miniaturas visibles independientemente del tema vivo. |
| Teclado/foco | **PARTIAL** | Tab desde Leer muestra borde de foco en Editar en Dark sin activarlo; no certifica recorrido completo ni todos los estados Light/Dark. UIA puede quedar un frame atrás: se priorizó pantalla y observación siguiente. |
| DPI | **PASS 125% observado** | Metadata por captura; tamaño mínimo 960×640 y otros escalados NOT_RUN en este lote. |
| Resto W/T | **No reejecutado completo** | Conservar resultados y límites del retest anterior en matriz. No elevar PARTIAL/NOT_RUN por CI. |

**P0 observados: 0**, sin generalizar ausencia fuera del alcance. **Dos familias P1 abiertas** (002/003), 004 cerrada en este retest. R4-C/R4-D **QA_PENDING**.

## Diagnóstico para el autor del código (sin fix autorizado)

- QA-V4-002: owners existentes `Themes/Modern.xaml` (`SgDocumentTabItemStyle`), `MainWindow.ReaderThumbnails.cs` (`BuildReaderThumbnailTemplate`), `MainWindow.Organize.cs:66` (RGB 243 fijo). Reproducción de contraste confirmada; no añadir owners nuevos ni tests cosméticos por color.
- QA-V4-003: `App.xaml` usa `ThemeMode="System"`; aliases compartidos `Modern.xaml` y consumidores del shell/Home requieren investigación causal. Las capturas muestran retención de superficies; **no demuestran que StaticResource sea la causa**. No se modificaron recursos ni se introdujo ThemeManager.

## Comparación V4 por región

Se observaron las cuatro referencias aprobadas y sus cuatro capturas correspondientes; cinco hashes de referencias coinciden con la especificación. No se exige coincidencia del contenido PDF ilustrativo.

| Región | Diferencia observada | Clasificación |
| --- | --- | --- |
| Marca y tabs | Cabecera muestra SG PDF sin logo grande aprobado; + queda en extremo derecho; selección subrayada inferior frente al tratamiento superior del mockup. Icono PDF correcto. | P2, lote B después de gate A |
| Rail | Falta DOCUMENTO; selección rectangular/franja frente a pastilla; mayor espaciado vertical y scroll para herramientas inferiores; glyphs y arte vigentes conservados. | P2 estructura; contraste en 002/003 |
| Toggle/drawer | Toggle diminuto en borde; faltan encabezado Navegación/cierre perceptible y agrupación de tabs; miniaturas grandes con selección rectangular. | P2 estructura; números Dark P1 |
| Canvas | Light usa gris neutro; faltan rótulo Vista del documento y profundidad/sombra del mockup. Dark separa papel del canvas, con paleta distinta. | P2; páginas blancas sintéticas válidas |
| Footer | Más alto y texto source.pdf/página persiste al volver a Home; referencia propone estado discreto. | P2 |
| Menú/comandos | Geometría y ajuste de zoom difieren de referencia. | DEFERRED / REQUIRES_APPROVAL, bloque 3 |

## Evidencia local íntegra

Carpeta **`C:\Users\felip\AppData\Local\Temp\SGPDF_V4_LOTE_A_20261018_142349`**. Cada PNG tiene sidecar con SHA de app, timestamp -05:00, tema, DPI y hash. Manifest `SHA256SUMS.txt` al cierre. Capturas crudas solo locales por posibles bordes de escritorio; no se subieron PNG, fixtures ni ZIP.

| PNG | SHA-256 |
| --- | --- |
| V01_LIGHT_CLOSED.png | `fb8473428325855a4a179f18de1f383f1231a2872c57e5b038f544d970de3bf6` |
| V02_LIGHT_OPEN.png | `f7f48dc08f122792b0c7a983f3d06119e6ff398d183a9fbcaf153b2f7d7378a8` |
| V03_DARK_CLOSED_FAIL.png | `749218b1e4e9aa7f87e9437fff01069fba6849ec74def31af6d7eb796160e07e` |
| V04_DARK_OPEN_FAIL.png | `b3dbd04ad3dd994d75efe9f113fd179d7f1441306e599caeb901fcf773555c04` |
| W09_A_BOOKMARKS_RESTORED.png | `1529511b501e7b1159b66edf9d5a0421645ca82eb80b33e1caa59c61daa09895` |
| W09_B_CLOSED.png | `7f3b09ea35047d2f3e42cf20330988df493a7b16dd79e7089de689ffad14c9c2` |

## NEXT

Entregar al autor del código **QA-V4-002/003 como único bloque correctivo del lote A**, sujeto a asignación expresa FIX_AND_VALIDATE; repetir focal de tema/contraste después del cambio. Lote B aún sin gate A; sin bloque 3, R5/T9.2/F14–F18 ni merge. No declarar GREEN visual.
