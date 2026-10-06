# SG PDF Editor — Resumen del plan de desarrollo

> **Fuente de verdad:** `MASTER_CONTEXT.md` + `MASTER_PLAN.md`.
> Este documento es un resumen operativo. No debe redefinir arquitectura ni dependencias.

## Objetivo

Construir una aplicación Windows WPF/.NET 10, KISS, gratuita y offline para:

1. PDF: leer, imprimir, firmar visualmente, organizar y editar de forma práctica.
2. Etiquetas térmicas: abrir ZPL/TXT/PRN, preview, cantidades, layout, PDF e impresión sin Internet.

## Stack base

- PDFium: PDF.
- BinaryKits.Zpl: candidato ZPL preferente, sujeto a Gate ZPL-A.
- Labelize: fallback condicionado.
- PDFsharp: composición puntual de etiquetas.
- Dependencias futuras solo cuando su fase las necesite.

## Orden de ejecución

```text
A0  Higiene/trazabilidad
F0  PDF base
F1  Gate ZPL-A
F2  Etiquetas
F3  Firma visual
F4  Lector completo
F5  Organizar
F6  Imágenes
F7  Texto V1
F8  Comentarios
F9  Utilidades
F10 OCR
F11 Texto V2
F12 Profesional
```

## Reglas

- App + Tests mientras sea suficiente.
- PDFium con exclusión global y trabajo fuera del hilo UI.
- Save As por defecto en primeras fases.
- No Labelary/API web en runtime.
- No AGPL/GPL fuerte ni SDK comercial sin aprobación.
- Datos reales de clientes/ML fuera del repo.
- Una dependencia entra solo si resuelve una necesidad actual mejor que el stack presente.
- No merge a `main` sin aprobación del usuario.

## Definition of Done

Una feature requiere evidencia real de build/tests/QA, funcionamiento offline, errores controlados, protección del original, dependencias auditadas y documentación alineada.

Para criterios de aceptación por fase, consultar `MASTER_PLAN.md`.
