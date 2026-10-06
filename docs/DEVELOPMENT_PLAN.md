# SG PDF Editor — Plan de desarrollo KISS

## Objetivo
Crear una aplicación Windows rápida, local y simple que cubra dos trabajos diarios:
1. leer, imprimir, firmar, organizar y editar PDFs;
2. abrir archivos de etiquetas térmicas ZPL, previsualizarlos, maquetarlos y convertirlos a PDF/imprimirlos sin Internet.

## Regla no negociable: offline
- Ninguna función principal depende de APIs web.
- No usar Labelary en producción.
- No exigir cuenta, servidor, nube ni conexión a Internet para trabajar.
- Dependencias permitidas solo si son gratuitas/open source y redistribuibles con licencia compatible.
- Las actualizaciones pueden descargarse cuando el usuario quiera, pero el trabajo diario debe funcionar con red desconectada.

## Decisiones KISS
1. **WPF + .NET 10**.
2. **Un proyecto de aplicación + un proyecto de pruebas**.
3. **PDFium**: lector/render/objetos/guardado PDF.
4. **Labelize CLI local**: render ZPL/EPL a PNG/PDF sin red; se distribuye junto a la app.
5. **PDFsharp 6.2.x Core**: crear PDFs nuevos de etiquetas y colocar imágenes; no reemplaza PDFium.
6. **qpdf solo bajo demanda**: merge/split/cifrado cuando realmente reduzca código.
7. **Tesseract solo al llegar OCR**.
8. **Save As primero** para proteger originales.
9. Cada fase es un vertical slice utilizable.

## Tipos de archivo iniciales
### PDF
- `.pdf`

### Etiquetas térmicas
- `.zpl`
- `.txt` cuando el contenido sea ZPL
- `.prn` cuando contenga ZPL

Detección ZPL simple: debe existir al menos un bloque válido `^XA ... ^XZ`. No se construye un parser ZPL propio completo.

## UX principal
Pantalla inicial:
- `Abrir PDF`
- `Abrir etiquetas`
- `Recientes`

Cuando se abre PDF:
`Leer | Firmar | Editar | Organizar | Comentar | Etiquetas`

`Etiquetas` también se puede abrir directamente desde Archivo > Abrir.

## Módulo Etiquetas ZPL
### Flujo
1. Abrir `.zpl`, `.txt` o `.prn`.
2. Detectar bloques `^XA ... ^XZ`.
3. Leer `^PQ` de cada bloque para conocer cantidad solicitada.
4. Para preview, normalizar temporalmente cada bloque a una copia (`^PQ1`) sin modificar el archivo original.
5. Renderizar cada diseño localmente con `labelize.exe` a PNG.
6. Mostrar miniaturas y preview.
7. Elegir cantidad y maquetación.
8. Exportar PDF o imprimir directamente.

### Cantidades
El usuario puede elegir:
- **Respetar `^PQ`**: usa las cantidades definidas en el archivo.
- **Una de cada diseño**: ignora cantidad y usa 1.
- **Cantidad personalizada** por diseño.

### Maquetación
Presets:
- 1 etiqueta por página;
- 2 por página;
- 3 por página;
- 4;
- 6;
- 8;
- 10;
- 12;
- personalizada: columnas × filas.

La etiqueta nunca se deforma. Se conserva aspect ratio y se permite rotación automática 90° si aprovecha mejor la hoja.

### Tamaño de hoja
- tamaño original de etiqueta / térmica;
- 4×6 in / 102×152 mm;
- 100×150 mm;
- A4;
- Carta;
- personalizado en mm;
- posteriormente detectar tamaños admitidos por la impresora Windows.

Controles:
- márgenes;
- separación horizontal/vertical;
- escala `Ajustar` / `Tamaño real`;
- rotación automática/manual;
- orden de etiquetas.

### Salidas
- Preview.
- Exportar a PDF.
- Imprimir a cualquier impresora Windows que acepte trabajos gráficos/PDF mediante su driver.
- No enviar ZPL directamente salvo función opcional futura.

## Casos reales Mercado Libre
Los archivos de bultos contienen múltiples bloques ZPL independientes con Code 128, QR, gráficos y texto. Los archivos de productos pueden usar `^PQ` para solicitar muchas copias del mismo diseño. El módulo debe preservar ambos conceptos: **diseño** y **cantidad**.

## PDF — modo Leer
- abrir;
- páginas;
- zoom;
- ajustar ancho/página;
- imprimir;
- navegación;
- miniaturas;
- buscar/copiar texto después del MVP básico.

## PDF — modo Firmar
Prioridad alta.
- importar PNG transparente;
- JPG/SVG después de normalizar;
- drag & drop;
- resize proporcional;
- mover;
- duplicar/eliminar;
- guardar copia;
- biblioteca local de firmas posteriormente.

## PDF — edición contextual
### Imagen: clic derecho
- Reemplazar imagen.
- Extraer/Guardar imagen como.
- Copiar.
- Girar.
- Cambiar tamaño.
- Opacidad.
- Orden de capa.
- Eliminar.

### Texto: clic derecho
- Editar texto.
- Copiar.
- Seleccionar bloque.
- Propiedades básicas.
- Eliminar cuando sea seguro.

## Orden de ejecución
### Fase 0 — Base PDF imprimible
1. PDFium abre documentos en Windows x64.
2. Render real en WPF.
3. Zoom y navegación mínima.
4. Imprimir.

### Fase 1 — Etiquetas térmicas offline
1. integrar `labelize.exe` local;
2. abrir los dos archivos Mercado Libre de referencia;
3. separar bloques ZPL y `^PQ`;
4. render individual PNG;
5. preview;
6. maquetación 1/2/3/4/6/8/10/12/custom;
7. PDFsharp genera el PDF compuesto;
8. imprimir;
9. probar con red deshabilitada.

### Fase 2 — Firma visual
1. PNG transparente;
2. overlay;
3. drag/resize;
4. coordenadas pantalla↔PDF;
5. insertar y guardar;
6. reabrir y verificar.

### Fases posteriores
Organizar → imágenes → texto → comentarios → utilidades → OCR → texto avanzado → herramientas profesionales.

## Criterios de calidad ZPL
- No llamar ninguna URL durante render.
- Los códigos de barras/QR deben mantenerse nítidos; no aplicar interpolación borrosa al escalar.
- Escalado uniforme únicamente.
- Preview y PDF deben conservar relación de aspecto.
- `^PQ` se interpreta correctamente.
- Unicode/`^CI28` y hex `^FH` deben probarse con archivos reales.
- Gráficos `^GFA`, Code 128 `^BC`, QR `^BQ`, field block `^FB`, reverse `^FR`, `^LH` y cantidades `^PQ` forman parte del corpus mínimo.
- Si un comando no se soporta, mostrar advertencia clara y no fingir un render correcto.

## Lo que NO hacemos
- No implementamos ZPL completo nosotros mismos.
- No usamos Labelary API.
- No servidor local obligatorio.
- No Docker para el usuario final.
- No Electron/Node dentro de la app.
- No microservicios.
- No DI container.
- No plugin system.
- No nube.
- No telemetría obligatoria.

## Definition of Done de cualquier función
- funciona sin Internet;
- build/test Windows pasa;
- error entendible si falla;
- no destruye el archivo original;
- se prueba con un archivo real, no solo con mocks.
