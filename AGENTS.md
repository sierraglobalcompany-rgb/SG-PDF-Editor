# Instrucciones para agentes de desarrollo

## Antes de tocar código

Leer en este orden:

1. `docs/MASTER_CONTEXT.md`
2. `docs/MASTER_PLAN.md`
3. `docs/ARCHITECTURE.md`
4. `docs/ROADMAP.md`
5. la Issue/PR de la fase activa

Si un documento secundario contradice `MASTER_CONTEXT` o `MASTER_PLAN`, prevalecen los maestros hasta alinear la documentación.

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

## Orden de ejecución actual

```text
A0  higiene/trazabilidad
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

1. revisar Issue y estado GitHub;
2. trabajar en rama aislada;
3. escribir/revisar prueba primero cuando haya comportamiento nuevo;
4. implementar mínimo;
5. verificar build/tests/CI;
6. QA manual/real cuando aplique;
7. actualizar docs/licencias;
8. actualizar PR;
9. generar Markdown histórico de estado;
10. no merge sin aprobación del usuario.
