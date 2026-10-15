# F13 — Auditoría de alineación con mockup aprobado y corrección de continuidad

**Fecha:** 2026-10-15  
**Estado:** `CORRECTION REQUIRED BEFORE TASK 5 CLOSE`  
**Alcance:** solo F13 shell/UI. No cambia F0–F12, engines, F14 routing, F15 Edit V3, paquetes ni `main`.

## Autoridad visual

- Mockup aprobado: `sg_pdf_f13_approved_mockup.jpg`.
- Spec: `docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`.
- Plan: `docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`.
- Implementación auditada: F13 base `12406d9e31b566c50174b4c9d7cb15cfb8f4b412` → Task 5 head `eb0be24071a9b205a6386e57e54912b3600ab3b1`.

El mockup manda en jerarquía, iconografía, rail, Home, workspace y panel contextual; no exige pixel-perfect si eso rompe responsive, accesibilidad o comportamiento nativo Windows.

## Veredicto

```text
ARQUITECTURA / OWNERSHIP     = ALINEADA
HOME STRUCTURE               = ALINEADA PARCIALMENTE
DOCUMENT RAIL STRUCTURE      = ALINEADA PARCIALMENTE
ICONOGRAPHY                  = NO ALINEADA
TOP COMMAND / CONTEXT FRAME  = TODAVÍA INCOMPLETO
OFFICIAL ICON SOURCE         = CONFIRMADO / NO BLOQUEADO POR APROBACIÓN
TASK 5                       = NO GREEN
F13 DESIGN ALIGNMENT         = PARTIAL — CORRECTION REQUIRED
```

No se justifica descartar Tasks 1–4: su estructura, catálogo y state owner son reutilizables. Sí se requiere corregir la ejecución visual antes de continuar a Task 6.

## Hallazgos materiales

### 1. Home cards ignoran `IconKey`

`ToolCatalog` ya define iconos distintos (`ShellIconMerge`, `ShellIconSplit`, `ShellIconEdit`, etc.), pero `HomeDashboard.xaml` renderiza el mismo literal `PDF` dentro de todos los icon tiles.

**Ruling:** deuda visual real. El mockup exige una identidad visual distinta por herramienta. No añadir paquete de iconos runtime; usar geometrías/vector resources WPF locales y reutilizables.

### 2. Home rail está text-only

`Inicio`, `Herramientas`, `Recientes` y `Configuración` existen y respetan la jerarquía, pero no usan icono + texto como el mockup aprobado.

**Ruling:** corregir dentro del mismo lenguaje visual del rail; no crear navegación nueva.

### 3. Document rail está text-only

Task 5 crea el rail de 96 px y el orden correcto, pero cada `Button.Content` es solo `descriptor.Title`. `descriptor.IconKey` no se consume.

**Ruling:** Task 5 no puede cerrarse visualmente así. El rail debe mostrar icono + texto, conservar el accessible name y presentar selected/disabled de forma clara. El selected state no debe depender solo del color.

### 4. Top command/context/document controls aún conservan demasiado baseline legacy

`MainWindow.xaml` todavía contiene el menú y navigation bars anteriores. Esto es parcialmente esperado por la migración aditiva, pero el cierre F13 debe converger al mockup:

- global command row: Archivo / Editar / Ver / Herramientas / Ayuda + búsqueda / zoom / imprimir;
- contextual toolbar separada de navegación global;
- right context frame ~320 px scrollable;
- controles de página/zoom con jerarquía visual coherente con el mockup;
- sin duplicación visual final entre rail nuevo y navegación legacy.

**Ruling:** reforzar criterios de Task 6–7; no anticipar negocio de F14/F15.

### 5. Icono oficial: el supuesto bloqueo de fuente es obsoleto

La imagen aprobada ya existe en la Library del proyecto con nombre:

`Icono PDF con lápiz azul y logo SG(1).png`

Referencia visual: hoja PDF con logo SG azul y lápiz azul, fondo exterior transparente.

**Ruling:** Task 8 ya no está bloqueado por falta de aprobación/fuente. Si la capa de herramientas no puede materializar los bytes directamente, eso se clasifica como limitación de transferencia/tooling, no como falta de diseño aprobado. No pedir una nueva decisión visual ni inventar otra imagen.

## Corrección de ejecución

1. **No avanzar a Task 6 todavía.** Mantener Task 5 abierto.
2. Preservar `eb0be240...`; no rewind/rebase destructivo.
3. Clasificar los 2 fallos actuales de Task 5 antes de alterar producto; no parchear tests ni producción a ciegas.
4. Ampliar el contrato visual de Task 5 para exigir:
   - consumo real de `IconKey`;
   - icono + texto en document rail;
   - selected/disabled accesibles;
   - ninguna ruta F14 activada.
5. Corregir deuda Home descubierta:
   - iconos distintos en cards;
   - icono + texto en Home rail;
   - conservar estructura/owners existentes.
6. GREEN focal → full Release suite → exact-head CI.
7. **Task 6 reforzada:** contextual toolbar + right frame + global command hierarchy visible coherente con mockup, sin business rewrite.
8. **Task 7 reforzada:** eliminar solo navegación legacy probadamente redundante y converger controles de documento sin saltarse guards.
9. **Icono oficial:** integrarlo en F13 antes de la QA visual final; usar la fuente ya aprobada. Verificar ventana, exe, taskbar y Alt+Tab. El instalador reutilizará la misma identidad después.
10. **Task 9:** Windows QA lado-a-lado contra mockup para Dashboard y Documento; no cerrar F13 por tests estructurales solamente.

## Guardrails

- KISS: corregir presentación, no reescribir shell.
- Noise Law: reutilizar `ToolCatalog`, `IconKey`, SG resources y owners actuales.
- Anti-patch: los 2 fallos actuales se clasifican antes de cualquier cambio.
- No MahApps/MaterialDesign/HandyControl/icon package runtime.
- No F14 routing dentro de F13.
- No F15 Edit V3 anticipado.
- No `main`, merge ni force push.

## Criterio de salida corregido de F13

F13 no es GREEN hasta que:

- Home y documento reproduzcan la jerarquía visual aprobada;
- Home cards y ambos rails tengan iconografía real, no placeholders genéricos;
- rail de documento conserve owners/guards;
- toolbar contextual y panel derecho estén alineados;
- navegación legacy redundante haya sido eliminada después de demostrar equivalencia;
- icono oficial esté integrado o exista un bloqueo técnico explícito de transferencia, no de aprobación;
- full suite + exact-head CI sean GREEN;
- Windows QA lado-a-lado no encuentre P0/P1.

## Decisión

**No continuar el plan como si Task 5 estuviera casi cerrado.** La arquitectura se conserva, pero el siguiente trabajo productivo debe ser la corrección visual de Task 5/Home conforme a este audit. Este documento actúa como override de continuidad sobre cualquier texto anterior que trate el icono oficial como “fuente no aprobada/no suministrada”.
