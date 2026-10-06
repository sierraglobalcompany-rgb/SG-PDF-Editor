# SG PDF Editor

Aplicación Windows local-first para trabajar con PDF y etiquetas térmicas ZPL sin depender de Internet, APIs ni servicios de pago.

## Objetivo

Resolver primero los flujos de uso diario:

### PDF
- abrir, leer, navegar e imprimir;
- firmar visualmente con PNG transparente;
- organizar páginas;
- editar imágenes;
- editar texto de forma conservadora cuando el PDF lo permita;
- comentar/anotar.

### Etiquetas térmicas
- abrir `.zpl`, `.txt` y `.prn` con ZPL;
- respetar cantidades `^PQ`;
- previsualizar localmente;
- maquetar 1/2/3/4/6/8/10/12 o grid personalizado por página;
- exportar a PDF;
- imprimir mediante Windows aunque la impresora no interprete ZPL directamente.

## Principios

- KISS + YAGNI.
- 100 % offline para funciones principales.
- Sin API keys, SaaS ni cuentas.
- Sin licencias comerciales obligatorias.
- Preferir MIT/BSD/Apache-2.0.
- `Guardar como` primero para proteger originales.
- No merge automático a `main`.

## Stack congelado

- C# / .NET 10 / WPF.
- PDFium como motor PDF principal.
- BinaryKits.Zpl como candidato preferente para ZPL, sujeto a Gate técnico.
- Labelize como fallback condicionado.
- PDFsharp únicamente para composición puntual de PDFs de etiquetas.
- Tesseract/PdfPig/qpdf/pdfcpu solo si una fase futura demuestra que hacen falta.

## Arquitectura KISS

Inicialmente solo:

```text
src/SGPdf.App
tests/SGPdf.App.Tests
```

No hay `Core`, `Infrastructure`, microservicios ni plugin framework preventivos.

## Estado actual

- Rama activa: `feat/kiss-vertical-slice`.
- PR #2: draft, sin merge.
- Arquitectura auditada/congelada.
- Fase actual: **A0 — higiene, trazabilidad y reproducibilidad**.
- Siguiente fase: **F0 — lector PDF base: open/render/cancel/zoom/navigation/print**.

## Fuente de verdad

Antes de implementar leer:

1. [`docs/MASTER_CONTEXT.md`](docs/MASTER_CONTEXT.md) — contexto completo y decisiones.
2. [`docs/MASTER_PLAN.md`](docs/MASTER_PLAN.md) — fases y ejecución.
3. [`AGENTS.md`](AGENTS.md) — reglas para agentes.

Los demás documentos resumen partes del plan y no deben contradecir a los maestros.

## Privacidad

Los documentos reales de clientes y ZPL reales de Mercado Libre no se versionan. CI usa fixtures sintéticos equivalentes.

## Licencia del código propio

Actualmente `All rights reserved` para Sierra Global Company. Las dependencias de terceros mantienen sus propias licencias y avisos.
