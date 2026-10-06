# Instrucciones para agentes de desarrollo

## Fuentes de verdad

La autoridad se divide así:

- arquitectura/decisiones permanentes: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`;
- estado operativo futuro: `.planning/STATE.md` cuando A1.2 lo cree;
- ejecución de la fase: `.planning/phases/<fase>/PLAN.md` cuando exista;
- código real: Git/GitHub;
- relaciones de código: Graphify cuando esté habilitado.

Si GitHub difiere de los documentos sobre qué se ejecutó, GitHub tiene prioridad para el estado real. Si un documento secundario contradice los MASTER docs sobre arquitectura, prevalecen los MASTER docs salvo cambio aprobado.

## Antes de tocar código

### Mientras A1 no esté completamente instalado

Leer:

1. `docs/MASTER_CONTEXT.md`;
2. `docs/MASTER_PLAN.md`;
3. la Issue/PR/fase activa;
4. solo los archivos de código necesarios.

### Después de A1.2 (GSD + Graphify operativos)

Usar contexto mínimo en este orden:

1. `.planning/STATE.md`;
2. plan de la fase activa;
3. consulta Graphify cuando ayude a localizar impacto/dependencias;
4. leer únicamente los archivos concretos identificados;
5. cargar `MASTER_CONTEXT.md` / `MASTER_PLAN.md` completos solo si la tarea toca arquitectura, licencias, cambio de fase o existe una contradicción.

No releer todo el repositorio por defecto. GSD y Graphify son herramientas de desarrollo, nunca dependencias runtime de SG PDF Editor.

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
A1  GSD Core + Graphify                         ▶ actual
F0  PDF base
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

1. revisar estado GitHub y GSD `STATE` cuando exista;
2. revisar Issue y plan de la fase;
3. trabajar en rama aislada;
4. usar Graphify para impacto/dependencias cuando aporte valor;
5. escribir/revisar prueba primero cuando haya comportamiento nuevo;
6. implementar mínimo;
7. verificar build/tests/CI;
8. QA manual/real cuando aplique;
9. actualizar docs/licencias;
10. actualizar PR;
11. actualizar `STATE`/`SUMMARY` cuando GSD esté operativo;
12. generar Markdown histórico de estado;
13. no merge sin aprobación del usuario.
