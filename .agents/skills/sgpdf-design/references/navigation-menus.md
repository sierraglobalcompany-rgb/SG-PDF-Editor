# Navigation and menus — SG PDF

Use for menu bar, toolbars, Home, side rails, contextual actions and document navigation.

## Placement by intent

- **Menu bar:** global/infrequent commands and discoverable application actions.
- **Toolbar/context bar:** frequent actions for the current document/task.
- **Home:** starting tasks and utilities, not a second copy of every contextual command.
- **Side rail/panel:** context or mode selection only when it materially helps the current workflow.

Do not expose the same command everywhere simply because space exists. Multiple entry points may call one owner, but each entry point must have a clear usage reason.

## Hierarchy

- Keep the top-level navigation stable and predictable.
- Group commands by user task, not by implementation class.
- Use separators only when they clarify groups; separator noise is still noise.
- One region should have one obvious primary task/action at a time.
- Avoid duplicated headings or duplicated tool categories such as parallel `Herramientas` surfaces with the same purpose.

## Wording

- Commands use clear verbs: `Abrir`, `Guardar como`, `Imprimir`, `Aplicar`, `Cerrar`.
- Use Windows-style `...` when a command opens a dialog or requires more information before it can execute.
- Keep Spanish sentence case and terminology consistent across menu, tooltip, dialog and status text.
- Do not shorten labels until their meaning becomes ambiguous merely to fit a bad layout.

## Keyboard and discoverability

- Preserve coherent access keys in menus (`_Archivo`, `_Abrir`, etc.).
- Frequent document commands should remain reachable with keyboard where the product already defines shortcuts.
- Icon-only commands require `ToolTip` plus an accessible name.
- Focus order follows visual/task order.
- Hover is supplemental; never make essential information available only on hover.

## States

- Disabled commands are semantically disabled and visually lower emphasis.
- If the reason for disabled state is not obvious, explain it via nearby state/status/help rather than letting the command appear broken.
- Selection/mode state must be persistent enough to answer “¿dónde estoy?” without relying solely on color.

## KISS guardrail

Do not build a generic navigation framework, command registry, plugin menu system or duplicated routing layer unless an approved requirement proves the existing shell cannot support the task. Reuse current action owners and handlers first.
