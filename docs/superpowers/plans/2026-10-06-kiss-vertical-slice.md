# KISS Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar una app Windows que abra un PDF, lo renderice, haga zoom, permita colocar una firma PNG transparente, guardar una copia e imprimir.

**Architecture:** Un proyecto WPF .NET 10 y un proyecto de pruebas. PDFium es el único motor del vertical slice y se consume mediante un interop C# mínimo. No DI, no Core/Infrastructure, no motor intercambiable.

**Tech Stack:** C#, .NET 10, WPF, PDFium.

**Spec:** `docs/DEVELOPMENT_PLAN.md`

## Global Constraints
- KISS/YAGNI.
- Windows x64 primero.
- El original no se sobrescribe por defecto.
- Sin GPL/AGPL.
- Solo dependencias necesarias para esta entrega.
- No merge automático a `main`.

## Review Focus
- PDF con muchas páginas no debe renderizarse completo al abrir.
- PNG con alpha debe conservar transparencia.
- Coordenadas firma pantalla↔PDF deben mantenerse tras guardar/reabrir.
- PDF protegido debe fallar con mensaje controlado o pedir contraseña, no cerrar la app.
- Cerrar/cambiar documento debe liberar handles PDFium.

---

### Task 1: Simplificar solución y CI
**Files:** `SGPdf.slnx`, `src/SGPdf.App/SGPdf.App.csproj`, `tests/*`, `.github/workflows/build.yml`
- [ ] Reducir a aplicación + pruebas.
- [ ] Añadir build/test Windows x64.
- [ ] Verificar `dotnet build` y `dotnet test` en CI.

### Task 2: Interop PDFium mínimo
**Files:** `src/SGPdf.App/Pdf/PdfiumNative.cs`, `PdfDocumentSession.cs`
- [ ] Integrar binario PDFium actualizado.
- [ ] Implementar init/shutdown y abrir/cerrar documento.
- [ ] Exponer page count, tamaño de página y errores.
- [ ] Probar lifecycle sin fuga/doble dispose.

### Task 3: Render de página y zoom
**Files:** `PdfPageRenderer.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`
- [ ] Renderizar página seleccionada a `BitmapSource`.
- [ ] Mostrar primera página al abrir.
- [ ] Implementar zoom +/− y ajustar ancho.
- [ ] No renderizar todas las páginas.

### Task 4: Firma visual PNG
**Files:** `Features/Sign/SignatureOverlay.cs`, `MainWindow.xaml(.cs)`, `PdfDocumentSession.cs`
- [ ] Importar PNG con transparencia.
- [ ] Overlay movible y redimensionable manteniendo proporción.
- [ ] Convertir coordenadas de pantalla a puntos PDF.
- [ ] Insertar bitmap como objeto PDFium.

### Task 5: Guardar y reabrir
**Files:** `PdfSave.cs`, UI guardar como
- [ ] Guardar únicamente a ruta nueva en MVP.
- [ ] Generar contenido de páginas modificadas al guardar.
- [ ] Reabrir copia y verificar firma en posición esperada.

### Task 6: Imprimir y prueba manual
**Files:** `Features/Reader/PdfPrintService.cs`, `docs/TEST_CHECKLIST.md`
- [ ] Imprimir página/documento mediante `PrintDialog`.
- [ ] Probar con Microsoft Print to PDF.
- [ ] Ejecutar checklist con PDF simple, multipágina, imagen, fuente incrustada y PNG transparente.
- [ ] Crear PR con evidencia CI y limitaciones conocidas.
