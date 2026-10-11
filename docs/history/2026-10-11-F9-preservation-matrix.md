# F9 — Preservation Matrix

Date: 2026-10-11  
Branch: `feat/f9-offline-utilities`  
Measured functional head: `3cf85840d26098048c57be2383ad11297875a990`

## Scope

This is the canonical F9 preservation matrix requested by the F9 plan. It consolidates the fresh Task 8 measurement of the qpdf structural/lossless optimize path; it does not infer preservation from earlier F8 writer results.

Fixture: `F8PreservationFixture`, intentionally reused because it contains all representative structures simultaneously. The Task 8 test runs a fresh qpdf rewrite and reopens that output.

## Optimize preservation matrix

| Structure / datum | Fresh F9 evidence after qpdf optimize rewrite | Result |
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
| Existing supported annotation | `TEXT` comment `Existing supported F8` remains | **PROVEN PRESERVED** |
| Unsupported representative annotation | unsupported annotation remains discoverable/read-only | **PROVEN PRESERVED / READ-ONLY** |

This matrix is representative evidence, not a proof for every legal PDF structure or every external reader.

## Optimize semantics measured

qpdf 12.4.2 is invoked with structural/lossless settings:

- object streams generated;
- Flate streams recompressed;
- compression level 9;
- unreferenced objects preserved.

Not used:

- image optimization/recompression;
- JPEG conversion;
- image downsampling;
- rasterization;
- flattening.

Success does not require the output file to be smaller. A valid semantically preserved output is the gate; source and output sizes are measurements only.

## Repair characterization

### Recoverable deterministic damage

A valid rich fixture has `startxref` deliberately replaced by `0`.

Measured result:

- qpdf exit code 3 (warnings);
- output created;
- output reopens through `PdfDocumentSession`;
- page count remains 2;
- damaged source remains byte-identical.

Classification: **RECOVERED WITH WARNINGS**.

### Unrecoverable deterministic damage

A severely truncated PDF contains only an incomplete catalog object.

Measured result:

- qpdf exit code 2;
- no usable output retained;
- damaged source remains byte-identical.

Classification: **UNRECOVERABLE**.

## Product decisions

- Structural/lossless optimize capability: **GO**.
- Generic Repair product feature: **NO-GO** for F9.
- Repair UI: **NOT IMPLEMENTED by design**.
- Task 8 does not expose a WPF Optimize command; the measured capability remains internal in F9.

## Automated evidence

Task 8 RED:

- commit `546c74b59d1e553d8b6ff564268959db7f285619`;
- workflow `38117316903`;
- job `114404689950`;
- build 0 warnings / 0 errors;
- 937 inherited PASS + exactly 5 Task 8 FAIL.

Task 8 functional GREEN:

- commit `3cf85840d26098048c57be2383ad11297875a990`;
- workflow `38117410013`;
- job `114404967287`;
- build 0 warnings / 0 errors;
- **942 PASS / 0 FAIL / 0 SKIP**.

Task 8 checkpoint exact-head:

- checkpoint `1eb4c2e580a158a544356a76358481a0259002f0`;
- workflow `38117535742`;
- job `114405329029`;
- build 0 warnings / 0 errors;
- **942 PASS / 0 FAIL / 0 SKIP**.

## Manual/private gates

- Adobe Reader manual reopen/interoperability: **NOT RUN**.
- Foxit manual reopen/interoperability: **NOT RUN**.
- Edge manual reopen/interoperability: **NOT RUN**.
- private malformed-PDF repair corpus: **NOT RUN**.

Automated preservation evidence must not be represented as manual external-reader compatibility evidence.
