# SG PDF Editor

## What This Is

SG PDF Editor es una aplicación Windows local-first para leer, imprimir, firmar visualmente, organizar y editar PDF de forma práctica. También incorpora un workspace offline para abrir, previsualizar, maquetar, convertir e imprimir etiquetas térmicas ZPL/TXT/PRN, especialmente las generadas por Mercado Libre.

## Core Value

Resolver los flujos PDF y ZPL de uso diario de forma rápida, privada, estable y totalmente utilizable sin Internet ni servicios de pago.

## Requirements

### Validated

- ✓ Base del repositorio reproducible: WPF + .NET 10, App + Tests, restore locked y CI Windows — A0.
- ✓ Arquitectura KISS/offline y política de licencias documentadas — A0.
- ✓ Flujo de desarrollo GSD Core + Graphify validado en entorno limpio, sin formar parte del runtime — A1.

### Active

- [ ] F0: lector PDF base — abrir, renderizar, cancelar, zoom, navegar e imprimir.
- [ ] F1: elegir motor ZPL mediante Gate BinaryKits.Zpl vs Labelize.
- [ ] F2: workspace de etiquetas ZPL offline.
- [ ] F3: firma visual PNG transparente.
- [ ] F4-F12: lector completo, organizar, imágenes, texto, comentarios, OCR y funciones profesionales según roadmap.

### Out of Scope

- Nube/SaaS/API obligatoria — contradice offline-first y privacidad.
- AGPL/GPL fuerte o licencia comercial obligatoria en el runtime — incompatible con la política del producto salvo aprobación explícita.
- Electron/Node como runtime del producto — WPF/.NET 10 cubre el objetivo Windows con menos piezas.
- Arquitectura empresarial preventiva (CQRS, event bus, plugin framework, múltiples proyectos por capas) — YAGNI.
- OCR, firma criptográfica, formularios y conversiones Office antes de sus fases — no adelantar dependencias.

## Context

- El usuario usa PDF de forma frecuente y necesita firma visual, organización y edición práctica.
- Mercado Libre genera ZPL que hoy se convierte manualmente con Labelary antes de imprimir; SG PDF Editor debe reemplazar ese flujo offline.
- PDFium es el motor PDF principal. BinaryKits.Zpl es el candidato ZPL preferente sujeto a Gate real; Labelize es fallback.
- GSD Core y Graphify son herramientas de desarrollo regenerables y project-scoped; no son requisitos para compilar ni ejecutar el producto.
- `docs/MASTER_CONTEXT.md` y `docs/MASTER_PLAN.md` conservan el contexto arquitectónico completo.

## Constraints

- **Plataforma**: Windows x64 inicialmente — prioriza el entorno real de uso.
- **Stack**: C# + .NET 10 LTS + WPF — arquitectura congelada salvo evidencia material.
- **Offline**: funciones principales deben operar sin Internet — privacidad y continuidad operativa.
- **Costo**: sin APIs, SaaS, activaciones o SDK comerciales obligatorios — producto gratuito de usar.
- **Licencias**: priorizar MIT/BSD/Apache-2.0 — evitar copyleft fuerte/comercial obligatorio.
- **Datos privados**: ZPL reales/clientes nunca se versionan — `tests/PrivateFixtures/` permanece local.
- **Git**: `main` estable; no merge automático — aprobación explícita del usuario.
- **PDFium**: llamadas nativas serializadas globalmente — PDFium no es thread-safe.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| WPF + .NET 10 | Menor complejidad para Windows, impresión y P/Invoke | ✓ Good |
| App + Tests inicialmente | KISS; evitar capas sin necesidad | ✓ Good |
| PDFium como motor PDF primario | Permisivo y cubre render/objetos/páginas | — Pending |
| BinaryKits.Zpl candidato preferente | .NET in-process, offline, MIT | — Pending Gate ZPL-A |
| GSD Core 1.15.0 project-scoped | Estado/planes reproducibles sin contaminar otros repos | ✓ Validated in A1 probe |
| Graphify 0.9.77 project-scoped | Grafo AST local y consultas dirigidas | ✓ Validated in A1 probe |
| No versionar `.codex/` ni `graphify-out/` | Son generados, grandes/cambiantes y reproducibles | ✓ Good |
| `Guardar como` inicialmente | Proteger originales mientras madura edición/guardado | — Pending |

## Evolution

Actualizar este archivo cuando cambie el alcance real, se valide una fase o una decisión material cambie. Mantenerlo resumido; el detalle histórico vive en `docs/history/`.

---
*Last updated: 2026-10-06 after A1 tooling validation.*
