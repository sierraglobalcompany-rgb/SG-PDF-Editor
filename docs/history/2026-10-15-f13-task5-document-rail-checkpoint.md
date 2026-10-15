# SG PDF — Checkpoint F13 Task 5 Document Tool Rail

Fecha: 2026-10-15
Estado: PAUSADO A PETICIÓN DEL USUARIO

## Objetivo actual

F13 Task 5: implementar el rail documental de 96 px y el routing mínimo de los cinco modos core sin duplicar lógica de negocio ni adelantar F14.

## Rama / PR

- Rama de producto: `feat/f13-document-tool-rail`
- PR draft: `#38` — `F13 Task 5: add document rail and core routing`
- Base de la PR: `feat/f13-shell-workspace-state`
- No se ha hecho merge a `main`.

## TDD ejecutado

### RED inicial

Commit: `d6730a2c48b48a135661b232dc67a95a3622ab42`

Se añadió `tests/SGPdf.App.Tests/MainWindowToolRailTests.cs` con contrato para:

- rail documental de 96 px;
- orden del catálogo: `read`, `edit`, `comment`, `sign`, `organize`, `forms`, `ocr`, `redact`, `compare`, `convert`, `protect`;
- accesibilidad y un único item seleccionado;
- router privado `HandleToolAction(string)` limitado a los cinco modos F13;
- no saltarse guards de Organize y Comments.

Resultado RED confirmado por CI:

- 1227 pruebas anteriores PASS;
- exactamente 4 pruebas nuevas FAIL por rail/router todavía inexistentes;
- no regresiones funcionales previas.

### Limpieza del test RED

Commit: `aef6e9cf3395f9ce88616138c1915315309e8c23`

Se eliminó un warning del analyzer xUnit en el propio test. El RED quedó limpio: build 0 warnings / 0 errors y los mismos 4 fallos esperados.

## Primer GREEN aplicado

Commit: `eb0be24071a9b205a6386e57e54912b3600ab3b1`

Archivo productivo modificado:

- `src/SGPdf.App/MainWindow.Shell.cs`

Diseño aplicado:

- rail creado desde el owner del shell, sin reescribir el XAML grande;
- inserta una columna de 96 px únicamente en workspace PDF y la colapsa en Home/ZPL;
- usa `ToolCatalog.Document` como única fuente del catálogo;
- las herramientas futuras quedan visibles pero deshabilitadas;
- `HandleToolAction(string)` reconoce únicamente `read`, `edit`, `comment`, `sign`, `organize`;
- antes de cambiar de modo reutiliza guards existentes de Comments, Edit, Organize y Signature;
- delega después a los owners existentes;
- no se añadió command bus, navigation service, framework, paquete, engine ni arquitectura paralela;
- no se tocó F14 ni se retiró todavía la franja legacy.

## Resultado del primer GREEN

CI del commit `eb0be24071a9b205a6386e57e54912b3600ab3b1`:

- build PASS;
- suite total: 1231 pruebas;
- 2 pruebas FAIL;
- 1229 PASS;
- el fallo quedó localizado en Test, no en Build.

Antes de pausar se inició la clasificación de esos dos fallos. La hipótesis actual es que al menos parte del problema puede ser sobreespecificación/estado artificial del contrato de test, pero esto **NO está cerrado ni demostrado todavía**. No se debe modificar producción ni parchear hasta leer el fallo exacto y contrastarlo contra los guards reales.

## Auditoría del diff de PR #38

Hasta el punto de pausa, el diff permanece acotado a:

- `src/SGPdf.App/MainWindow.Shell.cs`;
- `tests/SGPdf.App.Tests/MainWindowToolRailTests.cs`.

No se detectó:

- routing de utilidades F14;
- cambios de engines;
- paquetes nuevos;
- cambios de lockfiles;
- reescritura del XAML principal;
- duplicación de owners funcionales.

## Próximo paso exacto al reanudar

1. Leer los dos fallos exactos del CI del commit `eb0be240...`.
2. Clasificar cada uno como `TEST CONTRACT BUG` o `PRODUCT BUG` usando el owner/guard real.
3. Si el test construye un estado imposible o exige una señal incorrecta, corregir **solo el test**.
4. Si el producto realmente viola el guard/routing, corregir la causa mínima en `MainWindow.Shell.cs`.
5. Volver a CI y exigir `1231/1231 PASS`, build 0 warnings / 0 errors.
6. Solo después considerar Task 5 GREEN y continuar al siguiente bloque F13.

## Guardrails vigentes

- KISS: simple, eficiente, optimizado, sin arquitectura compleja.
- Ruido: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Anti-parche: reproducir → clasificar → raíz → owner → cambio mínimo → test causal → suite completa → CI exact-head.
- `main` continúa READ-ONLY; no merge sin aprobación explícita del usuario.
- No adelantar F14 desde Task 5.

## Punto de pausa

No continuar automáticamente desde este checkpoint. Esperar instrucción explícita del usuario.
