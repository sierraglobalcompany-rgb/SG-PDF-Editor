# SG PDF F13 T9.1 — R4-D V4 / microbloque 2 — Tool rail CODE PUSHED

**2026-10-17 · rama:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
**Estado:** `CODE_PUSHED / AUTOMATED_TESTS_NOT_RUN_HERE / EXACT_HEAD_CI_NOT_VERIFIED / WINDOWS_QA_NOT_RUN`.  
**R4-C:** `QA_PENDING`; **R5/T9.2:** `NOT STARTED`; **main:** no merge.

## Contexto

Los cuatro mockups V4 Light/Dark y drawer open/closed están aprobados por el usuario. Autoridad: [SGPDF V4](../design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md). Assets productivos canónicos: `SGPdf.App.ico`, `SGPdf.Document.png/.ico`, `Assets/HomeTools/*.png`; glyphs semánticos `read/comment/forms` siguen aprobados.

## Cambios publicados en microbloque 2

- Test de contrato WPF style/source: `ca0d90592bbf94b81d8fdda25ddc2e98a4d31c7b`, `tests/SGPdf.App.Tests/DocumentShellRealignmentContractTests.cs`.
- Ajuste owner del rail: `2055f703c99c0643e60143fd31941dc62e926f0c`, `src/SGPdf.App/MainWindow.Shell.cs`: elimina selección inicial hardcodeada de `read`; `UpdateDocumentToolRailSelection()` queda único responsable; etiquetas 72 DIP uniformes, margen vertical uniforme.
- Estilo V4: `da060c543b7c0b45dd1112b8414c495c4ce69460`, `src/SGPdf.App/Themes/Modern.xaml`: filas 60 DIP, centrado, estado disabled atenuado, selected background y borde accent.
- **Sin cambios a las acciones/IDs, ToolCatalog, archivos de iconos ni lógica de navegación/documentos.**

### Comprobación efectuada

Lectura de código en rama GitHub y búsqueda de las claves, estilos y rutas esperadas: **coherencia estática PASS**. No se ejecutó Windows, .NET, test focal/full, smoke ni GitHub Actions para estos commits. El test nuevo tiene intención de contrato, **no demuestra TDD RED ejecutado**.

### Gate obligatorio antes del siguiente microbloque

1. En Windows x64, desde rama limpia: `dotnet restore SGPdf.slnx --locked-mode` → `dotnet build SGPdf.slnx -c Release --no-restore` → `dotnet test SGPdf.slnx -c Release --no-build`.
2. `./tools/smoke-startup.ps1 -Executable "./src/SGPdf.App/bin/Release/net10.0-windows/SGPdf.App.exe" -TimeoutSeconds 30`.
3. Capturas reales comparadas con 4 mockups: rail en Light/Dark, 125% scaling, min-width 960 DIP, clic de herramienta activa, deshabilitadas, volver Leer, vistas Home/ZPL y botones con teclado/foco.
4. Verificar exact-head CI **posterior al último commit de documentación**. Guardar URL + SHA y actualizar checkpoint.
5. Si falla, arreglo causal y repetir gates. **No declarar GREEN** por solo compilar/contrato.
6. PNGs V4 de referencia aún pendientes de sincronizar como binarios en GitHub con SHA-256 comprobado según spec.

## Continuación

R4-D microbloque 3 (command bar) **NO STARTED**; aplazar hasta el gate de tabs+rail o autorización explícita para acumulación controlada. No ejecutar R5, no merge.

## Gate Windows y sincronización V4 — 2026-10-17

- HEAD inicial auditado: `13aaa651c012843f11c09b24b5c6c2c3bd8dea02`; CI inicial [39075875401](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39075875401) FAIL por expectativa antigua de selección inicial en Home.
- Reproducción focal: 1 FAIL / 4 PASS. Cambio `e4220b6`: solo test; Home no exige selección ficticia y el owner existente selecciona un único modo. Ningún cambio productivo, segundo registro o UI paralela añadido en este gate.
- Local: restore locked PASS, build 0 warnings / 0 errors, focal 13 PASS / 1 SKIP, full 1288 PASS / 0 FAIL / 1 SKIP / 1289 total, startup smoke 30s PASS. Skip de R5 conservado.
- QA real: Home sin rail accidental y un PDF sintético abierto mediante Recientes; pestaña activa, título corto, árbol visual sin contenido de tipo debajo del tab y rail observados en Light. La herramienta fue detenida por Escape físico del usuario antes de completar el segundo PDF. No se emitió evidencia visual completa ni capturas permanentes.
- NOT_RUN: 2+ PDFs, cierre activo/inactivo, dirty guard, título largo/ellipsis, confirmación visual del icono de tab, todos los modos/disabled/scroll/foco/teclado, ZPL, Dark, 125% y mínimo 960x640. Comparación contra V4 pendiente. P0/P1/P2 visuales no clasificados por cobertura incompleta.
- Cinco PNG extraídos del paquete completo recibido en Descargas; SHA-256 de los cinco coincide con la tabla de [autoridad V4](../design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md). Incorporados en `docs/design/references/sgpdf-v4/`; ZIP completo local en raíz, no versionado. Segundo ZIP no localizado y no necesario para recuperar estos cinco originales.
- Evidencia automatizada local: `C:\Users\felip\AppData\Local\Temp\SGPDF_R4D_20261017\red.log` y `verification.log`; smoke en `C:\Users\felip\AppData\Local\Temp\SGPdf-startup-c80fff9847044ca4a6ec988816408544`.
- Estado: **R4-C/R4-D QA_PENDING**. No GREEN visual, no bloque 3, no R5/T9.2, no merge. El CI del HEAD final se comprueba después del push; no confundirlo con el CI inicial fallido ni con QA manual.

## Verificación posterior del mismo HEAD: CI y referencias V4

- Commit `0427cfff84e500884759d5287fd53054738c2fb9` está en el HEAD de la rama y CI exact-head [39076821081](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39076821081) SUCCESS (job `117525580062`): build **0 Warning(s), 0 Error(s)**; startup `PASS: process survived 30 seconds`; full suite **1.288 PASS, 0 FAIL, 1 SKIP**.
- Directorio `docs/design/references/sgpdf-v4/` incluye cinco PNG (el ZIP original no se versiona), con SHA-256 declarado/registrado en la spec. Los PNG son mockups, no screenshots de QA.
- Ejecución Windows parcial previa: Light Home y un PDF sintético; no terminó 2+ PDFs ni Dark/125%; cobertura insuficiente. **R4-C/R4-D QA_PENDING**.
- Próximo único gate: [Matriz W01–W15 y V01–V04](../qa/2026-10-17-r4c-r4d-v4-windows-matrix.md). No bloque 3, R5, T9.2 ni merge. Este checkpoint actualiza documentación; el CI del nuevo SHA documental se verificará por separado.

### R4-D / QA automatizado adicional — 2026-10-17

- Test de contrato del `TabControl` header-only reforzado con XAML semántico en `f3aecace66f1b4a039487f3a864d8417141b2822` (no `ContentPresenter` de workspace debajo de las tabs).
- Test STA real de pestaña seleccionada en apertura PDF contra `ActiveDocument` en `e8001a0ea354a22f5b9868d1c792148e92d11e49`.
- Exact-head CI de `e8001a0`: [39080291059](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39080291059), SUCCESS: 1.288 PASS, 0 FAIL, 1 SKIP; 0 warnings/0 errors; startup 30 s PASS.
- **No prueba visual**: aún sin cuatro capturas WPF reales, Windows 125% ni cobertura completa W01–W15/T01–T08. R4-C/R4-D permanecen `QA_PENDING`. No abrir bloque 3/R5/T9.2 ni merge.
