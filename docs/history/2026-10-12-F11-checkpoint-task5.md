# F11 — Texto V2 — CHECKPOINT POST TASK 5

Fecha: 2026-10-12

## Estado de pausa

- Rama activa: `feat/f11-text-v2`
- Base apilada de F10: `42935a865c898dea9e3548fbf94e1b400c8a5732`
- HEAD funcional inmediatamente antes de este checkpoint: `024e0f043b6b46bee6b8901f8301c727c28aacbf`
- Estado de implementación: Tasks 1–5 implementadas; Task 6 NO iniciada.
- `main`: NO tocar / NO merge sin aprobación explícita del usuario.
- F10 y PR #29: NO tocar.
- Próximo trabajo autorizado al reanudar: Task 6 — UI mínima + hardening + cierre F11.
- STOP: este checkpoint ordena detener trabajo después de registrar el estado.

## Metodología vigente

- KISS / YAGNI / simple / optimizado / eficiente.
- PDFium-first.
- Offline/local-first.
- Sin nueva dependencia PDF salvo gate medible.
- TDD: RED → GREEN → refactor mínimo.
- Fail-closed ante layouts ambiguos o no seguros.
- Reusar writer/transacción de Texto V1; no crear un segundo writer.
- Save As; fuente original intacta.
- No merge a `main` sin aprobación explícita.

## Diseño y plan

- Spec: `docs/superpowers/specs/2026-10-12-f11-text-v2-design.md`
- Plan: `docs/superpowers/plans/2026-10-12-f11-text-v2.md`

## Tasks cerradas antes de la pausa

### Task 1 — Modelo + líneas + reading order

Implementado:
- `TextLayoutModel`
- `TextLayoutAnalyzer`
- reading order geométrico independiente del `PageObjectIndex`
- normalización y descarte conservador de fragmentos inválidos

### Task 2 — Párrafos + una/dos columnas

Implementado:
- agrupación determinista de párrafos
- columnas simples 1–2
- gutter estable
- layouts dudosos → `Ambiguous`
- rotación incompatible → `Unsupported`

### Task 3 — Gate PDFium + corpus

Implementado y demostrado:
- reutiliza extracción F7/PDFium
- PDFium suficiente para el alcance F11 definido
- no se añadió PdfPig
- corpus sintético/real relevante cubierto, incluido OCR F10

### Task 4 — `TextReflowPolicy`

Implementado:
- política pura
- solo permite `Simple`, una columna, horizontal, rectangular, homogéneo, sin solapes problemáticos y con preflight compatible
- layouts complejos/ambiguos/multicolumna rechazados antes de mutación

### Task 5A — `TextReflowPlanner`

RED:
- commit `75c5a6fc…`
- CI falló en Build porque `TextReflowPlanner` aún no existía, confirmando RED real.

GREEN:
- commit `3c496d45…`
- planner puro, conservador y fail-closed
- distribuye palabras solo entre objetos de texto existentes
- no crea ni mueve objetos
- rechaza si el texto no cabe o si quedarían objetos con contenido anterior
- exact-head Windows Build + Test: PASS

### Task 5B — Integración workspace + writer existente

RED:
- commit `43dccea5079df7aa7065dd9d20ccffcf8f176142`
- CI Build: FAIL esperado porque `ApplyReflowPlan` aún no existía.

GREEN:
- commit `d27a89eec7af3f5baaf15d91e5f60dc749058ab4`
- `ApplyReflowPlan` prepara todos los candidatos antes de mutar
- fallo en cualquier asignación deja el workspace intacto
- reutiliza `PdfEditWriter` de Texto V1
- Save As + reapertura exacta + fuente original intacta cubiertos
- workflow `38212158723`, job `114688528140`: Build PASS + Test PASS

### Task 5 — evidencia visual adicional

Commit funcional inmediatamente previo al checkpoint:
- `024e0f043b6b46bee6b8901f8301c727c28aacbf`
- mensaje: `test(f11): validate rendered reflow output`
- añade render real del PDF reabierto para comprobar salida renderizable después del reflow
- workflow exact-head `38212374059` estaba `in_progress` al momento de crear este checkpoint; NO registrar como PASS hasta verificarlo al reanudar.

## Archivos principales añadidos/modificados en Tasks 5A–5B

- `src/SGPdf.App/Features/Edit/Text/TextReflowPlanner.cs`
- `src/SGPdf.App/Features/Edit/Text/TextEditWorkspace.cs`
- `tests/SGPdf.App.Tests/TextReflowPlannerTests.cs`
- `tests/SGPdf.App.Tests/TextReflowWorkspaceIntegrationTests.cs`

## Contrato de reflow vigente

1. `TextReflowPolicy` decide si el párrafo es seguro.
2. `TextReflowPlanner` genera asignaciones puras sobre objetos existentes.
3. `TextEditWorkspace.ApplyReflowPlan` valida/prepara todas las asignaciones antes de cualquier commit.
4. `PdfEditWriter` existente materializa el PDF con su transacción Save As, preflight, temporal, validación y publicación atómica.
5. No existe ni debe crearse un writer F11 separado salvo evidencia futura que lo justifique.

## Próximo paso exacto al reanudar

1. Verificar resultado final del workflow `38212374059` del commit `024e0f043b6b46bee6b8901f8301c727c28aacbf`.
2. Si está GREEN, considerar Task 5 cerrada.
3. Abrir Task 6 en RED con tests de UI mínima sobre el flujo existente de `MainWindow.Edit.Text.cs`.
4. Exponer selección de párrafo reconocido, estado seguro/no seguro y comando de reflow limitado.
5. Mostrar motivo de rechazo en español.
6. Mantener UI pequeña; sin panel complejo nuevo.
7. Hardening, full Release, exact-head Windows CI, docs de cierre y draft PR apilado sobre `feat/f10-local-ocr`.
8. STOP antes de merge o F12.

## Prohibiciones al reanudar

- No empezar F12.
- No mergear a `main`.
- No modificar F10 ni PR #29 salvo instrucción explícita.
- No introducir PdfPig preventivamente.
- No abrir arquitectura nueva si el flujo F7/F11 actual basta.
- No declarar F11 completamente cerrada hasta terminar Task 6 y su CI exact-head.
