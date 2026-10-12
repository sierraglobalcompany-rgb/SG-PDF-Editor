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
| **F10 OCR Local** | **AUTOMATED CLOSURE DECLARED; exact-head integration gate** | **Windows OCR/external readers/private corpus NOT RUN** |
| F11 Texto V2 | NOT STARTED | — |
| F12 Profesional | NOT STARTED | — |

## Current position

```text
F0–F9        automated closed with manual/private gates where noted
F10          feature + preservation complete; closure declaration committed
              → exact-head CI → draft PR → STOP
F11–F12      not started
```

F10 scope remains local/offline: PDFium owns PDFs; Tesseract is recognition-only; qpdf remains narrow; no cloud/backend/database/accounts/API keys.

No merge to `main` without explicit user approval. Repository integration/governance is a separate future gate and is not part of F10 closure.
