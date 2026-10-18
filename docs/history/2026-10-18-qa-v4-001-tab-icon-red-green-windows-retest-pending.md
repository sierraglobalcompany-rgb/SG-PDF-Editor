# SG PDF · QA-V4-001 · Tab icon P1 — RED → GREEN de código; QA Windows pendiente

**Fecha:** 2026-10-18. **Proyecto:** F13 / Task9 / T9.1 / R4-C/R4-D.  
**Rama:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
**Estado:** `CODE_FIXED / CI_GREEN / WINDOWS_RETEST_REQUIRED / QA_PENDING`. No R5/T9.2, no bloque 3, no merge.

## Evidencia y diagnóstico

1. App WPF real `26db4b18a9cfe144be88775ac2f170e5939a53a0`: V01 Light cerrado, Windows 11 al 125%, icono oficial PDF ausente en tab `source.pdf`. P1 visual reproducido; [checkpoint original](2026-10-17-r4c-r4d-windows-qa-p1-tab-icon.md). No confundir screenshot con mockup ni subir capturas de escritorio sin privacidad.
2. El PNG oficial `src/SGPdf.App/Assets/SGPdf.Document.png` existe como `Resource` WPF y es correcto; SHA-256 `79221bc742b7d514a112222abf7936ad36713bf9861b7863c9ecc63dd541c46a` (dato comprobado en gate original).
3. Primer test `af1137ac9ae8b23dd336de4d2cddfca703fdae6d` dio fallo artificial: `FrameworkElementFactory must be in a sealed template`. **No se considera RED causal.**
4. Test corregido `ad69280517201d3a1804a82d15ca809179ea7708` usa pestaña **generada por WPF** con `ItemContainerGenerator`, `ApplyTemplate`, `Measure`, `Arrange`; [CI RED 39082039007](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39082039007) demuestra `Image.Source == null`. 1 fallo esperado, 1 SKIP; sin cambio productivo durante ese RED.
5. Fix `3f5d0c9b43c925ab5b740d69b7d8c5b17fb47077`: **una sola línea**, en `src/SGPdf.App/MainWindow.Documents.cs`, `BuildDocumentTabHeaderTemplate()`: URI relativa WPF `/SGPdf.App;component/Assets/SGPdf.Document.png` sustituida por `pack://application:,,,/SGPdf.App;component/Assets/SGPdf.Document.png` `UriKind.Absolute`. Mismo asset; un solo template/owner; no cambios de guard, selección, motor, arquitectura o iconos.
6. [CI GREEN 39082254808](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39082254808) exact-head de fix: **1.289 PASS, 0 FAIL, 1 SKIP, 1.290 total**; build **0 warnings, 0 errors**, smoke 30 s PASS. Se comprobó fuente no null, pack URI absoluta y bitmap decodificable en árbol WPF de pestaña.

## Gate abierto — Codex Windows

**No hay captura real posterior al fix, así que el P1 visual aún no puede cerrarse.** Solicitar preflight `git fetch --all --prune`, `git status --short` (ZIP local reconocido), SHA nuevo y build Release. Abrir mismo fixture sintético PDF A; capturar V01 Light/cerrado desde el EXE nuevo con PNG junto al título. Si icono aún falta, detener en `STOP_P1`, diagnosticar sin parches. Si pasa, continuar V02–V04 y W01–W15/T01–T08 en [matriz](../qa/2026-10-17-r4c-r4d-v4-windows-matrix.md). Mantener esquema PASS/FAIL/PARTIAL/NOT_RUN; conservar captura anterior y nuevos hashes separadamente. Verificar CI exact-head del **checkpoint final de documentación** antes de citarlo como el último commit.

**Reglas:** KISS / antirruido / antiparches; no simular QA Windows con CI, no avanzar al bloque 3, R5/T9.2 ni merge.
