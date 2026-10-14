# SG PDF Editor

Aplicación Windows local-first para trabajar con PDF y etiquetas térmicas ZPL sin depender de Internet, APIs ni servicios de pago.

## Estado actual — 2026-10-14

- F0–F12: **CLOSED / automated baseline PASS**.
- F12 está **feature-frozen**.
- Rama funcional autoritativa: `feat/f12-7-pdf-to-png-mvp-green3`.
- Functional HEAD: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.
- CI funcional exact-head: run `38555829715`, job `115824598759`, Release 0 warnings / 0 errors, 1202/1202 tests PASS.
- Packs A–D: preflight automatizado PASS; QA real/manual/private/physical **NOT RUN — ENVIRONMENT**.
- `main` permanece sin cambios en `31c0594758a83ec555d73ecdd7c597cdf8791fd7` hasta aprobación explícita.
- Trabajo actual: auditoría de ancestry/PRs → candidato de integración conservador → RC/installer/runtime/license gates.

## Objetivo

Resolver flujos diarios PDF + ZPL de forma rápida, privada, estable y offline.

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

## Principios

- KISS: simple, eficiente, optimizado y eficaz.
- Anti-ruido: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Guardrail anti-parches: reproducir, clasificar, causa raíz, owner, cambio mínimo, prueba de causa, suite completa, CI exact-head y retiro de workaround redundante.
- Funciones principales offline/local-first.
- Sin API keys, SaaS, cuentas, backend o base de datos.
- Preservar originales; usar salidas separadas/Guardar como donde el baseline lo define.
- No merge automático a `main`.

## Stack congelado

- C# / .NET 10 / WPF / Windows x64.
- `SGPdf.App + SGPdf.App.Tests`.
- PDFium (`bblanchon.PDFium.Win32 156.0.8076`) como motor PDF principal.
- Labelize 1.7.0 como único renderer ZPL runtime; BinaryKits es histórico y no fallback runtime.
- PDFsharp 6.2.4 con uso estrecho aprobado: composición/export de etiquetas, F12.2 firma PFX/P12, F12.3 AcroForm `/Tx` y fixtures/pruebas demostradas; no segundo motor PDF general.
- qpdf 12.4.2 como sidecar estrecho para la capacidad AES aprobada.
- Tesseract 5.5.3 para OCR local/offline.
- DejaVu Sans pinneado como fallback donde ya lo usa el baseline.
- ZXing.Net solo en tests/QA, no subsistema runtime de barcode.

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
5. spec/plan aprobado del slice concreto;
6. `docs/MASTER_CONTEXT.md` para arquitectura y decisiones durables;
7. `docs/MASTER_PLAN.md` y `docs/history/**` como contexto histórico/arquitectónico, nunca para sobreescribir un estado operativo más nuevo.

`AGENTS.md` contiene las reglas de ejecución para agentes.

## QA y release

El PASS automatizado no equivale a PASS manual/físico. Permanecen abiertos los gates reales documentados en Packs A–D: Windows real, corpus privado, impresora/ruler/scanner, foto/stylus/touch, PFX/P12 real, lectores externos, documentos representativos y offline físico.

También permanece abierto antes de release público el gate de procedencia/licencia de `ZplGSCustom.ttf`, junto con installer/runtime smoke y RC limpio.

## Privacidad

Documentos reales de clientes y ZPL reales de Mercado Libre no se versionan. Los fixtures privados deben permanecer ignorados, fuera de CI y fuera de Graphify.

## Licencia del código propio

Actualmente `All rights reserved` para Sierra Global Company. Las dependencias de terceros mantienen sus propias licencias y avisos.
