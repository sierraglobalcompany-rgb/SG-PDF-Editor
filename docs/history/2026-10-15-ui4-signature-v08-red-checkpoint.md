# SG PDF — Checkpoint UI-4 Firma + V08 RED

**Fecha:** 2026-10-15  
**Motivo:** checkpoint solicitado por el usuario para evitar pérdida de contexto/bloqueo.  
**Estado:** STOP — no continuar implementación hasta nuevo mensaje del usuario.

## Guardrails vigentes

1. KISS: simple, eficiente, optimizado, arquitectura A→B.
2. Ley de ruido: DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD.
3. Anti-parches: reproducir → clasificar → causa raíz → owner → cambio mínimo → test causal → suite completa → CI → retirar workaround redundante.

`main` sigue READ-ONLY. No merge a `main` sin aprobación explícita del usuario.

## Base UI auditada

Rama base: `feat/release-ui-modernization`  
HEAD base: `abd52d05279e3e656723ad4ff5a21d36de91508e`

## Firma visual / UI-4 P1

El fix local entregado por Codex fue auditado y reconstruido remotamente sin reinterpretación.

Rama remota revisada: `feat/fix-ui4-signature-overlay-reviewed`  
Commit: `8c4a0f2619ca0a4835f6160ce4958d2d1b981435`  
PR draft: `#31 UI4: unblock signature controls in shared properties pane`

Scope exacto:
- `src/SGPdf.App/MainWindow.Forms.cs`
- `src/SGPdf.App/MainWindow.Sign.cs`
- `tests/SGPdf.App.Tests/MainWindowSignatureTests.cs`

Stats: 3 archivos, +44 / -2.

Verificación:
- blobs reconstruidos coinciden con los prefijos SHA reportados por el patch local;
- diff remoto coincide con el patch local;
- parent directo = `abd52d...`.

CI PR:
- run `38680545052`;
- job `116243845264`;
- build PASS, 0 errors;
- 2 warnings NU1902/NU1903 ya conocidos por `System.Security.Cryptography.Pkcs 8.0.1`;
- tests `1215/1215 PASS`.

Nota: GitHub Actions ejecutó el merge-ref del PR (`73a9d875...`), no literalmente el SHA head. Como el base es exactamente el parent del head, el árbol probado es equivalente, pero no llamarlo “exact-head CI”.

Estado de Firma: **FIX AUDITADO + CI GREEN, pendiente revalidación Windows/manual y decisión de integración posterior.** No se hizo merge.

## V08 — Label Export/Print stale disabled

Causa raíz ya documentada en `docs/history/2026-10-15-ui4-v08-label-buttons-root-cause-proven.md`.

Resumen causal:
- `OpenZpl_Click` pone `_isBusy=true`;
- `CommitLoadedZpl(...)` crea un `LabelLayoutPlan` válido;
- `UpdateLabelPropertiesUi()` corre mientras `_isBusy=true`, por lo que Export/Print quedan deshabilitados en ese instante;
- `OpenZpl_Click` termina con `SetBusy(false)`;
- `SetBusy(false)` no refresca `UpdateLabelPropertiesUi()`;
- resultado: `_isBusy=false` + plan Thermal válido + botones aún disabled por estado stale.

### TDD RED creado y demostrado

Rama: `feat/fix-ui4-v08-label-actions`  
Base: `8c4a0f2619ca0a4835f6160ce4958d2d1b981435`  
RED commit: `1108ed7552fb5e2a8ebf19cec1b4f8f5405002d3`  
Archivo nuevo: `tests/SGPdf.App.Tests/MainWindowZplBusyCompletionUiTests.cs`  
PR draft: `#32 UI4 V08 RED: reproduce stale label actions after busy`

Secuencia causal del test:
`SetBusy(true) → CommitLoadedZpl(valid Thermal) → botones false → SetBusy(false) → se espera Export/Print true`.

CI RED:
- run `38680955209`;
- job `116245162997`;
- restore PASS;
- build PASS, 0 errors;
- test causal FAIL exactamente como se esperaba;
- suite: `1215 PASS / 1 FAIL / 1216 total`;
- fallo: `MainWindowZplBusyCompletionUiTests.EndingBusyState_RefreshesValidThermalLabelActions`;
- `Assert.True` Expected=True / Actual=False después de terminar busy.

Esto confirma el bug sin especulación.

## Próximo paso al reanudar

NO repetir diagnóstico.

Aplicar GREEN mínimo sobre `feat/fix-ui4-v08-label-actions`:
- preferir owner-local en `OpenZpl_Click`;
- después de `SetBusy(false)` refrescar el estado ZPL existente mediante `UpdateLabelPropertiesUi()`;
- no forzar `IsEnabled=true`;
- no modificar planner;
- no ampliar `SetBusy()` global salvo evidencia nueva;
- no mezclar Firma, PKCS, F13/F14/F15 ni rediseño.

Después:
1. test causal GREEN;
2. suite completa;
3. CI del PR;
4. revalidación Windows V08;
5. mantener Firma y V08 como cambios auditables separados hasta decidir integración.

## Trabajo posterior aún bloqueado

Hasta cerrar UI-4 sin P0/P1:
- F13 Product Shell no implementar;
- F14 productización no implementar;
- F15 Edit V3 no implementar.

La planificación F13/F14/F15 sigue válida y separada del fix UI-4.

## Rama auxiliar

`feat/fix-ui4-signature-overlay-remote` quedó creada desde `abd52d...` sin el commit final. No es autoritativa. La rama autoritativa de Firma es `feat/fix-ui4-signature-overlay-reviewed`.

No limpiar ni borrar ramas durante este STOP salvo solicitud posterior explícita.

## STOP

Checkpoint completo. No seguir ejecutando tareas, no hacer GREEN de V08, no fusionar PRs y no tocar `main` hasta que el usuario indique continuar.
