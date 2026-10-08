# Phase 3 — F2 ZPL Workspace — Continuity Index

**Status:** F2.1–F2.6 automated PASS; private + physical QA remain NOT RUN.  
**Purpose:** restore the missing GSD Phase 3 continuity node without rewriting historical commits/files.

## Historical note

Early F2 GSD slice files were created under `.planning/phases/02-f1-zpl-gate/` while the Gate and first product slices were being separated. They are historical artifacts and are intentionally not moved now because moving them would create unnecessary churn and weaken historical links.

Existing historical GSD files include:

- `.planning/phases/02-f1-zpl-gate/F2.1-PLAN.md`
- `.planning/phases/02-f1-zpl-gate/F2.2-PLAN.md`
- `.planning/phases/02-f1-zpl-gate/F2.3-PLAN.md`
- `.planning/phases/02-f1-zpl-gate/F2.4-PLAN.md`
- `.planning/phases/02-f1-zpl-gate/F2.5-PLAN.md`

F2.6 has its detailed Superpowers plan instead of a duplicate GSD file:

- `docs/superpowers/plans/2026-10-07-f2-6-validation-hardening.md`

## Authoritative detailed artifacts

### Architecture
- `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md`

### Plans
- `docs/superpowers/plans/2026-10-07-f2-1-zpl-parse-open.md`
- `docs/superpowers/plans/2026-10-07-f2-2-labelize-preview.md`
- F2.3 historical GSD plan above.
- `docs/superpowers/plans/2026-10-07-f2-4-layout-pdf-export.md`
- `docs/superpowers/plans/2026-10-07-f2-5-windows-thermal-print.md`
- `docs/superpowers/plans/2026-10-07-f2-6-validation-hardening.md`

### Histories
- `docs/history/2026-10-07-F2.1.md`
- `docs/history/2026-10-07-F2.2.md`
- `docs/history/2026-10-07-F2.3.md`
- `docs/history/2026-10-07-F2.4.md`
- `docs/history/2026-10-07-F2.5.md`
- `docs/history/2026-10-07-F2.6.md`

## Final heads / PRs

| Slice | Branch | PR | Final head | Automated status |
|---|---|---:|---|---|
| F2.1 | `feat/f2-1-zpl-parse-open` | #13 | `d6d69db0c8998eca569d5e4434e6c5c18234fabc` | PASS |
| F2.2 | `feat/f2-2-labelize-preview` | #14 | `9af600ce19812adb67a11718a47734c346b51bfc` | PASS |
| F2.3 | `feat/f2-3-quantity-dimensions` | #15 | `1f33f48390681c6e4000c17329f1763eb1551047` | PASS |
| F2.4 | `feat/f2-4-layout-pdf-export` | #16 | `1355ddf1ea92c7b3b25addf05f7722dec6fe57b9` | PASS |
| F2.5 | `feat/f2-5-windows-thermal-print` | #17 | `c003d6512a5df5be2f53aab2262d09c6dcbf92cf` | PASS |
| F2.6 | `feat/f2-6-validation-hardening` | #18 | `6c7d60a5bad22db20860685e955aa5ef03fbfa19` | PASS |

All remain stacked draft/unmerged by design.

## Open acceptance gates

- Private real Mercado Libre label corpus: NOT RUN.
- Physical thermal printer/ruler/feed/clipping QA: NOT RUN.
- Physical Code128/QR scanner QA: NOT RUN.

Synthetic/automated PASS does not upgrade those gates.
