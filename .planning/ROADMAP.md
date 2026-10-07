# Roadmap — SG PDF Editor

> GSD-managed product roadmap. A0/A1 fueron fases de preparación previas a este roadmap operativo. El detalle arquitectónico vive en `docs/MASTER_CONTEXT.md` y `docs/MASTER_PLAN.md`.

## Phase 1 — F0 PDF Base

**Goal:** primer lector PDF usable y estable.

**Success criteria:**
- abrir PDF local;
- render real PDFium sin bloquear UI;
- navegación mínima;
- zoom + fit page/width;
- cancelación/scheduler;
- impresión Windows;
- funciona offline.

**Requirements:** PDF-BASE-01..07

**Estado actual:** implementación y verificación automatizada F0.1–F0.6 PASS; cierre físico Windows todavía pendiente.

## Phase 2 — F1 Gate ZPL-A

**Goal:** elegir un único renderer ZPL con evidencia.

**Success criteria:**
- BinaryKits y Labelize ejecutados contra corpus real privado + sintético;
- comandos críticos y UTF-8 validados;
- benchmark 10/100/500 diseños;
- barcode/QR decodificables;
- decisión documentada por fidelidad/rendimiento/licencias.

**Requirements:** ZPL-GATE-01..04

**Estado actual:** Gate sintético ejecutado y decisión de motor aprobada: **Labelize 1.7.0**. Falta ejecutar corpus real privado de Mercado Libre para cerrar formalmente F1; esos archivos no se versionan.

## Phase 3 — F2 Etiquetas ZPL

**Goal:** reemplazar el flujo manual de Labelary de forma offline.

**Success criteria:**
- abrir ZPL/TXT/PRN;
- preview + cantidades `^PQ`;
- layouts/tamaños configurables;
- export PDF;
- impresión térmica Windows;
- validación digital y física de códigos.

**Requirements:** LABEL-01..08

**Estado actual:** F2.1 Parse + Open en ejecución/verificación. Esta slice es managed-only; el sidecar local Labelize empieza en F2.2.

## Phase 4 — F3 Firma Visual

**Goal:** firmar visualmente un PDF sin web.

**Success criteria:**
- PNG transparente;
- drag/move/resize/delete/duplicate;
- coordenadas UI↔PDF correctas;
- Guardar como + reopen validation.

**Requirements:** SIGN-01..04

## Phase 5 — F4 Lector Completo

**Goal:** completar la experiencia diaria de lectura.

**Success criteria:** scroll continuo, thumbnails, search/copy, bookmarks/links, password y atajos/recientes.

**Requirements:** READER-01..05

## Phase 6 — F5 Organizar

**Goal:** reorganizar y combinar documentos de forma segura.

**Success criteria:** move/rotate/delete/duplicate/insert/extract/merge/split con preflight y PDFium primero.

**Requirements:** ORG-01..04

## Phase 7 — F6 Imágenes

**Goal:** edición práctica de objetos de imagen.

**Success criteria:** select/context menu/extract/replace/move/resize/rotate/opacity/order/delete + undo/redo.

**Requirements:** IMG-01..04

## Phase 8 — F7 Texto V1

**Goal:** edición simple y conservadora de texto existente.

**Success criteria:** selección, edición segura, fallback font y validación save/reopen.

**Requirements:** TEXT-01..04

## Phase 9 — F8 Comentarios

**Goal:** anotaciones de uso diario.

**Success criteria:** highlight, underline/strike, notes, ink/shapes según soporte estable.

**Requirements:** COMMENTS

## Phase 10 — F9 Utilidades

**Goal:** añadir solo utilidades offline con valor demostrado.

**Success criteria:** conjunto priorizado y validado sin inflar el núcleo.

**Requirements:** UTILS

## Phase 11 — F10 OCR

**Goal:** OCR local para documentos escaneados.

**Success criteria:** Tesseract local y PDF buscable, español primero.

**Requirements:** OCR

## Phase 12 — F11 Texto V2

**Goal:** análisis/layout y reflow limitado.

**Success criteria:** reading order, líneas, párrafos y columnas con dependencia adicional solo si reduce complejidad.

**Requirements:** TEXT-V2

## Phase 13 — F12 Profesional

**Goal:** funciones profesionales como slices independientes.

**Success criteria:** cada subproyecto auditado por offline/licencias/compatibilidad antes de entrar.

**Requirements:** PRO

---

## Current Position

A0: complete.  
A1 Development Intelligence: complete.  
F0: automated PASS; physical Windows smoke pending.  
F1: synthetic Gate PASS for engine selection → **Labelize 1.7.0**; private Mercado Libre corpus pending formal close.  
**Current implementation:** Phase 3 / F2.1 — ZPL Parse + Open, stacked on the approved Labelize architecture, with no Labelize runtime call until F2.2.
