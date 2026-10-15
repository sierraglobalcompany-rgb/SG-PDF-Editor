# UI-4 GREEN — Gate 0 Closed

**Date:** 2026-10-15

## Accepted UI head

`b4f19fcd5f2854c9025e60385e3b7819bd798a29`

## Automated evidence

- locked restore PASS
- Release build PASS
- suite `1216/1216 PASS`
- CI run `38681417442` SUCCESS

## Root-cause fixes accepted

### Signature / shared properties pane

Cause: AcroForm `ScrollViewer` remained visible over the shared properties area and intercepted hit-testing for signature controls.

Fix: lifecycle/visibility correction in existing owner; no replacement dialogs, timers, topmost workaround or new architecture.

### V08 label actions

Cause: valid Thermal ZPL was committed while `_isBusy=true`; when busy ended, `SetBusy()` refreshed PDF viewer controls but not ZPL label actions, leaving Export/Print stale-disabled.

Fix: when a ZPL workspace is active, `SetBusy()` refreshes label properties/actions. No forced `IsEnabled`.

## Final Windows evidence

- V04 1366×768 PASS
- V05 Windows 125% scaling PASS
- V08 Thermal export/actions PASS; temporary PDF produced
- V10 Draw Signature PASS
- V11 Signature from Photo PASS
- V12 Signature Library PASS
- V13 signature-name dialog PASS
- V14 signature overlay + Save As + reopen PASS
- Edit/Save representative text flow + reopen PASS
- no P0/P1/P2 reproduced

V09 manual typing was not performed because the local computer-use workflow would not type authentication credentials. It is non-blocking because this accepted head contains automated coverage for wrong password + cancel preserving workspace, wrong→correct retry and password non-persistence.

## Decision

**UI-4 / Gate 0 = GREEN / CLOSED.**

Do not reopen without a reproducible regression.

## Next

Consolidate the already-proven test-only PKCS 8.0.2 hygiene delta on top of the accepted UI head. After consolidated CI GREEN, that head becomes the F13 implementation base.

`main` remains untouched; no merge without explicit user approval.
