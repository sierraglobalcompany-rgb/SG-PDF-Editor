# F7 — Task 2 Font Gate — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 2 — Gate de fuente fallback redistribuible  
**Estado:** **GREEN / PASS automatizado**  
**Rama:** `feat/f7-text-v1`  
**Base de Task 2:** `4d1a3c7015f6c324e7fae3dd776fd5942b88235e`  
**Head funcional GREEN:** `5b38cffb8cf4ff7f47177ce52770cfe8fd05ba20`

---

## 1. Resultado

Task 2 fija **DejaVu Sans 2.37** como la única fuente fallback inicial de F7.

La fuente se distribuye localmente con SG PDF Editor y no requiere red, fuente instalada en Windows, servicio externo ni paquete NuGet nuevo.

Producción expone internamente:

- `FallbackFontAsset.LoadBytes()`;
- `FallbackFontAsset.SupportsRune(Rune)`;
- `FallbackFontAsset.GetGlyphId(Rune)`;
- `FallbackFontAsset.ReadValidatedBytes(string)` como seam interno para guard de tamaño.

El writer F7 posterior deberá usar estos bytes/glifos con la ruta ya demostrada en Task 1:

`FPDFText_LoadCidType2Font + ToUnicode explícito + CIDToGIDMap explícito`.

`FPDFText_LoadFont` sigue rechazado como fallback de producto F7.

---

## 2. Procedencia exacta

Upstream oficial:

`https://github.com/dejavu-fonts/dejavu-fonts`

Release/tag:

`version_2_37`

Distribución oficial:

`dejavu-sans-ttf-2.37.zip`

SHA-256 del ZIP oficial:

`5c6e497a2f36552cb5ffb112c413a6af39c0f3c47653662b90b4fa6499822fd7`

Asset fijado:

`third_party/fonts/dejavu/DejaVuSans.ttf`

Tamaño exacto:

`757076 bytes`

SHA-256 del TTF:

`7da195a74c55bef988d0d48f9508bd5d849425c1770dba5d7bfc6ce9ed848954`

El asset fue descargado una sola vez desde el release oficial mediante un workflow bootstrap temporal que verificó ZIP hash + TTF hash + tamaño antes de hacer commit. Ese workflow fue eliminado antes del GREEN final y **no forma parte del estado final**.

---

## 3. Licencia / redistribución

Se conservan los notices de DejaVu/Bitstream Vera/Arev en:

`third_party/licenses/DejaVu-Fonts.txt`

`third_party/manifest.json` registra:

- upstream;
- versión 2.37;
- distribución oficial;
- hashes del archive y del TTF;
- tamaño;
- licencia/notices;
- purpose F7;
- política offline.

El TTF se mantiene sin modificar.

---

## 4. Runtime / build

`src/SGPdf.App/SGPdf.App.csproj` copia el asset a:

`fonts/DejaVuSans.ttf`

en build y publish mediante `PreserveNewest`.

No se usa `third_party/runtime/**`.

No existe descarga runtime.

No se añadió ningún PackageReference.

---

## 5. Guard de memoria y cobertura

`FallbackFontAsset` valida tamaño con `FileInfo.Length` **antes** de `File.ReadAllBytes`.

Límite:

`2 MiB`.

Archivos vacíos o mayores al límite se rechazan.

Los tests verifican:

- SHA-256 exacto del TTF copiado a output;
- cobertura ASCII imprimible;
- cobertura mínima española `áéíóúüñÁÉÍÓÚÜÑ¿¡`;
- presencia de APIs managed del asset;
- rechazo de archivo >2 MiB antes de lectura completa.

Producción usa `System.Windows.Media.GlyphTypeface`; **SkiaSharp permanece test-only** y no se añadió al runtime.

---

## 6. Evidencia RED

RED inicial:

SHA `de4b393b9158d15203a7ce6bb451552ac602050f`  
CI `38070931811`

- build: PASS, 0 warnings / 0 errors;
- tests: 687 PASS / 3 FAIL / 690 total;
- fallos esperados: asset ausente y `FallbackFontAsset` ausente.

RED de memory guard:

SHA `09b571b53292f1c066437d4963dcf6f745070db8`  
CI `38071158025`

- build: PASS, 0 warnings / 0 errors;
- tests: 687 PASS / 4 FAIL / 691 total;
- cuarto fallo esperado: production type/guard todavía ausente.

No hubo fallo accidental de WPF/sintaxis en RED.

---

## 7. GREEN exacto

SHA funcional:

`5b38cffb8cf4ff7f47177ce52770cfe8fd05ba20`

Workflow:

`38071435349`

Resultado:

- repository hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **691 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

Hubo un intento GREEN previo en `0f261e1...` que falló únicamente por `using System.IO` faltante. Se corrigió con el cambio mínimo; no implicó cambio de diseño ni de contrato.

---

## 8. Scope final de Task 2

Respecto a `4d1a3c7015f6c324e7fae3dd776fd5942b88235e`, el estado funcional final toca únicamente:

- `src/SGPdf.App/Features/Edit/Text/FallbackFontAsset.cs`;
- `src/SGPdf.App/SGPdf.App.csproj`;
- `tests/SGPdf.App.Tests/FallbackFontAssetTests.cs`;
- `third_party/fonts/dejavu/DejaVuSans.ttf`;
- `third_party/licenses/DejaVu-Fonts.txt`;
- `third_party/manifest.json`.

El workflow bootstrap temporal fue creado y borrado dentro de Task 2, por lo que no aparece en el diff final.

No hay segundo motor PDF, nuevo NuGet, network runtime, cuenta, SaaS, API key ni refactor arquitectónico.

---

## 9. Gobernanza

- F6 PR #25 continúa sin merge automático.
- No mergear a `main` sin aprobación explícita.
- Manual Windows QA continúa `NOT RUN`.
- Este checkpoint no autoriza avanzar silenciosamente a Task 3.

---

## 10. Próximo paso permitido

**Task 3 — promover únicamente la infraestructura F6 que ya es realmente compartida por EDITAR**, siguiendo RED → GREEN y conservando comportamiento F6.

No iniciar Task 4+ antes de cerrar Task 3.
