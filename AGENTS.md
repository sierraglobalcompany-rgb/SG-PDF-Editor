# Instrucciones para agentes de desarrollo

## Fuentes de verdad
- Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.
- Estado: `.planning/STATE.md`.
- Roadmap: `.planning/ROADMAP.md`.
- Requisitos: `.planning/REQUIREMENTS.md`.
- Plan de fase: `.planning/phases/<fase>/...`.
- Código/estado ejecutado: Git/GitHub.
- Relaciones: Graphify local cuando aporte valor.

GitHub manda sobre qué se ejecutó. MASTER docs mandan sobre arquitectura salvo cambio aprobado.

## Contexto mínimo antes de tocar código
1. `.planning/STATE.md`;
2. roadmap + plan de fase activa;
3. Graphify solo si reduce lectura;
4. archivos concretos;
5. MASTER completos solo ante arquitectura/licencias/cambio de fase/contradicción.

## Tooling
- GSD Core `1.15.0` dev-only.
- Graphify `0.9.77` dev-only.
- `.codex/`, `.devtools/`, `graphify-out/` son regenerables/ignorados.
- `.planning/` sí se versiona.

## Reglas obligatorias
1. KISS/YAGNI; no capas/frameworks/DI/plugin systems preventivos.
2. Mantener `SGPdf.App + SGPdf.App.Tests` mientras sea suficiente.
3. Windows x64 + WPF + .NET 10 salvo evidencia material.
4. PDFium es el motor PDF principal para lectura/render/edición; añadir solo APIs nativas necesarias.
5. PDFium no es thread-safe: toda llamada nativa usa `PdfiumRuntime.NativeGate`.
6. Labelize 1.7.0 es el único renderer ZPL runtime; local child-process; BinaryKits histórico.
7. PDFsharp 6.2.4 está limitado a composición/export de PDFs de etiquetas. **No usarlo para firma/edición PDF de F3.**
8. No AGPL/GPL fuerte/licencia comercial obligatoria sin aprobación explícita.
9. No Labelary ni APIs web runtime.
10. `Guardar como` es comportamiento seguro por defecto en primeras fases.
11. Datos reales Mercado Libre/clientes nunca se versionan/CI/Graphify.
12. Cada slice requiere evidencia fresca; no afirmar hardware/private PASS desde CI sintético.
13. No auto-merge a `main`.
14. No refactors preventivos masivos.
15. Cada cierre relevante actualiza STATE/ROADMAP/histórico.
16. Para etiquetas: mm/dpmm son autoridad; nunca shrink silencioso.
17. Thermal print reutiliza `LabelLayoutPlan`, CopyCount=1, imageable area solo advierte.
18. No RAW ZPL/vendor SDK/direct transport sin nuevo slice aprobado.
19. ZXing.Net 0.16.11 permanece test/QA-only.
20. Private QA F2 usa ignored `tests/PrivateFixtures/`; no subir customer artifacts.

## Reglas F3 — Firma Visual

- F3 es **firma visual**, no firma criptográfica. La firma criptográfica queda para fase profesional.
- Posición/tamaño de firma se guarda en **coordenadas PDF**, nunca como píxeles WPF durables.
- `PdfRenderedPage.DeviceTransform` / PDFium device→page transform es autoridad para reproyección de overlay.
- `SignatureEditState` F3.1 es current-page only; no sesión multipágina pendiente todavía.
- PDF original no se modifica. `PdfVisualSignatureWriter` abre el source separadamente, escribe temp, cierra/release `NativeGate`, reabre/renderiza para validar y solo entonces reemplaza/mueve destino.
- Nunca reabrir/renderizar el temp mientras se retiene `NativeGate`.
- Transparencia alpha y orientación deben seguir cubiertas por pruebas de save/reopen; no rasterizar la página completa como fallback.
- Si `FPDF_GetSignatureCount` no puede evaluarse, bloquear guardado. Si hay firmas criptográficas existentes, advertir que la modificación puede invalidarlas.
- No introducir PDFsharp como writer de F3, nuevas dependencias, nube, IA, OCR ni red.
- **Ruling F3.1:** el mode strip/panel/overlay se construye en `MainWindow.Sign.cs` durante inicialización en vez de reescribir el XAML grande. Mantener ese enfoque KISS salvo evidencia UX real.

### Reglas F3.2 — Foto/escaneo
- Entrada solo local `.png/.jpg/.jpeg`; máximo **20,000,000 píxeles decodificados**.
- `SignaturePhotoSource` y `SignatureAsset` son BGRA in-memory; no persistir intermediarios.
- La estimación de papel se calcula una sola vez desde la fuente full-resolution inmutable. Preview y Apply reutilizan exactamente ese `SignaturePaperColor`.
- Preview puede reducirse a máximo **1200 px** de lado mayor; jamás usar el preview como asset final.
- Apply siempre procesa la fuente full-resolution con los settings actuales.
- Background removal usa alpha suave y multiplica el alpha original; transparente nunca puede volverse opaco.
- Ajustes expuestos: quitar fondo, brillo, contraste, tinta Original/Negro/Azul y auto-crop. No crecer a editor fotográfico.
- Azul fijo F3.2: `#194196`.
- Cancel/failure deja placements, selección y dirty state intactos.
- Sin OpenCV/ImageSharp, AI/ML, cloud removal, OCR, HTTP, telemetry, upload o temp signature image.
- Calidad con fotos reales es gate manual separado; CI sintético no equivale a real-photo PASS.
- F3.3 y F3.4 requieren slices/planes separados; no implementarlos oportunísticamente dentro de F3.2.

## Orden actual
```text
A0  higiene/trazabilidad                         ✅ completada
A1  GSD Core + Graphify                         ✅ completada
F0  PDF base                                    ✅ automated PASS / ⏳ physical QA
F1  Gate ZPL-A                                  ✅ synthetic Gate / ⏳ private corpus
F2  etiquetas ZPL                               ✅ F2.1–F2.6 automated PASS
    private corpus / physical thermal-scanner   ⏳ NOT RUN
F3  firma visual
    F3.1 core visual signature                  ✅ automated PASS
    F3.2 foto/scan cleanup                      ✅ automated PASS / ⏳ real-photo QA
    F3.3 dibujo InkCanvas                       ▶ siguiente — design first
    F3.4 biblioteca local                       ⏳ después
F4  lector completo                             ⏳ pendiente
F5+ fases posteriores                           ⏳ pendiente
```

## F3.2 summary

F3.2 dejó:
- loader PNG/JPEG local con límite compartido 20M;
- paper estimate determinista + soft-alpha cleanup;
- brillo/contraste + Original/Negro/Azul;
- blank/noise rejection + auto-crop;
- preview checkerboard <=1200 px con stale-request guard;
- final Apply desde full source;
- `Crear desde foto...` integrado a FIRMAR;
- cancel/failure state-safe;
- no package/lock/PDFium writer/network changes.

Functional evidence: head `710340aed222d4c0bd97fc80099267af35ef6f95`, PR CI `37724154596` PASS, Release 0 warnings/0 errors, **214 tests PASS**. Final docs-head CI se registra en PR #20.

## Flujo por slice
1. revisar STATE/GitHub;
2. revisar roadmap/plan;
3. rama aislada;
4. TDD RED→GREEN;
5. implementación mínima;
6. build/tests/CI;
7. QA manual cuando aplique;
8. docs/estado/histórico;
9. PR draft;
10. no merge sin aprobación explícita.