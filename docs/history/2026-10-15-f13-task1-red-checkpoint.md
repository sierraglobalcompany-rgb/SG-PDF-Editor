# Checkpoint — F13 Task 1 RED Proven

**Date:** 2026-10-15

## Stable foundation

- UI-4 / Gate 0: GREEN / CLOSED.
- Accepted UI head: `b4f19fcd5f2854c9025e60385e3b7819bd798a29`.
- Release-truth branch: `integration/ui4-pkcs-release-truth-20261015`.
- Consolidated head: `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`.
- Draft PR #33.
- CI run `38685414773`: SUCCESS.
- Restore locked PASS; Release build 0 warnings / 0 errors; tests `1216/1216 PASS`.
- Consolidated delta is only test-graph PKCS 8.0.2 pin + lockfile; app/runtime unchanged.

## F13 Task 1 current state

Branch: `feat/f13-shell-primitives`
Base: `12406d9e31b566c50174b4c9d7cb15cfb8f4b412`
Head: `97876802fa6326051a185858b18e5c4a0943e4b2`
Draft PR: #34

Added only:
`tests/SGPdf.App.Tests/ShellThemeContractTests.cs`

CI run `38685764535` proves RED intentionally:
- restore PASS
- build PASS, 0 warnings / 0 errors
- `1217 PASS / 1 FAIL / 1218 total`
- failure: `ShellThemeContractTests.ModernTheme_DeclaresApprovedShellPrimitives`
- first missing key: `SgHomeCardStyle`

No product implementation has been added yet for Task 1.

## Resume exactly here

Extend only `src/SGPdf.App/Themes/Modern.xaml` with the minimum approved shell primitives required by the contract, using existing DynamicResource/SG theme resources and no hard-coded light-only hex colors or new UI/icon package.

Then focused tests → full suite → exact-head CI. Only after GREEN advance to F13 Task 2 ToolCatalog.

## Guardrails

KISS, anti-noise, anti-patch remain mandatory.
`main` untouched/read-only. No merge without explicit user approval.
Do not use Codex unless Windows-local/manual evidence is required.
