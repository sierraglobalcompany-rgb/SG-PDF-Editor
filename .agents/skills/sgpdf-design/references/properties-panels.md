# Properties panels — SG PDF

Use for right-side inspectors, properties panels, configuration panels, and especially the ZPL workspace.

## Purpose

A properties panel is a **contextual assistant for the current task**, not a dump of every property the model exposes.

Preferred information architecture:

1. compact context/summary;
2. task-critical controls;
3. conditional controls revealed by current selections;
4. advanced section for uncommon tuning;
5. clear validation near the affected group;
6. primary/secondary actions with deliberate hierarchy.

## Progressive disclosure rules

- If a setting cannot affect the current workflow, hide/collapse it rather than leaving a long disabled block unless discoverability materially requires otherwise.
- When a parent choice makes child fields relevant, reveal them immediately below that choice.
- Custom values appear only after choosing `Personalizado`/equivalent.
- Keep defaults safe and useful; do not force the user through settings that already have a valid default.
- `ScrollViewer` handles genuine overflow only. Do not use scrolling as justification for unlimited visible sections.
- Preserve capability: simplifying presentation does not mean removing supported functionality.

## ZPL reference pattern

The current ZPL panel contains valuable capability but too much is visible at once. The target pattern is contextual:

### Always/task-first
- short output summary: label size, resolution and effective quantity where useful;
- quantity mode;
- label size;
- output medium/workflow.

### Conditional
- custom quantity value → only for custom quantity;
- custom label width/height → only for custom label size;
- sheet size/orientation/layout → only when output uses A4/Carta/custom sheet;
- custom rows/columns → only for custom sheet layout;
- margins/gaps/rotation → only when relevant to the selected sheet layout or inside an advanced group.

### Thermal exact-size flow
When the output is a thermal label at exact media size, rows, columns, A4/Carta dimensions and unrelated sheet gaps should not dominate the panel.

### Actions
Do not present multiple actions as equally primary. Select the primary action from the approved workflow; keep the other clearly secondary. Never change functional semantics merely to improve styling.

## Grouping

Use meaningful task names such as:
- `Cantidad`
- `Tamaño de etiqueta`
- `Salida`
- `Hoja y distribución`
- `Opciones avanzadas`

Avoid implementation names when a user task term is clearer. Domain terms such as `dpmm` are allowed when operationally necessary; pair them with understandable context (for example approximate DPI).

## Validation and states

- Error text belongs next to the group that needs correction.
- Preserve the user's entered values after validation failure where safe.
- Disabled state must be visually clear and semantically disabled.
- Loading/rendering state must not look like a frozen application.
- Empty state should explain the next useful action.

## KISS implementation

Prefer existing WPF `Grid`, `StackPanel`, `Expander`, `RadioButton`, `ComboBox`, shared styles/resources and existing event/command owners. Do not introduce a generic property-grid framework, plugin system, new MVVM layer, or design framework solely for this redesign.
