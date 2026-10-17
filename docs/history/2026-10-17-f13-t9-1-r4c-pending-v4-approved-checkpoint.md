# Checkpoint — V4 aprobada / R4-C QA pendiente / R4-D listo

Fecha: 2026-10-17.
Repo: sierraglobalcompany-rgb/SG-PDF-Editor.
Rama canónica: feat/f13-task9-t9-1-document-shell-realignment-20261017.
SHA de implementación previo: 5ff21171abb4b467e8796ba18f54e0d337ab2418.
Último CI anterior a este checkpoint: https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39069863859, SUCCESS 1286/0/1, build 0/0, smoke 30s.

**Decisión de usuario:** aprobados los cuatro mockups V4, la identidad oficial de aplicación/documentos/utilidades y el plan de alinear el shell real. Se solicita registrar continuidad en GitHub por posible fin de chat.

**Hallazgos no cerrados:** pestañas poco distinguibles; fuga visible de nombre de tipo PdfDocumentWorkspace; tool rail irregular y poco alineado; top bar/zoom/menú inconexos. Windows QA manual de R4-C no completada (ver checkpoint externo compartido por usuario). El GREEN automatizado no subsana defectos UX.

Referencia canónica: [V4 approved shell](../design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md).

**Bloque actual:** R4-D visual alignment — aprobado como objetivo, aún no implementado. Primer microbloque: tabs/document content: test RED contra fuga de tipo y activo no ambiguo; patch mínimo; QA; checkpoint. Después rail, top bar y drawer por unidades verificables. No saltar a R5.

**Limitación de evidencia:** los cinco PNG V4 aprobados fueron entregados en el chat, todavía pendientes de subir como binarios al repositorio (hashes en referencia V4). No decir que repositorio conserva visual completo hasta que existan blobs y correspondan las huellas.

**STOP:** no merge a main; R5/T9.2 no iniciados; no marcar R4-C/D GREEN sin screenshots Windows reales y exact-head CI final.

## Microbloque R4-D/Tabs — avance de código 2026-10-17

- RED de contrato añadida: `83c136501722222b798d198efd473b5a8225f060` (**ejecución RED no verificada en este entorno**).
- Nuevo estilo `SgDocumentTabItemStyle`/`SgDocumentTabsStyle` en `Themes/Modern.xaml`: `b5095b9db91cda52331bed3e8609f50525d8da15`.
- UI owner `MainWindow.Documents.cs`: `14065f7608adaa13d75769236fba9f9e067e5132`; tab strip header-only, icono oficial `Assets/SGPdf.Document.png`, estilo seleccionado semántico; no se cambió `ActiveDocument` ni guard dirty.
- **Estado: CODE_PUSHED / CI_PENDING / WINDOWS_QA_NOT_RUN**. No declarar GREEN, R4-C sigue QA_PENDING.
- Próxima verificación: `dotnet restore SGPdf.slnx --locked-mode`; `dotnet build SGPdf.slnx -c Release --no-restore`; `dotnet test SGPdf.slnx -c Release --no-build`; `./tools/smoke-startup.ps1 -Executable ./src/SGPdf.App/bin/Release/net10.0-windows/SGPdf.App.exe -TimeoutSeconds 30`; pruebas reales Light/Dark tabs 2+ PDF, close dirty, tab selected, 125% scaling.
- Si falla compilación/CI, reparar solo causa del dueño `MainWindow.Documents.cs` / `Themes/Modern.xaml`, volver a ejecutar todo y registrar nuevo SHA/CI.
