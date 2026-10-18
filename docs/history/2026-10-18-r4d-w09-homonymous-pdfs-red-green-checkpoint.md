# SG PDF — R4-C/R4-D V4: W09 drawer homónimo RED→GREEN, QA Windows pendiente

**18 de octubre de 2026.** Rama `feat/f13-task9-t9-1-document-shell-realignment-20261017`. **Estado: W09 CODE_FIXED / CI_GREEN / WINDOWS_RETEST_REQUIRED; R4-C/R4-D QA_PENDING.** No bloque 3, R5/T9.2, F14–F18 ni merge.

## Evidencia de entrada íntegra

- ZIP `SGPDF_V4_POSTFIX_QA_20261018_085446.zip` (proporcionado por usuario), CRC OK. Verificados 40 SHA-256 de los archivos listados por `SHA256SUMS.txt` sin discrepancias. Las capturas no se versionan por posible información de escritorio.
- App ensayada `fc662c8`; checkpoint de Codex `68a27c44a64b476b32bc47368e8262a7e14230cd`; [CI 39151236094](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39151236094) SUCCESS (1.289 PASS, 0 FAIL, 1 SKIP).
- V01 icono PDF reparado: PASS; V02 parcial; V03/V04 FAIL por Dark. W01–W15 8 PASS/2 FAIL/4 PARTIAL/1 NOT_RUN. T01–T08 5 PASS/1 FAIL/2 PARTIAL. P0 observados 0, sin generalizar ausencia.
- 3 familias P1: QA-V4-002 contraste Dark, QA-V4-003 Light↔Dark caliente, QA-V4-004 drawer entre PDF homónimos. El primer smoke local terminó antes de 30 segundos con ExitCode 0; repetición PASS. No ocultar esa incidencia no diagnosticada.

## W09 — reproducción causal y fix

- **RED válido:** test WPF STA `SameNamedPdfs_PreserveIndependentReaderDrawerVisibility` genera dos PDF reales en rutas distintas con mismo basename `sample.pdf`; A drawer abierto/B drawer cerrado/alternancia A↔B. Test en `245ca412677c9eafb144d68d7961e5c6546ba0f3`, harness corregido `9eb73a67849fe0688d058d3e675aaf5583a35d4e`. [CI RED 39152982080](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39152982080) compiló, y el test falló en B: **Expected Collapsed / Actual Visible**. El primer commit de test tuvo un error de llamada a helper (`CS0411`) y no cuenta como RED causal.
- **Causa confirmada:** `MainWindow.Shell.cs` sincronizaba mediante `OnPropertyChanged` del `Window.Title`. Con dos PDF homónimos, el título no cambia aunque `ActiveDocument` sí, y el drawer se queda con la visibilidad anterior; bookmarks pueden cambiar de documento correctamente.
- **Fix mínimo:** `cc5d58d807be8bdac4a795773159c699f97b61b4`, `MainWindow.Documents.cs`: al cambiar PDF y limpiar estado ZPL, llamar `SyncShellWorkspaceFromActiveDocument()` (owner ya existente). **Sin nuevo estado, registro, servicio, arquitectura ni modificación de iconos**. El listener legado del título sigue existiendo para otras transiciones: revisar posible doble sincronización únicamente con evidencia, nunca como refactor especulativo.
- **GREEN:** [CI 39153249583](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39153249583) SUCCESS en `cc5d58d`: **1.290 PASS, 0 FAIL, 1 SKIP**, build 0 warnings/errors, smoke 30 s PASS.
- **W09 aún no es PASS Windows**: requiere abrir el EXE nuevo, alternar A/B con mismo basename, probar A abierto/B cerrado y luego A cerrado/B abierto, verificar que cada uno retenga su pestaña Páginas/Marcadores y no herede bookmarks ni visibilidad. Si no pasa, STOP_P1 y nueva evidencia.

## Próximos gates, sin saltar etapas

1. Codex Windows: retest acotado W09/T01 bajo el nuevo SHA, sin guardar originales; hacer captura real y hashes. Ejecutar build/smoke y entregar resultado `PASS/FAIL`, no declarar PASS por CI.
2. QA-V4-002 por microbloques causales: foreground de título TabItem Dark, número de miniaturas sobre papel blanco y fondo fijo de Organizar. Un owner, un test y CI por bloque. Preservar recursos/iconos aprobados.
3. QA-V4-003: diagnosticar recursos WPF `ThemeMode=System` y aliases `Modern.xaml` antes de alterar colores en cada vista.
4. Completar W05/W08/W13/W14/W15/T04/T07, conservar cuatro capturas reales y comparar contra mockups aprobados. P2 (marca, rail, toggle, lienzo, footer) después de P1. Command bar es bloque 3 **NO INICIADO**.

**Checkpoint documental:** este commit crea nuevo HEAD; comprobar CI exact-head de ese nuevo commit, separado del CI de código `cc5d58d`.
