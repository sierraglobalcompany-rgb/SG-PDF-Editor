# UI-4 signature P1 — root cause proven locally; remote audit/CI pending

**Date:** 2026-10-15  
**UI base HEAD:** `abd52d05279e3e656723ad4ff5a21d36de91508e`  
**Local branch reported:** `feat/fix-ui4-dark-mode-strip`  
**Remote integration status:** NOT YET PUSHED / NOT YET CI-VERIFIED

## Proven root cause

The four visual-signature controls shared the right properties area with the AcroForm UI. The AcroForm `ScrollViewer` stayed `Visible` after its inner panel was collapsed, so the transparent `ScrollViewer` remained topmost in the shared `Grid` and intercepted pointer hit-testing.

Windows evidence before the fix:
- `InputHitTest` at the signature-control centers returned the AcroForm `ScrollViewer`;
- click events did not reach the signature controls;
- the failure affected `Cargar PNG transparente...`, `Crear desde foto...`, `Dibujar firma...`, and `Biblioteca de firmas...`, proving a shared-container cause rather than three independent dialog bugs.

## Minimal local fix reported

Owner: `MainWindow.Forms.cs` lifecycle.

Behavioral change:
- AcroForm `ScrollViewer` starts collapsed;
- it becomes visible only while the AcroForm editor is active;
- it collapses again during AcroForm reset.

The pre-existing Dark Mode mode-strip fix in `MainWindow.Sign.cs` was preserved.

Reported changed files:
- `src/SGPdf.App/MainWindow.Forms.cs`
- `src/SGPdf.App/MainWindow.Sign.cs`
- `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs`

## Causal verification reported

- causal test: RED before fix / GREEN after fix;
- locked restore: PASS;
- Release build: PASS, 0 errors;
- full suite: `1215/1215 PASS`;
- `git diff --check`: PASS;
- no package/PKCS changes;
- no `main` changes;
- no push yet.

Windows reverify after local fix:
- PNG picker: PASS;
- V11 photo picker: PASS physical/UIA/keyboard;
- V10 Draw dialog: PASS physical/UIA/keyboard;
- V12 Library dialog: PASS physical/UIA/keyboard;
- V13 name dialog: reachable in an earlier verification;
- V14 remains PARTIAL: in-memory placement/dirty was seen, but stable overlay + saved/reopened PDF was not fully verified.

## V08 separate bug

Windows snapshot:
- `MediaKind = Thermal`;
- layout plan present;
- not busy;
- `ExportLabelPdfButton.IsEnabled = false`;
- `PrintLabelButton.IsEnabled = false`;
- validation text empty.

Classification: **PRODUCT BUG**, separate from the signature root cause. Do not mix it into the signature fix commit.

## Acceptance decision

The signature root cause is accepted as **PROVEN** and the reported local fix is directionally accepted under KISS/anti-patch.

However, it is **not yet accepted into release truth** until:
1. the exact three-file diff is pushed to an isolated remote branch;
2. the diff is audited from GitHub;
3. exact-head CI is GREEN;
4. Windows V10–V14 is rerun on that exact committed head.

## Next order

1. Commit only the demonstrated signature + Dark Mode + causal-test changes on the isolated branch.
2. Push that isolated branch; do not touch `main`.
3. Audit the remote diff.
4. Run/inspect exact-head CI.
5. Investigate/fix V08 in a separate commit only after root cause proof.
6. Finish V04/V05/V08/V09/V10–V14 Windows matrix.
7. UI-4 becomes GREEN only with no P0/P1 and complete required evidence.
