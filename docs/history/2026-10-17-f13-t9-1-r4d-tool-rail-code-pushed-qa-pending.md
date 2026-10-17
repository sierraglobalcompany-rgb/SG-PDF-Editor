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
