# SG PDF — Codex Windows — R4-C/R4-D: V01 posfix + comparación visual integral V4

**Fecha:** 2026-10-18  
**Objetivo:** producir evidencia REAL verificable, no programar ni simular éxito.  
**Repo:** `sierraglobalcompany-rgb/SG-PDF-Editor`  
**Rama:** `feat/f13-task9-t9-1-document-shell-realignment-20261017`  
**Referencia de producto probada automáticamente:** fix de icono `3f5d0c9b43c925ab5b740d69b7d8c5b17fb47077`.  
**Referencia de auditoría y estado previo:** `a8ef2cf967c8076e75ea7a5322504df246c6b67c`; CI [39083267232](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39083267232) SUCCESS (1289 PASS, 0 FAIL, 1 SKIP, build 0/0, smoke 30 s). **Consultar el HEAD remoto al iniciar; no asumir estas referencias como HEAD futuro.**

## Reglas / alcance obligatorio

- **QA primero:** reproducir V01 post-fix; si está bien, obtener V02/V03/V04 y W01–W15 + T01–T08; no cambiar runtime, código, estilos, tests ni activos. Excepciones documentales solo para registrar evidencia.
- **Mantener STOP_P1 / QA_PENDING** hasta evidencia de Windows suficiente. El éxito de CI no cierra el P1 visual.
- **No empezar:** bloque 3 (command bar), R5, T9.2, F14–F18 o merge. No incorporar IA, nube, Office automatizado, plugin framework ni nuevas herramientas/conversiones.
- **Preservar cambios locales:** `git fetch --all --prune`, `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git rev-parse @{u}` (PowerShell: entre comillas si es necesario). No hacer reset duro/rebase ni borrar el ZIP V4 local sin consentimiento. Fast-forward seguro si árbol tracked limpio.
- La captura de referencia V01 previa en `docs/history/2026-10-17-r4c-r4d-windows-qa-p1-tab-icon.md` es **anterior al arreglo**. Nunca sobrescribirla.

## Leer primero en este orden

1. `.planning/STATE.md`
2. `docs/history/2026-10-18-qa-v4-001-tab-icon-red-green-windows-retest-pending.md`
3. `docs/qa/2026-10-17-r4c-r4d-v4-windows-matrix.md`
4. `docs/qa/2026-10-18-auditoria-31-herramientas-sin-ia-y-brecha-visual-v4.md`
5. `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md` y 4 PNG en `docs/design/references/sgpdf-v4/`
6. `AGENTS.md` y `docs/design/SGPDF_DESIGN_LAW.md`.

## PASO 1 — Gate V01 independiente (OBLIGATORIO)

1. Compilar `SGPdf.slnx` en Release, ejecutar el EXE **real**; registrar SHA de código probado, ubicación del EXE, fecha, tema Light y DPI 125% desde Windows (no inferir DPI de pixels lógicos de WinForms).
2. Abrir el **mismo PDF sintético seguro** usado en la primera captura V01, preferiblemente vía Recientes si el diálogo da problemas, sin alterar el producto. El archivo del ejemplo contiene 2 páginas y outlines. No confundir página en blanco de fixture con fallo de render.
3. Inspeccionar encabezado activo `source.pdf`. ¿Aparece realmente el PNG oficial (icono PDF SG con lápiz) antes de `source.pdf`, sin pixel transparente/hueco? ¿Elipsis, close y resaltado activo correctos? No basta con `Image.Source != null` en test WPF.
4. Tomar **nueva captura** `R4D_V01_LIGHT_CLOSED_POSTFIX_REAL.png`, sin teclado virtual ni menús superpuestos; escribir SHA-256 de imagen. Compararla con `SG_PDF_MOCKUP_V4_CLARO_CERRADO_2026-10-17.png` a DPI/tamaño normalizado; sin exigir igualdad pixel a pixel del documento ilustrativo.
5. **Si el icono sigue ausente: STOP_P1 inmediatamente.** Guardar `QA-V4-001`, nuevos pasos y captura, propietario y diagnósticos mínimos. **No aplicar parches**; regresar evidencia al chat y actualizar checkpoint de fallo.
6. **Si aparece:** marcar el defecto original **V01/icon = PASS posfix**, pero **NO** cerrar todavía R4-D. Proseguir al Paso 2.

## PASO 2 — Barrido visual estructurado / interacción sin cambios productivos

Registrar cuatro capturas de WPF real y SHA-256, con tema, DPI y SHA de exe. No sustituirlas por mockups:

| ID | Estado requerido | Archivo |
|---|---|---|
| V01 | Light + navegación cerrada | `R4D_V01_LIGHT_CLOSED_POSTFIX_REAL.png` |
| V02 | Light + navegación abierta | `R4D_V02_LIGHT_OPEN_REAL.png` |
| V03 | Dark + navegación cerrada | `R4D_V03_DARK_CLOSED_REAL.png` |
| V04 | Dark + navegación abierta | `R4D_V04_DARK_OPEN_REAL.png` |

Además, completar todos W01–W15 y T01–T08 en la matriz canónica, con dos PDFs sintéticos (A con marcadores, B sin), ZPL no sensible, alternancia, cerrar con guard de cambios pendientes, menú/toggle/scroll, keyboard/foco, Light/Dark, escala 125% y ancho mínimo; si una prueba no fue ejecutada, **NOT_RUN**, nunca PASS. Si aparece un P0 o P1 funcional, detener interacciones que puedan causar daño. Registrar fallos de fidelidad visual de forma consolidada y no modificar código en este gate.

Contrastar explícitamente los siguientes **dueños y criterios** sin implementar nada:

| Sección | Aprobado V4 | Captura real previa / pregunta de QA | Dueño probable |
|---|---|---|---|
| Marca/tabs | Logo SG+PDF original, tabs compactos con icono documental, close y + integrados | Prev.: solo texto SG PDF, tab muy ancha, + lejos; ¿posfix? | `MainWindow.Documents.cs` |
| Rail | Encabezado `DOCUMENTO`; selected tipo pastilla azul suave + acento; icono, etiqueta, 11 modos ordenados | Prev.: franja lateral estrecha, sin título, scroll gris; ¿DPI normalizado? | `MainWindow.Shell.cs`, `Themes/Modern.xaml` |
| Navegación | Toggle cuadrado visible, panel Páginas/Marcadores usable, ancho recuperado al cerrar | Prev.: signo `›` pequeño sin contorno; verificar UIA/foco | `MainWindow.ReaderThumbnails.cs` |
| Lector | Gris claro diferenciado + márgenes, página blanca sombreada | Prev.: fondo cercano al blanco, página poco distinguible | Reader + tema |
| Toolbar | Zoom/paginación agrupados y consistentes, fit-width claro | Prev.: disperso; **solo registrar deuda del bloque 3**, no implementarlo | `MainWindow.Documents.cs` / XAML |
| Footer | Estado discreto y legible | Prev.: banda gruesa | XAML/tema |

Cuidar el escalado: el mockup es una imagen conceptual a tamaño fijo y la captura Windows estaba a 125%; comparar **DIP**, jerarquía, contraste, alineación y affordances, no imponer el mismo ancho en píxeles. La página ilustrativa del mockup NO es un objetivo de renderización de los PDF reales.

## PASO 3 — Evidencias y checkpoint (sin código)

Guardar un directorio local `SGPDF_V4_POSTFIX_QA_<fecha-hora>` con 4 screenshots (si se logró), `environment.json`, `results.md` por W/T/V, `SHA256SUMS.txt`, `diffs-v4.md` (P0/P1/P2, criterio V4, evidencia y owner), y `build-smoke.log`. Cuidar privacidad: no subir pantallas privadas, certificados ni documentos al repo.

Actualizar **un único checkpoint** en `docs/history/`, y `.planning/STATE.md` **solo si hay resultados nuevos verificables**. Push normal en rama autorizada; comprobar CI exact-head del commit documental. Reportar en orden: `BRANCH/HEAD → PREFLIGHT → BUILD/SMOKE → V01 ICON PASS/FAIL → V02–V04 → W01–W15 → T01–T08 → P0/P1/P2 → HASHES → CHECKPOINT/CI URL → NEXT / STOP`.

**Criterio de continuidad:** si V01 pasa pero persisten diferencias visuales del V4, presentar lista consolidada de modificaciones mínimas para que ChatGPT arregle **un dueño a la vez** en el mismo R4-D. No pedir a Codex que implemente cambios en este gate. Ningún P1 abierto puede convertirse en GREEN documental.