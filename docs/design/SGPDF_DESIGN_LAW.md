# Ley SG de Diseño

**Estado:** obligatoria para cualquier cambio que modifique cómo SG PDF se ve, se entiende o se usa.

## Regla maestra

`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

Esta ley convierte diseño en un gate de ingeniería, no en una decoración al final del desarrollo.

## INTENT — intención

Cada pantalla, panel, diálogo, barra o menú debe responder primero a una tarea humana concreta. Antes de añadir controles hay que poder completar esta frase: **“Esta superficie existe para que la persona pueda ___.”**

Si una superficie intenta resolver demasiadas tareas sin jerarquía, se divide por contexto o se revela progresivamente; no se añaden más secciones por comodidad del código.

## HIERARCHY — jerarquía

La interfaz muestra primero lo que la persona necesita para completar la tarea actual. Debe existir una prioridad visual comprensible entre información, acción primaria, acciones secundarias y opciones avanzadas.

No hacer competir visualmente diez acciones como si todas fueran igual de importantes. Una propiedad técnica no gana protagonismo solo porque exista en el modelo.

## DISCLOSURE — revelado progresivo

Mostrar por defecto únicamente lo necesario para la decisión o acción actual. Las opciones condicionales aparecen cuando la selección del usuario las hace relevantes; las opciones avanzadas permanecen plegadas o fuera del flujo principal hasta necesitarlas.

**Prohibido el “Excel infinito de opciones”**: un `ScrollViewer` no justifica presentar de una vez todas las propiedades disponibles.

No se elimina capacidad por esconder complejidad. Se conserva la función y se reduce el ruido cognitivo.

## CONSISTENCY — consistencia

La misma intención usa el mismo lenguaje, componente, owner, iconografía, espaciado y comportamiento donde sea posible.

- Reutilizar recursos y estilos WPF existentes antes de crear variantes.
- Una acción debe tener un owner funcional claro aunque pueda exponerse desde más de una entrada coherente.
- No duplicar paneles, pipelines o estados para resolver el mismo flujo.
- Mantener la identidad visual oficial de SG PDF y sus iconos aprobados.

## FEEDBACK — respuesta

Toda interacción debe producir una respuesta comprensible y estable: foco, hover cuando aplique, seleccionado, procesando, éxito, error o disabled.

Un control que parece disponible pero no actúa es un defecto de UX. Un proceso que tarda debe informar estado sin bloquear innecesariamente la interfaz. Los errores deben explicar qué ocurrió y qué puede hacer la persona para corregirlo.

## ACCESSIBILITY — accesibilidad

SG PDF debe poder operarse con teclado y debe conservar foco visible, orden de tabulación coherente, nombres accesibles para controles no autoexplicativos y contraste suficiente en Light y Dark.

- Texto normal: objetivo mínimo 4.5:1.
- Controles, límites y estados relevantes: objetivo mínimo 3:1 cuando corresponda a contraste no textual.
- El color nunca es el único indicador de estado o error.
- Los botones solo-icono requieren `ToolTip` y nombre accesible equivalente.
- No aplicar ciegamente reglas táctiles/móviles a un producto desktop WPF.

## VERIFY — verificación

Compilar no cierra una tarea visual. Antes de declarar GREEN una modificación de UI se revisa, como mínimo:

- Light y Dark;
- escalado Windows 125 %;
- teclado, tab order y foco visible;
- estados normal/hover/focus/selected/disabled/error/loading cuando existan;
- ausencia de clipping o scroll horizontal accidental en el tamaño objetivo;
- copy legible y alineación/espaciado consistentes;
- flujo real de la tarea, no solo screenshot estático;
- CI exact-head y pruebas automatizadas aplicables;
- QA manual real se mantiene `NOT RUN` hasta que realmente se ejecute.

## Relación con las demás reglas SG

**KISS** decide la arquitectura mínima. **Anti-ruido** obliga a intentar `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD` antes de sumar UI. La **Ley SG de Diseño** gobierna la experiencia visible. El **Guardrail anti-parches** sigue separado y se usa ante defectos: `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

El guardrail anti-parches no se renombra como una tercera ley numerada.

## Aplicación obligatoria al panel ZPL

El panel de propiedades de etiquetas es el caso de referencia para progressive disclosure:

- siempre visible: resumen de salida, cantidad, tamaño de etiqueta y medio/salida necesarios para el flujo actual;
- cantidad personalizada: mostrar el campo numérico solo cuando se elija esa modalidad;
- tamaño personalizado: mostrar ancho/alto solo cuando se elija `Personalizado`;
- salida térmica a tamaño exacto: no mostrar filas, columnas, tamaño de hoja y separaciones que no participan en ese flujo;
- A4/Carta/hoja personalizada: revelar `Hoja y distribución`;
- márgenes, gaps y rotación avanzada: mostrarlos solo cuando el layout seleccionado los necesita o dentro de una sección avanzada;
- validación: cerca del grupo que necesita corrección;
- acción principal y secundaria: claramente diferenciadas, nunca dos botones compitiendo sin jerarquía.

Esto no autoriza todavía una reimplementación del panel. Cada cambio real sigue pasando por spec/plan, contratos y QA.

## Fuente metodológica

Esta ley fue derivada de una auditoría de patrones útiles de UI/UX Pro Max (`nextlevelbuilder/ui-ux-pro-max-skill`, MIT), prácticas WPF y problemas observados en SG PDF. No se vendorean sus datasets, estilos web/mobile ni su repositorio completo. SG PDF conserva una skill propia, pequeña y desktop-first.

La guía ejecutable está en `.agents/skills/sgpdf-design/SKILL.md`.
