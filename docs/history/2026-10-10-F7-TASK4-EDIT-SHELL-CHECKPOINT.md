# F7 — Task 4 General EDITAR Shell — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 4 — promover shell general del modo EDITAR  
**Estado:** **GREEN / PASS automatizado**  
**Rama:** `feat/f7-text-v1`  
**Base Task 4:** `0752efe2e84d6d79907878458e94726c58bbb727`  
**RED:** `cdaa2235e69028bba10e568ff9c46810dc599092`  
**Head funcional:** `b6818ec88a7da5d817c4f175e36ba45eda891212`  
**Head GREEN verificado:** `e664dd441448e2280be14b23400fa786cb83a6fa`

## RED

Gate: `EditModeShellNamingTests`.

Primer intento CI `38072922408`:

- build: 0 warnings / 0 errors;
- el gate Task 4 falló como se esperaba;
- dos tests F5 fallaron además por no poder cargar `pdfium.dll`, incidencia ajena al cambio.

Se repitió **el mismo SHA** sin cambiar código. Segundo intento:

- build: 0 warnings / 0 errors;
- tests: **692 PASS / 1 FAIL / 693 total**;
- único fallo: `EditModeShellNamingTests`, por ausencia de los nombres generales del shell.

## GREEN aplicado

Refactor mecánico y behavior-preserving:

- `MainWindow.EditImages.cs` → `MainWindow.Edit.cs`;
- `MainWindow.EditImages.Hardening.cs` → `MainWindow.Edit.Hardening.cs`;
- un único `EditLoadedHookRegistered` / `InitializeEditUi`;
- estado general `_editModeActive`, `_editMaterializing`, `_editModeButton`, `_editSaveAsButton`, `_editOverlayCanvas`;
- entry/save/reset y guard chain promovidos a nombres `Edit*`;
- controles internos `EditSaveAsButton` y `EditOverlayCanvas`;
- destino/preflight/warnings promovidos a nombres del modo EDITAR.

Permanecen específicos de imagen:

- `ImageEditWorkspace` y `_imageEditWorkspace`;
- selección, gestos, comandos, extracción y reemplazo de imagen;
- `_saveImageEditCopy`;
- confirmación de descarte de cambios de imagen.

No se añadió soporte de texto todavía.

## KISS / scope

- No segundo `Loaded` hook.
- No `InitializeTextEditUi`.
- No segunda cadena de guards.
- Sin cambios de PDFium, writer, workspace o persistencia.
- Sin Task 5+.
- El workflow temporal de rename se autoeliminó y no aparece en el diff funcional.

## Auditoría del diff funcional

El diff desde RED hasta el head funcional contiene únicamente:

- los 2 archivos shell renombrados/generalizados;
- referencias necesarias en `MainWindow.EditImages.Commands.cs`, reader/shortcuts;
- tests F6 que reflejan esos nombres internos.

El gate RED fue excluido del reemplazo mecánico y conserva sus assertions de nombres legacy ausentes.

## GREEN exacto

CI: `38073275750`

- repository hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **693 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

## Gobernanza

- `main` no debe modificarse ni mergearse sin aprobación explícita.
- F6 PR #25 continúa sin merge automático.
- Manual Windows QA permanece `NOT RUN`.
- Este checkpoint cierra únicamente Task 4.

## Próximo paso permitido

**Task 5 — modelo/discovery de objetos de texto**, siguiendo RED → GREEN.

No iniciar Task 6+ antes de cerrar Task 5.
