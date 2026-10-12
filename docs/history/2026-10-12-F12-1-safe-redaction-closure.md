# F12.1 Safe Redaction — automated closure

Date: 2026-10-12
Branch: `feat/f12-professional`
F11 base / merge-base: `7a890b7c8ece75869730c95c275a1ee8202efe93`
Functional closure HEAD: `f40b117cba235e221df4c4c5776b41bc8e89dac4`
Checkpoint immediately after functional HEAD: `9a584779642af973a6255c9615e30b8fddb4ea18`
Main remains untouched: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## Closure status

F12.1 Safe Redaction is **AUTOMATED CLOSED / AUTO PASS**.

This does **not** close all of F12 Professional. Cryptographic signing, advanced forms, compare, batch, audited conversions and any later F12.x slice remain outside this closure.

## Fresh exact-head evidence

Windows CI run: `38288995654`
Job: `114944198278`
Exact tested SHA: `f40b117cba235e221df4c4c5776b41bc8e89dac4`

- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- build warnings: `0`;
- build errors: `0`;
- tests: `1111/1111 PASS`;
- skipped: `0`.

The test total intentionally dropped from 1112 to 1111 because duplicate multi-target coverage was merged, not because a guarantee was removed.

## Implemented guarantees

- Whole simple visible horizontal text objects can be selected conservatively for redaction.
- Unsupported/unsafe selections fail closed with a Spanish reason.
- Tagged PDFs / semantic `ActualText` risk are blocked for redaction in this slice rather than sanitized by a new subsystem.
- Redaction drafts stage in memory and do not mutate the source PDF.
- Source path + fingerprint identity is reused from EDITAR and rechecked before publication.
- A demonstrated source-race bug was closed by revalidating the existing fingerprint before atomic publish.
- The existing EDITAR transaction is reused; there is no second general PDF writer.
- The underlying approved text object is removed and replaced with an opaque black path.
- The reopened-output validator checks secret absence, preserved non-target text, page rendering and the expected opaque black path.
- Multiple safe targets on one page are supported and validated.
- Combined normal text editing + redaction is supported through the same transaction.
- `Redactar` only stages a draft; `Guardar como...` remains the only publication path.
- Save As redaction preserves the original source bytes.
- Signed documents remain blocked.
- Cancellation/failure/source drift publish no partial destination and temporary outputs are cleaned.

## KISS / Ley de Ruido audit

- No new PDF engine.
- No new runtime dependency.
- No `.csproj`, lockfile or runtime-manifest dependency change relative to F11.
- Existing PDFium, EDITAR workspaces, `TextObjectKey`, `RedactionPolicy`, `RedactionWorkspace`, `PdfEditWriter` transaction and current text properties panel were reused.
- No second sidebar, generic service layer, generic PDF object graph or parallel save architecture was introduced.
- Duplicate files `PdfEditWriterRedactionMultiTargetTests.cs` and `PdfEditWriterRedactionMultipleTargetsTests.cs` were reviewed; useful assertions were merged and the redundant file removed.

## Manual/private/physical gates still open

Automated closure does not imply manual/product closure. Still NOT RUN unless separately evidenced:

- real Windows redaction UX;
- representative private PDFs;
- complex real-world PDFs beyond the supported safe subset;
- Adobe / Foxit / Edge interoperability checks;
- offline/representative-hardware smoke;
- visual inspection of black redaction placement at normal zoom/print/export.

## Stop rule

STOP before F12.2 or any other new F12 Professional feature. Do not merge to `main` without explicit user approval. Any next F12 slice requires its own narrow spec/plan and must preserve KISS + fail-closed semantics.
