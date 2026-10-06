# Módulo Etiquetas ZPL — Especificación

> Decisiones globales: `MASTER_CONTEXT.md`. Orden de ejecución: `MASTER_PLAN.md`.

## Problema

Mercado Libre entrega etiquetas ZPL. El usuario posee una impresora térmica que puede imprimir PDF mediante Windows, pero no interpreta directamente esos archivos. Hoy el flujo manual usa Labelary para convertir/renderizar antes de imprimir.

SG PDF Editor debe reemplazar ese flujo completamente offline.

## Entradas

- `.zpl`
- `.txt` con ZPL
- `.prn` con ZPL

Contenido reconocible inicialmente: uno o más bloques `^XA ... ^XZ`.

## Corpus real conocido

### Bultos
- múltiples diseños independientes;
- alrededor de 4×6 / 102×152 mm a 203 dpi como preset inicial;
- aparecen gráficos `^GFA`, Code128 `^BC`, QR `^BQ`, `^FB`, `^FR`, `^FH`, `^CI28`, `^LH`.

### Productos
- varios diseños;
- cantidades mediante `^PQ`;
- etiquetas pequeñas (aprox. 38×25 mm en los casos observados);
- Code128 + texto + encoding/hex.

Los archivos reales son privados y no se versionan.

## Regla `^PQ`

`^PQ28` representa **un diseño con 28 copias**. Nunca materializar 28 renders idénticos.

Modelo mínimo:

```text
LabelJob
  SourcePath
  Designs[]

LabelDesign
  OriginalZpl
  RequestedCopies
  SelectedCopies
  WidthMm
  HeightMm
  Dpmm
  ContentHash
```

## Parser propio mínimo

Nuestro código puede:

1. separar bloques `^XA ... ^XZ`;
2. detectar/extraer cantidad `^PQ`;
3. mantener ZPL original;
4. preparar input de preview con una copia cuando sea necesario;
5. clasificar warnings/errores.

No implementar un renderer ZPL completo propio.

## Gate ZPL-A — obligatorio antes de la UI completa

Comparar:

### BinaryKits/BinaryKits.Zpl — candidato preferente
- MIT;
- .NET in-process;
- preview bitmap;
- PDF mediante Skia (`SKDocument.CreatePdf`);
- soporte de Code128/QR/gráficos/texto/templates;
- soporte conocido de `^DF`, `^XF`, `^CI28`.

### GOODBOY008/labelize — fallback
- offline;
- buena cobertura;
- CLI/librería Rust;
- proceso/binario adicional si se integra por CLI;
- release condicionado mientras no se resuelva satisfactoriamente la procedencia de `ZplGSCustom.ttf`.

### Corpus del Gate

Probar como mínimo:

- `^XA/^XZ`;
- `^CI28`;
- `^FH`;
- `^FB`;
- `^FR`;
- `^GFA`;
- `^BC`;
- `^BQ`;
- `^PQ`;
- `^DF`;
- `^XF`;
- tildes y ñ;
- direcciones largas;
- gráficos/logos;
- inversión/rotación.

Benchmark con 10/100/500 **diseños**, no copias `^PQ`.

Medir fidelidad, decode de códigos, CPU, RAM, tiempo, I/O, cold/warm, packaging, mantenimiento y licencias.

Si BinaryKits tiene fidelidad suficiente, gana y Labelize sale del runtime.

## Pipeline preferido si gana BinaryKits

Preview:

```text
ZPL → BinaryKits → bitmap → WPF
```

Salida:

```text
ZPL → BinaryKits DrawPdf() → PDF individual
                               ↓
                         PDFsharp XPdfForm
                               ↓
                          hoja/grilla PDF
                               ↓
                          Windows Print
```

La salida de BinaryKits es híbrida: texto/formas pueden mantenerse vectoriales; barcodes se dibujan como bitmap. El código actual utiliza nearest-neighbor para escalar módulos de barcode.

## Unicode `^CI28`

Aunque BinaryKits modela `UTF8 = 28`, validar explícitamente:

```text
Bogotá
Medellín
Nariño
á é í ó ú
ñ Ñ
```

y combinaciones `^FH` con UTF-8 hex.

## Comandos no soportados

No ignorar todo silenciosamente.

### Visual
Si cambia contenido/posición/barcode/encoding/gráfico: warning fuerte o error.

### Control de impresora sin impacto visual
Se puede ignorar con warning/debug.

### Recurso/template
Si luego se referencia algo que no pudo resolverse: error.

Nunca mostrar una etiqueta aparentemente correcta cuando falta contenido relevante.

## Cantidades de usuario

- respetar `^PQ`;
- una de cada diseño;
- cantidad personalizada por diseño.

## Layouts

Presets:
- 1;
- 2;
- 3;
- 4;
- 6;
- 8;
- 10;
- 12;
- columnas × filas personalizado.

Tamaños iniciales:
- original/térmica;
- 38×25 mm;
- 50×30 mm;
- 100×100 mm;
- 100×150 mm;
- 102×152 mm / 4×6;
- A4;
- Carta;
- personalizado.

Controles: margen, gap horizontal/vertical, rotación 0/90/auto y orden.

## Calidad barcode/QR

- no JPEG;
- no deformar X/Y de forma independiente;
- no antialias en módulos;
- nearest-neighbor si hay resize raster;
- mantener quiet zones;
- tamaño físico exacto;
- no escalar silenciosamente por hardware margins.

Validación automática:

```text
ZPL → salida final PDF → raster → ZXing.Net → payload debe coincidir
```

Y validación física con impresora/lector.

## Impresión

El PDF conserva el tamaño físico real. Consultar `PrintQueue.GetPrintCapabilities()` para tamaños, resolución, scaling e `PageImageableArea`.

Si el driver no admite el medio o el área imprimible recorta contenido, advertir en vez de alterar silenciosamente la escala.

Si un driver real destruye la escala, el primer fallback es rasterizar a la resolución nativa y usar el driver Windows; no mandar datos RAW en un lenguaje que la impresora no entienda.

## Privacidad

- archivos reales no salen del PC;
- `tests/PrivateFixtures/` local e ignorado;
- CI usa fixtures sintéticos con datos falsos;
- temporales en carpeta de app y limpieza segura;
- no telemetría con contenido/rutas sensibles.

## Pruebas mínimas

### Parser
- un bloque sin `^PQ` → 1;
- múltiples bloques;
- `^PQ10/^PQ28`;
- truncado → error;
- contenido `^FD` no rompe separación.

### Layout
- 1/2/3/10 por página;
- A4 y térmica;
- portrait/landscape;
- cantidades que cruzan página;
- 28 copias del mismo diseño sin 28 renders.

### Integración
- bultos ML privado;
- productos ML privado;
- red deshabilitada;
- exportar PDF;
- reabrir con PDFium;
- decodificar barcode/QR.

## Fuera del MVP

- editor visual ZPL;
- diseñador de etiquetas desde cero;
- RAW Zebra genérico;
- generador manual de barcodes;
- soporte de todos los lenguajes de impresora.
