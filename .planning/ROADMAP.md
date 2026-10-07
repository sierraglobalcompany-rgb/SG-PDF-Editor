# Roadmap — SG PDF Editor

> GSD-managed product roadmap. A0/A1 fueron fases de preparación. Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado diario: `.planning/STATE.md`.

## Phase 1 — F0 PDF Base

**Goal:** primer lector PDF usable y estable.  
**Requirements:** PDF-BASE-01..07  
**Estado:** F0.1–F0.6 automated PASS; cierre físico Windows todavía pendiente.

## Phase 2 — F1 Gate ZPL-A

**Goal:** elegir un único renderer ZPL con evidencia.  
**Requirements:** ZPL-GATE-01..04  
**Estado:** Gate sintético PASS y motor aprobado **Labelize 1.7.0**. Falta corpus real privado Mercado Libre para cierre formal F1.

## Phase 3 — F2 Etiquetas ZPL

**Goal:** reemplazar el flujo manual de Labelary de forma offline.

**Success criteria:** abrir ZPL/TXT/PRN; preview/cantidades; layouts/tamaños; export PDF; impresión térmica Windows; validación digital y física de códigos.

**Requirements:** LABEL-01..08

**Estado actual:**
- F2.1 Parse + Open — automated PASS;
- F2.2 Labelize Adapter + Preview — automated PASS;
- F2.3 Quantity UX + dimensiones físicas/dpmm — automated PASS;
- F2.4 Layout + PDF Export — automated PASS;
- F2.5 Windows Thermal Print — automated PASS / hardware physical QA NOT RUN;
- **F2.6 Validation + Hardening — automated PASS / private corpus NOT RUN / physical printer-scanner NOT RUN**: exact Code128/QR decode through Labelize→PDF→PDFium at 300 DPI, 90° regression, opt-in private corpus runner, cleanup/offline/privacy evidence.

The automated ZPL pipeline is complete. Private customer-label and physical paper/scanner acceptance remain explicit parallel gates; synthetic CI never substitutes for them.

## Phase 4 — F3 Firma Visual

**Goal:** firmar visualmente un PDF sin web.  
**Requirements:** SIGN-01..04  
**Estado:** **Next — design/approval first.**

## Phase 5 — F4 Lector Completo

**Goal:** completar la experiencia diaria de lectura.  
**Requirements:** READER-01..05

## Phase 6 — F5 Organizar

**Goal:** reorganizar y combinar documentos de forma segura.  
**Requirements:** ORG-01..04

## Phase 7 — F6 Imágenes

**Goal:** edición práctica de objetos de imagen.  
**Requirements:** IMG-01..04

## Phase 8 — F7 Texto V1

**Goal:** edición simple y conservadora de texto existente.  
**Requirements:** TEXT-01..04

## Phase 9 — F8 Comentarios

**Goal:** anotaciones de uso diario.  
**Requirements:** COMMENTS

## Phase 10 — F9 Utilidades

**Goal:** utilidades offline con valor demostrado.  
**Requirements:** UTILS

## Phase 11 — F10 OCR

**Goal:** OCR local para escaneados.  
**Requirements:** OCR

## Phase 12 — F11 Texto V2

**Goal:** análisis/layout y reflow limitado.  
**Requirements:** TEXT-V2

## Phase 13 — F12 Profesional

**Goal:** funciones profesionales como slices independientes.  
**Requirements:** PRO

---

## Current Position

A0: complete.  
A1 Development Intelligence: complete.  
F0: automated PASS; physical Windows smoke pending.  
F1: synthetic Gate PASS → **Labelize 1.7.0**; private corpus pending formal close.  
F2.1–F2.6: **automated PASS**.  
F2 private corpus: **NOT RUN**.  
F2 physical thermal/scanner acceptance: **NOT RUN**.  
**Current implementation next:** Phase 4 / F3 — Firma Visual; design/approve before implementation.
