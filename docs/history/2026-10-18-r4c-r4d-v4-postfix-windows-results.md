# R4-C/R4-D · Windows V4 posfix — icon PASS, otros P1 abiertos

**2026-10-18 · Solo QA · QA_PENDING / P1 OPEN**

Rama `feat/f13-task9-t9-1-document-shell-realignment-20261017`; app probada `fc662c8c9123c3f2e0ddbf685c3cf1e01d76a404` (fix `3f5d0c9` incluido). Preflight fast-forward desde `99102b7`, tracked limpio, ZIP V4 local conocido preservado. CI de entrada [39083688771](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39083688771) SUCCESS. **Ningún cambio de código, estilo, tests, iconos ni runtime en este gate.**

## Gates y entorno

- Restore locked PASS; build Release **0 warnings / 0 errors**.
- Primer smoke: FAIL, proceso terminó antes de 30s con ExitCode 0 y logs vacíos; causa no demostrada. Instancia visible posterior estable. Repetición `smoke-retry.log`: **30s PASS**, evidencia `SGPdf-startup-2c603eb8532f474981a8c6256f2e6edb`. No ocultar el intento fallido ni llamarlo crash probado.
- Windows 11 Home Single Language x64, build 26300; monitor 1920x1200, escala real **125%** consultada con `GetScaleFactorForMonitor`. Capturas 1500x950 físicos, ventana 1200x760 DIP. Comparación por jerarquía/contraste/proporciones, no igualdad de pixels con mockup conceptual 1600x950.
- Light ejecutado 08:56–09:02 -05:00; tras interrupción y continuación del usuario, Dark ejecutado 12:31–12:39 -05:00. Misma app/SHA. Tema inicial Light restaurado a 1/1; app QA cerrada limpiamente sin guardar fixtures.
- EXE: `C:\codex\SG PDF\src\SGPdf.App\bin\Release\net10.0-windows\SGPdf.App.exe`. SHA-256 del EXE/DLL, OS, tema y metadatos de cada captura en `environment.json` local.

## V01 independiente y cuatro vistas reales

**QA-V4-001 / icono original: PASS POSFIX VISUAL.** Mismo PDF A seguro (2 páginas y outlines), abierto por Recientes. Icono oficial PDF SG con lápiz aparece delante de `source.pdf`; cierre y borde activo visibles. La captura FAIL anterior de `26db4b1` no se sobrescribió. Esto no cierra la composición V4.

| Captura real | Resultado | SHA-256 |
|---|---|---|
| `R4D_V01_LIGHT_CLOSED_POSTFIX_REAL.png` | icon PASS; fidelidad global PARTIAL | `2624aeb6ad0e6f83427c6c6d8dd4381766a16ecd9eb6818deb4dd43b7a1092e1` |
| `R4D_V02_LIGHT_OPEN_REAL.png` | PARTIAL; drawer y miniaturas operativos | `3816cd8d9b9e3a1f2155f9f24261a55544732eb991c3eb2ff037ab1df225ee62` |
| `R4D_V03_DARK_CLOSED_REAL.png` | FAIL contraste del título de pestaña | `662380ef43b49b9935ab7da786d1833b4b57d21c0cd6ec220794104078bed0b7` |
| `R4D_V04_DARK_OPEN_REAL.png` | FAIL contraste de números de miniaturas | `6c6dedbde9d416c458fd56911396e4460d26c0d79e9f350d01c70ad717b39b46` |

Cuatro referencias aprobadas inspeccionadas contra estas vistas; página blanca del fixture no se interpreta como fallo de render.

## Resultados W/T

[Matriz canónica actualizada](../qa/2026-10-17-r4c-r4d-v4-windows-matrix.md), con observación/límite por fila.

- **PASS:** W01/W02/W03 navegación Light/W04/W06/W07/W10/W11; T01 funcional/T02/T05/T06 iconografía/T08.
- **FAIL:** W09 restauración visual entre archivos homónimos; W12 contraste; T03 contraste de tab Dark.
- **PARTIAL:** W05 (click navega, Enter probado ya en destino), W08 (alternancia sí, carga lenta pendiente no forzada), W13 (125% y scroll sí, mínimo 960x640 no alcanzado/verificado), W14 (Right/Enter/Space, no Tab-order íntegro), T04 (cinco modos, retorno directo con guard de modo no cubierto), T07 (fila observada; deuda bloque 3).
- **NOT_RUN:** W15, sin mecanismo seguro de inducir error nativo de outlines en UI sin tocar producto. PDF que no abre no sustituye esa prueba.
- A/B son `source.pdf` en carpetas temporales diferentes. A conserva Root→Child y Unsupported sin destino→Reachable child; clicks llevan a páginas 1/2; B muestra vacío honesto sin árbol viejo.
- Dirty guard real: rotación en memoria del fixture; No mantuvo documento/plan, Sí cerró y volvió Home. Cierre inactivo de fixture con título largo conservó B activo. Original no guardado.
- ZPL seguro `QA-V4.zpl` abierto por selector real, renderizó etiqueta 1/1 sin rail/tabs/drawer PDF. 11 herramientas accesibles por scroll, seis disabled; click en Proteger no ejecutó acción falsa.

## Hallazgos y propuesta mínima por owner — NO implementada

| ID / prioridad | Evidencia / reproducción | Owner y siguiente diagnóstico acotado |
|---|---|---|
| QA-V4-002 / P1 | V03: título negro sobre tab Dark. V04: números claros en miniaturas blancas. Organizar Dark: comandos claros sobre fondo RGB243/243/243 | `MainWindow.Documents.cs`/`Modern.xaml` para título; `ReaderThumbnails.cs` fija papel blanco y no define foreground de pageNumber; `Organize.cs:66` fija superficie clara. Corregir contraste por owner, con test causal y nueva QA; conservar papel e icono original |
| QA-V4-003 / P1 | Cambio Light→Dark vivo: Home claro con texto blanco; reiniciar Dark recupera superficies. Dark→Light vivo: superficies oscuras y texto negro | Recursos/brushes en `Themes/Modern.xaml` y owners de superficies. Hipótesis de recursos retenidos en cambio de tema, causa exacta NOT_CONFIRMED. Confirmar actualización de recursos antes de parches por pantalla |
| QA-V4-004 / P1 observado, causa por confirmar | W09: A open/B closed no restaura visibilidad al alternar; ambos basename `source.pdf`; bookmarks sí corresponden al PDF correcto | `ActivateDocumentAsync`/`ApplyReaderNavigationState`, `Shell.cs` sincroniza por cambio de Title, que es igual para archivos homónimos. Hipótesis: aplicación visual dependiente de Title. Repro causal con homónimos antes del fix; reutilizar owner actual, no crear estado paralelo |

**P0=0 observados**, cobertura parcial; no prueba exhaustiva de ausencia. Tres grupos P1 abiertos. No operaciones sobre datos privados ni cambios guardados.

Diferencias **P2 consolidadas** respecto a V4: (1) marca sin logo y `+` alejado de tabs; (2) rail sin DOCUMENTO/pastilla equivalente, iconos pequeños, ritmo/scroll y atenuación; (3) toggle pequeño sin affordance permanente y drawer sin cabecera/segmentado V4; (4) lienzo Light poco diferenciado del papel, sombra/rótulo ausentes; (5) toolbar/paginación/fit-width/imprimir: **solo deuda de bloque 3, NO autorizado**; (6) footer alto y status de rotación residual en Home tras cierre. Detalle/owners en `diffs-v4.md` local. No declarar coincidencia visual por pasar CI.

Orden mínimo propuesto para aprobación separada: contraste de tab → miniaturas → reproducción W09 → tema vivo → marca/tabs → rail → toggle/lienzo/footer; cada owner con su gate. Organizar requiere corrección de tema acotada, no nueva función. Command bar solo después de desbloqueo explícito.

## Evidencia y continuidad

Carpeta local exacta: `C:\Users\felip\AppData\Local\Temp\SGPDF_V4_POSTFIX_QA_20261018_085446`.

Contiene cuatro PNG principales, capturas auxiliares, JSON por captura con hora/tema/DPI/SHA, `environment.json`, `results.md` con cada W/T/V, `diffs-v4.md`, `build-smoke.log`, `smoke-retry.log` y `SHA256SUMS.txt`. Las capturas tienen pequeños bordes de escritorio; **no se versionan** por privacidad. No subidos fixtures/ZIP/logs masivos ni reemplazadas capturas con mockups.

**Resultado: R4-C/R4-D QA_PENDING / P1 OPEN.** V01/icon PASS posfix no autoriza cierre R4 ni bloque 3/R5/T9.2/F14–F18/merge. No solicitar cierre con estos fallos. Push solo documental; CI exact-head del commit final se verifica por separado y se reporta con su URL.
