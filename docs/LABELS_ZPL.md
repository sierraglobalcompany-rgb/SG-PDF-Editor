# Módulo Etiquetas ZPL — Especificación

## Problema
Algunas impresoras térmicas/Windows no interpretan directamente ZPL generado por plataformas como Mercado Libre. El usuario hoy abre el ZPL en un visor web, lo convierte a PDF y luego imprime ese PDF.

SG PDF Editor debe reemplazar ese flujo sin Internet.

## Entrada
Extensiones:
- `.zpl`
- `.txt`
- `.prn`

Contenido mínimo reconocible:
- uno o más bloques `^XA ... ^XZ`.

## Corpus inicial Mercado Libre
### Bultos
- 10 bloques de etiqueta independientes.
- 203 dpi / 8 dpmm como preset inicial.
- Tamaño esperado aproximado: 102×152 mm.
- Usa gráficos `^GFA`, Code 128 `^BC`, QR `^BQ`, bloques `^FB`, field reverse `^FR`, `^FH`, `^CI28`, `^LH`.

### Productos
- 7 diseños.
- cantidades `^PQ`: 10, 12, 20, 28, 10, 20, 10.
- tamaño esperado aproximado: 38×25 mm a 203 dpi.
- Code 128 + texto + `^FH/^CI28`.

Los tamaños son presets/candidatos iniciales; la UI permite corregirlos antes de exportar.

## Modelo de datos mínimo
```text
LabelJob
  SourcePath
  Designs[]

LabelDesign
  OriginalZpl
  PreviewZpl
  RequestedCopies
  SelectedCopies
  WidthMm
  HeightMm
  Dpmm
  PreviewPath
```

No almacenar una copia por `^PQ` en memoria: una etiqueta con `^PQ28` sigue siendo un diseño con cantidad 28.

## Parser mínimo
No interpretar ZPL completo.

Funciones permitidas:
1. separar bloques por `^XA`/`^XZ`;
2. encontrar el último `^PQ` válido del bloque;
3. si no existe, cantidad = 1;
4. crear `PreviewZpl` reemplazando/agregando `^PQ1` antes de `^XZ`;
5. preservar exactamente `OriginalZpl`.

## Render
Ejecutable local: Labelize.

Ejemplo conceptual:
```text
labelize convert preview.zpl -o preview.png --width 102 --height 152 --dpmm 8
```

Sin HTTP, sin API, sin servidor.

## Pantalla
```text
Etiquetas                         [Exportar PDF] [Imprimir]

Archivo: Etiquetas-de-productos.txt

[miniatura] Código EDEY87335       cantidad: 10  [−] [10] [+]
[miniatura] Código GPDH19039       cantidad: 12  [−] [12] [+]
...

Cantidad:
(*) Respetar archivo (^PQ)
( ) Una de cada
( ) Personalizada

Hoja: [Térmica 38×25 ▼]
Etiquetas por página: [1 ▼]
Márgenes: [0] mm   Separación: [0] mm
Rotación: [Auto ▼]

                       PREVIEW DE HOJA
```

## Layouts
Presets de etiquetas por página:
- 1
- 2
- 3
- 4
- 6
- 8
- 10
- 12
- Personalizado: columnas × filas

La app calcula el mayor rectángulo uniforme disponible por celda.

Reglas:
- aspect ratio siempre fijo;
- nunca estirar X e Y de forma diferente;
- rotación 0/90°;
- no recortar barcode/QR;
- no aplicar smoothing que vuelva borrosos los módulos del código;
- centrar dentro de la celda.

## Tamaños de hoja
Presets:
- Etiqueta original.
- 38×25 mm.
- 50×30 mm.
- 100×100 mm.
- 100×150 mm.
- 102×152 mm (4×6 aprox.).
- A4.
- Carta.
- Personalizado.

Más adelante: leer tamaños del driver de impresora.

## Exportar PDF
PDFsharp crea un documento nuevo:
1. nueva página del tamaño elegido;
2. por cada celda, colocar PNG respetando layout;
3. crear páginas adicionales cuando se llene la anterior;
4. guardar PDF local.

No recomprimir con pérdida imágenes monocromas de códigos.

## Impresión
Primera implementación segura:
- generar PDF temporal/final;
- imprimir mediante flujo Windows/PDF existente.

Posteriormente, si simplifica UX, imprimir `FixedDocument` directamente.

## Errores
Mostrar al usuario:
- archivo no parece ZPL;
- bloque incompleto sin `^XZ`;
- Labelize no pudo renderizar;
- comando no soportado/warning;
- tamaño inválido;
- salida PDF no pudo escribirse.

Nunca mostrar una página vacía como si el render fuera correcto.

## Seguridad y privacidad
- archivos no salen del PC;
- nombres/rutas con datos personales no se registran en telemetría;
- temporales en carpeta de app/Temp y limpieza al cerrar o terminar exportación;
- no ejecutar contenido del ZPL como shell; siempre pasar rutas/argumentos escapados a ProcessStartInfo.

## Pruebas mínimas
### Parser
- 1 bloque sin ^PQ -> 1 copia.
- múltiples bloques.
- ^PQ10/^PQ28.
- datos `^FD` que contienen caracteres especiales no rompen separación.
- archivo truncado genera error.

### Layout
- 1/2/3/10 por página.
- A4 y térmica.
- landscape/portrait.
- cantidades que cruzan página.
- 28 copias del mismo diseño.

### Integración
- archivo bultos Mercado Libre.
- archivo productos Mercado Libre.
- red Windows deshabilitada.
- exportar PDF, reabrirlo con PDFium y validar page count.

## No incluido en primera versión
- editor visual de ZPL;
- enviar comandos crudos a impresora Zebra;
- conversiones EPL/IPL/DPL;
- diseñador de etiquetas desde cero;
- generar códigos de barras manualmente desde una caja de texto.

Esas funciones se evalúan después del flujo importar → preview → maquetar → PDF/imprimir.
