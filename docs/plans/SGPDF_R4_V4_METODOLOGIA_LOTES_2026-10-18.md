# SG PDF Editor — Auditoría de 10 pasadas y plan optimizado para cerrar V4

**Fecha:** 2026-10-18  
**Proyecto:** SG PDF Editor · F13 / Task 9 / T9.1 · R4-C y R4-D  
**Rama de trabajo:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
**Último HEAD de código/checkpoint contrastado antes de esta propuesta:** `5084a7a4d05f967e6d2a071f2fc2cd77aa11dfab`  
**Situación real:** W09 corregido en código (CI `39153249583` PASS), **QA Windows W09 aún pendiente**. Dos P1 de tema (Dark y cambio en caliente) abiertos. Fidelidad V4 incompleta. Bloque 3/R5/T9.2/F14–F18 y merge **NO autorizados**. Antes de ejecutar la propuesta, comprobar siempre el HEAD remoto vivo.

## Dictamen ejecutivo

**La crítica de exceso de fragmentación está fundamentada.** El enfoque RED→GREEN de P1 reales ha sido útil (icono PDF: `Image.Source == null`; W09 entre PDF homónimos: `Expected Collapsed / Actual Visible`), pero el flujo se convirtió en una secuencia desproporcionada de commits y CI por microdetalle, mientras el conjunto visual todavía no coincide con V4. La ley antiparches **no exige** una suite completa, un checkpoint histórico y una nueva autorización **por cada cambio de color, margen o texto**. La skill SG manda tests causales para comportamiento y contratos focales *solo cuando previenen una regresión real*.

**Decisión propuesta:** sustituir «microbloque = test completo + nuevo CI + checkpoint» por **«lote coherente por superficie/causa = trabajo pequeño agrupado + pruebas focales durante el lote + 1 CI y 1 QA visual consolidado al final»**. Aislar únicamente cambios peligrosos (P0, datos, guard dirty, seguridad, corrupción), o root causes que requieren RED→GREEN causal. No sacrificar pruebas, sino reducir ejecuciones redundantes.

## Evidencia primaria utilizada

- Especificación de [mockups V4 aprobados](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/blob/5084a7a4d05f967e6d2a071f2fc2cd77aa11dfab/docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md) y 4 PNG oficiales en `docs/design/references/sgpdf-v4/`.
- ZIP local `SGPDF_V4_POSTFIX_QA_20261018_085446.zip`, cuatro capturas reales de Windows, casos W01–W15/T01–T08, captura de tema en vivo, reporte del agente.
- [Checkpoint real de Codex](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/blob/68a27c44a64b476b32bc47368e8262a7e14230cd/docs/history/2026-10-18-r4c-r4d-v4-postfix-windows-results.md).
- [W09 RED→GREEN](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/blob/5084a7a4d05f967e6d2a071f2fc2cd77aa11dfab/docs/history/2026-10-18-r4d-w09-homonymous-pdfs-red-green-checkpoint.md) y [CI del checkpoint](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39153601757): 1.290 PASS, 0 FAIL, 1 SKIP; build 0/0, smoke PASS. **No equivale a W09 PASS en pantalla real.**
- `docs/design/SGPDF_DESIGN_LAW.md`, `.agents/skills/sgpdf-design/SKILL.md`, `AGENTS.md`, `.planning/{STATE,ROADMAP,REQUIREMENTS}.md`, `docs/MASTER_CONTEXT.md`.
- Prompts anteriores: `SGPDF_CODEX_QA_W09_HOMONIMOS_POSFIX_2026-10-18.md`, `SG_PDF_CODEX_V4_RETEST_Y_COMPARACION_INTEGRAL_2026-10-18.md` y `SG_PDF_CONTINUIDAD_VISUAL_APROBADA_V4_Y_PROMPT_CODEX_2026-10-17.md`.

## Auditoría de diez pasadas

| # | Eje | Hallazgo contrastado | Decisión de mejora |
|---|---|---|---|
| 1 | Verdad viva de GitHub | HEAD/CI fiable, pero `STATE.md` aún contiene cronología y frases obsoletas (icono «pendiente» junto al PASS posterior, W01–W15 «sin ejecutar» junto a resultados reales). | `STATE` será un **índice operativo**, no bitácora; historia en checkpoints existentes, con un solo link a «última evidencia». |
| 2 | KISS / arquitectura | Un `MainWindow` parcial, un estado `PdfDocumentWorkspace`, un rail y un drawer son suficientes; W09 se arregló reutilizando `SyncShellWorkspaceFromActiveDocument`. | Mantener el owner actual; nada de `ThemeManager` genérico, nuevo router, viewer, DI, arquitectura adicional ni reescritura. |
| 3 | Antirruido | Varios Markdown reiteran estado, SHA, mismos gates y el mismo plan; se reescriben 3–4 docs por tareas mínimas. | Un único plan canónico de V4; **un checkpoint al final de cada lote**, no por parche cromático. Puntero corto en `STATE`, históricos inmutables. |
| 4 | Antiparches | Icono y W09 tuvieron test causal válido; primer intento de test en ambos casos sufrió problemas propios del harness (template sellado / CS0411). | Para fallos funcionales: reproducir → owner → test causal → arreglo. Para composición/espaciado: checklist visual + test focal si aporta valor; evitar pruebas que solo buscan strings. |
| 5 | Ley de diseño | Mockup V4 aprobado no coincide aún: logo SG, pestañas, ancho/selección de rail, toggle, cabecera de drawer, fondo del lector y footer. | Pasar de «errores aislados» a **matriz real vs V4 por región**, con criterios de estructura, interacción y contraste; no pixel-perfect con PDF ilustrativo. |
| 6 | Contraste Dark | V03 título ilegible; V04 numeración de miniaturas clara sobre papel blanco; Organizar usa superficie clara fija. | Tratar los 3 sitios como **un lote de accesibilidad/tema** con owners existentes y 1 CI conjunto tras diagnóstico. No cambiar fondo de papel real del PDF. |
| 7 | Tema en caliente | Light↔Dark deja mezclas de superficies y foreground. Los aliases `Modern.xaml` usan combinación de `DynamicResource` y `StaticResource`; no se demostró aún causalidad exacta. | Reproducir cambio en **misma instancia de WPF**, inspeccionar recursos retenidos y invalidación del tema. Reparar causa central, no override de cada pantalla; probar ida y vuelta. |
| 8 | Cobertura técnica | La full suite y smoke han demostrado estabilidad, pero no fidelidad del mockup; 1.290 PASS no cerró P1 visual. | Pirámide por lote: compilación/focal durante trabajo, full suite+smoke+CI exact-head **al finalizar lote**, Windows 4 vistas + flujos relevantes. |
| 9 | Codex / Antigravity | Codex Windows aporta render/capturas y pruebas reales; repetir toda la suite y volcar 40 archivos por cambio menor desperdicia esfuerzo. Antigravity puede revisar GitHub/código, pero su navegador cloud no equivale a WPF local. | Un **prompt portable con dos modos**: `QA_ONLY` para Codex y `FIX_AND_VALIDATE` solo con autorización, misma salida breve por diferencias, sin reescribir contexto completo. |
| 10 | Riesgo, secuencia y cierre | Congelar «bloque 3» impide aún igualar parte de la barra superior V4. Si se exige fidelidad completa pero no se permite tocar toolbar, la aceptación es imposible. | Separar «P1 + W09 + tema» de «alineación visual del shell»; registrar **deuda de barra de comandos congelada** hasta que el usuario autorice explícitamente incorporarla dentro de T9.1. No declarar «V4 completo» sin esa decisión. |

**Evaluación:** cumplimiento arquitectónico KISS **razonable**, antiparches **correcto en bugs causales pero aplicado excesivamente al UI cosmético**, antirruido **insuficiente en flujo y documentación**, diseño **aún no aprobado en la aplicación real**. Son juicios de auditoría, no métricas objetivas de porcentaje.

## Comparación visible con el V4 (capturas del mismo proyecto)

| Región | Referencia aprobada | Captura WPF real (125%) | Acción |
|---|---|---|---|
| Marca/cabecera | Logotipo oficial e identidad compacta | `SG PDF` en texto, sin icono principal en la barra | Alinear en lote visual, usando asset original |
| Tabs | Compactas, borde activo azul, icono PDF, `+` adyacente | Icono PDF posfix **sí**; tab grande, `+` muy alejado | Reducir distancias sin alterar propietario multi-PDF |
| Rail | Estrecho, rótulo `DOCUMENTO`, selección tipo pastilla, ritmo estable | Rail bastante ancho, sin rótulo, selection solo borde y scroll prominente | Reestilizar recurso existente y layout, no rehacer catálogo |
| Drawer | Toggle visible, navegación claramente segmentada | Botón `›` muy pequeño, sin affordance equivalente | Mejorar presentación en su mismo owner tras validar W09 |
| Lector | Canvas gris tenue contrastando con papel blanco; status discreto | Área casi blanca, papel difícil de separar; pie alto | Resolver tokens de superficie y tamaños, no falsear contenido PDF |
| Dark/Hot | Texto y superficies legibles, cambio de tema consistente | V03/V04 P1, transición en vivo con texto prácticamente invisible | **Bloque P1 primero**: accesibilidad / recursos WPF |
| Barra superior | Zoom/fit/imprimir compactos y agrupados | Comandos y agrupación difieren | Deuda «bloque 3» con autorización separada |

Nota: la página en blanco del PDF sintético es **contenido de prueba, no fallo del render**. Dimensiones: mockup conceptual ~1600×950 px; QA real ~1500×950 px al 125%. Comparación estructural, no píxel contra píxel.

## Plan de desarrollo optimizado — menos vueltas, mejor QA

### Lote A — Estabilidad visual P1 (una intervención coordinada)

**Incluye:** (A1) tab foreground Dark; (A2) miniaturas: papel blanco + número legible y estados focus/selected; (A3) Organizar: superficie y controles según tema; (A4) fallo Light↔Dark en caliente desde origen compartido (diagnóstico obligatorio). **W09 ya tiene fix técnico:** agregar retest manual al cierre del mismo lote, NO abrir otro ciclo documental autónomo.

**Dueños:** `Themes/Modern.xaml`, `MainWindow.Documents.cs`, `MainWindow.ReaderThumbnails.cs`, `MainWindow.Organize.cs`, `Shell/HomeDashboard.xaml`, según prueba. No suponer que todos requieren edición; arreglar upstream si el tema en vivo es una causa global.

**Ejecución:** 1 preflight → diagnóstico de recursos y reproducción P1 → 1 diff coordinado y tests focales pertinentes → build/focal → full suite + smoke una vez → commit(s) lógicos y **un CI exact-head de código al cerrar** → Codex QA real en una pasada Dark, Light y conmutación en caliente + W09 → checkpoint único. Separar un cambio en dos commits cuando son causas diferentes y el historial gana claridad; no exigir un CI por cada commit intermedio.

**Salida:** P0=0, P1=0 de contraste/tema y W09 en QA real; si P1 persiste, mantener QA_PENDING con lista consolidada y seguir solo en la región insegura. El criterio causal vale para fallos técnicos; no incorporar framework de temas.

### Lote B — Fidelidad estructural del shell V4

**Solo después del diagnóstico/gate de A.** Ajustar en **una revisión visual coherente**: (B1) marca y tabs; (B2) rail y estado activo; (B3) toggle/drawer (sin tocar navegación); (B4) canvas y footer. Usar 4 PNG V4 como autoridad, **no como recursos de producción**. Misma iconografía aprobada. No abrir herramientas funcionales ni IA.

**Pruebas:** contrato WPF por comportamiento afectado, si aplica; 4 capturas Light/Dark con drawer abierto/cerrado al 125 %, una ronda consolidada, medidas de diseño razonables (espaciado, contraste, foco, 960×640). Full/CI una vez al cerrar el lote, no por control.

**Exclusión explícita:** cambios de barra de comandos/zoom/fit/imprimir pertenecen al «bloque 3» congelado. Debe obtenerse **aprobación específica** para alinear esa superficie antes de declarar V4 100 % terminado; no esconder esa deuda tras un GREEN parcial.

### Lote C — Cierre de R4-C/R4-D

**Cobertura faltante:** W05 Enter en destino inicial, W08 alternancia bajo carga lenta, W13 mínimo 960×640, W14 tab-order completo, W15 caso negativo si hay manera segura, T04 guard en retorno, T07 barra; actualizar W09 con resultado real. Si W15 es inviable sin instrumentación insegura: `NOT_RUN/EXCEPTION_PROPOSED`, y solo una aprobación humana explícita puede aceptar la excepción.

Un informe de QA con resultados nuevos y 4 capturas, P0/P1=0, CI exact-head final y checkpoint único. **R4 no es GREEN** hasta esa evidencia. R5, T9.2, F14–F18, merge y herramientas nuevas siguen detenidos.

## Cómo reducir tiempo y tokens sin bajar calidad

- **Preflight una vez** por lote; no recitar SHA histórico en cada prompt. GitHub HEAD es fuente dinámica.
- **Modificar varios archivos del mismo root cause dentro de un lote**, con tests focales durante el desarrollo y una suite completa al cierre.
- **GitHub Actions se ejecutará automáticamente por push**, según configuración; preferir mantener cambios de trabajo locales agrupados y subirlos cuando el lote esté listo, en vez de empujar cada línea y crear múltiples CI. Si ChatGPT edita directo mediante GitHub, trabajar en commits agrupados cuando la herramienta lo permita y no encadenar publicaciones de pruebas incompletas.
- **CI exact-head al cierre del lote**; un commit documental posterior cambia HEAD, por lo que la verificación se cita sobre ese nuevo SHA solo cuando termine. Evitar tocar documentación trivial después del último CI.
- **Una sesión de QA Windows consolidada**, no mandar Codex repetidas veces por variación cromática; hacer screenshot/fichas de incidencia agrupadas.
- **Un único reporte** con cambios, test focal/full, CI, screenshots, P0/P1/P2, siguiente paso. Los logs/ZIP pesados quedan locales, sin subir archivos privados.
- **No imponer un RED por espaciado visual.** Sí pruebas RED causales para navegación, tema vivo, persistencia, dirty-close y seguridad.
- **No crear sistemas de plugins, temas, routers, motores o duplicados** por resolver V4.

## Roles: ChatGPT, Codex y Antigravity

| Responsable | Mejor uso | No hacer |
|---|---|---|
| ChatGPT conectado a GitHub | Auditoría, diseño de solución, cambios de código agrupados, revisión de diff, documentación mínima, validación CI | Declarar que observó Windows si no hubo captura real |
| Codex en Windows | Ejecutar la app, pruebas focales/full cuando corresponde, 4 capturas y escenarios W09/tema, feedback preciso, prueba de DPI/teclado | Rehacer arquitectura, nuevos módulos, publicar capturas privadas; generar 40 logs por control |
| Antigravity | Revisión de rama, implementación focal, código y CI, navegador para GitHub/PR; GUI Windows **solo** si dispone de acceso real al entorno | Dar por probada la interfaz WPF con una vista HTML/cloud |

**Recomendación:** usar ChatGPT como director técnico y autor de cambios de GitHub; Codex como ejecutor de Windows por *lote*. Antigravity es alternativa de agente para revisión/implementación y no una dependencia adicional que instalar obligatoriamente.

## Leyes SG aplicadas

- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.
- **Antirruido:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **Antiparches:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`, **a nivel de cambio causal, no ejecución de CI por cada línea**.
- **Diseño:** `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`; Light/Dark, hot-switch, 125%, teclado, contraste y mockup aprobado.

## Decisión de auditoría / checkpoint

- **CAMBIAR MÉTODO:** de micro-releases en cascada a lotes coherentes con gate final.
- **MANTENER SEGURIDAD:** P0/seguridad/datos tienen parada inmediata; P1 funcional requiere prueba causal y revalidación.
- **CONSOLIDAR DOCUMENTACIÓN:** `STATE.md` operativo, `ROADMAP.md` solo prioridades/fases, 1 checkpoint por lote; históricos como evidencia inmutable.
- **NO CERRAR:** icono PDF V01 PASS; W09 GREEN automatizado / Windows pendiente; 2 P1 de tema abiertos; fidelidad V4 incompleta; no R5/T9.2/F14/merge.
- **REQUERIR AUTORIZACIÓN:** no alinear «bloque 3» congelado sin aprobación explícita si esa superficie es necesaria para cumplimiento total de V4.

**Documento complementario para ejecutar el plan:** `SGPDF_PROMPT_UNICO_CODEX_ANTIGRAVITY_V4_LOTES_2026-10-18.md`. Mantener ambos como referencia del método, no copiarlos a cada checkpoint.