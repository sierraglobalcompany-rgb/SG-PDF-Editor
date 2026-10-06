# SG PDF Editor

Editor PDF local para Windows, diseñado para evolucionar por módulos y poder ser desarrollado con agentes de código como Google Antigravity.

## Objetivo
Crear un editor PDF moderno para Windows con funcionamiento principalmente local, sin depender de servidores para abrir o modificar documentos.

## Stack inicial
- C# / .NET 10
- WPF para interfaz nativa de Windows
- Arquitectura MVVM
- Motores PDF desacoplados mediante interfaces
- PDFium para renderizado (planificado)
- qpdf para operaciones estructurales (planificado)
- PoDoFo para edición de bajo nivel (planificado)
- Tesseract OCR para documentos escaneados (planificado)

> Los motores concretos se integrarán detrás de interfaces para poder reemplazarlos sin reescribir la aplicación.

## MVP 1
1. Abrir PDF.
2. Mostrar miniaturas y páginas.
3. Zoom y navegación.
4. Reordenar, rotar y eliminar páginas.
5. Unir y dividir PDFs.
6. Guardar como nuevo archivo.
7. Insertar texto, imagen y firma como capa.
8. Anotaciones básicas.
9. OCR opcional.

## Estructura
- `src/SGPdf.App`: interfaz WPF.
- `src/SGPdf.Core`: dominio, contratos y modelos.
- `src/SGPdf.Infrastructure`: adaptadores de motores PDF/OCR.
- `tests`: pruebas automatizadas.
- `docs`: arquitectura y roadmap.

## Principios
- Local-first.
- No modificar destructivamente el original por defecto.
- Separar UI de motores PDF.
- Funciones nuevas detrás de pruebas y contratos.
- Commits pequeños y reversibles.
- No introducir dependencias AGPL sin aprobación explícita.

## Estado
Scaffold inicial. Aún no hay motor PDF integrado.
