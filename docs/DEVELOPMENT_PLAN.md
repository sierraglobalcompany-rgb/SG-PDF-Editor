# SG PDF Editor — Plan de desarrollo KISS

## Objetivo
Crear un lector/editor PDF de escritorio para Windows que sea rápido, local, fácil de usar y que priorice las funciones de uso diario: leer, imprimir, firmar, organizar y editar.

## Decisiones
1. **WPF + .NET 10**: priorizamos madurez, integración Windows y simplicidad sobre adoptar tecnología nueva por moda.
2. **Un solo proyecto de aplicación + pruebas**: no Core/Infrastructure/DI hasta que exista una necesidad real.
3. **PDFium como motor principal**: render, objetos, texto, imágenes y guardado.
4. **Interop mínimo propio**: reutilizar patrones permisivos probados y declarar solo las funciones PDFium que usemos.
5. **qpdf bajo demanda**: CLI para merge/split/cifrado cuando resulte más simple que implementarlo con PDFium.
6. **Tesseract bajo demanda**: únicamente OCR.
7. **Save As primero**: proteger originales durante MVP.
8. **Vertical slices**: cada fase debe dejar una función completa utilizable.

## UX principal
El PDF abre en `Leer`.

Modos visibles:
`Leer | Firmar | Editar | Organizar | Comentar`

### Firmar
Prioridad alta.
- importar PNG/JPG/SVG (normalizar a bitmap cuando haga falta);
- conservar transparencia PNG;
- guardar firmas locales posteriormente;
- drag & drop;
- resize proporcional;
- mover con mouse/teclado;
- duplicar/eliminar;
- guardar copia.

### Menú contextual de imagen
- Reemplazar imagen.
- Extraer/Guardar imagen como.
- Copiar.
- Recortar (posterior).
- Girar.
- Cambiar tamaño.
- Opacidad.
- Orden de capa.
- Eliminar.

### Menú contextual de texto
- Editar texto.
- Copiar.
- Seleccionar bloque.
- Propiedades básicas.
- Eliminar cuando sea seguro.

## Primera ejecución: vertical slice
Implementar en este orden:
1. PDFium actualizado cargando correctamente en Windows x64.
2. Abrir un PDF y obtener número/tamaño de páginas.
3. Renderizar la primera página en WPF.
4. Zoom sin reabrir el archivo.
5. Render de página seleccionada.
6. Imprimir.
7. Importar PNG transparente.
8. Mostrar overlay de firma sobre la página y permitir mover/resize.
9. Convertir coordenadas pantalla↔PDF.
10. Insertar la imagen como objeto PDFium.
11. Guardar como archivo nuevo.
12. Reabrir el archivo guardado y verificar la firma.

## Criterios del vertical slice
- abre PDFs reales;
- no bloquea UI en un documento normal;
- zoom mantiene nitidez;
- PNG conserva transparencia;
- firma queda en la coordenada esperada al reabrir;
- impresión funciona en impresora/PDF virtual;
- el original no se modifica;
- build y tests pasan en GitHub Actions Windows.

## Lo que NO hacemos todavía
- contenedor DI;
- plugin system;
- microservicios;
- múltiples motores intercambiables;
- base de datos;
- telemetría;
- nube;
- OCR;
- conversión Office;
- edición de párrafos tipo Word;
- firma criptográfica.

Se agregan solo cuando una necesidad real lo exija.
