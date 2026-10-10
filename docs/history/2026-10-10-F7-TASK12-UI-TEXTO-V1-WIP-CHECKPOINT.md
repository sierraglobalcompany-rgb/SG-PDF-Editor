# SG PDF Editor — F7 Task 12 UI Texto V1 — WIP Checkpoint

Fecha: 2026-10-10

## Estado

**PAUSA SOLICITADA POR EL USUARIO.**

Task 12 está implementada en código hasta el primer GREEN candidato, pero **NO se declara cerrada** porque el CI exacto del head funcional seguía ejecutando la fase de tests al momento de crear este checkpoint.

No iniciar Task 13 hasta resolver/verificar este gate.

## Rama y SHAs

- Repo: `sierraglobalcompany-rgb/SG-PDF-Editor`
- Rama: `feat/f7-text-v1`
- Inicio exacto de Task 12: `d472557b0834911abf863f538bdea6e876834ff6`
- RED: `8a6cd285cc5d8e78961395bf53f6679d41dd73ae`
- UI partial: `82d859d0648de5447a0718bcdd95e2c1682a3d35`
- Integración shell EDITAR: `217ee7732d576885ab48816da6a1809bec3c7ca8`
- Head funcional antes del checkpoint: `01838665d1ba18df0218540f3ae7f924d2cda7cf`
- `main` verificado sin cambios: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## Alcance literal ejecutado

F7 Task 12 — **UI Texto V1**:

- enumerar objetos TEXT de la página activa al entrar a EDITAR;
- mantener `TextEditWorkspace` junto al workspace de imágenes;
- selección mixta imagen/texto usando el `EditObjectHitTester` ya construido;
- una sola selección autoritativa;
- panel derecho conservador para texto con:
  - contenido;
  - fuente actual informativa;
  - estrategia informativa;
  - tamaño;
  - color FILL;
  - botón Aplicar;
  - error/estado de validación;
- modo read-only para render modes no soportados;
- Apply delega validación y estrategia a `TextEditWorkspace` / `TextEditPolicy` existentes;
- `Ctrl+Z` dentro de un `TextBox` sigue perteneciendo al editor de texto y no roba undo de imagen;
- dirty compartido: imagen **o** texto activa el mismo guard existente;
- Save As sigue usando la ruta combinada ya implementada y preserva los mismos caminos de cancel/block/warning decline.

## RED demostrado

Commit RED:

`8a6cd285cc5d8e78961395bf53f6679d41dd73ae`

CI Windows:

`38088636806`

Resultado:

- Build: **PASS**, 0 warnings / 0 errors.
- Tests: **760 total**.
- **751 PASS**.
- **9 FAIL**.
- **0 SKIPPED**.

Los 9 fallos fueron conductuales y esperados: la UI anterior todavía dejaba `_textEditWorkspace = null` y no existía el panel Texto V1. No hubo fallo de compilación ni fallo accidental del harness.

## GREEN candidato implementado

Archivos modificados desde el inicio exacto de Task 12 hasta el head funcional `01838665...`:

1. `src/SGPdf.App/MainWindow.Edit.Text.cs` — nuevo partial de integración Texto V1.
2. `src/SGPdf.App/MainWindow.Edit.cs` — integración mínima con el shell EDITAR existente.
3. `src/SGPdf.App/MainWindow.Edit.Hardening.cs` — dirty compartido dentro del guard existente.
4. `tests/SGPdf.App.Tests/MainWindowEditTextTests.cs` — contrato UI/guard/Save As de Task 12.

Comparación `d472557...` → `01838665...`:

- 4 commits ahead;
- 0 behind;
- sin cambios en `main`.

### Decisiones KISS conservadas

- No se creó otro `TextEditPolicy`.
- No se duplicó hit-testing; UI usa `EditObjectHitTester`.
- No se creó otro writer ni validator.
- No se creó otra cadena de Save As.
- No se creó otro prompt de discard.
- No se añadió framework de UI/DI/commands.
- No se abrió Task 13.

## CI GREEN pendiente al pausar

Head funcional:

`01838665d1ba18df0218540f3ae7f924d2cda7cf`

CI Windows:

`38088880307`

Estado observado justo antes de este checkpoint:

- checkout/setup/hygiene/Labelize/restore: **PASS**;
- build: **PASS**;
- tests: **IN PROGRESS**;
- conclusión final: **todavía desconocida**.

Por tanto, **NO afirmar todavía 760/760 ni cierre de Task 12**.

## Exact NEXT al reanudar

1. Verificar el head vivo de `feat/f7-text-v1` contra este checkpoint.
2. Verificar el CI exact-head disparado por este checkpoint y/o el run funcional `38088880307`.
3. Si falla:
   - leer logs exactos;
   - corregir solo Task 12;
   - ejecutar GREEN completo otra vez.
4. Si pasa:
   - confirmar build 0 warnings / 0 errors;
   - confirmar conteo exacto de tests;
   - hacer auditoría de scope `d472557...` → head final;
   - crear checkpoint de **Task 12 CLOSED / AUTOMATED PASS**.
5. **Parar antes de Task 13** salvo nueva instrucción explícita del usuario.

## Explícitamente NO hecho

- No se cerró Task 12.
- No se inició Task 13 hardening.
- No se inició Task 14 closure/docs/PR.
- No se abrió F8.
- No se creó PR de F7.
- No se hizo merge.
- No se tocó `main`.
- No se afirmó QA manual Windows.
