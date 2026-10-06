# Instrucciones para agentes de desarrollo

## Misión
Desarrollar SG PDF Editor como una aplicación de escritorio Windows estable, modular y local-first.

## Reglas obligatorias
1. Antes de implementar una función, revisar `docs/ARCHITECTURE.md` y `docs/ROADMAP.md`.
2. Mantener la UI desacoplada del motor PDF.
3. No acoplar el proyecto a una única librería PDF: usar interfaces en `SGPdf.Core`.
4. No incorporar código o dependencias AGPL sin aprobación explícita.
5. Priorizar licencias MIT, BSD, Apache-2.0, MPL-2.0 o LGPL compatibles con distribución comercial.
6. Añadir pruebas para lógica no trivial.
7. No hacer cambios masivos no relacionados en un mismo commit.
8. No borrar funciones existentes para resolver fallos sin documentar la causa.
9. Windows es la plataforma objetivo inicial.
10. Cada fase debe quedar compilable antes de avanzar.

## Flujo sugerido para Antigravity
- Analizar la tarea.
- Proponer plan corto.
- Implementar en una rama de trabajo.
- Compilar.
- Ejecutar pruebas.
- Revisar cambios.
- Documentar decisiones relevantes.

## Primera tarea recomendada
Implementar el visor PDF de solo lectura: selección de archivo, apertura, renderizado de páginas, miniaturas, zoom y navegación. No implementar edición avanzada todavía.
