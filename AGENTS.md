# Instrucciones para agentes de desarrollo

## Misión
Construir SG PDF Editor como una aplicación Windows simple, rápida, local-first y útil. La arquitectura debe seguir KISS/YAGNI: resolver el problema actual con el mínimo número de capas, dependencias y abstracciones.

## Reglas obligatorias
1. Leer `docs/DEVELOPMENT_PLAN.md`, `docs/ARCHITECTURE.md` y `docs/ROADMAP.md` antes de implementar.
2. No crear capas, proyectos, interfaces genéricas, contenedores DI, buses de eventos ni patrones sin una necesidad actual demostrable.
3. Mantener inicialmente un solo proyecto de aplicación y un proyecto de pruebas.
4. PDFium es el motor PDF principal. Usar solo las funciones nativas necesarias.
5. qpdf, Tesseract u otra dependencia se añaden únicamente cuando una fase concreta las necesite y simplifiquen el trabajo.
6. No incorporar código/dependencias AGPL o GPL al producto sin aprobación explícita. Priorizar MIT, BSD y Apache-2.0.
7. Se puede reutilizar/adaptar código permisivo con atribución y revisión; nunca copiar código GPL/AGPL al producto.
8. Cada cambio debe terminar compilable y con una prueba manual concreta.
9. No hacer refactors masivos preventivos.
10. No hacer merge automático a `main`.

## Enfoque de implementación
- Construir vertical slices útiles, no subsistemas aislados.
- Primero: abrir → renderizar → zoom → firma PNG transparente → guardar copia → imprimir.
- Después expandir lector, organizar, imágenes y texto sobre la misma base.
- Save As por defecto durante las primeras fases para proteger el original.

## UX base
El documento abre siempre en modo `Leer`. Modos principales: `Leer | Firmar | Editar | Organizar | Comentar`.
Los menús de clic derecho son contextuales: imagen, texto, página o espacio vacío.
