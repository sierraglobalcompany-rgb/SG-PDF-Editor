# SG PDF — Gate 0 source/test audit before F13

**Fecha:** 2026-10-15  
**Estado:** SOURCE AUDIT COMPLETE / WINDOWS EVIDENCE PENDING  
**UI head auditado:** `abd52d05279e3e656723ad4ff5a21d36de91508e`  
**Objetivo:** clasificar los hallazgos UI-4 antes de aceptar arreglos y antes de iniciar F13.

## Resultado ejecutivo

No se justifica todavía ningún cambio de código productivo para V08, V09, V10, V11 o V12.

- **V10–V12 Firma:** los entrypoints, delegates y `ShowDialog()` existen. Los tests cubren botones, seams, estado y construcción de diálogos, pero no validan de extremo a extremo el modal real abierto desde el botón principal. Sigue siendo necesario localizar en Windows el punto exacto donde se corta el flujo.
- **V08 ZPL/Labels:** el comportamiento de habilitación está definido y probado. `Exportar PDF` requiere un `LabelLayoutPlan` válido y no-busy. `Imprimir` además requiere medio `Thermal`. El FAIL-P2 manual no puede clasificarse como bug sin conocer `MediaKind`, `_labelLayoutPlan` e `IsViewerBusy` en ese momento.
- **Editar / Guardar:** el comando `Guardar como...` existe, queda visible/habilitado al entrar correctamente a Editar y el writer combinado está cubierto. El reporte del usuario requiere reproducir si falló la entrada al modo, la selección/edición de objetos, una limitación segura de texto, el diálogo de destino o el guardado/reapertura. No hay evidencia de un comando ausente.
- **V09 PDF protegido:** el flujo funcional ya tiene cobertura automatizada con un PDF realmente protegido: contraseña incorrecta/cancelación preservan el workspace, reintento correcto abre el documento y la contraseña no queda persistida. Lo único pendiente es evidencia visual real del diálogo en Windows.

## 1. Firma — V10/V11/V12

### Código
`src/SGPdf.App/MainWindow.Sign.cs`

Los tres flujos tienen owner explícito:

- dibujar → `_drawSignature` → `SignatureDrawDialog.Draw(owner)`;
- foto → `OpenFileDialog` + `_prepareSignaturePhoto(owner, path)` → `SignaturePhotoDialog.Prepare(owner, path)`;
- biblioteca → `_openSignatureLibrary(owner, selectedAsset)` → `SignatureLibraryDialog.Open(owner, selectedAsset)`.

Los diálogos correspondientes construyen una ventana, asignan `Owner` y usan `ShowDialog()`.

### Tests existentes
- `MainWindowSignatureDrawTests.cs`
- `MainWindowSignaturePhotoTests.cs`
- `MainWindowSignatureLibraryTests.cs`
- `SignatureDrawDialogTests.cs`
- `SignaturePhotoDialogTests.cs`
- `SignatureLibraryDialogTests.cs`

La cobertura demuestra:
- botón/acción existe;
- el panel Firma lo habilita;
- el seam correcto recibe el owner;
- cancel/failure preservan estado;
- asset válido entra al mismo flujo normal;
- los diálogos se pueden construir en STA.

### Hueco real
No existe evidencia automatizada equivalente a una interacción física completa:

`click real → handler → delegate default → new dialog → Owner → ShowDialog visible`

Por ser modal, ese último salto debe trazarse en Windows.

### Evidencia requerida
En una sola ejecución Windows registrar para cada V10/V11/V12:

1. entra o no entra al click handler;
2. entra o no al método `Try*`;
3. se invoca o no el delegate default;
4. se construye o no la ventana;
5. `Owner`, `IsVisible`, `IsLoaded`, `WindowState`;
6. si `ShowDialog()` se alcanza;
7. si aparece fuera de pantalla, detrás del owner o falla por recursos/excepción.

**No añadir handlers duplicados ni sustituir `ShowDialog()` sin esa prueba.**

## 2. V08 — Labels / ZPL

### Código
`MainWindow.LabelLayout.cs` establece:

- `ExportLabelPdfButton.IsEnabled = _labelLayoutPlan is not null && !IsViewerBusy`
- `PrintLabelButton.IsEnabled = _labelLayoutPlan is not null && !IsViewerBusy && MediaKind == Thermal`

### Tests exactos
`MainWindowZplPdfExportUiTests.cs` prueba:
- Export inicia disabled;
- tras cargar ZPL con plan válido queda enabled;
- al invalidar el layout, `_labelLayoutPlan` queda null y Export vuelve a disabled.

`MainWindowZplPrintUiTests.cs` prueba:
- Print disabled sin ZPL;
- enabled con layout térmico válido;
- disabled para A4;
- disabled para Letter;
- vuelve a enabled para Thermal.

`MainWindowZplLayoutUiTests.cs` prueba que layouts físicos imposibles invalidan el plan.

### Clasificación actual
**P2 MANUAL / CLASSIFICATION PENDING, no bug demostrado.**

Para cerrar V08 deben registrarse al momento del supuesto fallo:
- `MediaKind`;
- `_labelLayoutPlan != null`;
- `IsViewerBusy`;
- estado `IsEnabled` de Export y Print;
- texto de `LayoutValidationText`.

Interpretación:
- Print disabled en A4/Letter/Custom puede ser **EXPECTED**.
- Export disabled con plan null o busy puede ser **EXPECTED**.
- Export disabled con plan válido + not busy sería **PRODUCT BUG** porque contradice el contrato probado.

## 3. Editar / Guardar

### Código
`MainWindow.Edit.cs`

Al completar correctamente `TryEnterEditModeAsync()`:
- `_editModeActive = true`;
- existe `_imageEditWorkspace`;
- `UpdateEditSaveCommandAvailability()` hace visible `EditSaveAsButton`;
- baseline actual lo habilita mientras Edit está activo, hay workspace y no está materializando.

### Tests exactos
`MainWindowEditImageSaveAsTests.cs` incluye:

`EditMode_ExposesSaveAsCommandIndependentOfSelection`

y comprueba:
- Content = `Guardar como...`;
- Visibility = `Visible`;
- `IsEnabled = true`.

El mismo archivo cubre:
- cancel del destino;
- warnings;
- bloqueo preflight;
- fallo writer;
- protección contra doble save;
- dirty/history.

`MainWindowEditCombinedSaveAsTests.cs` prueba que el guardado combinado:
- recibe workspace de imagen y texto;
- escribe una sola copia;
- marca baselines limpios solo después de éxito;
- preserva dirty si el writer falla.

### Clasificación actual
No hay evidencia de que falte el comando o el writer.

El reporte del usuario puede pertenecer a una de estas clases:

1. **PRODUCT BUG** — no entró realmente a Edit, save no apareció, diálogo no abrió, writer falló o copia no reabre correctamente.
2. **EXPECTED SAFE LIMITATION** — texto con `TextRenderMode != 0` u otra estructura no editable.
3. **UX / DISCOVERABILITY** — selección y propiedades no explican bien qué se puede editar ni cómo guardar.

F15 Edit V3 ya está aprobado precisamente para resolver la tercera clase, pero no debe ocultar un bug de las dos primeras.

### Evidencia Windows requerida
Sobre PDF sintético conocido con texto editable + imagen:
- entrar a Edit;
- confirmar `_editModeActive`;
- confirmar `_imageEditWorkspace != null`;
- capturar `EditSaveAsButton.Visibility/IsEnabled`;
- seleccionar texto, modificar, aplicar;
- mover/editar imagen;
- Guardar como nueva copia;
- reabrir copia;
- verificar cambios persistidos.

## 4. V09 — PDF protegido

### Cobertura automatizada existente
`MainWindowReaderPasswordTests.cs` ya crea un PDF realmente protegido y prueba:

- contraseña incorrecta + cancelación → preserva el workspace existente;
- contraseña incorrecta + contraseña correcta (`secret`) → publica el nuevo documento una sola vez;
- la contraseña no se guarda en título, estado ni campos string persistentes de la sesión.

### Clasificación actual
**SOURCE/FUNCTIONAL PATH COVERED.**

No hay un bug fuente conocido en V09.

### Evidencia pendiente
Solo falta la verificación visual real en Windows de que:

- aparece el diálogo de contraseña;
- el foco/teclado funcionan;
- cancelación y reintento son entendibles;
- el diálogo no queda oculto/detrás/fuera de pantalla.

No es necesario generar otro fixture con qpdf salvo que el fixture automatizado no pueda reutilizarse en la prueba manual.  
No añadir paquete, herramienta ni fixture permanente solo para V09.

## 5. Gate 0

### Estado
`BLOCKED — UI-4 FAIL-P1`

No iniciar código F13 todavía.

### Condiciones para GREEN
1. V10–V12 raíz demostrada y corregida/reclasificada.
2. Edit/Save reproducido y clasificado.
3. V08 clasificado con estado real.
4. V09 verificado visualmente en Windows.
5. V04 1366×768 ejecutado.
6. V05 scaling ejecutado.
7. V13/V14 ejecutados después de desbloquear Firma.
8. Todo P0/P1 resuelto y reverificado.
9. focused cause tests PASS.
10. full Release suite PASS.
11. exact-head CI GREEN.
12. evidencia Windows final aceptada.

## 6. Decisión anti-parche

**NO PRODUCT CODE CHANGE from this source audit.**

La siguiente modificación productiva solo se acepta si la traza Windows demuestra una causa concreta.

## 7. Relación con A+B / F13

El usuario aprobó **A+B**:

- A = spec visual del mockup aprobado.
- B = plan técnico F13.

Ambos están congelados y listos.  
La implementación F13 empieza inmediatamente después de que Gate 0 sea GREEN, con ChatGPT como implementador principal y Codex solo en gates Windows/manuales.
