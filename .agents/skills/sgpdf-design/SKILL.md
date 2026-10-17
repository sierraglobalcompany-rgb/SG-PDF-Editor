---
name: sgpdf-design
description: "Mandatory SG PDF UI/UX governance for WPF surfaces. Use before designing, building, reviewing, or fixing menus, bars, panels, dialogs, forms, copy, icon presentation, layout, states, accessibility, visual hierarchy, or any interaction visible to the user."
---

# SG PDF Design Skill

Project-owned design intelligence for **SG PDF Editor**, a Windows x64 / WPF / .NET 10 desktop productivity tool.

This skill is mandatory when work changes how SG PDF **looks, reads, behaves, navigates, exposes options, communicates state, or receives user input**. Skip it for pure backend/core work whose user-facing behavior and presentation do not change.

## Authority

Read in this order for UI work:

1. live GitHub exact HEAD + exact-head CI;
2. `.planning/STATE.md`;
3. approved slice/spec/plan;
4. `docs/design/SGPDF_DESIGN_LAW.md`;
5. this skill;
6. only the relevant reference file below;
7. current XAML/resources/handlers that own the affected surface.

Do not let an older mockup, historical Markdown, external style guide, or upstream skill override newer SG PDF product authority.

## Ley SG de Diseño

`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

### INTENT
State the user's task in one sentence before moving controls. If the intent is unclear, audit the existing workflow before designing.

### HIERARCHY
Identify the primary action, secondary actions, task-critical information, and supporting settings. Equal visual weight for everything is a design failure.

### DISCLOSURE
Use **progressive disclosure**. Default UI contains only task-relevant controls; conditional settings appear when the user's choice makes them relevant; advanced settings remain out of the main path until needed. A long `ScrollViewer` is an overflow mechanism, not an information architecture.

### CONSISTENCY
Reuse existing SG resources, components, action owners, wording, icon authority, spacing rhythm, and interaction patterns before adding variants.

### FEEDBACK
Every interactive control has an understandable normal/hover/focus/selected/disabled/loading/error/success state as applicable. No visually active dead controls.

### ACCESSIBILITY
Keyboard first-class; visible focus; coherent tab order; labels remain visible; icon-only controls have tooltips and accessible names; semantic state is not communicated only by color; verify Light/Dark contrast.

### VERIFY
UI work is not GREEN because it compiles. Verify actual flow, Light/Dark, 125% Windows scaling, keyboard/focus, states, clipping/overflow, copy, automated contracts, and exact-head CI. Manual/hardware QA remains NOT RUN until executed.

## Mandatory pre-implementation design gate

Before editing XAML or user-facing copy, write or establish these seven facts in the task/spec/plan:

1. **Intent:** what the person is trying to accomplish.
2. **Hierarchy:** what is primary, secondary, informational, or advanced.
3. **Default visibility:** what must be visible immediately.
4. **Conditional disclosure:** what appears only after a relevant choice/state.
5. **Language:** user-facing terms, units, labels, help and errors.
6. **Interaction/accessibility:** keyboard, focus, state and error behavior.
7. **Verification:** exact visual/behavioral evidence needed to close the task.

If those facts are absent, do not compensate by adding more controls.

## SG PDF design direction

- Desktop productivity tool, not decorative dashboard.
- Windows-native/Fluent-compatible visual language using the existing theme system.
- Quiet surfaces, strong task hierarchy, restrained motion.
- Spanish copy should be human and action-oriented; retain technical terms only when users need them to operate the domain correctly (for example `dpmm` in thermal printing), with helper text when useful.
- Prefer A→B workflow over mode forests, nested panels, duplicate toolbars, or generalized navigation frameworks.
- Preserve approved app/document/home icon authority; do not reinterpret official artwork during unrelated UX work.
- No new UI framework/package simply to achieve polish that WPF/resources/templates can already provide.

## Anti-patterns — reject before implementation

- “Excel infinito” property panels that expose every available field at once.
- Multiple primary buttons competing in the same region.
- Duplicate owners for the same action because two screens need it.
- Placeholder-only labels.
- Errors only at the top of a long form when the faulty field is elsewhere.
- Technical implementation language where task language exists.
- Arbitrary margins, widths, colors or icon styles copied per screen.
- Hardcoded light-only colors that bypass theme resources.
- Icon-only commands without tooltip/accessibility name.
- Disabled-looking controls that still act, or active-looking controls that do nothing.
- Scrollbars used to excuse poor grouping or disclosure.
- Copying mobile/web patterns 1:1 into WPF without a desktop reason.
- Vendoring the upstream UI/UX Pro Max datasets/repository into SG PDF.

## Reference routing

Read only what the task needs:

- Properties / inspectors / right panels / ZPL → `references/properties-panels.md`
- Menus / toolbar / navigation / rail / action placement → `references/navigation-menus.md`
- Labels / buttons / helper text / validation / Spanish UX copy → `references/forms-copy.md`
- WPF layout / theme / keyboard / focus / DPI / AutomationProperties → `references/wpf-accessibility.md`
- Final review / visual QA → `references/review-checklist.md`

## Workflow

1. Verify exact branch/HEAD/CI and current task scope.
2. Read the Design Law and only the relevant reference.
3. Audit the existing surface: preserve capabilities and owners before proposing movement/removal.
4. Apply anti-noise first: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
5. Freeze a minimal UI contract/mockup when the change is meaningful.
6. For behavior changes, use RED→GREEN. For structural governance/markup, use focused static contracts only when they prevent a real regression; do not test aesthetics mechanically.
7. Implement the smallest coherent change.
8. Run focused tests, full suite and exact-head CI.
9. Execute the review checklist. Do not convert unexecuted manual QA into PASS.
10. Update STATE/ROADMAP only when routing or durable product truth changed.

## Upstream relationship

UI/UX Pro Max was audited as an external reference. SG PDF absorbs useful principles such as accessibility-first review, progressive disclosure, visible labels, inline feedback, navigation hierarchy, semantic design tokens and WPF-specific implementation guidance. It does **not** import unrelated web/mobile datasets, styles, frameworks, animation presets, or product palettes.
