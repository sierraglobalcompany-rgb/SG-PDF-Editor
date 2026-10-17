# Forms and UX copy — SG PDF

Use for labels, inputs, helper text, buttons, validation, dialogs and status messages.

## Human language first

Write for the person performing the task, not for the internal implementation.

- Buttons: concise verbs/action phrases.
- Field labels: visible nouns or short phrases; never placeholder-only labels.
- Sections: task concepts, not class/service names.
- Helper text: only when it prevents a likely error or explains a necessary technical term.
- Units belong in the label or value format: `Ancho (mm)`, `203 dpi`, etc.

Technical domain language may remain when users genuinely need it. Example: thermal users may need `dpmm`; pair it with approximate DPI or helper text instead of replacing it with an inaccurate simplification.

## Spanish consistency

- Prefer natural Spanish over implementation jargon.
- Use the same noun and verb for the same concept across Home, menus, panels and dialogs.
- Prefer `Distribución` over `layout` in user-facing text unless an approved domain requirement says otherwise.
- Prefer `Guardar como PDF...` over vague `Exportar` when the resulting action is specifically saving a PDF and that wording matches the product contract.
- Do not over-explain obvious controls.

## Forms

- Keep labels visible when fields contain values.
- Place dependent fields immediately under/near the choice that reveals them.
- Choose sensible defaults that make the common path immediately usable.
- Do not validate aggressively on every keystroke when the value is naturally incomplete while typing; validate after a meaningful boundary (blur/apply/submit) unless instant validation materially helps.
- Preserve entered data after a correctable error where safe.
- Use the native control that best matches the choice: radio for a small exclusive set, combo for compact larger sets, checkbox for independent boolean state, text input only for free/custom values.

## Errors and feedback

A useful error answers:
1. what could not be completed;
2. what is wrong, when known;
3. what the user can do next.

Place field/group errors near the source of the problem. A top-level summary may complement inline errors for a long multi-error form, but must not replace them.

Avoid:
- `Error inesperado` when a specific cause is known;
- internal exception/class names in normal user copy;
- success dialogs for trivial actions when inline/status feedback is enough;
- warnings that do not explain consequence or next action.

## Action hierarchy

A dialog or panel should have one clear primary action. Secondary/cancel/destructive actions receive lower or distinct emphasis. Destructive actions are separated enough to reduce accidental activation and use explicit wording.
