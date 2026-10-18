# SG PDF — Prompt único optimizado para Codex / Antigravity · V4 por lotes

> **Instrucción:** copiar este documento al agente conectado a GitHub o al PC Windows. Es operativo y autoactualizable: **no supone que el último SHA histórico siga siendo HEAD**.

## Misión y restricciones

Eres agente técnico de **SG PDF Editor (WPF/.NET 10 Windows x64, local-first)**. Continúa **F13 / Task 9 / T9.1**, R4-C/R4-D, para cerrar la comparación de la app **real** con los **cuatro mockups V4 aprobados**. Ejecuta la tarea asignada dentro de su alcance. **No agregar IA ni características nuevas**, no introducir frameworks, no reescribir arquitectura, no hacer merge a `main`, no iniciar R5/T9.2/F14–F18 ni el «bloque 3» de command bar sin aprobación del usuario.

Repositorio: `sierraglobalcompany-rgb/SG-PDF-Editor`  
Rama esperada: `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
Rutas canónicas: `.planning/STATE.md`, `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md`, `docs/design/SGPDF_DESIGN_LAW.md`, `.agents/skills/sgpdf-design/SKILL.md`, `docs/qa/2026-10-17-r4c-r4d-v4-windows-matrix.md`, último checkpoint de `docs/history/` indicado desde STATE.

## Modo de operación (elegir UNO)

- **`QA_ONLY`** *(por defecto para Codex Windows)*: obtener evidencias reales, retest W09, tema Light↔Dark, contraste y cuatro capturas V4. NO modificar product code, iconos o estilos. Si aparece un fallo, agrupar incidencias en reporte y entregar al autor de código; documentar sin inflar contextos.
- **`FIX_AND_VALIDATE`** *(solo si ChatGPT/usuario asignó expresamente un lote A/B)*: analizar y corregir las causas dentro del alcance; ejecutar pruebas focales; cerrar con una única suite completa/CI y una sesión de QA real. Si no tienes escritorio Windows, no declarar PASS visual.

**Lote asignado:** `A` (estabilidad Dark/tema vivo + retest W09) o `B` (estructura visible V4 después de A); si no está autorizado, seleccionar `QA_ONLY` para A. No trabajar en ambos lotes ni en funciones nuevas por iniciativa propia.

## Preflight ligero (una sola vez)

1. `git fetch --all --prune`; confirmar rama, `git status --short`, HEAD y upstream. Si HEAD difiere del checkpoint histórico, **leer delta en vez de resetear**. Preservar ZIP/capturas/untracked conocidos; abortar operaciones destructivas si hay cambios no comprendidos.
2. Leer **solo** STATE, la especificación V4, Ley SG, skill WPF y el último checkpoint relevante; no ingerir toda la carpeta `docs/history` ni repetir prompts viejos.
3. Confirmar autoridad de iconos: `Assets/SGPdf.App.ico` (app), `Assets/SGPdf.Document.png/.ico` (PDF), `Assets/HomeTools/*.png` (herramientas) y los tres glyphs aprobados. Referencias V4 son mockups **no assets de producto**.
4. Registrar un baseline breve: SHA, Windows/DPI si corresponde, P1 abiertos y criterio exacto de este lote.

## Leyes (aplicar en ese orden)

- **KISS:** arquitectura simple, owner actual, camino A→B, sin duplicar estado/servicios.
- **Antirruido:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`; quitar duplicación documental y no crear utilidades generales por un caso puntual.
- **Antiparches:** error funcional/seguridad = reproducción, causa, owner, cambio mínimo y test causal. Estética/espaciado = comparación visual y contrato focal únicamente si previene un error real. **No test de presencia de strings por cada color/margen.**
- **Diseño SG:** intención, jerarquía, revelado progresivo, consistencia, feedback, accesibilidad, verificar Light/Dark/125%/teclado y las cuatro referencias V4.

## Trabajo de lote A — prioridad actual

**Estado comprobado al redactar:** V01 icono PASS en app real; W09 GREEN automatizado tras `cc5d58d`, pero aún sin retest Windows. Dark tiene título de tab y miniaturas ilegibles, Organizar con fondo fijo claro; cambio de tema en caliente deja colores mezclados. Tres familias de P1 no deben llamarse cerradas.

En `QA_ONLY`, ejecutar EN UNA SOLA SESIÓN:

1. W09 en dos PDFs sintéticos con idéntico basename: A abierto/Marcadores; B cerrado; alternar A↔B y luego B abierto/Páginas, A cerrado. Cada documento debe conservar drawer/tab propios; guardar resultado y 1–2 capturas **reales** relevantes.
2. Dark tras iniciar app: observar título seleccionado, miniaturas, Organizar, disabled; comprobar texto legible y fronteras perceptibles. Registrar muestras/resultados (sin calibración innecesaria) y capturas.
3. Cambiar Light→Dark→Light **sin reiniciar la aplicación** en Home y con PDF abierto; registrar recursos que queden con fondo o foreground incorrecto, captura antes/después y pasos reproducibles.
4. Reunir V01–V04 en mismo DPI 125%; comprobar rail/tab/drawer/canvas y teclado/foco; no confundir blanco del fixture con error de render. Documentar desviaciones V4 por **REGIÓN**, no un ticket por píxel.
5. Marcar W/T como PASS, FAIL, PARTIAL o NOT_RUN honestamente. No elevar a PASS por un test de CI. No subir imágenes con datos privados sin autorización.

En `FIX_AND_VALIDATE`, además de la reproducción, agrupar los problemas que comparten dueño en un diff coherente; primero investigar recursos compartidos WPF para cambio en caliente (no asumir que `StaticResource` es la causa), luego tab/miniaturas/Organizar si requieren cambios individuales. No agregar `ThemeManager` ni otra capa. Añadir test WPF de cambio de tema/contraste si es estable y causal; no inventar test de estilo innecesario. La prueba W09 ya existe; no duplicarla.

## Trabajo del lote B — solo tras gate A

Alinear en **una revisión del shell**: logo SG aprobado, tabs/`+` compactos, rail estrecho y selección tipo pastilla con encabezado DOCUMENTO, toggle perceptible y drawer organizado, canvas separado visualmente del papel, footer discreto. Reusar `MainWindow.*`, `Modern.xaml` y controles existentes; ninguna segunda UI. Capturar V01–V04 y probar 960×640/DPI 125%. **No cambiar comandos del bloque 3** hasta autorización explícita; registrarlos como `DEFERRED/REQUIRES_APPROVAL`, no fingir conformidad total.

## Estrategia de ejecución eficiente

- Trabajar con **un lote coherente**, varios cambios asociados si hay causa común; evitar commit/push/CI por cada setter.
- Durante implementación: **compilación y test focal** después de cambios relevantes. Para datos, seguridad y fallos funcionales peligrosos, RED→GREEN obligatorio; para estética no fabricar RED.
- Antes de publicar: revisar diff y eliminar código/estilos redundantes, tests cosméticos frágiles y archivos temporales.
- Al final del lote: `dotnet restore SGPdf.slnx --locked-mode` → `dotnet build SGPdf.slnx -c Release --no-restore` → `dotnet test SGPdf.slnx -c Release --no-build` → smoke de 30 s. **Una suite completa al cierre**; repetirla solo si falla o cambia el código después.
- Publicar uno o pocos commits lógicos sobre rama autorizada; verificar CI **exact-head**. Si hay commit documental posterior, su CI también deberá reportarse con honestidad.
- Validar **una sesión Windows integrada** con cuatro vistas y flujos afectados; no rehacer W01–W15 completos por cada retoque: revisar focal durante el trabajo y totalidad al gate R4.
- Un solo checkpoint por lote (en `docs/history/`), actualizar `.planning/STATE.md` como puntero operacional **solo** cuando cambia estado/dirección; no modificar ROADMAP, AGENTS, MASTER_CONTEXT salvo decisión durable.

## STOP/continuidad

**STOP inmediato:** riesgo P0 de datos/seguridad/corrupción, rama incorrecta, pérdida de trabajo, incompatibilidad o decisión fuera de alcance. Un P1 visual de contraste no peligroso **se documenta y se continúa recolectando evidencia** del lote si es seguro; no detener las otras capturas por el primer contraste malo. Ante P1 funcional reproducido, localizar causa y reparar dentro del lote autorizado o devolverlo sin simular PASS.

**Nunca declarar R4-C/R4-D GREEN** hasta P0/P1=0 observados, evidencia Windows suficientemente cubierta, CI exact-head y aceptación real V4; los casos PARTIAL/NOT_RUN y comando de bloque 3 deben explicitarse. Ningún merge/R5/F14 sin consentimiento.

## Formato de salida obligatorio (máximo ~1 página + anexos referenciados)

1. **Baseline**: rama/HEAD, modo, lote, estado árbol.
2. **Cambios**: solo archivos y causas afectadas; si `QA_ONLY`, "ninguno".
3. **Gates**: focal, build, full test, smoke, CI exact-head (URL/SHA), sin inventar.
4. **Matriz real**: W09, Dark, Live theme, V01–V04, keyboard/DPI y P0/P1/P2, PASS/FAIL/PARTIAL/NOT_RUN.
5. **Evidencia**: capturas locales/hashes, localización, privacidad, no subir binarios sensibles.
6. **NEXT**: 1 bloque siguiente autorizado o STOP con causa. Un checkpoint canónico y **sin copiar toda la historia**.

**Regla final:** la fidelidad al V4 aprobado y la experiencia funcional mandan; los tests sirven para demostrarla, no para sustituirla ni para multiplicar trabajo.