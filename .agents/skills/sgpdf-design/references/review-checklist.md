# SG PDF design review checklist

Use immediately before calling UI work complete. Mark only evidence actually executed.

## Task and hierarchy

- [ ] Can the purpose of the changed surface be stated in one sentence?
- [ ] Is the primary action obvious without competing equally with secondary actions?
- [ ] Is task-critical information visible before advanced detail?
- [ ] Are irrelevant controls hidden/collapsed instead of merely adding more scrolling?
- [ ] Is any conditional field revealed next to the choice that makes it relevant?

## Consistency / anti-noise

- [ ] Existing SG style/resource/action owner was reused where possible.
- [ ] No duplicate panel, toolbar, command owner or design framework was introduced.
- [ ] Spacing/alignment/icon style follows the existing design system rather than per-screen invention.
- [ ] Official app/document/Home tool icon roles remain unchanged unless that exact authority is the approved task.
- [ ] User-facing Spanish uses the same terms as related menus/dialogs/panels.

## Feedback and forms

- [ ] Interactive controls have clear applicable hover/focus/selected/disabled/loading/error states.
- [ ] No control appears actionable when it is not.
- [ ] Labels stay visible; placeholders are not used as the only label.
- [ ] Validation appears near the affected field/group and explains the next action.
- [ ] Defaults support the common safe path.

## Accessibility

- [ ] Keyboard can reach the essential workflow.
- [ ] Tab order matches the visual/task order.
- [ ] Focus is visible in Light and Dark.
- [ ] Icon-only controls have tooltip + accessible name.
- [ ] Information/state does not depend on color alone.
- [ ] Text/non-text contrast was checked where the change affects colors or states.

## Windows visual verification

- [ ] Light theme checked.
- [ ] Dark theme checked.
- [ ] Windows 125% scaling checked.
- [ ] Approved minimum/representative window size checked.
- [ ] No clipped text, overlap, accidental horizontal scrolling or inaccessible action.
- [ ] Empty/error/disabled/loading states relevant to this change were checked.

## Engineering evidence

- [ ] Focused tests/contracts PASS.
- [ ] Full suite PASS.
- [ ] Exact-head CI PASS.
- [ ] Manual/hardware/private QA remains explicitly `NOT RUN` if not actually executed.
- [ ] `.planning/STATE.md` / ROADMAP changed only if routing or durable truth actually changed.
- [ ] No merge to `main` occurred without explicit approval.

Any unchecked item that applies to the task keeps the UI work open or documents an explicit environment/coverage limitation.
