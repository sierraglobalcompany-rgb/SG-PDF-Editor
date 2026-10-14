# Instrucciones para agentes de desarrollo — SG PDF Editor

## Fuente de verdad y autoridad

En caso de contradicción, usar este orden:

1. GitHub live + CI del exact HEAD;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice concreto;
6. `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md` para arquitectura/producto;
7. handoffs e históricos solo como evidencia de recuperación.

No reabrir una fase cerrada porque un documento histórico diga que estaba pendiente.

## Estado actual

- Baseline funcional automatizado **F0–F12: CLOSED / AUTO PASS**.
- F12.7 cerró con PDF→PNG como única conversión baseline aprobada.
- Trabajo actual: **release preparation**, no expansión oportunista de features.
- QA real/manual/privado/físico: pendiente por packs.
- Integración de ramas/PRs y estabilización: pendiente.
- `main` permanece read-only hasta aprobación explícita del usuario.

Checklist de QA real: `docs/release/2026-10-14-real-qa-packs.md`.

## Leyes obligatorias

### 1. KISS / YAGNI

Arquitectura simple, directa y medible. Preferir A→B. No introducir capas, frameworks, DI, plugin systems, registries, workers, queues, databases o motores paralelos sin evidencia y aprobación material.

### 2. Ley del ruido

Antes de agregar algo evaluar, en este orden:

`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

Una eliminación debe demostrar que no rompe contratos vigentes. No acumular parches sobre parches; buscar causa raíz y owner existente.

### 3. Anti-patch

`REPRODUCIR → CLASIFICAR → CAUSA RAÍZ → OWNER → CAMBIO MÍNIMO → TEST CAUSA → SUITE COMPLETA → CI EXACT-HEAD → REMOVE REDUNDANT WORKAROUND`.

## Guardrails técnicos vigentes

- Mantener `SGPdf.App + SGPdf.App.Tests` mientras sea suficiente.
- Windows x64 + WPF + .NET 10 salvo evidencia material.
- PDFium sigue siendo el motor PDF principal. Toda llamada nativa respeta `PdfiumRuntime.NativeGate`.
- PDFsharp, qpdf y Tesseract permanecen limitados a sus owners/slices ya aprobados; no convertirlos en motores PDF generales.
- Labelize 1.7.0 sigue siendo el único renderer runtime aprobado para ZPL/QR.
- ZXing.Net permanece test/QA-only.
- No Labelary/APIs web runtime, cloud conversion, telemetría o backend para funciones locales baseline.
- No AGPL/GPL fuerte/licencia comercial obligatoria sin aprobación explícita.
- Datos reales de Mercado Libre/clientes nunca se versionan; fixtures privados usan ubicaciones ignoradas/locales.
- `Guardar como`/salida explícita y protección del original siguen siendo la regla segura para writers.
- No afirmar PASS físico/manual/privado desde CI sintético.
- No refactors masivos preventivos.
- No auto-merge a `main`.

## Estado funcional resumido

F0 PDF Base, F1 ZPL Gate, F2 ZPL Workspace, F3 Firma Visual, F4 Reader, F5 Organize, F6 Images, F7 Text V1, F8 Comments, F9 Utilities, F10 OCR, F11 Text V2 y F12 Professional están automatizadamente cerrados según `.planning/STATE.md` y `.planning/REQUIREMENTS.md`.

Los detalles finos de cada fase viven en `.planning/phases/`, `docs/superpowers/` y `docs/history/`. Este archivo no duplica contratos por fase para evitar que vuelva a quedar obsoleto.

## Release preparation — orden actual

1. Ejecutar QA real en packs acotados sin fingir evidencia no ejecutada.
2. Consolidar documentación operativa y marcar/retirar ruido demostrado.
3. Auditar ancestry de ramas/PRs y commits únicos.
4. Preparar una rama dedicada de integración conservadora.
5. Correr CI completa exact-head + QA real priorizada.
6. Cerrar installer/licencias/provenance y smoke offline.
7. Solo con aprobación explícita considerar merge a `main`.

## Cadencia por bloque

Para cada `continua` ejecutar un bloque de producto/release significativo y acotado; si el usuario pide dos bloques, ejecutar dos y parar.

Un bloque técnico cierra solo con evidencia fresca apropiada:

- revisión del estado/owner;
- cambio mínimo;
- prueba focal cuando aplique;
- build/tests/CI exact-head cuando aplique;
- auditoría KISS + ruido;
- actualización de estado solo si cambió una fuente de verdad.

No crear un Markdown por commit. Documentar únicamente cambios de source-of-truth, decisiones materiales, cierres de fase/release o recovery handoff necesario.

## Gate de nuevas funciones

F0–F12 baseline está congelado. Nuevas conversiones, simbologías, operaciones batch, firma avanzada, formatos de formularios o capacidades PDF requieren:

`VALUE GATE → CAPABILITY GATE → MVP estrecho → TEST → CLOSURE`

No convertir release preparation en una nueva fase de features.
