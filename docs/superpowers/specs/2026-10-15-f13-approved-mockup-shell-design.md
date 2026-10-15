# F13 Approved Mockup Shell Design

**Fecha:** 2026-10-15  
**Estado:** MOCKUP APROBADO / SPEC ESCRITO / IMPLEMENTACIÓN NO INICIADA  
**Rama de continuidad:** `plan/post-ui4-f13-f18-expansion-20261014`  
**Base funcional/UI:** `feat/release-ui-modernization` @ `abd52d05279e3e656723ad4ff5a21d36de91508e`  
**Complementa y, donde exista conflicto visual, reemplaza:** `docs/superpowers/specs/2026-10-14-f13-product-shell-design.md`

## 1. Autoridad visual

El mockup aprobado de SG PDF Editor es la referencia visual de F13 para:

- jerarquía;
- distribución;
- navegación;
- proporciones relativas;
- cards;
- rail izquierdo;
- barra contextual;
- panel derecho;
- sensación visual general.

No es una orden de copiar Adobe Acrobat ni iLovePDF pixel por pixel.  
Se adapta como diseño propio de SG PDF, con assets propios y WPF nativo.

Referencia visual aprobada:
- `sg_pdf_f13_approved_mockup.jpg`
- composición: Dashboard + Documento abierto / Editar.

## 2. Reglas obligatorias

### Regla 1 — KISS
`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → A→B`

### Regla 2 — RUIDO
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

### Regla 3 — ANTI-PARCHE
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI`

Consecuencias:

- no framework UI nuevo;
- no plugin manager;
- no command bus;
- no segundo motor PDF;
- no segundo writer;
- no backend;
- no cuenta/login;
- no publicidad;
- no IA;
- no cloud obligatorio.

## 3. Objetivo

SG PDF debe tener dos estados visuales claramente profesionales:

1. **Dashboard/Home**
2. **Documento abierto**

La transición cambia la presentación, no el ownership funcional.

## 4. Sistema visual

### Tipografía
- `Segoe UI` / tipografía Windows existente.
- Título principal: 26–30 px, SemiBold.
- Título de sección: 18–20 px, SemiBold.
- Card title: 14–15 px, SemiBold.
- Texto normal: 12–14 px.
- Texto secundario: 11–12 px.

No incorporar fuente externa solo para imitar el mockup.

### Espaciado
Escala base:
- 4
- 8
- 12
- 16
- 24
- 32

No usar márgenes arbitrarios si uno de estos valores resuelve el caso.

### Radios
- cards/paneles: 8–10 px;
- botones grandes: 6–8 px;
- icon tiles: 8 px.

### Bordes y sombras
- bordes sutiles con recursos del tema;
- sombras ligeras únicamente donde ayudan a distinguir superficies;
- no abusar de elevation.

### Tema
Todos los colores deben provenir de recursos dinámicos SG.

No añadir colores light-only hard-coded en:
- rail;
- cards;
- selected;
- hover;
- disabled;
- paneles.

Light y Dark son baseline.

## 5. App identity

El mockup usa un placeholder visual `SG`.

El icono oficial del ejecutable sigue siendo un requisito separado:

- solo fuente aprobada por el usuario;
- `.ico` multiresolución;
- ventana;
- `.exe`;
- Alt+Tab;
- taskbar;
- shortcut;
- installer.

El placeholder del mockup NO autoriza inventar el icono oficial.

## 6. Dashboard/Home

### Layout general

```text
┌─────────────────────────────────────────────────────────────┐
│ App/title bar                                               │
├──────────┬──────────────────────────────────────────────────┤
│ Home rail│ Hero / Abrir PDF / Abrir ZPL                    │
│          ├──────────────────────────────────────────────────┤
│          │ Herramientas — cards responsivas                │
│          ├──────────────────────────────────────────────────┤
│          │ Archivos recientes                              │
└──────────┴──────────────────────────────────────────────────┘
```

### Rail Home
Ancho objetivo: **96 px**.

Orden:
1. Inicio
2. Herramientas
3. Recientes

Abajo:
4. Configuración

Cada item:
- icono;
- texto visible;
- tooltip opcional;
- estado selected inequívoco;
- foco visible.

### Área principal
Padding exterior objetivo: **24 px**.

No horizontal scrolling en:
- 1366×768 @100%;
- perfil escalado validado en Windows.

### Hero
Debe contener:

- `¡Bienvenido a SG PDF Editor!`
- mensaje corto local/offline;
- botón primario `Abrir PDF`;
- botón secundario `Abrir ZPL`.

`Abrir PDF` debe dominar visualmente.

### Herramientas
Cards generadas desde catálogo, nunca duplicadas como bloques XAML independientes.

Primera ola de cards mostradas:

- Unir PDF
- Dividir PDF
- Organizar páginas
- Editar PDF
- OCR
- Censurar
- Proteger
- Firmar
- Comparar PDF
- PDF a JPG/PNG
- Imagen a PDF
- QR
- Etiquetas ZPL
- Marca de agua
- Numerar páginas

Regla:
- capacidades no implementadas todavía pueden aparecer únicamente si están marcadas inequívocamente como futuras/no disponibles;
- preferencia F13: mostrar primero lo que ya funciona para evitar botones decorativos.

### ToolCard
Objetivo visual:
- alto aproximado: 80–96 px;
- icon tile: 38–42 px;
- título + descripción corta;
- hover sutil;
- selected/pressed si aplica;
- cursor/keyboard activable;
- una sola acción por card.

### Responsive cards
No fijar tres columnas rígidas.

Usar WrapPanel/ItemsControl equivalente:

- ventana amplia: ~3 cards por fila;
- ventana media: ~2 cards;
- nunca recortar el texto principal;
- evitar scroll horizontal.

### Recientes
Reusar `RecentPdfStore`.

Columnas visuales:
- Nombre
- Ubicación
- Fecha de apertura
- Tamaño

Acciones:
- abrir;
- quitar/borrar recientes usando comportamiento existente;
- si archivo ya no existe, conservar cleanup actual.

No crear una segunda base de recientes.

## 7. Documento abierto

### Layout objetivo

```text
┌─────────────────────────────────────────────────────────────────────┐
│ Title/document bar                                                  │
├─────────────────────────────────────────────────────────────────────┤
│ Archivo  Editar  Ver  Herramientas  Ayuda    search zoom print     │
├──────────┬───────────────────────────────┬──────────────────────────┤
│ Tool rail│                               │ Right context panel      │
│ 96 px    │ PDF canvas / workspace       │ 320 px                   │
│          │                               │                          │
│          │ contextual toolbar           │ contextual properties    │
└──────────┴───────────────────────────────┴──────────────────────────┘
```

El panel contextual izquierdo de páginas/thumbnails existente puede permanecer adyacente cuando el owner lo necesite; no debe duplicar el rail.

## 8. Tool rail del documento

Ancho objetivo: **96 px**.

Orden aprobado:

1. Leer
2. Editar
3. Comentar
4. Firmar
5. Organizar
6. Formularios
7. OCR
8. Censurar
9. Comparar
10. Convertir
11. Proteger

Si no caben verticalmente a 1366×768:
- compactar spacing;
- permitir scroll vertical discreto;
- no esconder acciones esenciales arbitrariamente.

No usar menú hamburguesa como sustituto principal del rail.

### Selected
Herramienta activa:
- icono + texto claramente seleccionado;
- fondo/indicador usando recursos SG;
- nunca depender solo de color: forma/borde/indicador adicional.

### Disabled
Debe ser perceptible y accesible.
Tooltip/estado explica por qué cuando sea razonable.

## 9. Barra superior

### Title/document line
Debe mostrar:
- identidad SG PDF;
- documento abierto;
- controles Windows.

No necesitamos tabs múltiples en F13 aunque el mockup sugiera una pestaña.  
Baseline: un único documento activo.

### Global command line
Baseline:
- Archivo
- Editar
- Ver
- Herramientas
- Ayuda
- Buscar
- zoom
- Imprimir

Classic Menu puede coexistir temporalmente durante transición, pero no debe terminar duplicado visualmente para siempre.

## 10. Toolbar contextual

Aparece dentro del workspace, no como navegación global.

Ejemplo Editar:
- Seleccionar
- Deshacer
- Rehacer
- Guardar como...

Reglas:
- mostrar solo acciones reales del owner;
- Undo/Redo se habilita únicamente si el workspace activo lo soporta;
- no prometer undo universal;
- Guardar como debe llamar al save real del owner.

## 11. Canvas PDF

Reusar:
- `PdfScrollViewer`
- `PdfImage`
- transformaciones;
- render actual;
- navegación actual.

F13 no reescribe PDFium ni el renderer.

Objetivos:
- fondo canvas diferenciado;
- documento centrado;
- margen visual consistente;
- barra de navegación/páginas limpia.

La reubicación final de page controls puede hacerse en F13 solo si no altera lógica existente.

## 12. Panel derecho contextual

Ancho objetivo: **320 px**.

Frame común:
- heading del módulo;
- botón cerrar cuando aplique;
- contenido scrollable;
- acción primaria visible en parte inferior cuando exista.

Ejemplo Editar:
- Estado
- Contenido
- Fuente
- Tamaño
- Color
- Reflow seguro
- Censura
- Guardar como...

Ejemplo Imagen:
- Reemplazar
- Extraer
- Opacidad
- Rotar
- Z-order
- Eliminar

F13 construye el frame común; F15 profundiza Editar.

## 13. Ownership: mockup → código actual

| UI del mockup | Owner real |
|---|---|
| Abrir PDF | `OpenPdf_Click` / `TryOpenPdfPathAsync` |
| Abrir ZPL | `OpenZpl_Click` |
| Recientes | `RecentPdfStore` + ReaderShortcuts |
| Leer | `ReadMode_Click` |
| Editar | `EditMode_Click` / `TryEnterEditModeAsync` |
| Firmar | `SignMode_Click` |
| Comentar | `TryEnterCommentMode()` |
| Organizar | `TryEnterOrganizeMode()` |
| Formularios | `EditAcroForm_Click` |
| OCR | `Ocr_Click` |
| Censurar | owner Edit/Redaction existente |
| Comparar | `ComparePdf_Click` |
| Convertir PDF→PNG | `ExportPdfToPng_Click` |
| Proteger | `Protection_Click` |
| Firma digital | `CryptographicSign_Click` |
| QR | `QrCode_Click` |
| Batch | `BatchWatermark_Click` |
| ZPL | `OpenZpl_Click` |

El shell no implementa negocio.

## 14. Componentes WPF previstos

La spec permite estos componentes nuevos:

### `Shell/ToolDescriptor.cs`
Descriptor inmutable y pequeño.

### `Shell/ToolCatalog.cs`
Catálogo estático.

### `Shell/HomeDashboard.xaml`
Dashboard reusable.

### `Shell/HomeDashboard.xaml.cs`
Solo eventos/UI hosting; no PDF business logic.

### `MainWindow.Shell.cs`
Routing mínimo, visibility y transición Home/PDF/ZPL.

No se autoriza:
- `INavigationService` genérico;
- DI container;
- event aggregator;
- plugin framework.

## 15. Archivos existentes a reutilizar/modificar

### Shell/UI
- `src/SGPdf.App/MainWindow.xaml`
- `src/SGPdf.App/MainWindow.xaml.cs`
- `src/SGPdf.App/Themes/Modern.xaml`

### Owners
- `MainWindow.Reader*.cs`
- `MainWindow.ReaderShortcuts.cs`
- `MainWindow.Sign.cs`
- `MainWindow.Signing.cs`
- `MainWindow.Edit*.cs`
- `MainWindow.Organize*.cs`
- `MainWindow.Comments*.cs`
- `MainWindow.Forms.cs`
- `MainWindow.Ocr.cs`
- `MainWindow.Utilities.cs`
- `MainWindow.Compare.cs`
- `MainWindow.Conversions.cs`
- `MainWindow.Batch.cs`
- `MainWindow.QrCode.cs`
- `MainWindow.Labels.cs`

No mover negocio de estos owners solo por el rediseño.

## 16. Transición segura

### Etapa 1
Home + estilos, sin eliminar UI vieja.

### Etapa 2
Rail nuevo aditivo.

### Etapa 3
Tests de equivalencia:
- Read
- Edit
- Sign
- Organize
- Comment
- guards dirty-state.

### Etapa 4
Windows smoke real.

### Etapa 5
Eliminar franja centrada redundante solo después de equivalencia.

Regla de ruido:
la navegación anterior no permanece indefinidamente una vez la nueva está probada.

## 17. Estados del shell

### Home
- no PDF;
- no ZPL.

### PDF
- Home collapsed;
- PDF visible;
- rail PDF visible;
- paneles según owner.

### ZPL
- Home collapsed;
- workspace ZPL visible;
- acciones PDF no disponibles.

### Busy
No cambiar de shell de forma que pierda el workspace activo.

### Failed/cancelled open
No destruir documento previo.

## 18. Accesibilidad

Obligatorio:
- Tab/Shift+Tab;
- focus visible;
- Enter/Space;
- accessible names;
- tooltips en icon-only;
- selected state no solo por color;
- contraste light/dark.

## 19. Performance

F13 no puede:
- rerenderizar PDF por hover del shell;
- cargar todos los recientes con thumbnails pesados;
- hacer reflexión para descubrir herramientas;
- añadir procesos en background innecesarios.

Home debe sentirse instantáneo.

## 20. No objetivos

- pestañas multi-documento;
- IA;
- login;
- publicidad;
- nube;
- backend;
- Edit V3 completo;
- Compare V2;
- nuevos conversores;
- forms designer;
- publish/installer;
- migración UI framework.

## 21. QA visual obligatoria

Comparar la implementación contra el mockup aprobado en:

1. Dashboard 1920×1080/1200.
2. Dashboard 1366×768.
3. Documento abierto.
4. Editar activo.
5. Light.
6. Dark.
7. perfil Windows escalado.
8. keyboard/focus.
9. disabled states.
10. panel derecho con scroll.

No exigir igualdad pixel-perfect si rompe responsive, accesibilidad o comportamiento nativo Windows.

La métrica es:
**misma jerarquía y sensación visual + mejor comportamiento real.**

## 22. Gate de aceptación F13

F13 es GREEN solo si:

- Home sigue el mockup aprobado;
- rail izquierdo sigue el mockup aprobado;
- cards son coherentes y reales;
- canvas conserva funcionalidad;
- panel derecho es contextual;
- tool owners siguen siendo únicos;
- navegación vieja redundante fue eliminada después del gate;
- full suite PASS;
- exact-head CI PASS;
- Windows QA PASS;
- V04/V05 equivalentes reales PASS;
- ningún P0/P1 nuevo;
- icono oficial está GREEN o explícitamente bloqueado por falta de fuente aprobada.

## 23. Relación con F15

F15 Edit V3 debe implementarse **dentro de este shell**, no crear su propio shell.

El mockup del panel derecho Editar es directionally binding para:
- jerarquía;
- toolbar;
- selected state;
- Save As visible;
- propiedades contextuales.

La lógica exacta de Edit V3 sigue gobernada por:
`docs/superpowers/specs/2026-10-15-f15-edit-v3-design.md`.

## 24. Decisión final

El mockup queda aprobado como **contrato visual F13**.

Implementación:
`design tokens → Home → rail aditivo → equivalencia → workspace visual → eliminar navegación redundante → Windows visual QA`.

No se autoriza una reescritura del motor para alcanzar la apariencia.
