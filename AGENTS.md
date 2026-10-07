# Instrucciones para agentes de desarrollo

## Fuentes de verdad

La autoridad se divide así:

- arquitectura/decisiones permanentes: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`;
- estado operativo: `.planning/STATE.md`;
- roadmap GSD: `.planning/ROADMAP.md`;
- requisitos resumidos: `.planning/REQUIREMENTS.md`;
- ejecución de la fase: `.planning/phases/<fase>/PLAN.md` cuando exista;
- código real/estado ejecutado: Git/GitHub;
- relaciones de código: Graphify local cuando aporte valor.

Si GitHub difiere de los documentos sobre qué se ejecutó, GitHub tiene prioridad para el estado real. Si un documento secundario contradice los MASTER docs sobre arquitectura, prevalecen los MASTER docs salvo cambio aprobado.

## Antes de tocar código

Usar contexto mínimo en este orden:

1. `.planning/STATE.md`;
2. `.planning/ROADMAP.md` y plan de la fase activa;
3. consulta Graphify cuando ayude a localizar impacto/dependencias;
4. leer únicamente los archivos concretos identificados;
5. cargar `docs/MASTER_CONTEXT.md` / `docs/MASTER_PLAN.md` completos solo si la tarea toca arquitectura, licencias, cambio de fase o existe una contradicción.

No releer todo el repositorio por defecto. Si GSD/Graphify no están instalados, el trabajo del producto NO se bloquea: usar Git/GitHub/búsqueda normal y ejecutar `tools/setup-dev.ps1` cuando convenga.

## Tooling de desarrollo pinneado

- GSD Core `1.15.0`, project-scoped para Codex mediante `tools/setup-dev.ps1`.
- Graphify `0.9.77`, project-scoped y en virtualenv local bajo `.devtools/`.
- `.codex/`, `.devtools/` y `graphify-out/` son generados/regenerables y no se versionan.
- `.planning/` sí se versiona como memoria operativa.
- GSD/Graphify son dev-only y nunca dependencia runtime/build del producto.

## Misión

Construir SG PDF Editor como aplicación Windows simple, rápida, offline y útil para dos flujos principales:

1. PDF: leer, imprimir, firmar, organizar y editar de forma práctica.
2. Etiquetas térmicas: abrir ZPL/TXT/PRN, previsualizar, maquetar, exportar e imprimir sin Internet.

## Reglas obligatorias

1. KISS/YAGNI: no crear capas, proyectos, interfaces genéricas, DI complejo, buses de eventos, plugin systems ni patrones sin necesidad actual demostrable.
2. Mantener `SGPdf.App + SGPdf.App.Tests` mientras siga siendo suficiente.
3. WPF + .NET 10 se mantienen salvo evidencia material que obligue a cambiar.
4. PDFium es el motor PDF principal para lectura/render; añadir solo APIs nativas necesarias.
5. PDFium no es thread-safe: todas las llamadas nativas pasan por exclusión global y nunca bloquean innecesariamente el hilo UI.
6. Labelize 1.7.0 Windows x64 es el único motor ZPL runtime; sidecar local child-process. BinaryKits queda histórico y no es fallback.
7. PDFsharp 6.2.4 está aprobado desde F2.4 únicamente para composición/export de PDFs de etiquetas; **no** es lector/editor PDF principal.
8. qpdf/pdfcpu, Tesseract, PdfPig u otra dependencia entran únicamente cuando una fase concreta demuestre necesidad.
9. No incorporar AGPL/GPL fuerte ni licencia comercial obligatoria sin aprobación explícita. Priorizar MIT/BSD/Apache-2.0.
10. No usar Labelary ni APIs web en runtime.
11. `Guardar como` es el comportamiento seguro por defecto durante primeras fases.
12. Archivos reales Mercado Libre/clientes no se suben al repo público; usar `tests/PrivateFixtures/` local cuando corresponda.
13. Cada cambio debe quedar verificable con tests/CI/QA apropiados; no afirmar éxito sin evidencia fresca.
14. No hacer merge automático a `main`.
15. No realizar refactors preventivos masivos.
16. GSD Core y Graphify deben poder fallar/desinstalarse sin impedir compilar/usar el producto.
17. Graphify se usa cuando reduce lecturas; no sustituye compiler/tests/búsqueda simple.
18. Cada cierre relevante actualiza estado/resumen y genera Markdown histórico portable.
19. Para layouts de etiquetas, dimensiones físicas en milímetros son autoridad; nunca shrink-to-fit silencioso.
20. Impresión térmica Windows reutiliza el `LabelLayoutPlan` de F2.4; no duplicar un segundo motor de layout.
21. `PageImageableArea` solo advierte posible clipping; nunca debe causar escalado/traslación silenciosa.
22. SG PDF Editor es la autoridad de cantidad de etiquetas; `PrintTicket.CopyCount` se normaliza a 1.
23. No introducir RAW ZPL/vendor SDK/direct USB-serial-socket sin un slice y aprobación explícitos.

## Orden de ejecución actual

```text
A0  higiene/trazabilidad                         ✅ completada
A1  GSD Core + Graphify                         ✅ completada
F0  PDF base                                    ✅ automated PASS / ⏳ physical QA
F1  Gate ZPL-A                                  ✅ synthetic Gate / ⏳ private corpus
F2  etiquetas ZPL                               ▶ activa
    F2.1 Parse + Open                           ✅ automated PASS
    F2.2 Labelize + Preview                     ✅ automated PASS
    F2.3 Quantity UX + dimensiones              ✅ automated PASS
    F2.4 Layout + PDF Export                    ✅ automated PASS
    F2.5 Windows Thermal Print                  ✅ automated PASS / ⏳ physical QA
    F2.6 Validation/hardening                   ▶ siguiente
F3  firma visual                                ⏳ pendiente
F4  lector completo                             ⏳ pendiente
F5+ fases posteriores                           ⏳ pendiente
```

F2.5 dejó:

- preflight puro de tamaño/DPI/copias/área imprimible;
- tolerancia driver 0.5 mm por dimensión solo para aceptar cuantización, nunca para escalar;
- `LabelPrintPaginator` exacto, 1 etiqueta por página;
- Windows `PrintQueue` / `PrintTicket` / `PrintCapabilities` + `MergeAndValidatePrintTicket`;
- `CopyCount=1` y cantidades provenientes del modelo SG PDF Editor;
- clipping warning sin shrink-to-fit;
- botón `Imprimir etiquetas...` solo en layout Thermal válido;
- impresión no rerenderiza Labelize ni muta workspace;
- sin nuevo NuGet, RAW ZPL, vendor SDK, PDF intermediario ni red runtime.

**Importante:** CI no prueba papel real. F2.5 está automatizado; dimensiones físicas impresas, clipping real y lectura scanner permanecen NOT RUN hasta F2.6/manual hardware QA.

Continuar por slices pequeños. Antes de F2.6 revisar `STATE`, spec/plan vigente y definir claramente qué parte puede cerrarse en CI y qué requiere corpus/hardware privado.

## UX base

Un PDF abre siempre en `Leer`.

```text
Leer | Firmar | Editar | Organizar | Comentar
```

Los menús de clic derecho son contextuales para imagen, texto, página o espacio vacío.

## Flujo de trabajo

Para cada vertical slice:

1. revisar `.planning/STATE.md` y GitHub;
2. revisar roadmap/requisitos/plan activo;
3. rama/worktree aislado;
4. usar Graphify si reduce lectura/impacto;
5. prueba/reproducción primero para comportamiento nuevo;
6. implementar mínimo;
7. verificar build/tests/CI;
8. QA manual/real cuando aplique;
9. actualizar dependencias/licencias si cambian;
10. actualizar PR;
11. actualizar `STATE`/roadmap/histórico;
12. no merge sin aprobación del usuario.

## graphify

This project has a knowledge graph at `graphify-out/` with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill/instructions first.

Rules:
- For codebase questions, run `graphify query "<question>"` first when `graphify-out/graph.json` exists y aporta valor.
- Use `graphify path` for relationships and `graphify explain` for focused concepts.
- Dirty ignored graph outputs are expected; not a reason to skip Graphify.
- Use `graphify-out/wiki/index.md` for broad navigation when present.
- Read `GRAPH_REPORT.md` only for broad architecture review or if targeted queries are insufficient.
- After modifying code locally, `graphify update .` keeps the graph current; graph output remains ignored/regenerable.
