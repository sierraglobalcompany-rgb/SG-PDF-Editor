# F13 / Task9 / T9.1-R2 — Top shell + document tabs design gate

**Date:** 2026-10-17  
**Branch:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
**Baseline entering R2:** `6981114ad3b6a79d85f4f2f63ec339c80117e4c4` — exact-head CI GREEN.

## Scope

R2 only realigns the **top document shell and real PDF tabs**. R3 rail, R4 Pages/Bookmarks drawer, R5 context menus and R6 image-selection defect stay out of this slice.

## Mandatory SG Design Gate

### 1. INTENT
A person with one or more PDFs open must immediately know which PDF is active, switch between them without losing pending work, close one safely, and add another PDF without navigating away from the document workspace.

### 2. HIERARCHY
1. Document identity/navigation: tab strip.
2. Global/discoverable commands: menu row.
3. Frequent reader commands: compact page/zoom/print controls for the active PDF.
4. Document content below.

Tabs are not decorative: they are the primary document-navigation surface.

### 3. DEFAULT VISIBILITY
When the PDF workspace is active:
- show one tab per `OpenDocuments` workspace;
- show active state clearly;
- show tab file name;
- show close `×` on each tab;
- show `+` to add/open another PDF;
- show compact top commands without consuming excessive vertical space.

Home must not gain a duplicate document toolbar merely because R2 exists.

### 4. CONDITIONAL DISCLOSURE
- document tabs/top reader controls are visible for PDF work;
- ZPL keeps its own relevant navigation state;
- Home remains task-oriented and must not become a duplicate editor shell;
- dirty confirmation appears only when closing a dirty document, using the existing R1B guard.

### 5. LANGUAGE
- file tab: actual `DisplayName`;
- `+` tooltip/accessibility name: `Abrir otro PDF`;
- close button tooltip/accessibility name: `Cerrar <archivo>` where practical;
- menu wording remains Spanish/Windows style: Archivo, Editar, Ver, Herramientas, Ayuda.

### 6. INTERACTION / ACCESSIBILITY
- selecting a tab calls the existing `ActivateDocumentAsync()` owner;
- `+` calls the existing Open PDF route;
- `×` calls `TryCloseDocumentWithGuard()`; no duplicate close logic;
- keyboard focus must remain visible;
- icon-only `+` / `×` require tooltip and AutomationProperties name;
- active tab must be understandable beyond color alone through standard selected-tab state.

### 7. VERIFY
R2 cannot close on compile alone. Required automated evidence:
- R2 contract GREEN;
- selecting a document tab is routed to `ActivateDocumentAsync()`;
- closing a tab is routed to `TryCloseDocumentWithGuard()`;
- `+` uses the existing PDF-open owner;
- full suite;
- startup smoke;
- exact-head CI.

Manual visual evidence remains pending for R7 unless performed earlier:
- Light/Dark;
- 125% Windows scaling;
- 2+ real PDF tabs;
- switching preserves R1B pending state;
- no clipping/overlap.

## KISS architecture

Reuse exactly:

```text
OpenDocuments
ActiveDocument
PdfDocumentWorkspace
ActivateDocumentAsync()
TryCloseDocumentWithGuard()
OpenPdf_Click / existing open route
```

Do **not** add a second document collection, tab-session cache, navigation framework, DI service, event bus, ModeManager, or custom window/document framework.

## Visual direction

The approved mockup remains direction authority: quiet desktop-productivity shell, compact hierarchy, tabs visually attached to the document workspace, restrained selected state, no oversized controls, no duplicated toolbars. R2 should improve structure without prematurely implementing R3–R6.
