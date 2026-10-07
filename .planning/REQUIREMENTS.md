# Requirements — SG PDF Editor

**Source of truth:** `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.  
**Purpose:** requisito resumido y trazable para GSD. No reemplaza las especificaciones de cada fase.

## Product Requirements

### F0 — PDF Base

> **Estado 2026-10-07:** implementación + verificación automatizada de `PDF-BASE-01` a `PDF-BASE-07` = **PASS**. Los checkboxes permanecen abiertos hasta completar el smoke físico Windows, incluyendo Microsoft Print to PDF y prueba con red deshabilitada.

- [ ] **PDF-BASE-01** Abrir un PDF local desde la UI.
- [ ] **PDF-BASE-02** Renderizar páginas reales con PDFium sin bloquear la UI.
- [ ] **PDF-BASE-03** Navegar anterior/siguiente/ir a página.
- [ ] **PDF-BASE-04** Zoom + fit page + fit width.
- [ ] **PDF-BASE-05** Cancelar renders obsoletos y priorizar la página visible.
- [ ] **PDF-BASE-06** Imprimir mediante Windows, incluyendo Microsoft Print to PDF.
- [ ] **PDF-BASE-07** Funcionar con red deshabilitada.

### F1 — Gate ZPL-A

- [ ] **ZPL-GATE-01** Comparar BinaryKits.Zpl vs Labelize con corpus real privado y sintético.
- [ ] **ZPL-GATE-02** Validar `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF`, `^XF`.
- [ ] **ZPL-GATE-03** Benchmark de 10/100/500 diseños sin multiplicar por `^PQ`.
- [ ] **ZPL-GATE-04** Elegir un único motor según fidelidad, códigos, rendimiento, packaging y licencia.

### F2 — Etiquetas ZPL

- [ ] **LABEL-01** Abrir `.zpl`, `.txt` y `.prn` con ZPL.
- [ ] **LABEL-02** Separar diseños y conservar cantidad `^PQ` por separado.
- [ ] **LABEL-03** Preview local por diseño.
- [ ] **LABEL-04** Cantidad del archivo / una de cada / personalizada.
- [ ] **LABEL-05** Layout 1/2/3/4/6/8/10/12/custom y tamaños térmicos/A4/Carta/custom.
- [ ] **LABEL-06** Exportar PDF sin deformar códigos.
- [ ] **LABEL-07** Imprimir mediante driver Windows a tamaño físico exacto.
- [ ] **LABEL-08** Validar barcode/QR automáticamente y con prueba física.

### F3 — Firma visual

- [ ] **SIGN-01** Importar PNG transparente.
- [ ] **SIGN-02** Arrastrar, mover, redimensionar proporcionalmente, duplicar y eliminar.
- [ ] **SIGN-03** Convertir coordenadas UI ↔ PDF correctamente.
- [ ] **SIGN-04** Guardar como copia y reabrir preservando posición/transparencia.

### F4 — Lector completo

- [ ] **READER-01** Scroll continuo y miniaturas.
- [ ] **READER-02** Búsqueda y copia de texto.
- [ ] **READER-03** Bookmarks y links.
- [ ] **READER-04** PDFs con password.
- [ ] **READER-05** Atajos/recientes; pestañas solo si no complican estabilidad.

### F5 — Organizar

- [ ] **ORG-01** Mover, reordenar, rotar, eliminar y duplicar páginas.
- [ ] **ORG-02** Insertar, extraer, unir y dividir.
- [ ] **ORG-03** Preflight de firmas, formularios, bookmarks/destinations y estructuras relevantes.
- [ ] **ORG-04** PDFium primero; añadir otra utilidad solo ante una carencia demostrada.

### F6 — Imágenes

- [ ] **IMG-01** Detectar/seleccionar imágenes y mostrar menú contextual.
- [ ] **IMG-02** Extraer/guardar y reemplazar preservando geometría cuando sea viable.
- [ ] **IMG-03** Mover, resize, rotar, opacidad, orden y eliminar.
- [ ] **IMG-04** Undo/redo.

### F7 — Texto V1

- [ ] **TEXT-01** Detectar/seleccionar objetos de texto.
- [ ] **TEXT-02** Edición conservadora in-place cuando sea segura.
- [ ] **TEXT-03** Fallback con TTF redistribuible cuando aparezcan nuevos code points/subset dudoso.
- [ ] **TEXT-04** Propiedades básicas y save/reopen validation.

### F8-F12 — Posteriores

- [ ] **COMMENTS** Highlight, underline/strike, notas y dibujo/formas.
- [ ] **UTILS** Solo utilidades offline justificadas.
- [ ] **OCR** Tesseract local para texto buscable.
- [ ] **TEXT-V2** Líneas/párrafos/reading order/reflow limitado.
- [ ] **PRO** Firma criptográfica, formularios, redacción real, compare, batch y conversiones auditadas.

## Cross-cutting Requirements

- [ ] **OFFLINE** Ninguna función principal requiere Internet.
- [ ] **LICENSE** Dependencias runtime permisivas o explícitamente aprobadas.
- [ ] **PRIVACY** Datos reales de clientes/Mercado Libre no se versionan ni envían a servicios externos.
- [ ] **ORIGINAL** Proteger el original; `Guardar como` durante primeras fases.
- [ ] **CI** Cada slice debe mantener build/tests Windows verdes.
- [ ] **KISS** No añadir capas/dependencias preventivas.
- [ ] **NO-AUTOMERGE** Ningún merge a `main` sin aprobación explícita.

## Traceability

| GSD Phase | Product Phase | Requirements |
|-----------|---------------|--------------|
| 1 | F0 PDF Base | PDF-BASE-* |
| 2 | F1 Gate ZPL-A | ZPL-GATE-* |
| 3 | F2 Etiquetas | LABEL-* |
| 4 | F3 Firma visual | SIGN-* |
| 5 | F4 Lector completo | READER-* |
| 6 | F5 Organizar | ORG-* |
| 7 | F6 Imágenes | IMG-* |
| 8 | F7 Texto V1 | TEXT-* |
| 9 | F8 Comentarios | COMMENTS |
| 10 | F9 Utilidades | UTILS |
| 11 | F10 OCR | OCR |
| 12 | F11 Texto V2 | TEXT-V2 |
| 13 | F12 Profesional | PRO |
