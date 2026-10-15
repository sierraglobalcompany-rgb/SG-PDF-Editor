# SG PDF Editor — Checkpoint F15 Edit V3 Spec

**Fecha:** 2026-10-15  
**Rama:** `plan/post-ui4-f13-f18-expansion-20261014`

## Estado de producto

- F0–F12: baseline funcional cerrado.
- UI-4: `FAIL-P1`; no cerrar ni publicar todavía.
- PKCS: GREEN en rama aislada `feat/fix-pkcs-hygiene` @ `781cd6060121367f4d51efbe2724d820d1dfb842`, no integrado aún.
- F13: plan Product Shell/Dashboard listo.
- F14: plan de productización de capacidades existentes listo.
- F15: diseño A aprobado; spec escrito y autoauditado.

## F15 spec autoritativo

`docs/superpowers/specs/2026-10-15-f15-edit-v3-design.md`

## Decisiones congeladas F15

- Reutilizar motor actual; no crear editor paralelo.
- Owners conservados: `ImageEditWorkspace`, `TextEditWorkspace`, `RedactionWorkspace`, `PdfEditWriter`.
- Selección directa de texto/imagen en canvas.
- Panel derecho contextual.
- `Guardar como...` siempre visible en Editar.
- Sin dirty: Guardar deshabilitado.
- Con cualquier dirty: Guardar habilitado.
- Guardar siempre crea copia.
- Original permanece activo; no auto-reopen de la copia.
- Imágenes conservan Undo/Redo existente.
- Texto/censura no tienen historial hoy; no crear historial global ficticio.
- Censura sigue obligatoriamente `RedactionPolicy` + `RedactionWorkspace` + writer existente.
- Reflow sigue siendo capability-gated.
- PDF escaneado sugiere OCR; no ejecuta OCR automáticamente.
- Sin segundo writer, segundo motor, command bus, plugin framework, DI rewrite, backend, API cloud ni IA.

## Tres reglas

1. KISS: `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → A→B`.
2. Ruido: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. Anti-parche: `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI`.

## Gate actual

No escribir todavía el plan de implementación F15 hasta que el usuario revise/acepte explícitamente el spec escrito.

Mientras tanto, prioridad operativa sigue siendo:

1. recibir/auditar diagnóstico Windows P1;
2. resolver causa raíz real;
3. rerun UI-4;
4. cerrar UI-4 sin P0/P1;
5. integrar PKCS de forma conservadora;
6. ejecutar F13;
7. F14;
8. F15 después de sus gates de diseño/plan.

## Regla Codex

Codex solo para tareas que requieren Windows real/GUI/hardware/local-only evidence. ChatGPT hace repo, arquitectura, implementación, tests, GitHub y documentación siempre que sea posible.
