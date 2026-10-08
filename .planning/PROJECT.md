# SG PDF Editor

## What This Is

SG PDF Editor is a Windows local-first/offline-first application for practical daily PDF work and offline ZPL label workflows. It is built in small verified vertical slices rather than as an all-at-once Acrobat clone.

## Core Value

Resolver PDF + ZPL de uso diario de forma rápida, privada, estable y utilizable sin Internet, API keys, SaaS, accounts or mandatory commercial runtime licenses.

## Validated Foundation

- A0 repository/reproducibility foundation: PASS.
- A1 GSD Core + Graphify development intelligence: PASS, dev-only.
- WPF + .NET 10 + Windows x64 remains the frozen platform.
- PDFium remains the primary PDF engine.
- F0 PDF Base: automated PASS; physical smoke pending.
- Gate ZPL-A synthetic evidence selected **Labelize 1.7.0** as the single runtime ZPL renderer.
- F2.1–F2.6 ZPL Workspace: automated PASS; private/physical QA pending.
- F3.1–F3.3 visual-signature slices: automated PASS; their real-input/hardware QA remains separate.

## Active Work

**F3.4 — Local Signature Library**

- branch: `feat/f3-4-local-signature-library`;
- base: F3.3 final `8b5bfd35b59fbc75826f8d7616aaaca3e2f31233`;
- written spec exists and is awaiting user review;
- no product code yet;
- no F3.4 PR yet;
- next allowed gate after spec approval is the TDD implementation plan.

## Key Decisions

| Decision | Outcome |
|---|---|
| Windows x64 + C# + .NET 10 + WPF | Frozen unless evidence forces change |
| `SGPdf.App + SGPdf.App.Tests` | Keep KISS while sufficient |
| PDFium primary PDF engine | Active/current |
| PDFium native calls globally serialized | Required |
| Labelize 1.7.0 | **Selected runtime ZPL engine** |
| BinaryKits.Zpl | Historical Gate evidence only; **not fallback runtime** |
| PDFsharp 6.2.4 | Runtime only for label PDF composition/export |
| ZXing.Net 0.16.11 | Test/QA-only |
| GSD Core 1.15.0 / Graphify 0.9.77 | Project-scoped dev-only |
| Save As during early edit phases | Protect originals |
| No auto-merge | Explicit user approval required |

## Constraints

- Offline normal operation.
- No upload of PDFs/ZPL/signatures to runtime services.
- Real Mercado Libre/client fixtures stay local and ignored.
- No AGPL/GPL strong-copyleft or mandatory commercial runtime dependency without explicit approval.
- No preventive CQRS/event bus/plugin framework/complex DI.
- Do not add qpdf/pdfcpu/PdfPig/Tesseract/OpenCV/ImageSharp/SQLite before a slice demonstrates need.
- Main remains untouched until explicit merge approval.

## Current Roadmap

```text
A0/A1        PASS
F0           automated PASS / physical QA pending
F1           synthetic Gate PASS / private corpus pending
F2           F2.1–F2.6 automated PASS / private+physical QA pending
F3.1         automated PASS
F3.2         automated PASS / real-photo QA pending
F3.3         automated PASS / hardware QA pending
F3.4         current: written spec awaiting review
F4–F12       pending
```

## Sources of Truth

Execution: GitHub code/branches/CI.  
Operational state: `.planning/STATE.md`.  
Roadmap: `.planning/ROADMAP.md`.  
Requirements: `.planning/REQUIREMENTS.md`.  
Architecture: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.  
Portable current audit: `docs/history/2026-10-08-CONTINUITY-AUDIT.md`.

---
*Updated 2026-10-08 after continuity audit through F3.4 spec gate.*
