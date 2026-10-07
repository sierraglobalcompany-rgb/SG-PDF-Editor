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

**Success criteria:**
- abrir ZPL/TXT/PRN;
- preview + cantidades `^PQ`;
- layouts/tamaños configurables;
- export PDF;
- impresión térmica Windows;
- validación digital y física de códigos.

**Requirements:** LABEL-01..08

**Estado actual:**
- F2.1 Parse + Open — automated PASS;
- F2.2 Labelize Adapter + Preview — automated PASS;
- F2.3 Quantity UX + dimensiones físicas/dpmm — automated PASS;
- F2.4 Layout + PDF Export — automated PASS;
- **F2.5 Windows Thermal Print — automated PASS / physical hardware QA NOT RUN**: `PrintQueue`/`PrintTicket` preflight, media exacta, CopyCount=1, clipping warning sin escalado, paginator térmico exacto y UI de impresión;
- **Siguiente: F2.6 Validation + Hardening** — decode automático, corpus privado, impresora/scanner físico, offline/privacy/temp audit.

## Phase 4 — F3 Firma Visual

**Goal:** firmar visualmente un PDF sin web.  
**Requirements:** SIGN-01..04

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
F2.1: automated PASS — Parse + Open.  
F2.2: automated PASS — Labelize sidecar + local PNG preview.  
F2.3: automated PASS — quantities + dimensions + dpmm.  
F2.4: automated PASS — pure-mm layout + sheet preview + transactional PDF export + PDFium reopen validation.  
F2.5: automated PASS — exact-size Windows thermal print preflight/paginator/UI; **real physical print still pending**.  
**Current implementation next:** Phase 3 / F2.6 — Validation + Hardening.
