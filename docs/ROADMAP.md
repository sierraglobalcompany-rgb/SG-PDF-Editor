# Roadmap KISS

## Fase 0 — Base PDF imprimible
- Abrir PDF.
- Renderizar página.
- Zoom.
- Navegación mínima.
- Imprimir.

**Salida:** lector PDF básico y estable sobre el que se apoyan firma/edición.

## Fase 1 — Etiquetas térmicas ZPL offline
- Abrir `.zpl`, `.txt` y `.prn` con ZPL.
- Separar bloques `^XA ... ^XZ`.
- Leer y preservar cantidades `^PQ`.
- Renderizar localmente con Labelize; cero API/Internet.
- Preview por etiqueta.
- Cantidad: respetar `^PQ`, una de cada, personalizada.
- Hoja: térmica/original, 4×6, 100×150, A4, Carta, personalizada.
- Layout: 1, 2, 3, 4, 6, 8, 10, 12 o grid personalizado por página.
- Márgenes/separación/rotación automática.
- Exportar PDF con PDFsharp.
- Imprimir desde Windows.
- Validar con archivos Mercado Libre reales.

**Salida:** reemplaza el flujo manual de Labelary para el uso diario.

## Fase 2 — Firma visual
- Importar PNG con transparencia.
- Arrastrar/redimensionar.
- Guardar como copia.
- Biblioteca local de firmas posteriormente.

## Fase 3 — Lector completo
- Scroll continuo.
- Miniaturas.
- Navegación avanzada.
- Buscar/copiar texto.
- Marcadores y enlaces.
- PDFs con contraseña.
- Varias pestañas solo si no complica estabilidad.

## Fase 4 — Organizar
- Rotar, eliminar, duplicar, reordenar.
- Insertar páginas/PDF.
- Extraer páginas.
- Unir/dividir.
- Introducir qpdf solo si reduce código y riesgo.

## Fase 5 — Imágenes
- Clic derecho contextual.
- Extraer imagen.
- Guardar imagen como.
- Reemplazar imagen conservando posición/tamaño.
- Mover, redimensionar, rotar, eliminar.
- Undo/Redo.

## Fase 6 — Texto V1
- Detectar objetos de texto.
- Clic derecho > Editar texto.
- Cambiar contenido simple.
- Propiedades básicas cuando PDFium/fuente lo permita.
- Sin prometer reflow complejo todavía.

## Fase 7 — Comentarios
- Resaltar.
- Nota.
- Dibujo.
- Formas.

## Fase 8 — Utilidades esenciales
- Comprimir.
- Marca de agua.
- Numeración.
- Proteger/desbloquear.
- Reparar cuando sea viable.

## Fase 9 — OCR
- Tesseract.
- Detectar documento escaneado.
- Crear texto buscable.

## Fase 10 — Texto V2
- Agrupar líneas/párrafos.
- Reflow limitado.
- Mejor sustitución de fuentes.

## Fase 11 — Profesional
- Censura real.
- Formularios.
- Firma digital con certificado.
- Comparar PDFs.
- Procesamiento por lotes.
- Generador de códigos de barras/QR si aporta uso real.
- Conversiones solo donde exista solución madura, gratuita y offline.

## Regla de avance
Cada fase debe quedar usable y probada con archivos reales. No añadir infraestructura preventiva. Ninguna función principal puede requerir Internet.
