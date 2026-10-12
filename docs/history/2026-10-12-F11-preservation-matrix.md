# F11 Texto V2 — Preservation Matrix

**Fecha:** 2026-10-12  
**Rama:** `feat/f11-text-v2`  
**Propósito:** congelar qué preserva F11 automáticamente y qué sigue requiriendo QA real/manual.

| Área | Riesgo F11 | Guardrail / evidencia automatizada | Estado |
|---|---|---|---|
| PDF fuente | Sobrescritura accidental | F11 reutiliza el Save As transaccional de Texto V1; no introduce writer alterno | AUTO PASS |
| Layout desconocido | Modificar geometría ambigua | `TextLayoutClassification` usa `Simple/Ambiguous/Unsupported`; solo `Simple` puede llegar a reflow | AUTO PASS |
| Dos columnas | Intercalar o redistribuir incorrectamente | Reading order soporta dos columnas simples, pero `TextReflowPolicy` exige una sola columna para editar | AUTO PASS |
| Orden bruto PDF | Leer según object index en vez de geometría | Corpus sintético con orden almacenado incorrecto/entremezclado valida orden geométrico determinista | AUTO PASS |
| Rotación | Reflow de texto no horizontal | Orientación incompatible se clasifica `Unsupported` | AUTO PASS |
| Solapes | Cambiar bloques visualmente cruzados | Policy rechaza runs solapados | AUTO PASS |
| Estilo | Mezclar fuentes/tamaños/colores | Policy exige estilo homogéneo con tolerancia conservadora | AUTO PASS |
| OCR F10 | Editar capa invisible buscable | `TextRenderMode != Fill` se rechaza como solo lectura para reflow | AUTO PASS |
| Preflight general | Saltar bloqueo de firma/protección | Reflow exige `PdfEditPreflightResult.CanProceed` | AUTO PASS |
| Mutación parcial | Aplicar algunos runs y fallar después | `TextEditWorkspace.ApplyReflowPlan` prepara todos los candidatos antes de commit | AUTO PASS |
| Capacidad geométrica | Texto nuevo excede objetos existentes | Planner usa capacidad conservadora por run y rechaza sin mutar cuando no cabe | AUTO PASS |
| Objetos nuevos | Convertir F11 en motor de maquetación | Planner solo asigna texto a `TextObjectKey` existentes; no crea/mueve objetos | AUTO PASS |
| Escritura PDF | Duplicar lógica/transacción | Se reutiliza `PdfEditWriter` de Texto V1 | AUTO PASS |
| Reopen/semantic validation | Archivo guardado inválido | Writer existente reabre/valida; Task 5 cubre ruta transaccional | AUTO PASS |
| Render posterior | Texto semánticamente presente pero visualmente roto | Task 5 añadió validación de salida renderizada en el corpus automatizado | AUTO PASS |
| UI stale state | Aplicar reflow de otra selección/página | Estado F11 se resetea al ocultar/cambiar selección y valida page index activo antes de analizar | AUTO PASS |
| Error de análisis | Romper Texto V1 por falla F11 | UI F11 captura cancelación/error, deshabilita reflow y mantiene la edición básica separada | AUTO PASS |
| Dependencias | Añadir segundo parser/motor innecesario | Gate PDFium demostró suficiencia; PdfPig = NO-NEED | AUTO PASS |
| Privacidad | Fixture privado versionado | Se mantiene hygiene de `tests/PrivateFixtures/**`; F11 usa corpus sintético | AUTO PASS |

## Evidencia clave

- Integración PDFium: `3340e7ede89b08984b57783999c19da25a68b3f3`; CI `38209383197` / `114679912957`.
- Policy de reflow: `8de3a746568a4662438269048e0d60093786158f`.
- Writer/render de reflow: `024e0f043b6b46bee6b8901f8301c727c28aacbf`.
- UI Task 6 RED: `0e2d930e487716a7417304b595d17b86978aa327`; CI `38261990458` / `114854559091`.
- UI Task 6 GREEN: `554406390cf87a2e9a61471b60105c7fba2ba4fb`; CI `38262423101` / `114856039976`; 1085/1085, 0 warnings/errors.

## No certificado por esta matriz

Estas áreas siguen **NOT RUN** y no deben inferirse desde CI:

- UI real de Windows con mouse/touch/stylus;
- documentos privados y layouts editoriales reales complejos;
- fidelidad visual subjetiva en un corpus amplio;
- Adobe Acrobat, Foxit y Edge sobre salidas F11;
- comportamiento offline físico en hardware representativo;
- impresión física posterior a edición.

## Regla de cierre

Esta matriz respalda cierre **automatizado** de F11 únicamente después de que el commit documental que la contiene pase CI exact-head. No autoriza merge a `main` ni inicio de F12.
