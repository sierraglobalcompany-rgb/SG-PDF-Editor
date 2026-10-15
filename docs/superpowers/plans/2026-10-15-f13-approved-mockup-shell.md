# F13 Approved Mockup Shell Implementation Plan

> **Execution:** ChatGPT/native task-by-task after Gate 0 is GREEN. Use Codex only for real Windows GUI/equivalence evidence or a defect that materially reproduces only on the user's PC.

**Estado:** A+B APROBADO / PRE-FLIGHT AUDITADO Y CORREGIDO / IMPLEMENTACIÓN BLOQUEADA POR GATE 0  
**Goal:** llevar SG PDF Editor desde el shell actual al mockup aprobado de Dashboard + Documento abierto, reutilizando los owners funcionales existentes y sin arquitectura paralela.  
**Architecture:** WPF actual + catálogo estático pequeño + `HomeDashboard` + host Home/PDF/ZPL + rail izquierdo. La migración permanece aditiva hasta probar equivalencia; después se elimina navegación redundante. PDFium, writers, workspaces y feature owners no se reemplazan.  
**Tech:** C# / .NET 10 / WPF / `Themes/Modern.xaml` / `ThemeMode="System"`; sin framework UI ni paquete de iconos nuevo.  
**Spec:** `docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`  
**Mockup:** `docs/design/2026-10-15-f13-approved-mockup-reference.md`

## 0. Correcciones del preflight 2026-10-15

Este plan conserva el A+B aprobado, pero corrige cuatro huecos detectados al contrastarlo contra el código real `abd52d05279e3e656723ad4ff5a21d36de91508e`:

1. **F13/F14 no duplican routing.** F13 solo enruta la navegación nuclear necesaria para reemplazar la franja global actual: `Leer`, `Editar`, `Comentar`, `Firmar`, `Organizar`, además de abrir PDF/ZPL y recientes. F14 es el único owner del productizado/routing completo de Forms/OCR/Censura/Comparar/Convertir/Proteger/Batch/QR/etc.
2. **Eventos válidos.** No usar `EventHandler<string>`; `EventHandler<T>` exige `T : EventArgs`. Para el hosting local se permiten `event Action?` / `event Action<string>?` o un `EventArgs` mínimo si los tests lo justifican. Sin messaging framework.
3. **F13 no cambia la semántica de Edit Save.** El baseline actual deja `Guardar como...` visible/habilitado cuando Edit está activo, hay workspace y no está materializando. La decisión `no dirty → disabled / dirty → enabled` pertenece a F15 Edit V3.
4. **La franja legacy tiene dependencias de inicialización.** `Sign` crea el `modePanel`; `Organize`, `Edit` y `Comments` llaman `FindModePanel()` durante `Loaded`; `EditSaveAsButton` es contextual aunque hoy viva dentro de esa franja. Task 7 debe desacoplar esas dependencias antes de borrar la franja.

## 1. Leyes y límites

- **Gate 0:** no escribir código F13 mientras UI-4 siga `FAIL-P1`.
- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → A→B`.
- **RUIDO:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **ANTI-PARCHE:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.
- `main` read-only; no merge sin aprobación explícita.
- Reusar `RecentPdfStore`, `PdfScrollViewer`, `PdfImage`, file-open flows, handlers/workspaces actuales y writers transaccionales actuales.
- No segundo motor PDF/writer, backend, cloud obligatorio, IA, login, publicidad, plugin framework, event bus, navegación genérica, DI rewrite ni migración de UI framework.
- No paquete productivo nuevo para F13.
- No eliminar menú clásico ni franja legacy hasta equivalencia demostrada.
- F13 no implementa Edit V3, Compare V2, nuevos conversores ni forms designer.
- `PDF a PNG` es la etiqueta honesta; JPG queda F16.
- `Imagen a PDF` solo puede aparecer deshabilitado/`Próximamente` antes de F16.
- El `SG` rojo del mockup no es autorización para inventar el icono oficial.

## 2. Boundary F13 ↔ F14

### F13 sí posee
- visual primitives del shell;
- Dashboard/Home;
- estado Home/PDF/ZPL;
- rail visual de documento;
- routing nuclear: `read`, `edit`, `comment`, `sign`, `organize`;
- `open-pdf`, `open-zpl`, recientes;
- contextual frame común;
- eliminación segura de la franja centrada legacy;
- visual QA del shell.

### F13 no posee
- routing completo de `forms`, `ocr`, `redact`, `compare`, `convert`, `protect`, `batch`, `qr`, firma criptográfica y demás utilidades;
- productizado completo de Home cards;
- cambios de negocio internos en owners.

Esos puntos son F14. Durante F13 pueden verse en el catálogo/rail para respetar el mockup, pero deben quedar **no ejecutables** hasta que F14 los conecte. La función original sigue disponible por sus entradas actuales cuando corresponda.

## 3. File / responsibility map

### Nuevos
- `src/SGPdf.App/Shell/ToolDescriptor.cs`
- `src/SGPdf.App/Shell/ToolCatalog.cs`
- `src/SGPdf.App/Shell/HomeDashboard.xaml`
- `src/SGPdf.App/Shell/HomeDashboard.xaml.cs`
- `src/SGPdf.App/MainWindow.Shell.cs`

### Existentes a extender mínimamente
- `src/SGPdf.App/Themes/Modern.xaml`
- `src/SGPdf.App/MainWindow.xaml`
- `src/SGPdf.App/MainWindow.xaml.cs` solo si el init no cabe limpiamente en `MainWindow.Shell.cs`
- `MainWindow.Sign.cs`, `MainWindow.Edit.cs`, `MainWindow.Organize.cs`, `MainWindow.Comments.cs` solo para routing/reubicación/desacople de UI; negocio permanece en su owner.
- `src/SGPdf.App/SGPdf.App.csproj` únicamente cuando exista fuente real aprobada del icono.

### Tests nuevos
- `ShellThemeContractTests.cs`
- `ToolCatalogTests.cs`
- `HomeDashboardContractTests.cs`
- `MainWindowShellStateTests.cs`
- `MainWindowToolRailTests.cs`
- `MainWindowContextShellTests.cs`

No reorganizar tests no relacionados.

---

## Gate 0 — UI-4 must be GREEN first

**F13 files:** ninguno.

- [ ] Auditar el `WINDOWS_P1_RESULT` ya solicitado.
- [ ] Resolver solo causa raíz demostrada.
- [ ] Focused cause tests PASS.
- [ ] Full Release suite PASS.
- [ ] Exact-head CI GREEN.
- [ ] Rerun Windows V04/V05/V08/V09/V10–V14.
- [ ] Cerrar UI-4 solo sin P0/P1.

**STOP:** si UI-4 continúa FAIL-P1, no iniciar Task 1.

---

## Task 1 — SG shell visual primitives

**Files:** `Themes/Modern.xaml`, `ShellThemeContractTests.cs`.

### Contract
Recursos reutilizables para:
- Home/card container;
- Home/document rail item;
- selected/hover/disabled rail state;
- primary/secondary shell button;
- icon tile/host;
- contextual toolbar;
- context panel/card/section;
- title/subtitle/muted text;
- solo las geometrías vectoriales necesarias.

### Steps
- [ ] RED: resource keys existen y no introducen light-only literals.
- [ ] RED: ningún asset Adobe/iLovePDF ni paquete de iconos.
- [ ] Implementación mínima usando SG theme resources y `DynamicResource` donde aplique runtime theme.
- [ ] Focused PASS.
- [ ] Full suite PASS.
- [ ] Commit: `feat(shell): add approved mockup visual primitives`.

No normalizar aquí todos los módulos legacy; solo crear primitives.

---

## Task 2 — Static display catalog, no dead executable cards

**Files:** `Shell/ToolDescriptor.cs`, `Shell/ToolCatalog.cs`, `ToolCatalogTests.cs`.

### Minimal descriptor

```csharp
internal sealed record ToolDescriptor(
    string Id,
    string Title,
    string Category,
    string Description,
    string IconKey,
    string? ActionId,
    bool RequiresPdf,
    string? DisabledReason = null);
```

`ActionId == null` significa **no ejecutable desde el shell en esta fase**. No significa que el engine histórico no exista.

### Lists
```csharp
internal static IReadOnlyList<ToolDescriptor> ToolCatalog.Home { get; }
internal static IReadOnlyList<ToolDescriptor> ToolCatalog.Document { get; }
```

### Document visual order
`read, edit, comment, sign, organize, forms, ocr, redact, compare, convert, protect`.

Solo estos son ejecutables en F13:
`read`, `edit`, `comment`, `sign`, `organize`.

Los demás quedan visibles/deshabilitados hasta F14, con razón corta cuando corresponda.

### Home
Mostrar el set visual aprobado, pero en F13:
- `Abrir PDF`, `Abrir ZPL`, recientes y el card de Etiquetas ZPL pueden resolver a owners F13 existentes;
- herramientas PDF que requieren documento están disabled en Home limpio;
- QR/Batch/otros owners independientes se conectan en F14;
- `Imagen a PDF` = disabled `Próximamente`;
- no afirmar JPG antes de F16.

### Steps
- [ ] RED: IDs únicos; orden esperado; title/description/icon no vacíos.
- [ ] RED: ningún `ActionId` desconocido ejecutable.
- [ ] RED: F13 solo tiene ActionIds ejecutables de su boundary.
- [ ] RED: `Image to PDF` y futuros sin ActionId.
- [ ] Implementar catálogo estático; sin reflection/delegates/MainWindow refs.
- [ ] Focused PASS → full suite PASS.
- [ ] Commit: `feat(shell): add phase-honest tool catalog`.

---

## Task 3 — Home Dashboard

**Files:** `HomeDashboard.xaml`, `.xaml.cs`, `HomeDashboardContractTests.cs`.

### Hosting contract

```csharp
internal IReadOnlyList<ToolDescriptor> Tools { get; set; }
internal event Action? OpenPdfRequested;
internal event Action? OpenZplRequested;
internal event Action<string>? RecentPdfRequested;
internal event Action<string>? ToolRequested;
```

Un `EventArgs` mínimo es aceptable si aparece una necesidad real; no usar bus/event aggregator.

Home emite intención; no abre PDF/ZPL por sí mismo.

### Visual
- rail 96 px;
- content padding 24 px;
- hero local/offline;
- `Abrir PDF` primario;
- `Abrir ZPL` secundario;
- `ItemsControl` + wrapping panel para cards;
- recientes reutilizando `RecentPdfStore` desde host;
- no horizontal card scrolling;
- Settings al fondo;
- no login/cloud/AI/ads.

### Steps
- [ ] RED structure/accessibility tests.
- [ ] RED disabled-card behavior: null ActionId nunca dispara ToolRequested.
- [ ] Implementar dashboard mínimo con recursos Task 1.
- [ ] Focused PASS → full suite PASS.
- [ ] Commit: `feat(shell): add approved local dashboard`.

Windows visual evidence queda Task 9.

---

## Task 4 — MainWindow shell state Home / PDF / ZPL

**Files:** `MainWindow.Shell.cs`, `MainWindow.xaml`, mínimo `MainWindow.xaml.cs`, `MainWindowShellStateTests.cs`.

### State
```csharp
private enum ShellWorkspaceKind { Home, Pdf, Zpl }
```

Un owner local, sin navigation service.

### Required behavior
- startup limpio → Home;
- PDF activo → PDF;
- ZPL activo → ZPL;
- failed/cancelled open preserva workspace previo;
- recent missing preserva documento y cleanup existente;
- busy no destruye/cambia workspace prematuramente.

### Steps
- [ ] RED shell-state tests.
- [ ] Insertar Home aditivamente; PDF/ZPL actuales permanecen.
- [ ] Wire Home OpenPdf/OpenZpl/Recent a owners existentes.
- [ ] Focused PASS.
- [ ] Full suite PASS.
- [ ] Exact-head CI GREEN.
- [ ] Commit: `feat(shell): host home pdf and zpl workspaces`.

---

## Task 5 — Additive document rail, core routing only

**Files:** `MainWindow.xaml`, `MainWindow.Shell.cs`, feature partial solo si hace falta un wrapper mínimo, `MainWindowToolRailTests.cs`.

### Explicit router
`private void HandleToolAction(string actionId);`

Async solo cuando el owner real lo exija. Switch explícito; sin registry.

### F13 route map
- `read` → owner Read existente;
- `edit` → `EditMode_Click` / `TryEnterEditModeAsync`;
- `comment` → `TryEnterCommentMode()`;
- `sign` → owner Sign existente;
- `organize` → `TryEnterOrganizeMode()`.

### Not routed until F14
`forms`, `ocr`, `redact`, `compare`, `convert`, `protect` y demás professional/utility actions.

### Steps
- [ ] RED rail structure: 96 px target, order, one selected, text visible, keyboard/accessibility.
- [ ] RED exact owner equivalence for five core modes + dirty guards.
- [ ] RED null/non-F13 action cannot execute.
- [ ] Add rail **sin borrar** centered legacy strip.
- [ ] Keep 240 px thumbnail/context area separate when owner needs it; center flexible; right panel 320 px.
- [ ] Implement explicit router.
- [ ] Focused PASS → full suite PASS → exact-head CI GREEN.
- [ ] Commit: `feat(shell): route core document tool rail`.

---

## Task 6 — Context toolbar + right frame, preserve owner semantics

**Files:** `MainWindow.xaml`, `MainWindow.Shell.cs`, `Modern.xaml` only if primitive genuinely missing, `MainWindowContextShellTests.cs`.

F13 owns el **frame**, no el negocio de Edit V3.

### Rules
- toolbar contextual separado de navegación global;
- right pane ~320 px y scrollable;
- owner decide qué acciones reales aparecen;
- Edit puede exponer `Seleccionar`, Undo/Redo cuando ya existan para la selección compatible y `Guardar como...`;
- **no cambiar en F13 la regla actual de habilitación de `EditSaveAsButton`;** F15 será quien aplique `dirty` gating;
- no fingir Undo/Redo universal para texto/redaction;
- no selection → placeholder corto;
- cambio de modo limpia solo controles contextuales del owner anterior.

### Theme debt visible in shell
Antes de cerrar Task 6, auditar las superficies dinámicas visibles que hoy usan light-only literals (`CommentToolbar`, `OrganizeSurface`/templates y cualquier surface equivalente). Sustituir solo colores/borders por recursos SG cuando sea necesario para Light/Dark; **sin mover negocio**.

### Steps
- [ ] RED context-frame tests.
- [ ] RED Edit Save As routes to the existing `TrySaveEditWorkspace` path and preserves baseline availability semantics.
- [ ] Add frame mínimo.
- [ ] Normalize only shell-visible hard-coded light surfaces that break theme contract.
- [ ] Focused PASS → full suite PASS.
- [ ] Commit: `feat(shell): add contextual document frame`.

---

## Task 7 — Prove equivalence, decouple legacy init, then delete centered strip

**Current dependency graph that must be respected:**
- `MainWindow.Sign.cs` creates the centered `modePanel` in `OnInitialized`.
- `MainWindow.Organize.cs` `InitializeOrganizeUi()` calls `FindModePanel()`.
- `MainWindow.Edit.cs` `InitializeEditUi()` calls `InitializeOrganizeUi()` + `FindModePanel()` and injects both `EditModeButton` and contextual `EditSaveAsButton`.
- `MainWindow.Comments.cs` `InitializeCommentUi()` calls `InitializeEditUi()` + `FindModePanel()` and injects `CommentModeButton`; its own comment toolbar is contextual and must remain.

### Required classification before deletion
Every legacy child/wiring = one of:
1. global navigation → replace by rail;
2. contextual command → move/preserve in context frame;
3. guard/state owner → keep wiring in owner.

### Compatibility rule
Avoid churn in existing tests/automation. When the legacy buttons disappear, preserve established names (`ReadModeButton`, `SignModeButton`, `EditModeButton`, `CommentModeButton`, `OrganizeModeButton`, `EditSaveAsButton`) by reusing/registering them on the replacement controls where technically safe instead of rewriting dozens of unrelated tests.

### Steps
- [ ] Characterize references to `_readModeButton`, `_signModeButton`, `_editModeButton`, `_organizeModeButton`, `_commentModeButton`, `FindModePanel()`.
- [ ] Add equivalence tests while old + new nav coexist.
- [ ] Move/preserve contextual `EditSaveAsButton` outside legacy modePanel.
- [ ] Refactor Organize/Edit/Comments init so contextual surfaces no longer require `FindModePanel()`.
- [ ] Bounded Windows smoke via Codex: Read/Edit/Sign/Organize/Comment + one dirty guard + Light/Dark selected state.
- [ ] Audit Windows evidence.
- [ ] Delete only proven redundant centered global mode controls/border.
- [ ] Delete `FindModePanel()` only when call count is zero and tests prove replacement.
- [ ] Preserve owner guards/handlers; no business rewrite.
- [ ] Focused PASS → full suite PASS → exact-head CI GREEN.
- [ ] Commit: `refactor(shell): remove proven redundant mode navigation`.

Classic menu may remain through F14.

---

## Task 8 — Official app icon, source-gated

**BLOCKER:** actual user-approved source image must be present in the conversation/repository in usable form.

- [ ] Verify actual source + provenance.
- [ ] If missing: mark `BLOCKED — approved icon source unavailable`; do not synthesize.
- [ ] If present: create multi-resolution `.ico` from that source only.
- [ ] Wire project/window identity minimally.
- [ ] Release build + full suite.
- [ ] Windows verify exe/window/Alt+Tab/taskbar.
- [ ] Commit: `feat(shell): add official sg pdf application icon`.

Installer later reuses the same asset.

---

## Task 9 — F13 final QA / closure

### Automated prerequisite
- full Release suite PASS;
- exact-head CI GREEN;
- no automated P0/P1.

### Windows evidence — Codex only
- Dashboard large;
- 1366×768 @100%;
- one scaled Windows profile;
- PDF open;
- Edit active;
- Light/Dark;
- Tab/Shift+Tab/focus;
- disabled states;
- right-panel scroll;
- no primary horizontal clipping;
- hierarchy compared with approved mockup.

### Close only when
- [ ] P0/P1 = 0 after rerun.
- [ ] centered navigation removed and owners remain single.
- [ ] no duplicate F14 routing snuck into F13.
- [ ] icon GREEN or explicitly source-blocked; final release may not silently omit it.
- [ ] `.planning/STATE.md` + `.planning/ROADMAP.md` updated.
- [ ] F14 recorded next for complete owner productization.
- [ ] F15 remains next Edit UX layer after shell/productization prerequisites.

Closure commit: `docs(state): close F13 approved product shell`.

---

## Verification cadence

Every code task:
`RED → minimal change → focused PASS → full Release suite → checkpoint`.

Tasks 4, 5, 7 and final closure also require exact-head CI at their stated checkpoints. Windows evidence is requested only when automation cannot establish GUI truth.

## Final self-review

- Home/dashboard: Tasks 1–4.
- 96 px rails/cards/theme/icon language: Tasks 1–3/5.
- Home/PDF/ZPL state: Task 4.
- core rail routing only: Task 5.
- context toolbar/right frame: Task 6.
- safe legacy-strip removal with init decoupling: Task 7.
- official icon: Task 8.
- Light/Dark/1366/scaling/keyboard/mockup comparison: Task 9.
- full capability routing is intentionally **F14**, not duplicated here.
- Edit dirty-save UX is intentionally **F15**, not pulled forward.

This is the executable F13 plan once Gate 0 is GREEN.