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
8. `docs/history/**` como checkpoints/evidencia; nunca sobreescriben GitHub+STATE.

Los snapshots viejos pueden contener estados ya sustituidos. GitHub + STATE mandan sobre cualquier histórico.

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
- F13: **ACTIVE**. Tasks 1–8 cerradas según sus checkpoints.
- Task9 T9.0: DONE.
- Task9 T9.1: ACTIVE.
- DG-0 SG Design Governance: **GREEN / CLOSED**.
- R1 reader multi-document core: **GREEN / CLOSED** en `454dcadb6eca13ab98b5d219146dd10ec25af78d`.
- R1B per-document pending state + close guard: **GREEN / CLOSED técnicamente** en `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`.
- Exact-head CI R1B: run `39040326178`, job `117418290462`, Build PASS 0 warnings/0 errors, startup smoke PASS 30 s, tests `1274 passed / 0 failed / 4 skipped / 1278 total`.
- Los 4 skips corresponden deliberadamente a R2–R5 y no deben borrarse: tabs, rail con arte oficial, drawer Páginas/Marcadores y context menu PDF.
- Rama canónica: `feat/f13-task9-t9-1-document-shell-realignment-20261017`.
- Próximo bloque elegible: **T9.1-R2 — top shell + real document tabs**. No está iniciado por este checkpoint.
- Último anchor GREEN de producto previo al realineamiento: `0abae7ceb5f08611180c883346e21589d1509664`.
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`, read-only hasta aprobación explícita.
- No merge aprobado. T9.2 no iniciado. F14–F18 frozen.

Checkpoint de continuidad R1B:
`docs/history/2026-10-17-f13-t9-1-r1b-pending-state-green-checkpoint.md`.

### Verdad durable de R1B

- cada `PdfDocumentWorkspace` posee un `PdfDocumentPendingState`;
- cambiar de PDF captura/detacha/restaura el estado de modelo del documento, no guarda controles WPF;
- Edit, Sign, Comments, Organize y AcroForm reutilizan sus owners existentes;
- cierre dirty usa guard explícito antes de disposal;
- abrir otro PDF, Ctrl+O y Recientes no deben forzar discard del trabajo pendiente del documento actual;
- Open ZPL, cambios de modo y cierre de ventana conservan sus guards correspondientes;
- `MainWindow.Comments.Integration.cs` fue eliminado como owner obsoleto de guards de apertura PDF;
- no se introdujo ModeManager, DI container, event bus ni framework documental.

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

No diseñar paneles como un “Excel infinito” de opciones. La complejidad se revela según intención/contexto sin eliminar capacidad.

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

## Git y ramas

- No merge a `main` sin aprobación explícita del usuario.
- No force-push/rebase por comodidad.
- No limpieza destructiva de ramas durante auditoría.
- No reusar una rama histórica divergente como base operativa.
- Preservar evidencia exact-head.
- Una rama RED/incompleta nunca sustituye al último anchor GREEN como base transversal hasta que su exact-head CI sea verde.
- Los contratos futuros pueden permanecer `Skip` con fase explícita; no borrarlos para maquillar un gate.

## Flujo por slice o bloque

1. verificar STATE + HEAD + CI;
2. clasificar feature/fix/QA/docs/integración/release;
3. aplicar KISS + anti-ruido;
4. si cambia UI/UX, ejecutar Ley SG de Diseño + skill antes de implementar;
5. reutilizar owners/evidencia existente;
6. comportamiento nuevo/fix: RED→GREEN y cambio mínimo;
7. build/tests/CI exact-head;
8. QA real cuando aplique;
9. actualizar STATE/ROADMAP/checkpoint si cambió el ruteo o verdad durable;
10. no merge sin aprobación explícita.

## Orden actual

```text
F0–F12                                      ✅ CLOSED / FROZEN (auto)
F13 Tasks 1–8                              ✅ CLOSED
F13 Task9 / T9.0                           ✅ DONE
DG-0 SG Design Governance                  ✅ GREEN / CLOSED
T9.1 / R1 reader multi-document core       ✅ GREEN / CLOSED
T9.1 / R1B per-document pending state      ✅ GREEN / CLOSED
T9.1 / R2 top shell + tabs                 ▶ NEXT / NOT STARTED
T9.1 / R3 rail oficial                     ⏳
T9.1 / R4 Páginas/Marcadores drawer        ⏳
T9.1 / R5 context menus                    ⏳
T9.1 / R6 image-selection root cause       ⏳
T9.1 / R7 final QA                         ⏳
T9.2                                       🔒 NOT STARTED
F14–F18                                    🔒 FROZEN
main                                       🔒 sin aprobación explícita
```

Siguiente acción elegible: ejecutar R2 con TDD y Ley SG de Diseño sobre los owners R1/R1B existentes. No adelantar T9.2 automáticamente.
