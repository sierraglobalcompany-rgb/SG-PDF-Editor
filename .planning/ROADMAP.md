# Roadmap — SG PDF Editor

> GSD-managed product roadmap. Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado diario: `.planning/STATE.md`.

## Phase 1 — F0 PDF Base
**Goal:** lector PDF usable y estable.  
**Estado:** automated PASS; smoke físico Windows pendiente.

## Phase 2 — F1 Gate ZPL-A
**Goal:** elegir renderer ZPL con evidencia.  
**Estado:** synthetic Gate PASS → **Labelize 1.7.0**; corpus privado pendiente.

## Phase 3 — F2 Etiquetas ZPL
**Goal:** reemplazar Labelary manual de forma offline.  
**Estado:** **F2.1–F2.6 automated PASS**. Private corpus y physical printer/scanner siguen NOT RUN.

## Phase 4 — F3 Firma Visual
**Goal:** firmar visualmente un PDF sin web.  
**Requirements:** SIGN-01..04.

**Diseño aprobado:**
- F3.1 Core placement + PDF Save;
- F3.2 Foto/scan → transparencia y mejora local;
- F3.3 Dibujar firma con WPF InkCanvas;
- F3.4 biblioteca local posterior.

**Estado actual:**
- **F3.1 Core visual signature — automated PASS**: PNG transparente, coordenadas PDF estables, move/resize proporcional/duplicate/delete, PDFium writer con alpha, Save As transaccional, reopen validation, crypto-signature warning y dirty guard.
- **F3.2 Photo/scan preparation — automated PASS / real-photo QA NOT RUN**: PNG/JPEG local, estimación de papel, soft-alpha background removal, brillo/contraste, Original/Negro/Azul, auto-crop, preview <=1200 px y `Crear desde foto...` integrado a FIRMAR. Final head `2aa1f58a6e01397e84d8f8cfcf7eb6e0e168cf62`; push CI `37796057421` + PR CI `37796065775` PASS; **214 tests PASS**, Release 0 warnings/0 errors.
- **F3.3 Draw signature / InkCanvas — functional GREEN; closure exact-head CI required**: native WPF ink, negro/azul, 3 grosores, Undo/Redo/Clear, crop transparente, 300-DPI-equivalente, 20M guard y `Dibujar firma...` integrado al mismo `AddSignatureAsset(...)`. Functional head `d3e7d5e0fe460255ec37d41d8b5017272088406d` → CI `37802064739` PASS; **246 tests PASS**, Release 0 warnings/0 errors. Hardware mouse/touch/stylus QA NOT RUN.
- **F3.4 Local signature library — próximo slice**: design/approval antes de código.

## Phase 5 — F4 Lector Completo
**Goal:** completar experiencia diaria de lectura.  
**Requirements:** READER-01..05.

## Phase 6 — F5 Organizar
**Requirements:** ORG-01..04.

## Phase 7 — F6 Imágenes
**Requirements:** IMG-01..04.

## Phase 8 — F7 Texto V1
**Requirements:** TEXT-01..04.

## Phase 9 — F8 Comentarios
**Requirements:** COMMENTS.

## Phase 10 — F9 Utilidades
**Requirements:** UTILS.

## Phase 11 — F10 OCR
**Requirements:** OCR.

## Phase 12 — F11 Texto V2
**Requirements:** TEXT-V2.

## Phase 13 — F12 Profesional
**Requirements:** PRO.

---

## Current Position

A0/A1: complete.  
F0: automated PASS / physical smoke pending.  
F1: synthetic PASS / private corpus pending.  
F2.1–F2.6: automated PASS / private + physical gates pending.  
F3.1: **automated PASS**.  
F3.2: **automated PASS / real-photo manual QA NOT RUN**.  
F3.3: **functional GREEN / exact-head closure CI pending at documentation time / hardware QA NOT RUN**.  
**Next product slice after F3.3 closure: F3.4 Local Signature Library — design/approve first.**