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
- Graphify auto-update permanece desactivado; actualizar manualmente cuando aporte valor.
- GSD 1.15.0 no acepta `graphify.enabled`; no usar claves de versiones futuras sin auditar upgrade.

## Misión

Construir SG PDF Editor como aplicación Windows simple, rápida, offline y útil para dos flujos principales:

1. PDF: leer, imprimir, firmar, organizar y editar de forma práctica.
2. Etiquetas térmicas: abrir ZPL/TXT/PRN, previsualizar, maquetar, exportar e imprimir sin Internet.

## Reglas obligatorias

1. KISS/YAGNI: no crear capas, proyectos, interfaces genéricas, DI complejo, buses de eventos, plugin systems ni patrones sin necesidad actual demostrable.
2. Mantener inicialmente `SGPdf.App + SGPdf.App.Tests`.
3. WPF + .NET 10 se mantienen salvo evidencia material que obligue a cambiar.
4. PDFium es el motor PDF principal; añadir solo las APIs nativas que la fase necesite.
5. PDFium no es thread-safe: todas las llamadas nativas pasan por exclusión global y nunca bloquean el hilo UI.
6. BinaryKits.Zpl es candidato preferente para ZPL, pero no se declara ganador hasta pasar Gate ZPL-A. Labelize es fallback condicionado.
7. PDFsharp se usa de forma puntual para composición de PDFs de etiquetas, no como lector/editor principal.
8. qpdf/pdfcpu, Tesseract, PdfPig u otra dependencia entran únicamente cuando una fase concreta demuestre la necesidad.
9. No incorporar AGPL/GPL fuerte ni dependencias que exijan licencia comercial sin aprobación explícita. Priorizar MIT/BSD/Apache-2.0.
10. No usar Labelary ni APIs web en runtime.
11. `Guardar como` es el comportamiento seguro por defecto durante las primeras fases.
12. Los archivos reales Mercado Libre/clientes no se suben al repo público; usar `tests/PrivateFixtures/` local.
13. Cada cambio debe quedar verificable con tests/CI/QA apropiados; no afirmar éxito sin evidencia fresca.
14. No hacer merge automático a `main`.
15. No realizar refactors preventivos masivos.
16. GSD Core y Graphify son dev-only y deben poder fallar/desinstalarse sin impedir compilar/usar el producto.
17. Graphify se usa cuando reduce lecturas; no sustituye compiler, tests ni una búsqueda simple cuando esta sea más rápida.
18. Cada cierre de trabajo relevante actualiza estado/resumen y genera Markdown histórico portable para continuidad entre chats.

## Orden de ejecución actual

```text
A0  higiene/trazabilidad                         ✅ completada
A1  GSD Core + Graphify                         ▶ validación final
F0  PDF base                                    ⏭ siguiente
F1  Gate ZPL-A
F2  etiquetas ZPL
F3  firma visual
F4  lector completo
F5  organizar
F6  imágenes
F7  texto V1
F8+ fases posteriores
```

## UX base

Un PDF abre siempre en `Leer`.

Modos principales:

```text
Leer | Firmar | Editar | Organizar | Comentar
```

Los menús de clic derecho son contextuales para imagen, texto, página o espacio vacío.

## Flujo de trabajo

Para cada vertical slice:

1. revisar `.planning/STATE.md` y estado GitHub;
2. revisar Issue/ROADMAP/plan de la fase;
3. trabajar en rama aislada;
4. usar Graphify para impacto/dependencias cuando aporte valor;
5. escribir/revisar prueba primero cuando haya comportamiento nuevo;
6. implementar mínimo;
7. verificar build/tests/CI;
8. QA manual/real cuando aplique;
9. actualizar docs/licencias;
10. actualizar PR;
11. actualizar `STATE`/`SUMMARY`;
12. generar Markdown histórico de estado;
13. no merge sin aprobación del usuario.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
