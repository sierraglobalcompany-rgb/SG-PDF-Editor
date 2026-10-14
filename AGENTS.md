# Instrucciones para agentes de desarrollo

## Autoridad operativa

Orden obligatorio antes de tocar código o documentación operativa:

1. GitHub live: exact HEAD + CI exact-head;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice concreto;
6. `docs/MASTER_CONTEXT.md` para arquitectura y decisiones durables;
7. `docs/MASTER_PLAN.md` y `docs/history/**` como contexto arquitectónico/histórico, nunca para sobreescribir un estado operativo más nuevo.

`docs/MASTER_PLAN.md` nació como plan de ejecución y contiene snapshots antiguos de fases. Sus estados por fase son históricos salvo que GitHub/STATE/ROADMAP vigentes los confirmen.

## Contexto mínimo antes de actuar

1. leer `.planning/STATE.md`;
2. comprobar HEAD/CI reales de la rama de continuidad indicada por STATE;
3. leer ROADMAP + REQUIREMENTS del bloque actual;
4. abrir solo los archivos concretos necesarios;
5. usar Graphify local solo si reduce lectura/impacto;
6. consultar MASTER completos o históricos solo ante arquitectura, licencia, regresión, contradicción o mantenimiento de un slice cerrado.

## Estado actual — 2026-10-14

- F0–F12: **CLOSED / automated baseline PASS**.
- F12 está **feature-frozen**.
- Rama funcional autoritativa: `feat/f12-7-pdf-to-png-mvp-green3`.
- Functional HEAD: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.
- CI funcional: run `38555829715`, job `115824598759`, Release 0 warnings/0 errors, 1202/1202 tests PASS.
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`, read-only hasta aprobación explícita.
- Packs A–D: automated evidence preflight **PASS**; real/manual/private/physical gates **NOT RUN — ENVIRONMENT**.
- No promover CI sintético a PASS físico/manual.
- Siguiente trabajo: consolidación/routing → auditoría de ancestry/PRs → candidato de integración conservador → RC. No nuevas features oportunistas.

## Leyes obligatorias

### 1. KISS
Simple, eficiente, optimizado, eficaz. Preferir A→B. No arquitectura preventiva, no capas genéricas sin necesidad demostrada.

### 2. Anti-ruido
Antes de ADD: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

No borrar por intuición: demostrar primero que el elemento es redundante, histórico, sustituido o innecesario. Evitar Markdown-per-commit, ramas duplicadas, wrappers paralelos y abstracciones especulativas.

### Guardrail anti-parches
No es una tercera ley; es el flujo obligatorio ante defectos:

`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Arquitectura congelada / límites

- Windows x64 + WPF + .NET 10 mientras no exista evidencia material para cambiar.
- Mantener `SGPdf.App + SGPdf.App.Tests` mientras sea suficiente.
- PDFium es el motor PDF principal para lectura/render/edición.
- Toda llamada nativa PDFium usa `PdfiumRuntime.NativeGate`; PDFium no se trata como thread-safe.
- Labelize 1.7.0 es el único renderer ZPL runtime, como proceso local pinneado.
- BinaryKits es histórico; no fallback runtime.
- No Labelary ni APIs web runtime.
- PDFsharp 6.2.4 tiene uso **estrecho y aprobado**: composición/export PDF de etiquetas, F12.2 firma criptográfica PFX/P12, F12.3 AcroForm `/Tx` y fixtures/pruebas donde ya está demostrado. **No convertirlo en segundo motor PDF general ni usarlo como writer genérico del editor.**
- qpdf 12.4.2 permanece sidecar estrecho para la capacidad aprobada de protección AES; no motor PDF general.
- Tesseract permanece OCR local/offline.
- ZXing.Net 0.16.11 permanece test/QA-only.
- DejaVu Sans pinneado sigue siendo fallback permitido donde la baseline lo usa.
- No backend, DB, cloud, plugin system, segundo viewer, segundo PDF engine, generalized ModeManager, generic conversion framework o BatchEngine sin decisión explícita.
- No AGPL/GPL fuerte/licencia comercial obligatoria sin aprobación explícita.

## Alcance profesional congelado F12

- F12.1: safe redaction conservadora.
- F12.2: PFX/P12 RSA + detached SHA-256 CMS; no Windows Certificate Store, HSM/smartcard, TSA, revocación ni cualificación legal baseline.
- F12.3: editar campos AcroForm clásicos `/Tx`; no `/Btn`, `/Ch`, XFA, diseñador/creación general ni universal compatibility claim.
- F12.4: Compare local con señales texto/visual/estructura; no side-by-side/overlay/semantic diff.
- F12.5: Batch `TextWatermark` únicamente.
- F12.6: QR PNG únicamente.
- F12.7: PDF→PNG, todas las páginas, 144 DPI fijo, nombres deterministas, source untouched.

Cualquier ampliación requiere nuevo gate de valor/capacidad y aprobación explícita; no extender por conveniencia mientras se prepara release.

## Seguridad de datos y guardado

- Datos reales Mercado Libre/clientes nunca se versionan, CI, Graphify ni historial público.
- Private QA usa rutas ignoradas como `tests/PrivateFixtures/` cuando aplique.
- Preservar originales; `Guardar como`/outputs separados son la política segura donde la baseline lo define.
- No afirmar universal fidelity/preservation/interoperability si el requisito no lo demuestra.
- Real-photo, stylus/touch, impresora, scanner, PFX real, lector externo, private corpus y offline físico requieren evidencia real separada.

## Tooling de desarrollo

- GSD Core 1.15.0: dev-only.
- Graphify 0.9.77: dev-only.
- `.codex/`, `.devtools/`, `graphify-out/`: regenerables/ignorados.
- `.planning/`: versionado.
- Ningún tooling de desarrollo es requisito de runtime del producto.

## Git y ramas

- No merge a `main` sin aprobación explícita del usuario.
- No force-push/rebase como comodidad.
- No limpieza destructiva de ramas durante auditoría.
- No reusar ramas históricas divergentes como base operativa.
- Preservar evidencia exact-head.
- Para fixes reales, usar rama focal `feat/fix-*`; no meter código de producto dentro de una mega-rama de QA/documentación.

Ramas release-QA preflight vigentes como evidencia documental:
- `plan/release-pack-a-qa`;
- `plan/release-pack-b-qa`;
- `plan/release-pack-c-qa`;
- `plan/release-pack-d-qa`.

Estas ramas no sustituyen la rama funcional autoritativa.

## Flujo por slice o bloque

1. verificar STATE + HEAD + CI;
2. clasificar si es feature, fix, QA, documentación, integración o release;
3. reutilizar evidencia existente antes de crear tests/docs;
4. rama aislada cuando corresponda;
5. para comportamiento nuevo/fix: RED→GREEN y cambio mínimo;
6. build/tests/CI exact-head;
7. QA real cuando aplique;
8. actualizar STATE; actualizar ROADMAP solo si cambia el ruteo; crear histórico solo si aporta evidencia durable;
9. no merge sin aprobación explícita.

## Orden actual

```text
F0–F12 automated functional baseline             ✅ CLOSED / FROZEN
Pack A preflight                                 ✅ PASS / real QA NOT RUN
Pack B preflight                                 ✅ PASS / real QA NOT RUN
Pack C preflight                                 ✅ PASS / real QA NOT RUN
Pack D preflight                                 ✅ PASS / real QA NOT RUN
Routing/documentation consolidation              ▶ actual
Integration ancestry + PR audit                  ⏭ siguiente
Conservative integration candidate               ⏳ después
Installer/runtime/license + RC gates             ⏳ después
main                                              🔒 sin aprobación explícita
```

Para mantenimiento de slices cerrados, consultar REQUIREMENTS + closure/preservation doc correspondiente en `docs/history/**`; no reabrir trabajo porque un snapshot histórico diga “pendiente”.
