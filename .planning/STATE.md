---
gsd_state_version: '1.0'
status: planning
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: `.planning/PROJECT.md` (updated 2026-10-06)

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.
**Current focus:** Phase 1 — F0 PDF Base.

## Current Position

Phase: 1 of 13 (F0 PDF Base)
Plan: 0 of 0 — phase not planned yet
Status: Ready to discuss/plan
Last activity: 2026-10-06 — A0 repo foundation y A1 GSD/Graphify bootstrap quedaron preparados/validados antes de iniciar el roadmap de producto.

Progress: [░░░░░░░░░░] 0%

## Development Tooling

- GSD Core pin: `1.15.0`, project-scoped, installer-owned `.codex/` ignored.
- Graphify pin: `0.9.77`, project-scoped, `graphify-out/` ignored y regenerable.
- Graph corpus: solo `src/` + `tests/` mediante `.graphifyignore`.
- Graphify auto-update: OFF; actualizar manualmente cuando aporte valor.
- Setup reproducible: `tools/setup-dev.ps1`.
- Tooling es dev-only: nunca requisito de build/runtime de SG PDF Editor.

## Accumulated Context

### Decisions

- WPF + .NET 10 y solución App + Tests se mantienen por KISS.
- PDFium es motor PDF principal; llamadas nativas serializadas globalmente.
- BinaryKits.Zpl es candidato preferente, sujeto a Gate ZPL-A.
- GSD/Graphify no se venden ni distribuyen con el producto; sus payloads/grafos generados no se versionan.
- `main` no recibe merge sin aprobación explícita del usuario.

### Pending Todos

- Planificar Phase 1 / F0 PDF Base mediante GSD.
- Después de F0 ejecutar Gate ZPL-A con corpus privado + sintético.

### Blockers/Concerns

- Ninguno para iniciar F0.
- Antigravity project-scoped está soportado upstream, pero este A1 validó Codex; validarlo en host real cuando se use.

## Deferred Items

| Category | Item | Status | Deferred At | Milestone |
|----------|------|--------|-------------|-----------|
| Tooling | Activar integración Graphify automática de una versión GSD futura (`graphify.enabled`) | Deferred until audited upgrade | A1 | v0.x |
| Product | Installer/autoupdate/cloud/accounts | Out of current scope | A0 | v1+ |

## Session Continuity

Last session: 2026-10-06
Stopped at: A1 Development Intelligence completed; next action is discuss/plan F0 PDF Base.
Resume file: `docs/history/2026-10-06-A1.2.md`
