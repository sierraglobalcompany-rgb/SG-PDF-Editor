# SG PDF Editor

Aplicación Windows local-first para trabajar con PDF y etiquetas térmicas ZPL sin depender de Internet, APIs ni servicios de pago.

## Estado actual — 2026-10-17

- F0–F12: **CLOSED / FROZEN** en la baseline automatizada documentada.
- F13 Tasks 1–8: cerradas; **Task9 ACTIVE**.
- T9.0: DONE.
- T9.1: ACTIVE — realineamiento visual/document shell y UX.
- Último anchor GREEN anterior al R1 multidocumento: `0abae7ceb5f08611180c883346e21589d1509664`, exact-head CI run `39000181654` SUCCESS.
- R1 actual permanece **RED / INCOMPLETE** en `feat/f13-task9-t9-1-document-shell-realignment-20261017` @ `bbae51f49ee3c54e3667c5ea477c2c3f179d3c46` mientras se valida DG-0.
- DG-0: SG Design Governance en `feat/f13-dg0-sg-design-governance-20261017`, aislado desde el anchor GREEN.
- `main` continúa read-only en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`; no hay merge aprobado.

El PASS automatizado no equivale a QA real/manual/private/physical. Esos gates permanecen separados donde corresponda.

## Objetivo

Resolver flujos diarios PDF + ZPL de forma rápida, privada, estable, comprensible y offline.

### PDF

- abrir, leer, navegar e imprimir;
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

No hay microservicios, plugin framework, segundo viewer, segundo motor PDF, BatchEngine genérico ni framework general de conversiones.

## Fuente de verdad

Antes de tocar código o documentación operativa:

1. GitHub live: HEAD exacto + CI exact-head;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice;
6. para UI/UX, Ley SG de Diseño + skill;
7. `docs/MASTER_CONTEXT.md` para decisiones durables;
8. históricos solo como contexto.

`AGENTS.md` contiene las reglas obligatorias de ejecución para agentes.

## Autoridad visual

El icono de aplicación, icono de documento PDF y los 15 iconos Home aprobados tienen roles separados. No se rediseñan durante tareas ajenas. `PDF a PNG` permanece como contrato funcional actual; no se anuncia JPG hasta que exista esa capacidad. La asociación Windows de archivos pertenece al instalador futuro, no a hacks de Registry.

## Privacidad

Documentos reales de clientes y ZPL reales de Mercado Libre no se versionan. Fixtures privados permanecen ignorados, fuera de CI y fuera de Graphify.

## Licencia del código propio

Actualmente `All rights reserved` para Sierra Global Company. Las dependencias y referencias de terceros mantienen sus propias licencias y avisos.
