# WPF accessibility and implementation — SG PDF

Use when translating an approved UI decision into WPF/XAML.

## Layout

- Prefer `Grid` with `Auto` / `*` sizing for complex/resizable regions.
- Use `StackPanel` for genuinely linear groups, not as a substitute for hierarchy.
- Avoid Canvas/absolute-position forms and margin hacks for structural alignment.
- Avoid fixed widths/heights on resizable content unless the value is an intentional compact-control/token constraint.
- Use Min/Max constraints where they protect usability without blocking DPI scaling.
- A `ScrollViewer` handles real overflow; it must not hide an information-architecture problem.
- Large lists should preserve WPF virtualization/recycling.

## Theme and resources

- Reuse `Themes/Modern.xaml` and existing semantic SG resources before adding local colors/styles.
- Do not hardcode light-only colors into screens.
- Use `StaticResource` for values that are truly static and `DynamicResource` only when runtime theme/resource replacement requires it; avoid nested resource expressions that are invalid for the target property.
- Keep shared spacing/icon/state values consistent instead of hand-tuning each screen.

## Keyboard and focus

- Every essential action must be reachable without a mouse unless the interaction is inherently spatial and has an approved keyboard alternative/limitation.
- Tab order follows visual/task order.
- Focus must remain visible in Light and Dark.
- Menu access keys and existing keyboard shortcuts must not be removed casually.
- Opening/closing a dialog or contextual surface should move/restore focus predictably where practical.
- Do not trap keyboard focus inside a non-modal region.

## Accessibility metadata

- Controls with visible descriptive text normally derive their accessible name from that text; do not duplicate metadata without reason.
- Icon-only controls need `ToolTip` and `AutomationProperties.Name` (or equivalent accessible naming in the owning control/template).
- Add `AutomationProperties.HelpText` only when a short accessible hint materially helps.
- Selection, pressed, expanded, disabled and error state must be represented by actual control semantics where WPF supports it, not merely painted.
- Decorative images/icons should not add noisy duplicate announcements.

## Contrast and state

- Normal text target: at least 4.5:1 against its actual background.
- Meaningful non-text boundaries/focus/state indicators target: at least 3:1 where applicable.
- Color is never the only state/error cue.
- Disabled styling stays legible enough to identify the control while clearly lowering emphasis.
- Verify dividers, focus, selected and disabled states independently in Light and Dark.

## DPI / resize

Mandatory visual gate for changed UI includes Windows 125% scaling. Also verify the approved minimum window size/profile for the affected surface.

No clipped labels, overlapped controls, accidental horizontal scroll or unreadable fixed-size typography. Use WPF device-independent units and layout instead of pixel assumptions.

## Desktop-first adaptation

Do **not** copy mobile safe-area, 44/48dp touch-target, bottom-navigation or gesture rules into SG PDF merely because an external UX guide recommends them for mobile. Apply the underlying accessibility/interaction intent in a Windows desktop form.

## Interaction and performance

- Keep UI thread responsive; long render/OCR/PDF work uses the existing async/background patterns and owners.
- Do not animate width/height or large layout regions for decoration.
- Motion, when used, should communicate state/change and remain restrained.
- Avoid adding animation libraries for polish.

## Testing boundary

Automated XAML/static tests may protect concrete contracts (resource use, control existence, owner wiring, accessibility metadata). They do not prove aesthetics, contrast on the composed screen, clipping at real DPI, or human comprehension; those require real visual QA.
