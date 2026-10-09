# F5 Task 7 checkpoint — Insertar + Combinar PDF

Fecha: 2026-10-09
Rama: `feat/f5-organize`
Base de Task 7: `ee26a7ecac9d8e95792e6de624051c707914f6ae` (checkpoint Task 6)
Head funcional verificado antes de este checkpoint: `69854f645e466d11268ed2fa3fbbe47472dbec68`

## Alcance cerrado

Task 7 implementa únicamente F5.3:

- Insertar páginas desde otro PDF.
- Elegir todas las páginas o un rango explícito.
- Insertar antes de la selección, después de la selección o al final.
- Combinar otro PDF reutilizando el mismo flujo de inserción con todas sus páginas al final.
- Abrir y validar el PDF secundario antes de publicar cualquier mutación del `OrganizePlan`.
- Rechazar PDF inválido, protegido con contraseña o bloqueado por preflight sin alterar el plan activo.
- Renderizar miniaturas de páginas insertadas desde su archivo de origen real.
- Mantener Extraer y Dividir fuera de alcance y deshabilitados.

Task 8 NO fue iniciado.

## Reglas de seguridad preservadas

- Los PDF secundarios se abren con `PdfDocumentSession.Open(path)` sin reintento ni diálogo de contraseña.
- No se almacena ni reutiliza ninguna contraseña.
- Un origen secundario con preflight `Block` nunca se publica en el plan.
- El plan solo se reemplaza después de completar open + conteo + geometría + preflight + parsing/rango.
- `OrganizeSource.Capture(...)` mantiene el fingerprint barato de longitud + última escritura UTC.
- El writer existente vuelve a verificar fingerprints antes de crear/escribir el temporal.
- Un secundario cambiado o faltante conserva intacto un destino existente.
- `Save As` continúa siendo la única materialización de F5.
- No se añadió un segundo motor PDF ni dependencia runtime nueva.
- `main` no fue modificado ni fusionado.

## Parser de páginas

Archivo nuevo:

- `src/SGPdf.App/Features/Organize/OrganizePageRangeParser.cs`

Contrato:

```csharp
internal static IReadOnlyList<int> Parse(string expression, int pageCount);
```

Semántica congelada:

- Entrada humana 1-based.
- Salida ordenada 0-based.
- Acepta ejemplos como `1`, `1,3,5-7`, espacios alrededor de tokens y rangos.
- Rechaza vacío, cero, negativos, rangos descendentes, fuera de rango, duplicados y sintaxis malformada.

### RED parser

Commit:

`a07704ab459933a329c2493aef58df3133f18ebb`

CI:

`37971902296`

Resultado esperado:

- Build: 0 warnings / 0 errores.
- 486 tests anteriores PASS.
- 20 tests nuevos FAIL únicamente porque `OrganizePageRangeParser` aún no existía.

### GREEN parser

Commit:

`20c9500fcb2fc770d5c643060f702389225e478a`

Los runs posteriores confirmaron todos los contratos del parser en verde.

## Candidate-first Insert / Merge

Suite nueva:

- `tests/SGPdf.App.Tests/MainWindowOrganizeInsertTests.cs`

Commits RED/contrato:

- `b4fd6f3b8c856e246194c6a3304668572c4d07e2` — contratos candidate-first de Insert/Merge.
- `1ed2ebcdefe3671c6005e777ab94adfd3efc4b60` — disponibilidad de comandos avanzada a Task 7.
- `43c0ee2511871187d0b258a63c027c3f0575df74` — caracterización writer multi-origen + secundario stale.

El RED cubrió:

- PDF inválido no muta el plan.
- PDF protegido no muta el plan y no invoca el flujo de contraseña de F4.
- Secundario con preflight Block no muta el plan.
- Inserción completa al inicio/medio/final.
- Rango exacto.
- Cancelación.
- Merge como insert-all append.
- Insert/Merge habilitados; Extract/Split siguen deshabilitados.
- Writer multi-origen conserva el orden lógico exacto.
- Secundario stale/missing preserva un destino existente y limpia temporales.

### RED limpio de integración

Run:

`37972341318`

El primer intento produjo 7 fallos: 5 contratos esperados de Task 7 más 2 tests antiguos de disponibilidad PDFium que no pudieron cargar `pdfium.dll` en ese runner.

Se reejecutó exactamente el mismo job, sin cambio de código. En el reintento:

- Build: 0 warnings / 0 errores.
- Total: 512.
- PASS: 507.
- FAIL: 5.
- Los 5 fallos restantes fueron exclusivamente los contratos Task 7 todavía no implementados.
- Los dos tests PDFium antiguos volvieron a PASS, confirmando que el primer resultado era ruido transitorio del runner y no una regresión.

## Implementación candidate-first

Archivo nuevo:

- `src/SGPdf.App/MainWindow.Organize.Insert.cs`

Commit principal:

`05bd2ea8324b3ac77c6623e43345bc8174d4e32a`

Flujo:

1. Verificar que ORGANIZAR permite mutación.
2. Seleccionar ruta secundaria.
3. Abrir directamente el PDF sin password retry.
4. Obtener page count.
5. Obtener geometría completa.
6. Ejecutar preflight.
7. Rechazar Block antes de publicar estado.
8. Capturar `OrganizeSource` y fingerprint.
9. Resolver todas las páginas o parsear rango.
10. Resolver posición de inserción.
11. Reutilizar `OrganizePlanOperations.InsertSourcePages(...)` ya existente desde Task 2.
12. Publicar el nuevo plan únicamente después de validar todo lo anterior.
13. Seleccionar los nuevos `ItemId` insertados.

No fue necesario modificar `OrganizePlanOperations.cs`: el método puro existente ya cumplía el contrato de Task 7, por lo que se reutilizó sin duplicar lógica.

Merge usa exactamente el mismo pipeline candidate-first, con todos los índices y `insertionIndex = plan.Pages.Count`.

## Wiring de comandos

Archivo nuevo:

- `src/SGPdf.App/MainWindow.Organize.Task7Wiring.cs`

Commits:

- `eb9c91c16031bc78691e48db7cf5c74daed7198b` — wiring inicial Insert/Merge.
- `3dda7d0d84e0f5f4c9b3868395a0578f79c0dd61` — sincronización del estado enabled con el ciclo de ORGANIZAR.

Insertar y Combinar quedan habilitados cuando ORGANIZAR está activo y no se está materializando/validando. Extraer y Dividir permanecen deshabilitados.

## Miniaturas multi-origen

RED adicional:

- Test nuevo: `MainWindowOrganizeSecondaryThumbnailTests.InsertedSecondaryPage_RendersThumbnailFromItsOwnSourceSession`
- Commit: `5da7fbe18c15f15fd9cba5ffa46ea73c27ec1e3c`
- Run: `37973006083`

Resultado RED:

- Build: 0 warnings / 0 errores.
- Total: 513.
- PASS: 510.
- FAIL: 3.
- 2 fallos eran disponibilidad de botones Task 7.
- 1 fallo demostraba el defecto real: una página secundaria terminaba con `HasError = true` porque Task 5 solo aceptaba thumbnails del origen primario.

GREEN:

Commit:

`69854f645e466d11268ed2fa3fbbe47472dbec68`

Cambio estrecho en `MainWindow.Organize.cs`:

- +25 / -4 líneas frente al checkpoint Task 6.
- Se eliminó el hard-block de thumbnails no primarios.
- Para el origen primario se reutiliza la sesión activa.
- Para una página secundaria se abre temporalmente `PdfDocumentSession.Open(source.Path)`, se renderiza esa miniatura y se dispone inmediatamente la sesión.
- No se persisten sesiones ni credenciales secundarias.
- Se conservan scheduler, cancelación, stale-publication checks, aislamiento por tile y ventana lazy/bounded de Task 5.

## GREEN funcional exacto

Head:

`69854f645e466d11268ed2fa3fbbe47472dbec68`

CI:

`37973459757`

Resultado:

- Repository hygiene: PASS.
- Labelize 1.7.0 staging: PASS.
- Restore locked: PASS.
- Release build: PASS.
- Build warnings: 0.
- Build errors: 0.
- Tests: 513 PASS / 0 FAIL / 0 SKIP.

## Auditoría Task 6 → Task 7

Comparación:

`ee26a7ecac9d8e95792e6de624051c707914f6ae..69854f645e466d11268ed2fa3fbbe47472dbec68`

9 archivos modificados/agregados, todos dentro del alcance Task 7:

- `src/SGPdf.App/Features/Organize/OrganizePageRangeParser.cs`
- `src/SGPdf.App/MainWindow.Organize.Insert.cs`
- `src/SGPdf.App/MainWindow.Organize.Task7Wiring.cs`
- `src/SGPdf.App/MainWindow.Organize.cs`
- `tests/SGPdf.App.Tests/MainWindowOrganizeInsertTests.cs`
- `tests/SGPdf.App.Tests/MainWindowOrganizeSecondaryThumbnailTests.cs`
- `tests/SGPdf.App.Tests/MainWindowOrganizeTask6Tests.cs`
- `tests/SGPdf.App.Tests/OrganizePageRangeParserTests.cs`
- `tests/SGPdf.App.Tests/PdfOrganizeWriterTests.cs`

No hubo cambios en:

- PDFium bindings.
- NativeGate.
- Writer de producción.
- LEER.
- FIRMAR.
- ZPL.
- dependencias/NuGet.
- workflow CI.

## Estado de ramas

Antes de crear este checkpoint:

- `feat/f5-organize`: `69854f645e466d11268ed2fa3fbbe47472dbec68`.
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.

`main` sigue intacto. No se hizo merge.

## QA manual

No se afirma QA físico/manual en Windows. Continúa pendiente la validación manual real prevista para el cierre de F5.

## Siguiente punto exacto

Detener aquí.

La siguiente aprobación `continua` habilita únicamente **Task 8 — F5.4 Extraer + Dividir**, con RED→GREEN y checkpoint independiente.
