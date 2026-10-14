# Pack A automated preflight — Reader / Organize / Editing

Date: 2026-10-14
Branch: `integration/release-candidate`

## Scope

This is an **automated readiness preflight**, not execution of the real/manual Pack A from `docs/release/2026-10-14-real-qa-packs.md`.

The canonical checklist explicitly says that automated PASS never upgrades a manual/private/physical item to PASS. This environment cannot operate the interactive Windows UI, Microsoft Print to PDF, a real external PDF reader, or a network-disabled desktop session. Therefore the real Pack A remains **NOT RUN — ENVIRONMENT**.

## Candidate automated evidence

Integration candidate CI at `2a0269635cfaa2ca87983f0c579d689ae8fa05ad`:
- workflow run `38516169305`;
- Windows job `115695345151`;
- exact checkout SHA: `2a0269635cfaa2ca87983f0c579d689ae8fa05ad`;
- repository hygiene: PASS;
- pinned Labelize/qpdf/Tesseract staging: PASS;
- locked restore: PASS;
- Release build: PASS, 0 warnings / 0 errors;
- tests: 1201/1201 PASS, 0 failed, 0 skipped.

## Existing automated coverage relevant to Pack A

### Reader

`ReaderLayoutPlannerTests` exercises representative mixed portrait/landscape dimensions, manual zoom, Fit Width, Fit Page, current-page resolution and bounded render-window behavior.

This supports automated readiness of reader layout/navigation mechanics, but it does **not** prove real Windows interaction, search/select behavior, print UX, or external-reader compatibility.

### Organize

`OrganizePlanOperationsTests` covers reorder/move, rotation, deletion safeguards, duplication, insertion from a second source, ordering and invalid-input rejection.

This supports the core organize plan mechanics. It does **not** replace real UI execution, export/reopen visual inspection, extract/split verification on representative documents, or external-reader inspection.

### Image editing / preservation

`ImageEditPreservationTests` writes a real edited copy and reopens it, checks the image change, preserves representative structures including forms/bookmarks/named destinations/internal links/tagged structure/page labels/attachments, and preserves representative metadata.

This is meaningful preservation evidence, but it does **not** replace interactive image editing on representative real PDFs.

### Text editing / safe reflow

`TextReflowPlannerTests` covers allowed simple-paragraph reflow and fail-closed behavior for over-capacity, insufficient-run and unsafe-layout cases.

This supports the conservative Text V2 policy but does **not** replace interactive editing, Save As visual inspection and external-reader verification.

## Real Pack A status

| Canonical Pack A item | Status | Reason |
|---|---|---|
| Open/navigate/zoom/fit/search/select text in Windows UI | NOT RUN | Interactive Windows UI unavailable here |
| Print to Microsoft Print to PDF | NOT RUN | Desktop print subsystem unavailable here |
| Organize + reopen representative output | NOT RUN | Requires real UI and representative document evidence |
| Edit image/text + Save As + reopen | NOT RUN | Requires real interactive evidence |
| Open outputs in Adobe/Foxit/Edge or equivalent | NOT RUN | External desktop reader unavailable here |
| Confirm originals unchanged for real Save-As fixtures | NOT RUN | Real Pack A fixtures were not executed |
| Run normal local workflows with network disabled | NOT RUN | Network-disabled desktop session unavailable here |

## Preflight verdict

**Automated readiness preflight: PASS.**

**Real Pack A gate: NOT RUN — ENVIRONMENT.**

No product defect was found by the exact-head automated suite, and no new architecture or workaround is justified by this preflight. Release readiness must not increase from this result alone.

## Next real evidence required

On an interactive Windows machine, execute the canonical Pack A exactly as written using representative PDFs, record the exact candidate HEAD, Windows build, fixture identities/hashes where safe, output names, original hashes for Save-As flows, and at least one external-reader result. Any failure must be classified root-cause-first before code changes.