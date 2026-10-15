# SG PDF — UI4 V08 GREEN checkpoint

**Fecha:** 2026-10-15  
**Estado:** CHECKPOINT / STOP  
**Regla:** no continuar trabajo productivo desde este punto hasta nueva instrucción del usuario.

## Firma UI4 V10–V12

- Fix causal remoto congelado en `feat/fix-ui4-signature-controls`.
- Head remoto: `8c4a0f2619ca0a4835f6160ce4958d2d1b981435`.
- Causa demostrada: `ScrollViewer` de AcroForm visible/transparente interceptaba hit-testing sobre controles de Firma.
- Suite verificada previamente: `1215/1215 PASS`.
- Sin merge a `main`.

## V08 Labels — causa raíz

Síntoma Windows:
- `MediaKind = Thermal`
- `_labelLayoutPlan != null`
- `_isBusy = false`
- Export/Print seguían disabled.

Causa demostrada:
- `CommitLoadedZpl()` refrescaba propiedades mientras `_isBusy` aún era `true`;
- `SetBusy(false)` no refrescaba la UI ZPL;
- los botones conservaban el estado disabled calculado durante busy.

## TDD V08

Rama: `feat/fix-ui4-v08-label-actions`

RED:
`busy=true → CommitLoadedZpl(valid Thermal) → busy=false → Export/Print deben habilitarse`

GREEN mínimo head:
`b4f19fcd5f2854c9025e60385e3b7819bd798a29`

Cambio productivo único en `MainWindow.SetBusy(bool)`:

```csharp
UpdateViewerControlsUi();
if (_zplDocument is not null)
    UpdateLabelPropertiesUi();
```

Propiedades:
- 2 líneas productivas;
- condicionado a workspace ZPL activo;
- no fuerza `IsEnabled`;
- reutiliza owner/UI existente;
- sin arquitectura, helper, paquete ni flujo nuevo;
- cumple KISS, Ruido y Anti-parche.

Diff V08 contra el fix de Firma:
- `src/SGPdf.App/MainWindow.xaml.cs`: +2 líneas;
- `tests/SGPdf.App.Tests/MainWindowZplBusyCompletionUiTests.cs`: test causal nuevo.

## CI V08 GREEN

GitHub Actions run: `38681417442`  
Job Windows: `116246600509`  
PR merge-ref probado: `2bff61420498287b1025c3346c705fa36d01cbf4`

Resultado:
- hygiene PASS;
- Labelize/qpdf/Tesseract staging PASS;
- locked restore PASS;
- Release build PASS, 0 errors;
- tests **1216/1216 PASS**;
- warnings PKCS `NU1902/NU1903`: conocidos/preexistentes;
- Node.js 20 warning: tooling externo, no producto.

Nota: el CI corre sobre merge-ref del PR, no se etiqueta como exact-head del branch.

## Pendientes al reanudar

1. Reverificar V08 en Windows real.
2. Completar UI4 manual pendiente: V04, V05, V09 visual, V13 y V14 completo.
3. Si no hay P0/P1: cerrar UI4 GREEN.
4. Solo después iniciar F13 A+B.
5. No mergear a `main` sin aprobación explícita.

## Estado de seguridad

- `main`: no tocado.
- PKCS: no tocado.
- F13/F14/F15: no implementados en este bloque.
- Sin merge.
- Sin force-push.
- STOP aquí hasta que el usuario diga `continua`.
