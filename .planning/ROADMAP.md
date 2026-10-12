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
| **F12 Profesional** | **IN PROGRESS — F12.1 Safe Redaction CLOSED / AUTO PASS** | **redaction Windows/private/external-reader QA NOT RUN** |

## Current position

```text
F0–F11       automated closed with manual/private gates where noted
F12.1        Safe Redaction automated closed
              → true whole-object removal + opaque black mark
              → fail-closed policy + tagged semantic-text block
              → transactional Guardar como... only
              → 1111/1111 exact-head Windows tests PASS
F12 overall  still open; remaining professional slices not started by this closure
```

F12.1 stays deliberately narrow: only complete simple visible horizontal text objects are eligible; unsafe/ambiguous cases fail closed; tagged PDFs are blocked for redaction rather than sanitized by a new subsystem; existing PDFium, EDITAR transaction, fingerprint model, `TextObjectKey`, text properties panel and Save As path are reused. No new PDF engine or runtime dependency was added.

Closure evidence: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`.

No merge to `main` without explicit user approval. Do not start F12.2 implicitly; the next F12 Professional slice requires its own narrow spec/plan.
