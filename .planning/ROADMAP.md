# Roadmap — SG PDF Editor

> GitHub exact-head CI manda sobre snapshots históricos. Estado operativo: `.planning/STATE.md`. Requisitos: `.planning/REQUIREMENTS.md`.

## Estado funcional

F0–F12 baseline: **CLOSED / AUTO PASS**.

F12 Professional cerrado en baseline:
- F12.1 Safe Redaction;
- F12.2 Cryptographic Signature PFX/P12 MVP;
- F12.3 Existing AcroForm `/Tx`;
- F12.4 Compare;
- F12.5 Batch — TextWatermark MVP;
- F12.6 Code Generator — QR PNG MVP;
- F12.7 Audited Conversions — PDF→PNG MVP.

Los gates manuales/privados/físicos permanecen abiertos donde `.planning/REQUIREMENTS.md` lo indique.

## Release path — estado actual

### R1. Bounded real QA packs — DEFINED / NOT RUN

Checklist canónico: `docs/release/2026-10-14-real-qa-packs.md`.

Packs congelados:
- A — Reader / Organize / Editing;
- B — ZPL / QR / physical print-scan;
- C — visual + cryptographic signatures;
- D — OCR / Forms / Compare / Batch / PDF→PNG.

Definirlos no equivale a ejecutarlos. Todos siguen **NOT RUN** hasta que exista evidencia real.

### R2. Documentation consolidation — BASELINE PASS

`AGENTS.md` dejó de duplicar estado histórico F3 y ahora contiene únicamente autoridad, leyes, owners, guardrails y cadencia de release vigentes.

Los detalles finos se conservan en `.planning/phases/`, `docs/superpowers/`, `docs/history/` y requisitos. No crear handoffs/documentos por commit.

### R3. Release integration / stabilization — NEXT

Ejecutar como bloques separados y auditables:
1. mapear branches/PRs activos, ancestry y commits únicos;
2. clasificar cada branch/PR: required / superseded / historical / unknown;
3. comprobar que la línea funcional F0–F12 es íntegra y no depende de commits laterales perdidos;
4. elegir estrategia KISS de integración;
5. crear rama dedicada de integración solo después de la auditoría;
6. no rebase/force-push/merge destructivo durante la auditoría;
7. mantener `main` read-only hasta aprobación explícita;
8. correr CI exact-head y luego QA real priorizada.

### R4. Installer / license / offline stabilization — OPEN

Antes de RC:
- provenance/licencia de assets/runtime, incluido Labelize/fonts;
- clean-clone + locked restore;
- packaged runtime/installer smoke;
- Windows offline smoke;
- no P0/P1 abiertos.

### R5. Release candidate / final — OPEN

RC no agrega features. Solo estabiliza, integra, verifica y documenta limitaciones reales.

## KISS post-F12

No reabrir features baseline oportunísticamente. Nuevas conversiones, simbologías, operaciones batch, formularios o firma avanzada requieren:

`VALUE GATE → CAPABILITY GATE → MVP estrecho → TEST → CLOSURE`.

No merge a `main` sin aprobación explícita del usuario.
