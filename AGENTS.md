# Instrucciones para agentes de desarrollo

## Autoridad operativa

Orden obligatorio antes de tocar código o documentación operativa:

1. GitHub live: exact HEAD + CI exact-head;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice concreto;
6. para cualquier cambio visible/interactivo: `docs/design/SGPDF_DESIGN_LAW.md` + `.agents/skills/sgpdf-design/SKILL.md`;
7. `docs/MASTER_CONTEXT.md` para arquitectura y decisiones durables;
8. `docs/MASTER_PLAN.md` y `docs/history/**` como contexto histórico, nunca para sobreescribir estado más nuevo.

Los snapshots viejos pueden contener estados de fase ya sustituidos. GitHub + STATE mandan sobre cualquier histórico.

## Contexto mínimo antes de actuar

1. leer `.planning/STATE.md`;
2. comprobar HEAD/CI reales de la rama indicada por STATE;
3. leer ROADMAP + REQUIREMENTS del bloque actual;
4. abrir solo los archivos concretos necesarios;
5. si la tarea cambia UI/UX, leer obligatoriamente `.agents/skills/sgpdf-design/SKILL.md` y solo sus referencias pertinentes;
6. usar Graphify/local tooling solo si reduce lectura/impacto;
7. consultar MASTER/históricos solo ante arquitectura, licencia, regresión, contradicción o mantenimiento de un slice cerrado.

## Estado actual — 2026-10-17

- F0–F12: **CLOSED / FROZEN** en el sentido automatizado ya documentado; QA real/manual/private/physical permanece separado donde corresponda.
- F13: **ACTIVE**.
- F13 Tasks 1–8: cerradas según sus checkpoints; Task9 está activo.
- Task9 T9.0: DONE.
- Task9 T9.1: ACTIVE — realineamiento visual/document shell y UX.
- Último anchor verde inmediatamente anterior al R1 multidocumento: `0abae7ceb5f08611180c883346e21589d1509664`, exact-head CI run `39000181654` SUCCESS.
- R1 multidocumento/document-shell está **RED / INCOMPLETE** en `feat/f13-task9-t9-1-document-shell-realignment-20261017` @ `bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`; no tratarlo como baseline verde ni continuar allí mientras DG-0 se valida.
- DG-0 SG Design Governance se valida aisladamente desde `0abae7...` en `feat/f13-dg0-sg-design-governance-20261017`.
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`, read-only hasta aprobación explícita.
- No merge aprobado.

## Leyes obligatorias

### KISS

`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.

No arquitectura preventiva, capas genéricas, segundo sistema ni framework porque “puede servir después”.

### Anti-ruido

Antes de ADD: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

No borrar por intuición: demostrar redundancia, sustitución o falta de valor. Evitar Markdown-per-commit, ramas duplicadas, wrappers paralelos, settings redundantes y abstracciones especulativas.

### Ley SG de Diseño

Para cualquier UI/UX:

`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

Autoridad completa: `docs/design/SGPDF_DESIGN_LAW.md`.
Skill ejecutable: `.agents/skills/sgpdf-design/SKILL.md`.

Regla operativa: no diseñar paneles como un “Excel infinito” de opciones. La complejidad se revela según intención/contexto sin eliminar capacidad. Menús, barras, paneles, copy, iconos, estados y alineaciones deben pasar el gate de diseño antes de implementación.

### Guardrail anti-parches

Es un guardrail separado, no una ley numerada:

`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

No parchear síntomas, duplicar owners o apilar workarounds.

## Gate obligatorio para UI/UX

Antes de cambiar XAML, copy, navegación o interacción se debe establecer:

1. intención humana de la superficie;
2. jerarquía: acción primaria/secundarias/información/avanzado;
3. qué queda visible por defecto;
4. qué se revela condicionalmente;
5. lenguaje, unidades, ayudas y errores;
6. teclado/foco/estados/accesibilidad;
7. evidencia de cierre: Light, Dark, 125% scaling, estados, clipping, tests y CI.

Después aplicar solo la referencia pertinente de `.agents/skills/sgpdf-design/references/`.

Compilar no equivale a QA visual. CI no convierte pruebas manuales/hardware/private en PASS.

## Arquitectura congelada / límites

- Windows x64 + WPF + .NET 10 mientras no exista evidencia material para cambiar.
- Mantener `SGPdf.App + SGPdf.App.Tests` mientras sea suficiente.
- PDFium es motor PDF principal; toda llamada nativa pasa por `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 es el único renderer ZPL runtime, proceso local pinneado; BinaryKits es histórico.
- No Labelary ni APIs web runtime.
- PDFsharp 6.2.4 tiene uso estrecho aprobado; no segundo motor PDF general.
- qpdf 12.4.2 permanece sidecar estrecho para protección AES aprobada.
- Tesseract permanece OCR local/offline.
- ZXing.Net permanece test/QA-only.
- No backend, DB, cloud, plugin system, segundo viewer, segundo PDF engine, ModeManager genérico, conversion framework genérico ni BatchEngine genérico sin decisión explícita.
- No nuevo framework UI para “pulir” lo que WPF/resources/templates existentes pueden resolver.
- No AGPL/GPL fuerte/licencia comercial obligatoria sin aprobación explícita.

## Autoridad visual congelada

- Icono de aplicación, icono de documento PDF y los 15 iconos Home aprobados tienen roles distintos y no se reinterpretan durante otros cambios.
- `PDF a PNG` sigue siendo el contrato funcional aunque su arte oficial sea genérico PDF→imagen; no prometer JPG sin capacidad real.
- Asociación de archivos Windows pertenece al instalador futuro; no hacks de Registry durante UI work.

## Seguridad de datos y guardado

- Datos reales Mercado Libre/clientes nunca se versionan, CI, Graphify ni historial público.
- Private QA usa rutas ignoradas cuando aplique.
- Preservar originales; `Guardar como`/outputs separados donde el baseline lo define.
- No afirmar fidelity/preservation/interoperability universal sin evidencia.
- Foto real, stylus/touch, impresora, scanner, PFX real, lector externo, private corpus y offline físico requieren evidencia real separada.

## Tooling de desarrollo

- GSD Core y Graphify son dev-only.
- `.codex/`, `.devtools/`, `graphify-out/`: regenerables/ignorados.
- `.planning/`: versionado.
- `.agents/skills/sgpdf-design/`: gobernanza versionada, no dependencia runtime.
- Ningún tooling de desarrollo es requisito del producto instalado.

## Git y ramas

- No merge a `main` sin aprobación explícita del usuario.
- No force-push/rebase por comodidad.
- No limpieza destructiva de ramas durante auditoría.
- No reusar una rama histórica divergente como base operativa.
- Preservar evidencia exact-head.
- Una rama RED/incompleta nunca sustituye al último anchor GREEN como base de validación transversal.

## Flujo por slice o bloque

1. verificar STATE + HEAD + CI;
2. clasificar feature/fix/QA/docs/integración/release;
3. aplicar KISS + anti-ruido;
4. si cambia UI/UX, ejecutar Ley SG de Diseño + skill antes de implementar;
5. reutilizar owners/evidencia existente;
6. usar rama aislada cuando corresponda;
7. comportamiento nuevo/fix: RED→GREEN y cambio mínimo;
8. build/tests/CI exact-head;
9. QA real cuando aplique;
10. actualizar STATE/ROADMAP solo si cambió el ruteo o verdad durable;
11. no merge sin aprobación explícita.

## Orden actual

```text
F0–F12                                      ✅ CLOSED / FROZEN (auto)
F13 Tasks 1–8                              ✅ CLOSED
F13 Task9 / T9.0                           ✅ DONE
F13 Task9 / T9.1                           ▶ ACTIVE
DG-0 SG Design Governance                  ▶ validar antes de continuar R1
R1 document-shell/multi-document           🟥 RED / congelado mientras DG-0
T9.2                                       ⏳ no iniciado
F14–F18                                    🔒 FROZEN
main                                       🔒 sin aprobación explícita
```

Cuando DG-0 quede GREEN, reanudar R1 desde su checkpoint conservando la gobernanza validada; no adelantar T9.2 automáticamente.
