# SG PDF — UI-4 result audited / next signature trace

**Fecha:** 2026-10-15  
**Estado:** UI-4 FAIL-P1 / ROOT CAUSE NOT PROVEN  
**HEAD probado:** `abd52d05279e3e656723ad4ff5a21d36de91508e`

## Resultado recibido

Windows 11 Home Single Language x64 build 10.0.26300, 1920×1200 @100%, .NET SDK 10.0.402.

- V01 PASS
- V02 PASS después del fix local no commit del mode strip oscuro
- V03 PASS
- V04 NOT RUN
- V05 NOT RUN
- V06 PASS
- V07 PASS
- V08 FAIL-P2: Exportar PDF e Imprimir aparecieron disabled; clasificación pendiente
- V09 NOT RUN visualmente
- V10 FAIL-P1: Dibujar firma no abrió el canvas/modal
- V11 FAIL-P1: Crear desde foto no abrió file picker
- V12 FAIL-P1: Biblioteca de firmas no abrió diálogo
- V13 NOT RUN
- V14 FAIL-P1 por flujos de firma

Build PASS, 1214/1214 tests PASS, `git diff --check` limpio.

Local diff a preservar:
`src/SGPdf.App/MainWindow.Sign.cs`

Rama local reportada:
`feat/fix-ui4-dark-mode-strip`

No push, no main, no PKCS.

## Auditoría

El resultado confirma el síntoma pero NO demuestra la causa raíz de V10–V12.

La fuente remota demuestra que:
- `CreateSignatureFromPhotoButton` está conectado a `PrepareSignaturePhoto_Click` y éste llama `OpenFileDialog.ShowDialog(this)`;
- `DrawSignatureButton` está conectado a `DrawSignature_Click` → `TryDrawSignature()` → `_drawSignature(this)`;
- `SignatureLibraryButton` entra a `TryOpenSignatureLibrary()` → `_openSignatureLibrary(...)`;
- los tres viven en `SignaturePropertiesPanel`;
- ninguno tiene `IsEnabled=false` explícito;
- el panel derecho hereda `IsEnabled` del Border ligado a `OpenPdfMenuItem`.

Por tanto no se acepta:
- duplicar handlers;
- cambiar `ShowDialog()` a ciegas;
- forzar Topmost;
- tocar Z-order sin hit-test;
- crear reemplazo de diálogo.

## Siguiente prueba causal

La siguiente corrida Windows debe distinguir primero:

1. problema de panel/hit-test;
2. handler no alcanzado;
3. delegate/default seam no alcanzado;
4. `ShowDialog()` alcanzado pero modal oculto/fuera de pantalla;
5. excepción capturada.

Control comparativo obligatorio:
`Cargar PNG transparente...`

Ese botón comparte el mismo `SignaturePropertiesPanel`; si abre correctamente, el panel/hit-test común queda fuertemente descartado.

Prompt externo generado para Codex:
`SG_PDF_CODEX_UI4_SIGNATURE_ROOT_CAUSE_TRACE_2026-10-15.md`

## V08

No hacer fix todavía.

Registrar en la corrida causal:
- `MediaKind`;
- `_labelLayoutPlan is null`;
- `IsViewerBusy`;
- Export enabled;
- Print enabled;
- `LayoutValidationText`.

Expected contracts ya probados:
- Export enabled solo si plan válido + not busy;
- Print additionally requiere Thermal.

## Gate 0

Permanece:
`BLOCKED — UI-4 FAIL-P1`

F13 A+B está aprobado y preflight corregido, pero no se inicia código F13 hasta eliminar P0/P1 reales.

## Regla de siguiente cambio

La siguiente modificación productiva en Firma solo se acepta después de:

`REPRODUCE → TRACE → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → WINDOWS REVERIFY`
