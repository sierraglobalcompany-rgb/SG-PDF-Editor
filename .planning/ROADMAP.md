# Roadmap — SG PDF Editor

> Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo: `.planning/STATE.md`. GitHub exact heads/CI mandan sobre snapshots históricos.

| Fase | Estado automatizado | QA manual/privado/físico |
|---|---|---|
| F0 PDF Base | CLOSED / AUTO PASS | NOT RUN completo |
| F1 Gate ZPL-A | synthetic PASS | private corpus NOT RUN |
| F2 ZPL Workspace | CLOSED / AUTO PASS | private + thermal/ruler/scanner NOT RUN |
| F3 Firma Visual | CLOSED / AUTO PASS | Windows/photo/stylus/library NOT RUN |
| F4 Lector Completo | CLOSED / AUTO PASS | Windows real NOT RUN |
| F5 Organizar | CLOSED / AUTO PASS | Windows real NOT RUN |
| F6 Imágenes | CLOSED / AUTO PASS | Windows real NOT RUN |
| F7 Texto V1 | CLOSED / AUTO PASS | Windows real NOT RUN |
| F8 Comentarios | CLOSED / AUTO PASS | stylus/touch/external readers NOT RUN |
| F9 Utilidades | CLOSED / AUTO PASS | Windows/external/private repair corpus NOT RUN |
| F10 OCR Local | CLOSED / AUTO PASS | Windows OCR/external readers/private corpus NOT RUN |
| F11 Texto V2 | CLOSED / AUTO PASS | Windows reflow/private layouts/external readers NOT RUN |
| **F12 Profesional** | **IN PROGRESS — F12.1 Safe Redaction + F12.2 PFX Signature MVP CLOSED / AUTO PASS** | **real/private/external-reader signing + redaction QA NOT RUN** |

## Current position

```text
F0–F11       automated closed with manual/private gates where noted
F12.1        Safe Redaction automated closed
              → true whole-object removal + opaque black mark
              → fail-closed policy + tagged semantic-text block
              → transactional Guardar como... only
F12.2        Cryptographic Signature PFX/P12 MVP automated closed
              → RSA PFX/P12 + detached SHA-256 CMS
              → PDFsharp writer + PDFium inspector
              → independent ByteRange/CMS validator
              → transactional temp/validate/recheck/publish flow
              → minimal Firmar digitalmente WPF command
              → representative preservation gate PASS
              → 1150/1150 exact-head Windows tests PASS
F12 overall  still open; later professional slices are not started by this closure
```

F12.2 stays deliberately narrow. It supports local PFX/P12 RSA signing with an invisible cryptographic signature and `Guardar como...`, validates the produced file locally and fails closed on unsupported or inconsistent signature structures. It reuses existing PDFium, PDFsharp, edit preflight and source fingerprint machinery. No second PDF engine, direct PKCS package, TSA, HSM, Windows Certificate Store integration, backend/account system or visual cryptographic-signature appearance was added.

Representative automated preservation covers exact source bytes, page count, text, image XObject, bookmark, URI link, a pre-existing AcroForm field, selected metadata, rendering and independent CMS validation. This is not universal corpus proof and does not replace real/private/external-reader QA.

Closure evidence:
- F12.1: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`.
- F12.2: `docs/history/2026-10-12-F12-2-cryptographic-signature-closure.md`.

No merge to `main` without explicit user approval. STOP after F12.2 closure; do not start F12.3 implicitly.
