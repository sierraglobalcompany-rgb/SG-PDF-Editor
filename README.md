# SG PDF Editor

Aplicación Windows local-first para trabajar con PDF y etiquetas térmicas ZPL sin depender de Internet, APIs ni servicios de pago.

## Estado actual — 2026-10-24

- F14/F15 funcional y T9.1-R5 publicados en `feat/f15-a-compress-20261023`; R5 exact-head CI `40047923449` **SUCCESS**. F14/F15 no quedan globalmente GREEN: Dark/125%, foco, corpus complejo y QA real/manual permanecen separados.
- R6 test causal WPF/PDFium de selección de imagen: focal PASS y suite aislada **1379 PASS / 0 FAIL / 0 SKIP**. El CI base 40047923449 no contiene el nuevo test; revisar el exact-head CI de su checkpoint publicado.
- Publish `win-x64` autocontenido generado e inventariado fuera del repo. La app no se lanzó; GUI `NOT_VERIFIED` por WIP no aislada y offline smoke `NOT_RUN`. La decisión pública de licencia para `ZplGSCustom.ttf` sigue pendiente de una persona responsable.
- `main` permanece read-only en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`; sin merge ni release. R7 continúa `PARTIAL / NOT_CLOSED` hasta QA Windows aislada.
- La sección “Continuidad T9.1” más abajo conserva el routing snapshot del 2026-10-17; no es la ruta vigente. Consultar `.planning/STATE.md` y `.planning/ROADMAP.md`.

El PASS automatizado no equivale a QA real/manual/private/physical. Esos gates permanecen separados donde corresponda.

## Objetivo

Resolver flujos diarios PDF + ZPL de forma rápida, privada, estable, comprensible y offline.

### PDF

- abrir, leer, navegar e imprimir;
- varios PDF abiertos con estado independiente;
- pestañas reales para cambiar y cerrar documentos;
- preservar trabajo pendiente de Editar, Firmar, Comentar, Organizar y Formularios al cambiar de documento;
- firma visual con PNG/foto/InkCanvas y biblioteca local;
- firma criptográfica PFX/P12 dentro del alcance aprobado;
- organizar páginas;
- editar imágenes y texto de forma conservadora;
- comentarios/anotaciones;
- utilidades offline, OCR local, formularios `/Tx`, Compare, Batch TextWatermark, QR PNG y PDF→PNG.

### Etiquetas térmicas

- abrir `.zpl`, `.txt` y `.prn`;
- respetar cantidades `^PQ`;
- previsualizar con Labelize 1.7.0 local;
- maquetar Thermal/A4/Carta/custom;
- exportar PDF;
- imprimir mediante Windows;
- validar barcode/QR automatizadamente y mantener QA físico separado.

## Principios de ingeniería

- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.
- **Anti-ruido:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **Ley SG de Diseño:** `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.
- **Guardrail anti-parches:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.
- Offline/local-first; sin API keys, SaaS, cuentas, backend o base de datos.
- Preservar originales y usar salidas separadas/Guardar como donde el baseline lo define.
- No merge automático a `main`.

El guardrail anti-parches permanece separado; no se trata como una ley numerada.

## Diseño y UX obligatorios

Cualquier cambio en menús, barras, paneles, diálogos, formularios, copy, iconos visibles, layout, estados, accesibilidad o jerarquía visual debe leer primero:

- `docs/design/SGPDF_DESIGN_LAW.md`
- `.agents/skills/sgpdf-design/SKILL.md`

La skill obliga a diseñar desde la intención del usuario, aplicar progressive disclosure y verificar Light/Dark, 125% Windows scaling, teclado/foco, estados, clipping, pruebas y CI. Un panel no debe convertirse en un “Excel infinito” de propiedades solo porque todas existan en el modelo.

## Stack congelado

- C# / .NET 10 / WPF / Windows x64.
- `SGPdf.App + SGPdf.App.Tests`.
- PDFium como motor PDF principal.
- Labelize 1.7.0 como único renderer ZPL runtime; BinaryKits es histórico y no fallback runtime.
- PDFsharp 6.2.4 con uso estrecho aprobado; no segundo motor PDF general.
- qpdf 12.4.2 como sidecar estrecho para AES aprobado.
- Tesseract para OCR local/offline.
- ZXing.Net solo tests/QA.
- Sin framework UI adicional mientras WPF + recursos/templates existentes sean suficientes.

## Arquitectura KISS

```text
src/SGPdf.App
tests/SGPdf.App.Tests
```

No hay microservicios, plugin framework, segundo viewer, segundo motor PDF, ModeManager genérico, BatchEngine genérico ni framework general de conversiones.

El multi-documento se mantiene simple:

```text
OpenDocuments
ActiveDocument
PdfDocumentWorkspace por PDF
└─ PdfDocumentPendingState
```

R1B guarda **estado de modelo**, no controles WPF. Al cambiar de PDF se captura el documento activo, se desacopla su presentación y se restaura/reconstruye la presentación del documento objetivo. Abrir otro PDF, Ctrl+O y Recientes no fuerzan discard del trabajo pendiente. Cerrar un documento dirty sí usa guard explícito antes de disposal.

R2 y R3 añaden presentación sobre esos owners existentes, no una arquitectura paralela.

## Fuente de verdad

Antes de tocar código o documentación operativa:

1. GitHub live: HEAD exacto + CI exact-head;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice;
6. para UI/UX, Ley SG de Diseño + skill;
7. `docs/MASTER_CONTEXT.md` para decisiones durables;
8. históricos solo como contexto/checkpoints.

`AGENTS.md` contiene las reglas obligatorias de ejecución para agentes.

## Autoridad visual

El icono de aplicación, icono de documento PDF y los 15 iconos Home aprobados tienen roles separados. No se rediseñan durante tareas ajenas.

En el rail documental, el arte oficial aprobado ya se reutiliza para Editar, Firmar, Organizar, OCR, Censurar, Comparar, Convertir y Proteger. Leer, Comentar y Formularios mantienen glyph fallback hasta que exista arte aprobado específico.

`PDF a PNG` permanece como contrato funcional actual; no se anuncia JPG hasta que exista esa capacidad. La asociación Windows de archivos pertenece al instalador futuro, no a hacks de Registry.

## Continuidad T9.1

Orden aprobado:

```text
R1 reader multi-document core             ✅ GREEN / CLOSED
R1B pending state + close guard           ✅ GREEN / CLOSED
R2 top shell + document tabs              ✅ GREEN / CLOSED
R3 document rail + approved icons         ✅ GREEN / CLOSED técnico
R4 Pages/Bookmarks collapsible drawer     ▶ NEXT / NOT STARTED
R5 contextual menus                       ⏳
R6 image-selection root cause             ⏳
R7 final visual/functional QA             ⏳
T9.2                                      🔒 NOT STARTED
```

R4 debe reutilizar el renderer/scheduler de thumbnails ya existente y auditar la fuente real de bookmarks antes de declarar esa pestaña funcional.

## Privacidad

Documentos reales de clientes y ZPL reales de Mercado Libre no se versionan. Fixtures privados permanecen ignorados, fuera de CI y fuera de Graphify.

## Licencia del código propio

Actualmente `All rights reserved` para Sierra Global Company. Las dependencias y referencias de terceros mantienen sus propias licencias y avisos.
