# Roadmap KISS

## Fase 0 — Vertical slice útil
- Abrir PDF.
- Renderizar página.
- Zoom.
- Imprimir.
- Importar PNG con transparencia como firma.
- Arrastrar/redimensionar firma.
- Guardar como copia.

**Salida:** una aplicación ya utilizable para leer y firmar PDFs.

## Fase 1 — Lector completo
- Scroll continuo.
- Miniaturas.
- Navegación por página.
- Buscar/copiar texto.
- Marcadores y enlaces.
- PDFs con contraseña.
- Varias pestañas si no complica estabilidad.

## Fase 2 — Organizar
- Rotar, eliminar, duplicar, reordenar.
- Insertar páginas/PDF.
- Extraer páginas.
- Unir/dividir.
- Introducir qpdf solo si reduce código y riesgo.

## Fase 3 — Imágenes
- Clic derecho contextual.
- Extraer imagen.
- Guardar imagen como.
- Reemplazar imagen conservando posición/tamaño.
- Mover, redimensionar, rotar, eliminar.
- Undo/Redo.

## Fase 4 — Texto V1
- Detectar objetos de texto.
- Clic derecho > Editar texto.
- Cambiar contenido simple.
- Propiedades básicas cuando PDFium/fuente lo permita.
- Sin prometer reflow complejo todavía.

## Fase 5 — Comentarios
- Resaltar.
- Nota.
- Dibujo.
- Formas.

## Fase 6 — Utilidades esenciales
- Comprimir.
- Marca de agua.
- Numeración.
- Proteger/desbloquear.
- Reparar cuando sea viable.

## Fase 7 — OCR
- Tesseract.
- Detectar documento escaneado.
- Crear texto buscable.

## Fase 8 — Texto V2
- Agrupar líneas/párrafos.
- Reflow limitado.
- Mejor sustitución de fuentes.

## Fase 9 — Profesional
- Censura real.
- Formularios.
- Firma digital con certificado.
- Comparar PDFs.
- Procesamiento por lotes.
- Conversiones solo donde exista una solución madura reutilizable.

## Regla de avance
No se inicia una fase si la anterior no abre, guarda y prueba PDFs reales sin regresiones graves.
