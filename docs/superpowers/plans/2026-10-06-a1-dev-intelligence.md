# A1 Development Intelligence — Execution Plan

**Goal:** integrar GSD Core + Graphify como tooling project-scoped, reproducible y dev-only, sin cambiar el runtime de SG PDF Editor.

**Estado:** ✅ COMPLETADO el 2026-10-06.

## Constraints

- No tocar `main`.
- No modificar código de producto en `src/`.
- No añadir dependencias runtime.
- Mantener KISS/YAGNI.
- GSD/Graphify son dev-only.
- `docs/MASTER_CONTEXT.md` y `docs/MASTER_PLAN.md` siguen siendo autoridad arquitectónica.
- GitHub es autoridad sobre el estado real de ejecución.
- No hacer merge sin aprobación explícita del usuario.

## A1.1 — preparación ✅

- [x] Crear rama `feat/a1-dev-intelligence` desde A0 cerrado (`4333674`).
- [x] Actualizar `AGENTS.md` con flujo de contexto reducido.
- [x] Añadir A1 a `docs/ROADMAP.md`.
- [x] Actualizar `docs/MASTER_CONTEXT.md` y `docs/MASTER_PLAN.md`.
- [x] Preparar `.gitignore` para tooling generado sin ocultar planning operativo.
- [x] Verificar que `src/` y dependencias runtime no cambian.
- [x] Verificar build/tests/CI.
- [x] Generar histórico portable A1.1.

### Evidencia A1.1

- PR #6 draft, apilado sobre `feat/kiss-vertical-slice`.
- Head de cierre A1.1: `75c1b6855dfe5cc65b1a6297a5755edfb6f23c7c`.
- Windows CI run `37526412889` = success.
- Histórico: `docs/history/2026-10-06-A1.1.md`.

## A1.2 — instalación y validación ✅

- [x] Auditar instalación oficial de `open-gsd/gsd-core` project-scoped.
- [x] Fijar GSD Core a `1.15.0` y validar con Node 24.
- [x] Crear/reconciliar `.planning/PROJECT.md`, `REQUIREMENTS.md`, `ROADMAP.md`, `STATE.md` y `config.json`.
- [x] Fijar Graphify a `0.9.77`.
- [x] Crear `.graphifyignore` para limitar AST a `src/` + `tests/`.
- [x] Construir grafo local en entorno limpio.
- [x] Ejecutar query real sobre `PdfDocumentSession`.
- [x] Verificar que `tests/PrivateFixtures/` no aparece en el grafo.
- [x] Medir superficies generadas y definir política de versionado.
- [x] Mantener `graphify.auto_update=false`.
- [x] Crear setup reproducible `tools/setup-dev.ps1`.
- [x] Registrar GSD/Graphify en `third_party/manifest.json` como tooling dev-only no vendorizado.
- [x] Validar GSD `health` y `state-snapshot`.
- [x] Verificar que build/test del producto sigue independiente del tooling.

## Decisiones A1.2

### GSD

- pin: `1.15.0`;
- project-scoped Codex install;
- `.planning/` versionado;
- `.codex/` generado, ignorado y regenerable;
- GSD 1.15.0 no soporta la clave `graphify.enabled`; no usar configuración de versiones futuras.

### Graphify

- pin: `0.9.77`;
- virtualenv local en `.devtools/` mediante `tools/setup-dev.ps1`;
- `graphify-out/` regenerable e ignorado;
- corpus: solo `src/` + `tests/`;
- sin backend LLM/API para el uso normal;
- auto-update desactivado.

### Medición

- `.codex/`: ~17 MB / 833 archivos → no versionar;
- `graphify-out/`: ~472 KB / 22 archivos → no versionar;
- grafo validado: 12 code files / 161 nodes / 187 edges / 16 communities.

## Evidencia A1.2

Tooling probe final:

```text
Run 37530638793
GSD Core install                 PASS
GSD validate health              healthy / 0 warnings / 0 errors
GSD state-snapshot               Phase 1 = F0 PDF Base / planning
Graphify 0.9.77 install          PASS
Graph build                      PASS
PdfDocumentSession query         PASS
PrivateFixtures exclusion        PASS
Artifact upload                  PASS
```

El query Graphify identificó correctamente relaciones con `PdfiumRuntime.EnsureInitialized`, `FPDF_LoadDocument`, `FPDF_LoadPage`, page-size APIs y close APIs.

Windows build/test también se verificó durante A1.2 sobre la configuración permanente.

## Acceptance A1 — resultado

- [x] una sesión nueva puede orientarse con `.planning/STATE.md`;
- [x] roadmap/requisitos operativos están versionados;
- [x] Graphify devuelve relaciones útiles;
- [x] no hay servicio externo requerido;
- [x] fixtures privados quedan fuera del grafo;
- [x] GSD/Graphify siguen siendo dev-only;
- [x] el build/test del producto no depende del tooling;
- [x] payloads grandes/regenerables no contaminan Git.

## Siguiente

**F0 — PDF Base.**

Antes de código nuevo, usar GSD para discutir/planificar Phase 1. Primer slice recomendado para mantener KISS:

```text
F0.1 — abrir PDF desde UI + renderizar una página real en WPF
```

Después: navegación/zoom → scheduler/cancelación → fit → impresión.
