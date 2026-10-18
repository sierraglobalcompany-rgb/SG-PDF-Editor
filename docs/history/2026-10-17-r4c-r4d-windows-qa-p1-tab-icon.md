# R4-C/R4-D — Windows QA detenido por P1: icono de pestaña ausente

Fecha: 2026-10-17, America/Bogota. Rama `feat/f13-task9-t9-1-document-shell-realignment-20261017`.
App compilada/probada: `26db4b18a9cfe144be88775ac2f170e5939a53a0`.
Preflight: fast-forward seguro desde `0427cff`; único untracked explicado: ZIP V4 local. CI de entrada [39080493385](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39080493385) SUCCESS. Sin código/test/estilo de producto cambiado en esta ejecución.

## Build / smoke / entorno

Restore locked PASS; build Release 0 warnings / 0 errors; smoke proceso sobrevive 30s PASS. No se repitió la suite: ejecución solo QA; CI de entrada disponible. Windows 11 Home Single Language x64, build 26300, escala nativa consultada 125%, tema de apps/sistema Light (1/1). Bounds reportados por WinForms sin awareness: 1536x960 lógicos; no interpretarlos como resolución física. Medida física separada en `display-physical.json`.

## Defecto y STOP

**QA-V4-001 — P1 / T06 FAIL:** icono de documento PDF aprobado ausente en la pestaña, espacio vacío antes de `source.pdf`. Visible en V01 y distinto de la referencia V4 Light cerrada (icono antes de cada título). Título, cierre y borde de selección sí visibles. Ningún nombre de clase se observó pintado debajo de las tabs; el árbol UIA sí nombra el objeto, lo cual no se promociona a fuga visual.

Reproducción real: iniciar EXE Release del SHA indicado → Inicio/Recientes → abrir el primer `source.pdf` sintético A (ruta en evidencia) → esperar estado `source.pdf — página 1 de 2` → observar el encabezado activo. Captura a las 18:58:39 -05:00. El archivo es fixture sintético A con dos páginas y outlines, no documento privado. Se reutilizó la entrada Recientes preparada durante el gate previo, sin editar producto ni el manifest en esta ejecución.

**Owner:** `src/SGPdf.App/MainWindow.Documents.cs:210–224`, `BuildDocumentTabHeaderTemplate()`, crea Image 18x18 y BitmapImage con URI relativa de componente, insertado en la plantilla. El PNG existe, es no vacío, y `SGPdf.App.csproj:15` lo incluye como Resource. SHA-256 del asset: `79221bc742b7d514a112222abf7936ad36713bf9861b7863c9ecc63dd541c46a`.

**Root cause:** acotada a presentación/resolución de la ImageSource del encabezado; causa precisa de por qué no se materializa **NOT_CONFIRMED**. Hipótesis: resolución de la URI relativa/BaseUri dentro de FrameworkElementFactory. No atribuirlo al parser PDF ni declarar probada la hipótesis por inspección estática.

**Propuesta mínima, NO implementada:** comprobar primero ImageFailed/BitmapSource/URI resuelta en la plantilla real; reproducir focalmente la carga del bitmap, y solo si confirma la hipótesis resolver el mismo asset mediante URI pack absoluta en este owner. Conservar un único template/owner, el mismo PNG y close guard. No reemplazar icono, no wrapper, no margen compensatorio. Solicitar autorización separada para fix; esta orden solo permite QA/diagnóstico.

Se detuvo la interacción de QA al identificar P1 según el prompt. No se intentaron Dark, navegación abierta, segundo documento ni corrección.

## Matriz ejecutada (mismo SHA/entorno)

| Caso | Estado | Evidencia / observación |
|---|---|---|
| V01 Light cerrado | FAIL | Captura real generada; comparación con V4 falla por QA-V4-001 |
| V02 Light abierto | NOT_RUN | STOP P1 |
| V03 Dark cerrado | NOT_RUN | STOP P1 |
| V04 Dark abierto | NOT_RUN | STOP P1 |
| W01 | PASS | A abierto, drawer cerrado, página 1/2, captura V01 |
| W02 | NOT_RUN | STOP P1 |
| W03 | NOT_RUN | STOP P1 |
| W04 | NOT_RUN | STOP P1 |
| W05 | NOT_RUN | STOP P1 |
| W06 | NOT_RUN | STOP P1 |
| W07 | NOT_RUN | STOP P1 |
| W08 | NOT_RUN | STOP P1 |
| W09 | NOT_RUN | STOP P1 |
| W10 | NOT_RUN | STOP P1 |
| W11 | PARTIAL | Home sin rail/toggle observado; ZPL no ejecutado |
| W12 | PARTIAL | Light de Home/lector observado; Dark/empty/error no ejecutados |
| W13 | PARTIAL | 125% real confirmado, lector observado; mínimo/teclado/overflow no cubiertos |
| W14 | NOT_RUN | STOP P1 |
| W15 | NOT_RUN | STOP P1 |
| T01 | PARTIAL | Una pestaña con título corto/activa/cierre visible; falta A+B/elipsis/alternar |
| T02 | NOT_RUN | STOP P1 |
| T03 | NOT_RUN | STOP P1 |
| T04 | NOT_RUN | STOP P1 |
| T05 | PARTIAL | UIA lista 11 tools y seis disabled; acciones/scroll no probados |
| T06 | FAIL | P1 icono de pestaña ausente; tooltip/teclado y resto de estados no cubiertos |
| T07 | PARTIAL | Fila Light observada; no Dark/mínimo ni comparación completa; sin desarrollar bloque 3 |
| T08 | PARTIAL | Home sin rail observado; ZPL no ejecutado |

P0: 0 observados, cobertura parcial. P1: 1 reproducido. P2: no clasificados; no equivalen a ausencia demostrada.

## Evidencias / privacidad

Carpeta local: `C:\Users\felip\AppData\Local\Temp\SGPDF_V4_WINDOWS_QA_20261017_185707`.
- `R4D_V01_LIGHT_CLOSED_REAL.png`, SHA-256 `d67c680c2051aa475b25fe9e30ae73aae3c15d22b7d1e2b1e67502d77251b5bd`.
- `environment.json`, `display-physical.json`, `build-smoke.log`, `matrix-source.md`, `matrix-results.md`, `SHA256SUMS.txt`.
- La captura tiene leves bordes del escritorio/ventana de fondo. **No se versiona** por privacidad; tampoco se suben fixtures ni logs. Se conserva original local sin recortes ni mockup como evidencia real.
- V02–V04 no existen; no fueron sustituidas por mockups.

## Resultado y continuidad

**QA_PENDING / STOP_P1. No se puede solicitar cierre.** Falta causa confirmada/fix autorizado, nueva QA V01–V04/W01–W15/T01–T08 y P0=P1=0. El CI del nuevo commit documental se verifica tras push y no sustituye QA visual. No bloque 3, R5/T9.2, merge, cambios de runtime ni nuevos frameworks.
