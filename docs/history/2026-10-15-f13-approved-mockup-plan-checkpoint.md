# SG PDF Editor — Checkpoint F13 approved mockup implementation plan

**Fecha:** 2026-10-15  
**Estado:** F13 MOCKUP APPROVED / SPEC APPROVED / PLAN READY FOR USER REVIEW / PRODUCT CODE NOT STARTED

## Protected refs

- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7` — read-only.
- UI-4 tested head: `feat/release-ui-modernization` @ `abd52d05279e3e656723ad4ff5a21d36de91508e`.
- PKCS isolated GREEN: `feat/fix-pkcs-hygiene` @ `781cd6060121367f4d51efbe2724d820d1dfb842`.
- Planning branch: `plan/post-ui4-f13-f18-expansion-20261014`.

## Current blocker

UI-4 remains `FAIL-P1`. F13 product-code implementation is blocked until the Windows rerun has no P0/P1.

## F13 authoritative artifacts

1. Visual/spec contract: `docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`
2. Technical implementation plan: `docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`
3. Mockup identity/hash: `docs/design/2026-10-15-f13-approved-mockup-reference.md`
4. State: `.planning/STATE.md`

The old `docs/superpowers/plans/2026-10-14-f13-product-shell.md` is `SUPERSEDED / DO NOT EXECUTE` and now only points to the new authority.

## F13 implementation structure

- Gate 0: close UI-4 P0/P1.
- Task 1: SG shell visual primitives/icons.
- Task 2: static capability-honest tool catalog.
- Task 3: approved Home Dashboard.
- Task 4: Home/PDF/ZPL shell state.
- Task 5: additive document rail routed to existing owners.
- Task 6: contextual toolbar + right contextual frame.
- Task 7: prove equivalence, then delete redundant centered navigation.
- Task 8: official application icon, source-gated.
- Task 9: full CI + real Windows visual QA + F13 closure.

## Frozen guardrails

- KISS / Noise / Anti-patch laws remain mandatory.
- No new UI framework/package.
- No plugin/event-bus/navigation-framework architecture.
- No second PDF engine or writer.
- No backend/cloud/login/AI/advertising baseline.
- No capability inflation: F13 says `PDF a PNG`; JPG is F16. `Imagen a PDF` may appear only disabled/`Próximamente` before F16.
- Mockup red `SG` is not the official app icon.
- Main remains untouched without explicit user approval.
- ChatGPT/native implementation is default; Codex only for Windows/manual gates.

## Next action

User reviews the F13 technical plan. After approval, do not bypass Gate 0: first finish the Windows P1 diagnostic/UI-4 stabilization. Once UI-4 is GREEN, execute F13 task-by-task with RED→GREEN, full suite, exact-head CI and bounded checkpoints.
