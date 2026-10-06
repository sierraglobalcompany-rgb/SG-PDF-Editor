# A1 Development Intelligence — Execution Plan

**Goal:** preparar SG PDF Editor para integrar GSD Core + Graphify como herramientas de desarrollo project-scoped, sin cambiar el runtime del producto.

**Scope A1.1:** documentación, reglas e higiene previa. La instalación real de GSD/Graphify queda para A1.2.

## Constraints

- No tocar `main`.
- No modificar código de producto en `src/`.
- No añadir dependencias runtime.
- Mantener KISS/YAGNI.
- GSD/Graphify son dev-only.
- `docs/MASTER_CONTEXT.md` y `docs/MASTER_PLAN.md` siguen siendo autoridad arquitectónica.
- GitHub es autoridad sobre el estado real de ejecución.
- No hacer merge sin aprobación explícita del usuario.

## A1.1 — preparación

- [x] Crear rama `feat/a1-dev-intelligence` desde A0 cerrado (`4333674`).
- [x] Actualizar `AGENTS.md` con el flujo futuro de contexto: STATE → phase PLAN → Graphify → archivos concretos → MASTER docs solo si hace falta.
- [x] Añadir A1 al `docs/ROADMAP.md` entre A0 y F0.
- [x] Actualizar `docs/MASTER_CONTEXT.md` para marcar A0 cerrado y A1 como fase actual.
- [x] Actualizar `docs/MASTER_PLAN.md` para insertar A1 y mover F0 a fase posterior.
- [x] Añadir reglas de `.gitignore` para cachés/artefactos locales de GSD/Graphify sin ignorar los documentos operativos que debamos versionar.
- [x] Verificar que A1.1 no modifica `src/` ni dependencias runtime.
- [x] Verificar build/tests existentes y CI.
- [x] Generar histórico portable al cerrar A1.1.

## Evidencia A1.1

- Diff contra A0: solo docs/config, sin cambios en `src/`.
- PR: #6 draft, apilado sobre `feat/kiss-vertical-slice`.
- Windows CI run verificado durante el bloque: `37526170939` = success.
- Histórico: `docs/history/2026-10-06-A1.1.md`.

## A1.2 — siguiente bloque, fuera de este cambio

1. instalar `open-gsd/gsd-core` project-scoped;
2. onboard del repo existente;
3. reconciliar `.planning/PROJECT.md`, `ROADMAP.md`, `STATE.md` y config con los MASTER docs;
4. instalar Graphify project-scoped;
5. construir grafo inicial sobre `src/` + `tests/`;
6. habilitar integración GSD↔Graphify;
7. probar consultas reales;
8. decidir con datos qué artefactos de `.planning/graphs/` se versionan;
9. mantener Graphify auto-update desactivado hasta medir costo/beneficio.

## Acceptance A1.1

A1.1 pasa si:

- A0 aparece como completada y A1 como fase siguiente/actual en toda la documentación relevante;
- `AGENTS.md` ya describe la futura estrategia de contexto reducido;
- no se añadió GSD/Graphify al runtime de la aplicación;
- `src/` no cambia;
- build/tests Windows siguen verdes;
- `main` permanece intacto;
- no existe merge automático.
