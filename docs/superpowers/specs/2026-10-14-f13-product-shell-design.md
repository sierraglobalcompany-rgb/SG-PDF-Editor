# F13 Product Shell / Dashboard Design

**Date:** 2026-10-14
**Status:** design ready; implementation blocked until UI-4 P1 gate is GREEN
**Parent spec:** `docs/product/2026-10-14-local-suite-expansion.md`

## Problem

SG PDF already contains many useful F0–F12 capabilities, but the current shell exposes them through a mix of menus, dynamically inserted mode buttons, contextual panels and separate tool entry points. Real UI-4 use showed that the app is visually improved but still not sufficiently discoverable, especially Edit/Save and Sign flows.

The user wants:
- a home/dashboard closer to the discoverability of a PDF utility suite;
- an open-document experience inspired by the clarity of Acrobat's left-side tools;
- Read/Edit/etc. moved from the centered mode strip to the left;
- icons on primary actions;
- a stronger professional visual hierarchy;
- the official app icon applied throughout Windows.

F13 reorganizes the shell only. It does not rewrite PDF engines or business workflows.

## Design principles

1. One primary navigation system.
2. Existing business handlers/workspaces remain owners of behavior.
3. Contextual controls appear only when relevant.
4. Unsupported actions explain why they are unavailable.
5. Native WPF + current `ThemeMode="System"` + `Themes/Modern.xaml`; no third-party UI framework.
6. No plugin system or generalized mode manager.
7. Preserve keyboard shortcuts and menu fallbacks until replacement coverage is proven.
8. Light/dark, small-screen and scaled-screen behavior are first-class acceptance gates.

## Shell states

### State A — Home

When no PDF/ZPL document is active, the central application surface is the dashboard.

The Home state contains:
- SG PDF identity/app icon;
- title + concise local/offline message;
- primary `Abrir PDF` action;
- secondary `Abrir ZPL` action;
- drag/drop zone;
- `Recientes` using the existing `RecentPdfStore`;
- tool-card groups generated from a small static catalog.

The current plain `EmptyStateText` is replaced by the Home experience, not supplemented by a second independent window.

### State B — PDF Workspace

When a PDF is active:

```text
┌──────────────────────────────────────────────────────────────────────┐
│ Compact document top bar                                            │
├────────────┬───────────────────┬──────────────────────┬─────────────┤
│ Tool rail  │ Left context      │ PDF canvas           │ Properties  │
│ Read       │ pages/bookmarks/  │                      │ contextual  │
│ Edit       │ search as needed  │                      │             │
│ Comment    │                   │                      │             │
│ Sign       │                   │                      │             │
│ Organize   │                   │                      │             │
│ ...        │                   │                      │             │
├────────────┴───────────────────┴──────────────────────┴─────────────┤
│ Status                                                               │
└──────────────────────────────────────────────────────────────────────┘
```

### State C — ZPL Workspace

Use the same shell frame but show only tool actions relevant to ZPL/labels. Do not pretend PDF-only commands are available.

## Left primary tool rail

### Baseline width

Use a compact fixed rail around `92–104 px`, sufficient for icon + one-word label. It must not consume the existing pages/thumbnails context panel.

### Core items

Order:
1. Leer
2. Editar
3. Comentar
4. Firmar
5. Organizar
6. Formularios
7. OCR
8. Censurar
9. Comparar
10. Convertir
11. Proteger
12. Más

Rules:
- icon above/beside short text;
- selected state clearly visible;
- disabled state visually distinct and tooltip-explained where possible;
- no separate centered `LEER / FIRMAR / EDITAR` mode strip after the new rail proves equivalent routing;
- tool rail actions call existing handlers/workspaces instead of duplicating them.

## Left contextual pane

The current 240 px left column is valuable for document navigation and should be preserved as a context pane rather than being replaced by tool buttons.

Examples:
- Read → pages/thumbnails/bookmarks/search;
- Organize → page thumbnails/selection;
- Compare → document/difference navigation if later implemented;
- Home → hidden.

F13 should not build all these panels. It only establishes the shell slots and preserves current reader thumbnail behavior.

## Top bar

Keep it compact. First F13 baseline:
- current filename/document identity;
- Open;
- Save As only when the active mode has a real save command;
- Undo/Redo only when the active workspace truly supports them;
- Search;
- Print;
- zoom/navigation can remain in the existing navigation bar until a later bounded slice proves a cleaner consolidation.

The classic menu may remain as a fallback during F13/F14. Do not delete it in the same task that introduces the new navigation.

## Right properties pane

Reuse the current 320 px right properties host. F13 establishes a stable contextual frame but does not rewrite feature editors.

Rules:
- show only properties relevant to current tool/selection;
- provide a concise placeholder when there is no selection;
- never show controls that imply a capability the engine does not support.

## Home tool catalog

### Minimal descriptor

Use a small immutable model, not a plugin architecture.

Proposed fields:
- `Id` — stable string id;
- `Title` — Spanish visible label;
- `Category` — small enum/string grouping;
- `Description` — one concise line;
- `IconKey` — key to an existing local vector/glyph resource;
- `Availability` — simple state/identity allowing Home to show current vs future capability;
- `ActionId` — simple routing identity resolved by the host.

Avoid delegates stored in global static descriptors if they make testing/lifetime harder; route through the existing window owner.

## Icon strategy

Do not add a UI/icon package.

Preferred order:
1. reusable native vector `Geometry` resources in the existing `Themes/Modern.xaml` where practical;
2. Windows/Fluent system glyphs only when stable and visually adequate;
3. small custom SVG-like `Path` geometry authored for SG PDF if needed.

Do not use Adobe/iLovePDF proprietary icon assets.

## Official app icon

Separate from in-app tool icons.

Requirements:
- actual user-approved source image must be available;
- generate multi-resolution `.ico` from that source only;
- wire MSBuild/project icon + WPF window icon where needed;
- verify `.exe`, title bar/window, Alt+Tab and taskbar on Windows;
- later reuse exact same identity in installer/shortcut.

If source image is not available, F13.4 remains blocked; do not synthesize a replacement.

## Existing code reuse map

- `MainWindow.xaml` — current shell slots and main canvas.
- `Themes/Modern.xaml` — current visual resources; extend instead of adding a UI framework.
- `MainWindow.Sign.cs` — existing dynamic Read/Sign mode strip and sign workspace.
- `MainWindow.Edit.cs` — existing Edit workspace/button/save flow.
- `MainWindow.Organize*.cs` — existing organize mode/actions.
- `MainWindow.Comments*.cs` — existing comment mode.
- `MainWindow.Reader*.cs` — reader, thumbnails, search, shortcuts.
- `MainWindow.ReaderShortcuts.cs` + `Features/Reader/RecentPdfStore.cs` — reuse recent files; do not create another recent-store implementation.
- `MainWindow.Forms.cs`, `Ocr.cs`, `Utilities.cs`, `Compare.cs`, `Conversions.cs`, `Batch.cs`, `QrCode.cs` — route existing capabilities in F14.

## Transition strategy

### Step 1 — Catalog + Home
Build Home without altering document-mode routing. Existing menu and mode strip remain intact.

### Step 2 — Tool rail shell
Introduce left rail but initially wire only the already-safe modes. Keep old mode strip during automated equivalence tests.

### Step 3 — Equivalence gate
Prove new rail invokes the same existing owners, dirty-state guards and lifecycle transitions.

### Step 4 — Remove redundant mode strip
Only after equivalence + real Windows QA. Deletion must reduce code/noise rather than leave two navigation systems.

### Step 5 — App icon
Materialize only when approved source is present.

## Accessibility / responsive requirements

- keyboard focus visible;
- Tab/Shift+Tab predictable;
- tool rail buttons expose accessible names through visible text/content;
- disabled actions remain understandable;
- no critical horizontal clipping at 1366×768 @100%;
- one real scaled profile must pass;
- light and dark themes must pass;
- Home cards wrap/reflow rather than requiring horizontal scrolling.

## Non-goals for F13

- Edit V3 behavior redesign;
- new PDF utilities;
- Compare V2;
- form designer;
- installer/publish;
- backend/cloud/account;
- UI framework migration;
- arbitrary plugin architecture;
- deleting all classic menus immediately.

## F13 acceptance

F13 is complete only when:
- Home exists and uses current recent/open flows;
- left tool rail replaces the legacy centered global mode strip after equivalence proof;
- document canvas and right properties remain functional;
- existing mode handlers/guards are not duplicated;
- automated full suite + exact-head CI are GREEN;
- real Windows light/dark, keyboard, 1366×768 and scaled profile are GREEN;
- no P0/P1 regression;
- official icon is either verified from approved source or explicitly recorded as source-blocked (cannot be silently substituted).
