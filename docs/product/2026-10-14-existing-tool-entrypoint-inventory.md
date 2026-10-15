# SG PDF Editor — Inventario real de herramientas y entrypoints para F13/F14

**Fecha:** 2026-10-14  
**Base auditada:** `feat/release-ui-modernization` @ `abd52d05279e3e656723ad4ff5a21d36de91508e`  
**Propósito:** evitar botones decorativos, duplicación de lógica y rewrites durante F13/F14.

## Reglas

1. **KISS:** el nuevo Home/rail invoca entrypoints existentes; no reimplementa motores.
2. **Ruido:** reutilizar primero. No mantener dos rutas funcionales diferentes para la misma acción.
3. **Anti-parche:** si una herramienta falla, arreglar su owner; el shell no oculta el fallo.

---

## Hallazgo arquitectónico

El producto ya tiene muchas capacidades, pero su descubrimiento está fragmentado:

- `MainWindow.Sign.cs` crea dinámicamente `LEER/FIRMAR` y su panel de firma.
- `MainWindow.Edit.cs` agrega `EDITAR` y `Guardar como...` a esa franja.
- `MainWindow.Organize.cs` agrega `ORGANIZAR` y su superficie propia.
- `MainWindow.Comments.cs` agrega `COMENTAR` y toolbar propia.
- `MainWindow.Utilities.cs` crea dinámicamente el menú `Herramientas`.
- `MainWindow.Compare.cs` inyecta `Comparar con otro PDF...` en Archivo.
- `MainWindow.Forms.cs` inyecta `Campos de formulario...` en Editar.
- `MainWindow.Signing.cs` inyecta `Firmar digitalmente` en Archivo.

**Decisión F13:** unificar navegación/descubrimiento sin trasladar todavía la lógica funcional de sus owners.

---

## Matriz de entrypoints existentes

| ToolId propuesto | UI futura | Owner actual | Entry point / ruta actual | Requiere PDF abierto | Estado para F13 |
|---|---|---|---|---:|---|
| `open-pdf` | Abrir PDF | `MainWindow.xaml.cs` / reader | `OpenPdf_Click` / `TryOpenPdfPathAsync` | No | REUSE |
| `open-zpl` | Abrir ZPL | `MainWindow.Labels.cs` | `OpenZpl_Click` | No | REUSE |
| `recent-pdf` | Recientes | `MainWindow.ReaderShortcuts.cs` + `RecentPdfStore` | `RecentFileMenuItem_Click` | No | REUSE |
| `print-pdf` | Imprimir | reader/printing | `PrintPdf_Click` | Sí | REUSE |
| `reader` | Leer | `MainWindow.Sign.cs` | `ReadMode_Click` | Sí | REUSE |
| `sign-visual` | Firmar | `MainWindow.Sign.cs` | `SignMode_Click` | Sí | REUSE, P1 Windows abierto |
| `sign-png` | Cargar firma PNG | `MainWindow.Sign.cs` | `LoadSignature_Click` | Sí | REUSE |
| `sign-photo` | Firma desde foto | `MainWindow.Sign.cs` | `PrepareSignaturePhoto_Click` | Sí | REUSE, P1 Windows abierto |
| `sign-draw` | Dibujar firma | `MainWindow.Sign.cs` | `DrawSignature_Click` | Sí | REUSE, P1 Windows abierto |
| `sign-library` | Biblioteca de firmas | `MainWindow.Sign.cs` | `TryOpenSignatureLibrary()` | Sí | REUSE, P1 Windows abierto |
| `sign-digital` | Firma digital PFX/P12 | `MainWindow.Signing.cs` | `CryptographicSign_Click` guarded by `CanStartCryptographicSigning()` | Sí | REUSE |
| `edit-pdf` | Editar PDF | `MainWindow.Edit.cs` | `EditMode_Click` → `TryEnterEditModeAsync()` | Sí | REUSE, UX/P1 revisión abierta |
| `edit-save-as` | Guardar copia editada | `MainWindow.Edit.cs` | `EditSaveAs_Click` → `TrySaveEditWorkspace()` | Sí | REUSE, UX/P1 revisión abierta |
| `redact-text` | Censurar texto seleccionado | `MainWindow.Edit.Text.cs` | `TryStageSelectedTextRedaction()` + `TrySaveEditWorkspace()` | Sí | REUSE dentro de EDITAR |
| `organize` | Organizar páginas | `MainWindow.Organize.cs` | `OrganizeMode_Click` → `TryEnterOrganizeMode()` | Sí | REUSE |
| `rotate-pages` | Rotar | `MainWindow.Organize.*` | toolbar `OrganizeRotate*` | Sí | REUSE |
| `delete-pages` | Eliminar páginas | `MainWindow.Organize.*` | `OrganizeDeleteButton` | Sí | REUSE |
| `duplicate-pages` | Duplicar páginas | `MainWindow.Organize.*` | `OrganizeDuplicateButton` | Sí | REUSE |
| `insert-pages` | Insertar páginas | `MainWindow.Organize.Insert.cs` | `OrganizeInsertButton` | Sí | REUSE |
| `merge-pdf` | Unir PDF | `MainWindow.Organize.*` | `OrganizeMergeButton` | Sí | REUSE |
| `extract-pages` | Extraer páginas | `MainWindow.Organize.ExtractSplit.cs` | `OrganizeExtractButton` | Sí | REUSE |
| `split-pdf` | Dividir PDF | `MainWindow.Organize.ExtractSplit.cs` | `OrganizeSplitButton` | Sí | REUSE |
| `comments` | Comentar | `MainWindow.Comments.cs` | mode button → `TryEnterCommentMode()` | Sí | REUSE |
| `comment-highlight` | Resaltar | `MainWindow.Comments.cs` | `SetCommentTool(CommentTool.Highlight)` | Sí | REUSE |
| `comment-underline` | Subrayar | `MainWindow.Comments.cs` | `CommentTool.Underline` | Sí | REUSE |
| `comment-strikeout` | Tachado | `MainWindow.Comments.cs` | `CommentTool.Strikeout` | Sí | REUSE |
| `comment-note` | Nota | `MainWindow.Comments.cs` | `CommentTool.Note` | Sí | REUSE |
| `comment-ink` | Lápiz | `MainWindow.Comments.cs` | `CommentTool.Ink` | Sí | REUSE |
| `comment-shape` | Rectángulo/óvalo | `MainWindow.Comments.cs` | `CommentTool.Rectangle/Ellipse` | Sí | REUSE |
| `forms-text` | Campos de formulario | `MainWindow.Forms.cs` | `EditAcroForm_Click` | Sí | REUSE `/Tx` only |
| `watermark-text` | Marca de agua | `MainWindow.Utilities.cs` | `WatermarkText_Click` | Sí | REUSE |
| `batch-watermark` | Marca de agua por lotes | `MainWindow.Batch.cs` | `BatchWatermark_Click` | No* | REUSE |
| `page-numbering` | Numerar/Bates | `MainWindow.Utilities.cs` | `PageNumbering_Click` | Sí | REUSE |
| `protect-pdf` | Proteger con contraseña | `MainWindow.Utilities.cs` | `Protection_Click` | Sí | REUSE |
| `ocr-pdf` | OCR / PDF buscable | `MainWindow.Ocr.cs` | `Ocr_Click` | Sí | REUSE |
| `qr-png` | Generar QR | `MainWindow.QrCode.cs` | `QrCode_Click` | No* | REUSE |
| `pdf-to-png` | PDF → PNG | `MainWindow.Conversions.cs` | `ExportPdfToPng_Click` | Sí | REUSE |
| `compare-pdf` | Comparar | `MainWindow.Compare.cs` | `ComparePdf_Click` | Sí | REUSE; mejorar UX en F17 |

`*` El guard actual usa `CanStartBatch()` y no necesita una sesión PDF activa, aunque sí exige un estado limpio/no ocupado.

---

## Decisiones cerradas de mapeo

### Firma digital

No se crea un segundo flujo. La tarjeta/rail `Firma digital` debe llegar al owner existente `CryptographicSign_Click`; la autoridad de disponibilidad sigue siendo `CanStartCryptographicSigning()`.

### Censura

La censura segura actual es deliberadamente una operación de selección de texto dentro de EDITAR. No crear writer ni workspace paralelo.

Si F13 muestra `Censurar` como acceso visible, el flujo correcto es:

`Censurar → entrar a EDITAR → indicar seleccionar texto → TryStageSelectedTextRedaction() → Guardar como...`

No prometer censura libre por rectángulo hasta un capability gate separado.

### ZPL

La tarjeta `Etiquetas ZPL` debe reutilizar `OpenZpl_Click`. Ese entrypoint carga `.zpl/.txt/.prn`, renderiza vía `LabelizeProcessRenderer` y conmuta el workspace actual. No backend, no servidor local y no segundo renderer.

---

## Herramientas ya implementadas que deben aparecer en Home

### Acceso principal
- Abrir PDF
- Recientes
- Abrir etiquetas ZPL

### Organizar
- Unir
- Dividir
- Organizar páginas
- Insertar
- Extraer
- Rotar
- Eliminar/duplicar como acciones contextuales, no tarjetas separadas inicialmente

### Editar
- Editar PDF
- Comentar
- Censurar, con copy explícito “selecciona texto en Editar”
- Marca de agua
- Numerar páginas / Bates

### Firmar
- Firma visual
- Firma digital PFX/P12

### Seguridad
- Proteger PDF

### OCR
- Hacer PDF buscable

### Formularios
- Rellenar campos AcroForm `/Tx`

### Convertir
- PDF → PNG

### Profesional
- Comparar PDF
- Batch watermark
- QR
- ZPL

---

## Reglas de navegación F13

### Home
Una tarjeta no implementa negocio. Solo dispara el comando/entrypoint existente.

### Documento abierto
El rail izquierdo reemplaza progresivamente la franja de modos como punto de entrada global, pero no se elimina la franja anterior hasta demostrar equivalencia completa.

### Panel derecho
Sigue perteneciendo al owner de cada modo:
- firma → `SignaturePropertiesPanel`;
- Editar → propiedades de texto/imagen/redacción existentes;
- comentarios → `CommentPropertiesPanel`;
- formularios → `AcroFormPropertiesPanel`;
- compare → presentación actual primero, Compare V2 después;
- ZPL → `LabelPropertiesPanel`.

### Disponibilidad
El catálogo puede describir disponibilidad, pero la autoridad funcional sigue siendo el guard existente del owner (`CanRunUtility`, `CanStartBatch`, `CanStartCryptographicSigning`, modo activo, sesión, busy, etc.). No duplicar la lógica de guards en el catálogo.

---

## Funciones futuras: no crear entrypoint falso

Todavía no existen como baseline y deben seguir su capability gate antes de aparecer habilitadas:

- Optimize/Compress UI;
- Unlock autorizado;
- Image → PDF;
- Crop;
- PDF → JPG studio;
- watermark de imagen;
- Forms `/Btn`/`/Ch`;
- Compare V2.

Se pueden reservar visualmente en diseño solo si quedan claramente fuera del build funcional; preferencia actual: **no mostrar tarjetas que todavía no funcionan**.

---

## Consecuencia KISS

F13 no necesita un `PluginManager`, DI container nuevo, command bus ni framework de módulos.

Máximo necesario:

```text
ToolDefinition
  Id
  Title
  Category
  IconKey
  RequiresOpenPdf
  ActionKey
```

Y un adaptador pequeño en `MainWindow` que enrute `ActionKey` a los entrypoints ya existentes.

No mover servicios PDF a otra capa solo por el nuevo dashboard.

---

## Gate de eliminación de UI vieja

Solo eliminar un entrypoint visual legado cuando:

1. el nuevo Home/rail llega a la misma función;
2. los guards se conservan;
3. pending-changes guards siguen funcionando;
4. teclado/accesibilidad siguen funcionando;
5. test de equivalencia PASS;
6. Windows smoke real PASS.

Hasta ese gate: ocultar/convivir de forma controlada si hace falta; no borrar preventivamente.
