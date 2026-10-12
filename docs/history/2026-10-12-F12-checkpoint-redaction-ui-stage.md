# F12 checkpoint — redaction UI + draft staging

Date: 2026-10-12
Branch: `feat/f12-professional`
Functional HEAD before this checkpoint doc: `142d7b063faabc897ee56c015ba4de5f35071013`
Status: STOPPED BY USER REQUEST after two GREEN microblocks. No Save As publication work started.

## Scope completed

### Microblock A — selection eligibility presentation

- RED: `ac3a35444724c76b7598585506933b530c45a87d`
- GREEN: `4e91710a5397c9c34f0d42058b137c10bc9682b2`
- Reused existing `RedactionPolicy`; no new ViewModel/service/workspace architecture.
- Added `EditTextRedactionStateText` to the existing text properties panel.
- Safe selected text reports redaction availability.
- Unsafe selected text reports the conservative policy reason.
- No writer/materializer changes.
- Windows CI run `38285822979`, job `114933825149`: Release build 0 warnings / 0 errors; 1110/1110 tests PASS.

### Microblock B — Redactar stages draft only

- RED: `d7dc6db249ac65255119a697cf27a2db0a2e1ad9`
- GREEN: `142d7b063faabc897ee56c015ba4de5f35071013`
- Added `EditTextRedactButton` in the existing text panel.
- Reused existing `RedactionWorkspace` and `RedactionPolicy`.
- Workspace is created lazily only when source path + fingerprint match the active EDITAR source workspace.
- `Redactar` performs policy evaluation then `Stage(plan)` only.
- Already-staged text disables the button and reports draft state.
- Redaction dirty state participates in the existing unsaved-changes guard.
- Test verifies the source PDF bytes remain unchanged after staging.
- No publication/materialization/Save As integration was added.
- Windows CI run `38286648237`, job `114936510676`: Release build 0 warnings / 0 errors; 1111/1111 tests PASS.

## Guardrails preserved

- KISS / YAGNI.
- RED -> GREEN with test-only RED before production changes.
- Existing `TextObjectKey`, `RedactionPolicy`, `RedactionWorkspace` and current text properties panel reused.
- No second sidebar, no new service layer, no parallel writer pipeline.
- Fail closed on unsafe/unsupported selections.
- Original PDF is never mutated by staging.
- `Guardar como...` remains the intended only publication path.
- No merge to `main` without explicit user approval.

## Exact stop point

The next logical microblock was identified but NOT started:

1. Inspect the existing redaction materialization contract and current shared Save As pipeline.
2. Write a test-only RED proving staged redactions are published only through `Guardar como...`.
3. Integrate the existing `PdfRedactionMaterializer` into the existing save pipeline with the minimum mutation order required by F12.
4. Preserve source bytes and fail closed on fingerprint/source drift.
5. Run focused + full Windows CI before opening any further F12 scope.

No code, test, or documentation for that next microblock has been committed beyond this checkpoint declaration.

## Resume instruction

Resume from functional HEAD `142d7b063faabc897ee56c015ba4de5f35071013` plus this checkpoint documentation commit. Re-read the F12 plan and inspect the existing Save As / redaction materializer contracts before writing the next RED. Do not skip directly to GREEN and do not introduce another save architecture.
