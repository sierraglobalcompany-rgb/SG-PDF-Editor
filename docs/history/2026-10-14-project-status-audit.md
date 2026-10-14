# SG PDF — Auditoría general de estado y plan de continuidad

**Fecha:** 2026-10-14  
**Repositorio:** `sierraglobalcompany-rgb/SG-PDF-Editor`  
**Baseline auditado:** `149f716b704c43ea43bf94fa517fa5922920237b`  
**Baseline CI:** Release build 0 warnings / 0 errors; `1178/1178` tests PASS; 0 skipped.

## 1. Resumen ejecutivo

El proyecto **sí está avanzando** y la arquitectura funcional no muestra señales de sobreingeniería relevante. La sensación de “ping-pong” proviene principalmente de la forma de operar el desarrollo, no de un bloqueo técnico del producto.

Estado estimado de ingeniería:

- **Implementación funcional automatizada: ~96%.**
- **Preparación real para release: ~86%.**
- **Estado global ponderado: ~91%.**

Son estimaciones de ingeniería basadas en el roadmap y evidencia del repositorio; no son telemetría.

La brecha entre 96% funcional y 86% de release readiness se explica por:
- QA manual/privado/físico aún pendiente en varios módulos;
- interoperabilidad real pendiente donde aplica;
- deuda de integración de ramas;
- `main` muy atrasado respecto a la línea activa;
- limpieza/consolidación de ramas y PRs antes de release.

## 2. Qué está realmente terminado

### F0–F11

Automated CLOSED / PASS según matrices e historial del proyecto, manteniendo los gates manuales/privados/físicos documentados como NOT RUN.

### F12.1 — Safe Redaction

**CLOSED / AUTO PASS.**

Redacción real conservadora, fail-closed, transaccional y sin segundo motor.

### F12.2 — Cryptographic Signature PFX/P12 MVP

**CLOSED / AUTO PASS.**

Firma RSA PFX/P12 local, CMS detached SHA-256, validación independiente y publicación transaccional.

### F12.3 — Existing AcroForm `/Tx`

**CLOSED / AUTO PASS.**

Campos de texto clásicos existentes, incluidos flat/hierarchical/inherited cases. La preservación se declara como **representative preservation gate**, no universal.

### F12.4 — Compare core

**CLOSED / GREEN.**

Capacidad demostrada:
- identical;
- text-only;
- visual-only;
- structural page-count difference.

Sin nueva dependencia ni segundo motor.

### F12.4 — Compare UI

**DESIGN APPROVED / SPEC WRITTEN / IMPLEMENTATION PLAN READY.**

Todavía no implementada.

## 3. Qué falta funcionalmente

La línea base del `MASTER_PLAN` deja cuatro bloques principales:

1. **Cerrar F12.4 Compare UI.**
2. **F12.5 Batch.**
3. **F12.6 Code Generator.**
4. **F12.7 Audited Conversions.**

Después de eso queda el bloque de **release integration + stabilization + real/manual/private QA**.

Advanced form types y advanced signing no se consideran baseline pendiente: solo se abren si el usuario los aprueba explícitamente.

## 4. Por qué se sintió como ping-pong

### Hallazgo A — TDD demasiado expuesto al usuario

El proceso correcto RED → GREEN → CI → audit se convirtió en varios checkpoints conversacionales separados para cambios muy pequeños.

Ejemplo: el núcleo Compare terminó siendo aproximadamente un servicio pequeño y una suite focal, pero se detuvo externamente en cada RED/GREEN/CI.

**Diagnóstico:** buena disciplina técnica, mala granularidad de interacción.

**Corrección:** desde esta auditoría, RED/GREEN/CI/auditoría son subpasos internos de un task significativo.

### Hallazgo B — documentación operativa atrasada

Antes de este checkpoint:
- `.planning/STATE.md`;
- `.planning/ROADMAP.md`;
- `.planning/REQUIREMENTS.md`

seguían describiendo F12.2 como último estado, aunque F12.3 ya estaba cerrado y F12.4 Compare ya tenía core terminado.

**Impacto:** nuevos chats/hilos debían reconstruir el estado por GitHub + handoffs, dando impresión de retroceso.

**Corrección:** los tres archivos se sincronizan en este mismo checkpoint y vuelven a ser fuente de verdad compacta.

### Hallazgo C — `main` no representa el producto actual

`main` permanece en:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

La línea activa auditada está aproximadamente **1155 commits por delante**.

**Impacto:** alto ruido operativo y riesgo de integración; mirar solo `main` hace parecer que el producto casi no existe.

**Corrección recomendada:** antes de release, abrir un gate explícito de consolidación/integración. No hacer merge/rebase destructivo automáticamente.

### Hallazgo D — ramas/PRs apilados

El historial de F12 tiene múltiples ramas intermedias por task/checkpoint.

**Impacto:** más ruido mental y más puntos de reconstrucción.

**Corrección:** mantener una rama de slice durante el desarrollo y cerrar tareas significativas dentro de ella; crear ramas auxiliares solo cuando exista necesidad real de aislamiento.

## 5. Auditoría KISS / Ley del Ruido

### Resultado: PASS con deuda operativa

No se encontró necesidad de rediseñar la arquitectura central.

Se conserva:
- PDFium como owner principal de inspección/render;
- PDFsharp/qpdf/Tesseract en roles estrechos ya aprobados;
- WPF con partials focales;
- Save As/transacciones donde se modifica;
- local/offline;
- sin backend/account/cloud;
- sin segundo motor PDF;
- sin DI enterprise preventiva;
- sin generic object graph.

El ruido encontrado está en:
- documentación de estado atrasada;
- exceso de micro-checkpoints visibles;
- ramas/PRs apilados;
- drift severo frente a `main`.

Eso debe resolverse operativamente, no con nuevas capas de software.

## 6. Riesgos actuales

| Riesgo | Nivel | Acción |
|---|---|---|
| `main` ~1155 commits detrás de línea activa | **ALTO** | gate de integración controlada antes de release |
| Manual/private/physical QA pendiente | **MEDIO-ALTO** | matriz priorizada de QA por riesgo |
| Branch/PR sprawl | **MEDIO** | consolidar cuando el slice profesional cierre |
| Estado documental desactualizado | **CORREGIDO en este audit** | mantener updates por task/slice cerrado |
| Arquitectura de producto | **BAJO** | conservar KISS; no reescribir |
| Dependencias/segundo engine | **BAJO** | continuar fail-closed ante nuevas deps |

## 7. Nuevo modelo de trabajo

A partir de este checkpoint:

> **1 `continua` = 1 task significativo cerrado**, no 1 microacción TDD.

Un task incluye internamente:
1. RED;
2. confirmar fallo correcto;
3. GREEN mínimo;
4. focused verification;
5. exact-head full CI;
6. KISS/Ley del Ruido;
7. checkpoint de cierre + porcentaje.

Se detiene antes solo si aparece:
- decisión funcional del usuario;
- fork de arquitectura;
- dependencia/engine nuevo;
- capability NO-GO;
- acción Git irreversible;
- ampliación material de alcance.

Esto mantiene TDD sin convertir la conversación en ping-pong.

## 8. Plan de desarrollo desde ahora

### Bloque A — terminar Compare

**Task 3.1:** presentation contract + minimal Compare owner.  
**Task 3.2:** menu/select B/right-panel flow.  
**Task 3.3:** lifecycle/guards/errors.  
**Task 3.4:** closure exact-head.

Estos tasks se ejecutan como unidades significativas completas, no con un mensaje por RED y otro por GREEN.

### Bloque B — Batch

Primero capability/scope gate. No asumir operaciones. Reusar funciones existentes.

### Bloque C — Code Generator

Definir primero qué tipos de códigos necesita realmente el producto. Evitar implementar catálogo innecesario.

### Bloque D — Audited Conversions

Una conversión por capacidad demostrada; no prometer fidelidad universal.

### Bloque E — release stabilization

- consolidación Git aprobada;
- CI exact-head completo;
- QA manual/private/physical priorizado;
- interoperabilidad;
- installer/license/provenance;
- release candidate.

## 9. Porcentajes y cómo leerlos

### ~96% funcional automatizado

El producto ya tiene cerradas F0–F11, redaction, PFX signing, forms y Compare core. Falta una parte relativamente pequeña de la baseline profesional.

### ~86% release-ready

Baja por deuda que no se resuelve escribiendo más features:
- integración;
- real/manual/private QA;
- hardware/interoperability;
- branch/release hygiene.

### ~91% global

Promedio ponderado útil para responder “¿qué tan terminado está el sistema?” sin confundir feature completion con readiness real.

## 10. Decisiones de esta auditoría

- **NO** reescribir arquitectura.
- **NO** añadir capa de gestión de modos.
- **NO** introducir segundo PDF engine.
- **NO** merge/rebase/cleanup destructivo de `main` sin aprobación.
- **SÍ** sincronizar estado/roadmap/requirements.
- **SÍ** ejecutar tasks más grandes y útiles por `continua`.
- **SÍ** actualizar porcentaje al cierre de cada task significativo.
- **SÍ** tratar integración y QA real como trabajo explícito de release, no como notas sueltas.

## 11. Próximo task de producto

**F12.4 Compare UI — Task 3.1: presentation contract + minimal Compare owner.**

Ese task debe cerrarse en una sola continuación con RED + GREEN + CI + auditoría interna, salvo que aparezca un bloqueo real.

No se ha realizado merge a `main` durante esta auditoría.
