# F9 Task 8 — Optimize/repair capability gates and preservation matrix

Date: 2026-10-11  
Branch: `feat/f9-offline-utilities`  
Base / Task 7 checkpoint: `1e864a295133a8319c08927dd0026989fd38f207`

## Scope

Task 8 is evidence only. It does **not** expose a new WPF command and does not start Task 9.

Goals measured in this task:

1. characterize a lossless structural optimization path using the already-pinned qpdf 12.4.2 runtime;
2. measure preservation of the representative structures required by the F9 plan;
3. characterize deterministic damaged-PDF inputs;
4. keep Repair fail-closed unless the corpus is strong enough to justify a reliable product feature.

## TDD evidence

### RED

Commit: `546c74b59d1e553d8b6ff564268959db7f285619`  
Workflow: `38117316903`  
Job: `114404689950`

Result:

- build: **0 warnings / 0 errors**;
- inherited tests: **937 PASS**;
- new Task 8 tests: **5 FAIL**;
- total: **937 PASS / 5 FAIL / 0 SKIP**;
- all five new failures were caused by the deliberate absence of `PdfOptimizeRepairCapabilityAdapter`.

### GREEN

Functional commit: `3cf85840d26098048c57be2383ad11297875a990`  
Workflow: `38117410013`  
Job: `114404967287`

Result:

- qpdf 12.4.2 staging: PASS;
- build: **0 warnings / 0 errors**;
- tests: **942 PASS / 0 FAIL / 0 SKIP**.

## Optimize capability decision — GO

Implementation:

- `src/SGPdf.App/Features/Utilities/PdfOptimizeRepairCapabilityAdapter.cs`
- `tests/SGPdf.App.Tests/PdfOptimizeRepairCapabilityTests.cs`

The capability rewrite uses qpdf with:

- `objectStreams = generate`;
- `recompressFlate`;
- `compressionLevel = 9`;
- `preserveUnreferenced`.

It deliberately does **not** use `optimizeImages`, JPEG conversion, downsampling, rasterization, flattening, or any other lossy transform.

The gate does not define success as “output is smaller.” Source and output lengths are measured and returned, but the test only requires a valid non-empty output and semantic preservation. A larger output is therefore allowed.

The source file remains byte-identical. A pre-cancelled operation produces no output and no leaked qpdf job JSON.

## Fresh F9 optimize preservation matrix

Fixture: `F8PreservationFixture`, reused because it contains all required representative structures simultaneously. Task 8 measures the qpdf optimization output afresh; it does not inherit the F8 writer result.

| Structure / datum | Fresh Task 8 evidence after qpdf optimize rewrite | Result |
|---|---|---|
| AcroForm | `GetOrganizeFormType() != 0` after reopen | **PROVEN PRESERVED** |
| Bookmarks | bookmark `F8 Root` reopens and targets page 0 | **PROVEN PRESERVED** |
| Named destinations | `HasOrganizeNamedDestinations()` remains true | **PROVEN PRESERVED** |
| Internal links | page 0 internal link still targets page 1 | **PROVEN PRESERVED** |
| Tagged structure | `IsOrganizeTagged()` remains true | **PROVEN PRESERVED** |
| Page labels | `HasOrganizePageLabels()` remains true | **PROVEN PRESERVED** |
| Attachment | `f8-note.bin` reopens with exact payload `f8-exact-payload-2026` | **PROVEN PRESERVED** |
| Metadata | exact title `SG PDF F8 preservation fixture` and author `SG PDF Task 11 F8` remain | **PROVEN PRESERVED** |
| Page rotation | page 2 remains quarter-turn `1` (`/Rotate 90`) | **PROVEN PRESERVED** |
| Supported annotation | existing `TEXT` comment `Existing supported F8` remains | **PROVEN PRESERVED** |
| Unsupported representative annotation | unsupported annotation remains discoverable as read-only/unknown subtype | **PROVEN PRESERVED / READ-ONLY** |

This matrix is representative evidence, not a proof that every possible PDF structure in the specification is preserved.

## Repair characterization

Task 8 intentionally keeps repair at capability-gate level only.

### Recoverable deterministic damage

Fixture: valid rich F8 PDF with `startxref` deliberately replaced by `0`.

Observed under exact-head CI:

- qpdf exit code: **3** (warnings);
- output file: created;
- output reopens through `PdfDocumentSession`;
- reopened page count: 2;
- damaged source remains byte-identical.

Classification: **RECOVERED WITH WARNINGS**.

### Unrecoverable deterministic damage

Fixture: severely truncated PDF containing only an incomplete catalog object.

Observed under exact-head CI:

- qpdf exit code: **2**;
- no usable output is retained;
- damaged source remains byte-identical.

Classification: **UNRECOVERABLE**.

## Repair product decision — NO-GO

`PdfOptimizeRepairCapabilityAdapter.RepairProductDecision` is pinned to `NoGo` for F9.

Reason:

- qpdf demonstrably recovers some structural damage;
- qpdf demonstrably cannot recover other deterministic damage;
- this small synthetic corpus is not sufficient evidence for a user-facing generic “Reparar PDF” promise;
- therefore SG PDF must not expose a placebo Repair command in F9.

No bypass, password removal, signature manipulation, or destructive in-place repair path was introduced.

## Scope audit

Comparison before this checkpoint documentation:

`1e864a295133a8319c08927dd0026989fd38f207..3cf85840d26098048c57be2383ad11297875a990`

Result:

- ahead: 2 commits;
- behind: 0;
- merge base: exact Task 7 checkpoint;
- functional/test paths changed: exactly 2.

Changed functional/test paths:

1. `src/SGPdf.App/Features/Utilities/PdfOptimizeRepairCapabilityAdapter.cs`
2. `tests/SGPdf.App.Tests/PdfOptimizeRepairCapabilityTests.cs`

No changes to:

- packages or lock files;
- third-party manifest or qpdf pin;
- workflow files;
- licenses;
- `MainWindow` or WPF dialogs;
- watermark/numbering/protection product writers;
- `main`.

This checkpoint file is the only additional documentation path.

## Manual QA

- interactive Windows UI QA: **NOT RUN** — no Task 8 UI exists;
- Adobe Reader external-reader QA: **NOT RUN**;
- Foxit external-reader QA: **NOT RUN**;
- Edge external-reader QA: **NOT RUN**;
- private damaged-PDF corpus: **NOT RUN**.

## Task 8 disposition

- Optimize capability/preservation gate: **GO**.
- Generic Repair product feature: **NO-GO**.
- Repair UI: **NOT IMPLEMENTED by design**.
- Task 9: **NOT STARTED**.
- `main` remains frozen pending explicit merge approval.
