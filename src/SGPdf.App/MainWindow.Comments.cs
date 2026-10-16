using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using SGPdf.App.Features.Comments;
using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private enum CommentSelectionGestureKind
    {
        None,
        Move,
        Resize
    }

    private sealed record CommentPaletteItem(string Name, CommentColor Color)
    {
        public override string ToString() => Name;
    }

    private sealed record CommentHitEntry(CommentKey? Key, PdfCommentInfo Info);

    private static readonly bool CommentLoadedHookRegistered = RegisterCommentLoadedHook();
    private static readonly bool CommentOpenMenuGuardRegistered = RegisterCommentOpenMenuGuard();

    private bool _commentUiInitialized;
    private bool _commentModeActive;
    private bool _commentMaterializing;
    private bool _updatingCommentProperties;
    private CommentTool _commentTool = CommentTool.Select;
    private CommentWorkspace? _commentWorkspace;
    private Border? _commentToolbar;
    private Canvas? _commentOverlayCanvas;
    private StackPanel? _commentPropertiesPanel;
    private TextBlock? _commentPropertyTypeText;
    private ComboBox? _commentColorComboBox;
    private Slider? _commentOpacitySlider;
    private TextBox? _commentNoteTextBox;
    private ComboBox? _commentBorderWidthComboBox;
    private Button? _commentDeleteButton;
    private Button? _commentUndoButton;
    private Button? _commentRedoButton;
    private Button? _commentSaveAsButton;
    private TextBox? _commentInlineNoteEditor;
    private CommentKey? _commentInlineEditingKey;
    private CommentKey? _selectedCommentKey;
    private readonly List<CommentKey> _commentKnownKeys = new();
    private readonly Dictionary<CommentKey, PdfCommentInfo> _commentSourceInfoByKey = new();
    private readonly Dictionary<int, List<PdfCommentInfo>> _commentReadOnlyByPage = new();
    private readonly HashSet<int> _commentLoadedPages = new();
    private CommentColor _commentDefaultColor = new(255, 220, 0, 180);
    private double _commentDefaultBorderWidth = 2d;

    private int _commentGesturePageIndex = -1;
    private CommentPoint? _commentGestureStartPoint;
    private List<CommentPoint>? _commentInkPreviewPoints;
    private CommentRect? _commentShapePreviewRect;

    private CommentSelectionGestureKind _commentSelectionGestureKind;
    private CommentKey? _commentSelectionGestureKey;
    private CommentPoint? _commentSelectionGestureStartPoint;
    private CommentRect? _commentSelectionGestureOriginalRect;
    private CommentRect? _commentSelectionPreviewRect;

    private Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfCommentInfo>> _getCommentsForUi =
        static (session, pageIndex, token) => session.GetCommentsOnPage(pageIndex, token);
    private Func<string?> _selectCommentPdfDestination = static () =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar PDF comentado como",
            Filter = "PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = "comentado.pdf"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };
    private Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> _inspectCommentPreflight =
        static (session, token) => new PdfCommentPreflightInspector().Inspect(session, token);
    private Func<PdfEditPreflightResult, bool> _confirmCommentWarnings = static result =>
        MessageBox.Show(
            string.Join(Environment.NewLine, result.Findings
                .Where(finding => finding.Severity == PdfEditFindingSeverity.Warning)
                .Select(finding => $"• {finding.Message}")) +
            "\n\n¿Deseas continuar con Guardar como...?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    private Func<bool> _confirmDiscardCommentChanges = static () =>
        MessageBox.Show(
            "Hay comentarios sin guardar.\n\n¿Deseas descartarlos y continuar?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    private Action<CommentWorkspace, string, bool, CancellationToken> _saveCommentCopy =
        static (workspace, destination, warningsConfirmed, token) =>
            new PdfCommentWriter().SaveAsCopy(workspace, destination, warningsConfirmed, token);

    private static bool RegisterCommentLoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(CommentHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static bool RegisterCommentOpenMenuGuard()
    {
        EventManager.RegisterClassHandler(
            typeof(MenuItem),
            MenuItem.ClickEvent,
            new RoutedEventHandler(CommentOpenMenuItem_Click),
            handledEventsToo: false);
        return true;
    }

    private static void CommentHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeCommentUi();
    }

    private static void CommentOpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || Window.GetWindow(menuItem) is not MainWindow window)
            return;
        if (!ReferenceEquals(menuItem, window.OpenPdfMenuItem) &&
            !ReferenceEquals(menuItem, window.OpenZplMenuItem))
        {
            return;
        }

        if (window._commentModeActive && !window.TryLeaveCommentModeWithGuard())
            e.Handled = true;
    }

    private void InitializeCommentUi()
    {
        if (_commentUiInitialized)
            return;

        _commentUiInitialized = true;
        _ = CommentLoadedHookRegistered;
        _ = CommentOpenMenuGuardRegistered;
        InitializeEditUi();
        InitializeReaderUi();
        InitializeReaderSelectionUi();

        var toolbarPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(8, 6, 8, 6)
        };
        AddCommentToolButton(toolbarPanel, "CommentSelectButton", "Seleccionar", CommentTool.Select);
        AddCommentToolButton(toolbarPanel, "CommentHighlightButton", "Resaltar", CommentTool.Highlight);
        AddCommentToolButton(toolbarPanel, "CommentUnderlineButton", "Subrayar", CommentTool.Underline);
        AddCommentToolButton(toolbarPanel, "CommentStrikeoutButton", "Tachado", CommentTool.Strikeout);
        AddCommentToolButton(toolbarPanel, "CommentNoteButton", "Nota", CommentTool.Note);
        AddCommentToolButton(toolbarPanel, "CommentInkButton", "Lápiz", CommentTool.Ink);
        AddCommentToolButton(toolbarPanel, "CommentRectangleButton", "Rectángulo", CommentTool.Rectangle);
        AddCommentToolButton(toolbarPanel, "CommentEllipseButton", "Óvalo", CommentTool.Ellipse);

        _commentUndoButton = AddCommentCommandButton(toolbarPanel, "CommentUndoButton", "Deshacer", (_, _) => UndoComment());
        _commentRedoButton = AddCommentCommandButton(toolbarPanel, "CommentRedoButton", "Rehacer", (_, _) => RedoComment());
        _commentSaveAsButton = AddCommentCommandButton(toolbarPanel, "CommentSaveAsButton", "Guardar como...", (_, _) => TrySaveCommentWorkspace());

        var toolbar = new Border
        {
            Name = "CommentToolbar",
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = Brushes.LightGray,
            Background = Brushes.White,
            Child = toolbarPanel,
            Visibility = Visibility.Collapsed
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        if (Content is not DockPanel root)
            throw new InvalidOperationException("No se encontró el contenedor principal para COMENTAR.");
        root.Children.Insert(Math.Min(2, root.Children.Count), toolbar);
        RegisterName(toolbar.Name, toolbar);
        _commentToolbar = toolbar;

        var propertyScroll = LabelPropertiesPanel.Parent as ScrollViewer
            ?? throw new InvalidOperationException("No se encontró el scroll de propiedades para COMENTAR.");
        var propertyGrid = propertyScroll.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el panel derecho para COMENTAR.");
        _commentPropertiesPanel = BuildCommentPropertiesPanel();
        propertyGrid.Children.Add(_commentPropertiesPanel);
        RegisterName(_commentPropertiesPanel.Name, _commentPropertiesPanel);

        if (_readerContinuousSurface is null || _readerPageList is null)
            throw new InvalidOperationException("No se encontró el visor continuo para COMENTAR.");

        var overlay = new Canvas
        {
            Name = "CommentOverlayCanvas",
            Background = null,
            Visibility = Visibility.Collapsed,
            ClipToBounds = true
        };
        Panel.SetZIndex(overlay, 25);
        _readerContinuousSurface.Children.Add(overlay);
        RegisterName(overlay.Name, overlay);
        _commentOverlayCanvas = overlay;

        _readerPageList.PreviewMouseLeftButtonDown += CommentReader_MouseLeftButtonDown;
        _readerPageList.PreviewMouseMove += CommentReader_MouseMove;
        _readerPageList.PreviewMouseLeftButtonUp += CommentReader_MouseLeftButtonUp;
        _readerPageList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler((_, _) => RefreshCommentOverlay()));
        _readerPageList.SizeChanged += (_, _) => RefreshCommentOverlay();

        if (_readModeButton is not null)
            _readModeButton.PreviewMouseLeftButtonDown += ExistingModeAfterComment_PreviewMouseLeftButtonDown;
        if (_signModeButton is not null)
            _signModeButton.PreviewMouseLeftButtonDown += ExistingModeAfterComment_PreviewMouseLeftButtonDown;
        if (_organizeModeButton is not null)
            _organizeModeButton.PreviewMouseLeftButtonDown += ExistingModeAfterComment_PreviewMouseLeftButtonDown;

        PreviewKeyDown += CommentHost_PreviewKeyDown;
        Closing += CommentWindow_Closing;
        UpdateCommentCommands();
    }

    private Button AddCommentToolButton(Panel panel, string name, string content, CommentTool tool)
    {
        var button = new Button
        {
            Name = name,
            Content = content,
            Tag = tool,
            Padding = new Thickness(9, 4, 9, 4),
            Margin = new Thickness(2)
        };
        button.Click += (_, _) => SetCommentTool(tool);
        panel.Children.Add(button);
        RegisterName(name, button);
        return button;
    }

    private Button AddCommentCommandButton(Panel panel, string name, string content, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Name = name,
            Content = content,
            Padding = new Thickness(9, 4, 9, 4),
            Margin = new Thickness(2),
            IsEnabled = false
        };
        button.Click += handler;
        panel.Children.Add(button);
        RegisterName(name, button);
        return button;
    }

    private StackPanel BuildCommentPropertiesPanel()
    {
        var panel = new StackPanel
        {
            Name = "CommentPropertiesPanel",
            Margin = new Thickness(16),
            Visibility = Visibility.Collapsed
        };
        panel.Children.Add(new TextBlock
        {
            Text = "Comentarios",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        _commentPropertyTypeText = new TextBlock
        {
            Text = "Sin selección",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(_commentPropertyTypeText);

        panel.Children.Add(new TextBlock { Text = "Color", FontWeight = FontWeights.SemiBold });
        _commentColorComboBox = new ComboBox
        {
            Name = "CommentColorComboBox",
            Margin = new Thickness(0, 5, 0, 12),
            ItemsSource = CommentPalette(),
            SelectedIndex = 0
        };
        _commentColorComboBox.SelectionChanged += CommentColor_SelectionChanged;
        panel.Children.Add(_commentColorComboBox);
        RegisterName(_commentColorComboBox.Name, _commentColorComboBox);

        panel.Children.Add(new TextBlock { Text = "Opacidad", FontWeight = FontWeights.SemiBold });
        _commentOpacitySlider = new Slider
        {
            Name = "CommentOpacitySlider",
            Minimum = 20d,
            Maximum = 100d,
            Value = 70d,
            TickFrequency = 10d,
            IsSnapToTickEnabled = true,
            Margin = new Thickness(0, 5, 0, 12)
        };
        _commentOpacitySlider.ValueChanged += CommentOpacity_ValueChanged;
        panel.Children.Add(_commentOpacitySlider);
        RegisterName(_commentOpacitySlider.Name, _commentOpacitySlider);

        var borderLabel = new TextBlock
        {
            Name = "CommentBorderWidthLabel",
            Text = "Grosor de borde",
            FontWeight = FontWeights.SemiBold,
            Visibility = Visibility.Collapsed
        };
        panel.Children.Add(borderLabel);
        RegisterName(borderLabel.Name, borderLabel);

        _commentBorderWidthComboBox = new ComboBox
        {
            Name = "CommentBorderWidthComboBox",
            ItemsSource = new[] { 1d, 2d, 3d, 5d },
            SelectedItem = 2d,
            Margin = new Thickness(0, 5, 0, 12),
            Visibility = Visibility.Collapsed
        };
        _commentBorderWidthComboBox.SelectionChanged += CommentBorderWidth_SelectionChanged;
        panel.Children.Add(_commentBorderWidthComboBox);
        RegisterName(_commentBorderWidthComboBox.Name, _commentBorderWidthComboBox);

        var noteLabel = new TextBlock
        {
            Name = "CommentNoteLabel",
            Text = "Contenido de nota",
            FontWeight = FontWeights.SemiBold,
            Visibility = Visibility.Collapsed
        };
        panel.Children.Add(noteLabel);
        RegisterName(noteLabel.Name, noteLabel);

        _commentNoteTextBox = new TextBox
        {
            Name = "CommentNoteTextBox",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 90d,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 5, 0, 12),
            Visibility = Visibility.Collapsed
        };
        _commentNoteTextBox.LostKeyboardFocus += (_, _) => CommitSelectedNoteFromProperties();
        _commentNoteTextBox.PreviewKeyDown += CommentNoteProperties_PreviewKeyDown;
        panel.Children.Add(_commentNoteTextBox);
        RegisterName(_commentNoteTextBox.Name, _commentNoteTextBox);

        _commentDeleteButton = new Button
        {
            Name = "CommentDeleteButton",
            Content = "Eliminar comentario",
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsEnabled = false
        };
        _commentDeleteButton.Click += (_, _) => DeleteSelectedComment();
        panel.Children.Add(_commentDeleteButton);
        RegisterName(_commentDeleteButton.Name, _commentDeleteButton);

        panel.Children.Add(new TextBlock
        {
            Text = "Ctrl+Z / Ctrl+Y: deshacer/rehacer · Supr: eliminar · Esc: cancelar o deseleccionar.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 14, 0, 0)
        });

        return panel;
    }

    private static IReadOnlyList<CommentPaletteItem> CommentPalette() => new[]
    {
        new CommentPaletteItem("Amarillo", new CommentColor(255, 220, 0, 255)),
        new CommentPaletteItem("Verde", new CommentColor(56, 176, 0, 255)),
        new CommentPaletteItem("Azul", new CommentColor(0, 120, 215, 255)),
        new CommentPaletteItem("Rojo", new CommentColor(220, 50, 47, 255)),
        new CommentPaletteItem("Rosa", new CommentColor(232, 87, 153, 255)),
        new CommentPaletteItem("Naranja", new CommentColor(245, 130, 32, 255)),
        new CommentPaletteItem("Negro", new CommentColor(20, 20, 20, 255))
    };

    private bool TryEnterCommentMode()
    {
        InitializeCommentUi();
        if (_session is null || _navigation is null || _isBusy)
            return false;
        if (_commentModeActive)
            return true;
        if (_editModeActive && !TryLeaveEditModeWithGuard())
            return false;
        if (_organizeModeActive && !TryLeaveOrganizeModeWithGuard())
            return false;
        if (_signatureEditState?.IsDirty == true &&
            !TryResolvePendingSignatureEdits(SignatureGuardReason.LeaveSignMode))
        {
            return false;
        }

        if (_session.OpenedWithPassword)
        {
            ShowCommentBlock("Este PDF se abrió con contraseña y no se puede comentar de forma segura en esta versión.");
            return false;
        }

        try
        {
            if (_getCryptographicSignatureCount(_session) > 0)
            {
                ShowCommentBlock("Este PDF contiene una firma criptográfica y no se puede modificar desde COMENTAR.");
                return false;
            }

            _commentWorkspace = CommentWorkspace.Create(_session.FilePath, _session.OpenedWithPassword);
            _commentModeActive = true;
            _signatureModeActive = false;
            _commentTool = CommentTool.Select;
            _selectedCommentKey = null;
            _commentKnownKeys.Clear();
            _commentSourceInfoByKey.Clear();
            _commentReadOnlyByPage.Clear();
            _commentLoadedPages.Clear();
            CancelCommentCreationGesture();
            CancelCommentSelectionGesture();

            if (_signatureOverlayCanvas is not null)
                _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
            if (_signaturePropertiesPanel is not null)
                _signaturePropertiesPanel.Visibility = Visibility.Collapsed;
            if (_organizeSurface is not null)
                _organizeSurface.Visibility = Visibility.Collapsed;
            if (_editOverlayCanvas is not null)
                _editOverlayCanvas.Visibility = Visibility.Collapsed;
            LabelPropertiesPanel.Visibility = Visibility.Collapsed;
            PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
            _commentToolbar!.Visibility = Visibility.Visible;
            _commentPropertiesPanel!.Visibility = Visibility.Visible;
            _commentOverlayCanvas!.Visibility = Visibility.Visible;
            ShowContinuousReaderSurface();

            LoadCommentPageIfNeeded(_navigation.CurrentPageIndex);
            RefreshCommentOverlay();
            RefreshCommentPropertiesPanel();
            UpdateCommentCommands();
            StatusText.Text = "COMENTAR activo. Selecciona una herramienta para crear o editar anotaciones PDF.";
            return true;
        }
        catch (Exception ex)
        {
            ResetCommentState();
            StatusText.Text = $"No se pudo iniciar COMENTAR: {ex.Message}";
            return false;
        }
    }

    private void ShowCommentBlock(string message)
    {
        StatusText.Text = message;
        if (IsVisible)
        {
            MessageBox.Show(
                this,
                message,
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SetCommentTool(CommentTool tool)
    {
        if (!_commentModeActive)
            return;

        CommitInlineNoteEditor();
        CancelCommentCreationGesture();
        CancelCommentSelectionGesture();
        _commentTool = tool;
        _selectedCommentKey = null;
        ClearReaderTextSelection();
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
        StatusText.Text = tool switch
        {
            CommentTool.Select => "Seleccionar comentarios.",
            CommentTool.Highlight => "Arrastra sobre texto para resaltar.",
            CommentTool.Underline => "Arrastra sobre texto para subrayar.",
            CommentTool.Strikeout => "Arrastra sobre texto para tachar.",
            CommentTool.Note => "Haz clic para agregar una nota.",
            CommentTool.Ink => "Arrastra para dibujar con Lápiz.",
            CommentTool.Rectangle => "Arrastra para crear un rectángulo.",
            CommentTool.Ellipse => "Arrastra para crear un óvalo.",
            _ => "COMENTAR activo."
        };
    }

    private bool ReaderSelectionCanOwnGesture()
        => !_commentModeActive || _commentTool is CommentTool.Highlight or CommentTool.Underline or CommentTool.Strikeout;

    private bool TryCreateCommentFromReaderSelection()
    {
        if (!_commentModeActive || _commentWorkspace is null || _readerTextSelection is null)
            return false;

        var subtype = _commentTool switch
        {
            CommentTool.Highlight => CommentSubtype.Highlight,
            CommentTool.Underline => CommentSubtype.Underline,
            CommentTool.Strikeout => CommentSubtype.Strikeout,
            _ => (CommentSubtype?)null
        };
        if (subtype is null)
            return false;

        var rects = _readerTextSelection.PdfRects;
        if (rects.Count == 0)
            return false;

        var state = new CommentState(
            CommentKey.New(),
            _readerTextSelection.PageIndex,
            subtype.Value,
            TextMarkupQuadBuilder.GetBounds(rects),
            _commentDefaultColor,
            TextMarkupQuadBuilder.BuildQuads(rects),
            string.Empty,
            Array.Empty<CommentStroke>(),
            null,
            isNew: true,
            deleted: false);

        CommitNewComment(state);
        ClearReaderTextSelection();
        StatusText.Text = subtype switch
        {
            CommentSubtype.Highlight => "Resaltado agregado.",
            CommentSubtype.Underline => "Subrayado agregado.",
            _ => "Tachado agregado."
        };
        return true;
    }

    private CommentKey CreateCommentNoteAtPdfPoint(int pageIndex, CommentPoint point)
    {
        if (!_commentModeActive || _commentWorkspace is null)
            throw new InvalidOperationException("COMENTAR no está activo.");

        const double size = 20d;
        var bottom = Math.Max(0d, point.Y - size);
        var top = bottom + size;
        var state = new CommentState(
            CommentKey.New(),
            pageIndex,
            CommentSubtype.Text,
            new CommentRect(point.X, bottom, point.X + size, top),
            _commentDefaultColor,
            Array.Empty<CommentQuad>(),
            string.Empty,
            Array.Empty<CommentStroke>(),
            null,
            isNew: true,
            deleted: false);

        CommitNewComment(state);
        OpenInlineNoteEditor(state.Key);
        StatusText.Text = "Nota agregada. Escribe el contenido y presiona Ctrl+Enter para confirmar.";
        return state.Key;
    }

    private bool BeginCommentCreationGesture(int pageIndex, CommentPoint point)
    {
        if (!_commentModeActive || _commentWorkspace is null ||
            _commentTool is not (CommentTool.Ink or CommentTool.Rectangle or CommentTool.Ellipse))
        {
            return false;
        }

        CancelCommentCreationGesture();
        _commentGesturePageIndex = pageIndex;
        _commentGestureStartPoint = point;
        if (_commentTool == CommentTool.Ink)
            _commentInkPreviewPoints = new List<CommentPoint> { point };
        RefreshCommentOverlay();
        return true;
    }

    private bool UpdateCommentCreationGesture(CommentPoint point)
    {
        if (_commentGestureStartPoint is null || _commentGesturePageIndex < 0)
            return false;

        if (_commentTool == CommentTool.Ink)
        {
            _commentInkPreviewPoints ??= new List<CommentPoint>();
            if (_commentInkPreviewPoints.Count == 0 || _commentInkPreviewPoints[^1] != point)
                _commentInkPreviewPoints.Add(point);
        }
        else if (_commentTool is CommentTool.Rectangle or CommentTool.Ellipse)
        {
            _commentShapePreviewRect = NormalizeCommentRect(_commentGestureStartPoint.Value, point);
        }
        else
        {
            return false;
        }

        RefreshCommentOverlay();
        return true;
    }

    private bool CompleteCommentCreationGesture(CommentPoint point)
    {
        if (_commentWorkspace is null || _commentGestureStartPoint is null || _commentGesturePageIndex < 0)
            return false;

        UpdateCommentCreationGesture(point);
        CommentState? state = null;
        if (_commentTool == CommentTool.Ink && _commentInkPreviewPoints is { Count: >= 2 } inkPoints)
        {
            state = new CommentState(
                CommentKey.New(),
                _commentGesturePageIndex,
                CommentSubtype.Ink,
                BoundsForInk(inkPoints),
                _commentDefaultColor,
                Array.Empty<CommentQuad>(),
                string.Empty,
                new[] { new CommentStroke(inkPoints) },
                null,
                isNew: true,
                deleted: false);
        }
        else if (_commentTool is CommentTool.Rectangle or CommentTool.Ellipse && _commentShapePreviewRect is CommentRect shapeRect)
        {
            state = new CommentState(
                CommentKey.New(),
                _commentGesturePageIndex,
                _commentTool == CommentTool.Rectangle ? CommentSubtype.Square : CommentSubtype.Circle,
                shapeRect,
                _commentDefaultColor,
                Array.Empty<CommentQuad>(),
                string.Empty,
                Array.Empty<CommentStroke>(),
                _commentDefaultBorderWidth,
                isNew: true,
                deleted: false);
        }

        CancelCommentCreationGesture(refresh: false);
        if (state is null)
        {
            RefreshCommentOverlay();
            return false;
        }

        CommitNewComment(state);
        return true;
    }

    private static CommentRect? NormalizeCommentRect(CommentPoint start, CommentPoint end)
    {
        var left = Math.Min(start.X, end.X);
        var right = Math.Max(start.X, end.X);
        var bottom = Math.Min(start.Y, end.Y);
        var top = Math.Max(start.Y, end.Y);
        if (right - left < 0.5d || top - bottom < 0.5d)
            return null;
        return new CommentRect(left, bottom, right, top);
    }

    private static CommentRect BoundsForInk(IReadOnlyList<CommentPoint> points)
    {
        var left = points.Min(item => item.X);
        var right = points.Max(item => item.X);
        var bottom = points.Min(item => item.Y);
        var top = points.Max(item => item.Y);
        if (right - left < 1d)
        {
            left -= 0.5d;
            right += 0.5d;
        }
        if (top - bottom < 1d)
        {
            bottom -= 0.5d;
            top += 0.5d;
        }
        return new CommentRect(left, bottom, right, top);
    }

    private void CommitNewComment(CommentState state)
    {
        var workspace = _commentWorkspace ?? throw new InvalidOperationException("COMENTAR no está activo.");
        workspace.CommitCandidate(workspace.PrepareCreate(state));
        _commentKnownKeys.Add(state.Key);
        _selectedCommentKey = state.Key;
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
    }

    private bool TryHandleCommentShortcut(Key key, ModifierKeys modifiers, DependencyObject? source)
    {
        if (!_commentModeActive || _commentWorkspace is null || IsReaderShortcutEditableSource(source))
            return false;

        var control = (modifiers & ModifierKeys.Control) != 0;
        if (control && key == Key.Z)
            return UndoComment();
        if (control && key == Key.Y)
            return RedoComment();
        if (modifiers == ModifierKeys.None && key == Key.Delete)
            return DeleteSelectedComment();
        if (modifiers == ModifierKeys.None && key == Key.Escape)
        {
            HandleCommentEscape();
            return true;
        }
        return false;
    }

    private bool UndoComment()
    {
        if (_commentWorkspace?.Undo() != true)
            return false;
        NormalizeCommentSelectionAfterHistory();
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
        return true;
    }

    private bool RedoComment()
    {
        if (_commentWorkspace?.Redo() != true)
            return false;
        NormalizeCommentSelectionAfterHistory();
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
        return true;
    }

    private bool DeleteSelectedComment()
    {
        if (_commentWorkspace is null || _selectedCommentKey is not CommentKey key)
            return false;
        try
        {
            _commentWorkspace.CommitCandidate(_commentWorkspace.PrepareDelete(key));
        }
        catch (KeyNotFoundException)
        {
            _selectedCommentKey = null;
            return false;
        }

        if (_commentInlineEditingKey == key)
            CloseInlineNoteEditor();
        _selectedCommentKey = null;
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
        return true;
    }

    private void NormalizeCommentSelectionAfterHistory()
    {
        if (_selectedCommentKey is not CommentKey key || _commentWorkspace is null)
            return;
        try
        {
            if (_commentWorkspace.GetState(key).Deleted)
                _selectedCommentKey = null;
        }
        catch (KeyNotFoundException)
        {
            _selectedCommentKey = null;
        }
        if (_selectedCommentKey is null)
            CloseInlineNoteEditor();
    }

    private void HandleCommentEscape()
    {
        if (_commentInlineNoteEditor is not null)
        {
            CancelInlineNoteEditor();
            return;
        }
        if (_commentGestureStartPoint is not null || _commentSelectionGestureKind != CommentSelectionGestureKind.None)
        {
            CancelCommentCreationGesture();
            CancelCommentSelectionGesture();
            return;
        }
        if (_selectedCommentKey is not null)
        {
            _selectedCommentKey = null;
            RefreshCommentOverlay();
            RefreshCommentPropertiesPanel();
            UpdateCommentCommands();
            return;
        }
        if (_commentTool != CommentTool.Select)
            SetCommentTool(CommentTool.Select);
    }

    private bool TrySaveCommentWorkspace()
    {
        if (!_commentModeActive || _commentMaterializing || _commentWorkspace is null || _session is null || !_commentWorkspace.IsDirty)
            return false;

        CommitInlineNoteEditor();
        var destination = _selectCommentPdfDestination();
        if (string.IsNullOrWhiteSpace(destination))
            return false;

        var workspace = _commentWorkspace;
        var session = _session;
        _commentMaterializing = true;
        UpdateCommentCommands();
        try
        {
            var preflight = _inspectCommentPreflight(session, CancellationToken.None);
            if (!preflight.CanProceed)
            {
                StatusText.Text = preflight.Findings
                    .FirstOrDefault(finding => finding.Severity == PdfEditFindingSeverity.Block)?.Message
                    ?? "El PDF no se puede guardar de forma segura desde COMENTAR.";
                return false;
            }

            var warningsConfirmed = false;
            if (preflight.RequiresWarningConfirmation)
            {
                if (!_confirmCommentWarnings(preflight))
                {
                    StatusText.Text = "Guardado cancelado. No se modificó el PDF fuente.";
                    return false;
                }
                warningsConfirmed = true;
            }

            _saveCommentCopy(workspace, destination, warningsConfirmed, CancellationToken.None);
            workspace.MarkSaved();
            StatusText.Text = $"PDF comentado guardado como {Path.GetFileName(destination)}.";
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo guardar el PDF comentado: {ex.Message}";
            return false;
        }
        finally
        {
            _commentMaterializing = false;
            UpdateCommentCommands();
        }
    }

    private bool TryLeaveCommentModeWithGuard()
    {
        if (!_commentModeActive)
            return true;
        if (_commentMaterializing)
            return false;
        if (_commentWorkspace?.IsDirty == true && !_confirmDiscardCommentChanges())
            return false;

        ResetCommentState();
        return true;
    }

    private void ResetCommentState()
    {
        CommitInlineNoteEditor(commitOnlyIfActiveMode: false);
        CancelCommentCreationGesture(refresh: false);
        CancelCommentSelectionGesture(refresh: false);
        _commentModeActive = false;
        _commentMaterializing = false;
        _commentTool = CommentTool.Select;
        _commentWorkspace = null;
        _selectedCommentKey = null;
        _commentKnownKeys.Clear();
        _commentSourceInfoByKey.Clear();
        _commentReadOnlyByPage.Clear();
        _commentLoadedPages.Clear();
        ClearReaderTextSelection();

        if (_commentOverlayCanvas is not null)
        {
            _commentOverlayCanvas.Children.Clear();
            _commentOverlayCanvas.Visibility = Visibility.Collapsed;
        }
        if (_commentToolbar is not null)
            _commentToolbar.Visibility = Visibility.Collapsed;
        if (_commentPropertiesPanel is not null)
            _commentPropertiesPanel.Visibility = Visibility.Collapsed;
        if (_session is not null && _zplDocument is null)
            PropertiesPlaceholderText.Visibility = Visibility.Visible;
        UpdateCommentCommands();
    }

    private void ExistingModeAfterComment_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_commentModeActive && !TryLeaveCommentModeWithGuard())
            e.Handled = true;
    }

    private void CommentWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!TryLeaveCommentModeWithGuard())
            e.Cancel = true;
    }

    private void CommentHost_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryHandleCommentShortcut(e.Key, Keyboard.Modifiers, e.OriginalSource as DependencyObject))
            e.Handled = true;
    }

    private void CommentReader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_commentModeActive || ReaderSelectionCanOwnGesture() || _readerPageList is null)
            return;
        if (!TryGetCommentPdfPoint(e.GetPosition(_readerPageList), out var pageIndex, out var point))
            return;

        LoadCommentPageIfNeeded(pageIndex);
        if (_commentTool == CommentTool.Note)
        {
            CreateCommentNoteAtPdfPoint(pageIndex, point);
            e.Handled = true;
            return;
        }

        if (_commentTool is CommentTool.Ink or CommentTool.Rectangle or CommentTool.Ellipse)
        {
            if (BeginCommentCreationGesture(pageIndex, point))
            {
                _readerPageList.CaptureMouse();
                e.Handled = true;
            }
            return;
        }

        if (_commentTool == CommentTool.Select)
        {
            var selected = SelectCommentAtPdfPoint(pageIndex, point);
            if (selected && e.ClickCount >= 2 && SelectedCommentSubtype() == CommentSubtype.Text)
            {
                OpenInlineNoteEditor(_selectedCommentKey!.Value);
                e.Handled = true;
                return;
            }

            if (selected && BeginCommentSelectionGesture(point))
                _readerPageList.CaptureMouse();
            e.Handled = true;
        }
    }

    private void CommentReader_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_commentModeActive || _readerPageList is null || !_readerPageList.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
            return;
        if (!TryGetCommentPdfPoint(e.GetPosition(_readerPageList), out var pageIndex, out var point))
            return;
        if (pageIndex != _commentGesturePageIndex && _commentGestureStartPoint is not null)
            return;

        var changed = _commentSelectionGestureKind != CommentSelectionGestureKind.None
            ? UpdateCommentSelectionGesture(point)
            : UpdateCommentCreationGesture(point);
        if (changed)
            e.Handled = true;
    }

    private void CommentReader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_commentModeActive || _readerPageList?.IsMouseCaptured != true)
            return;

        var changed = false;
        if (TryGetCommentPdfPoint(e.GetPosition(_readerPageList), out _, out var point))
        {
            changed = _commentSelectionGestureKind != CommentSelectionGestureKind.None
                ? CompleteCommentSelectionGesture(point)
                : CompleteCommentCreationGesture(point);
        }
        else
        {
            CancelCommentCreationGesture();
            CancelCommentSelectionGesture();
        }

        _readerPageList.ReleaseMouseCapture();
        e.Handled = changed;
    }

    private bool SelectCommentAtPdfPoint(int pageIndex, CommentPoint point)
    {
        if (_commentWorkspace is null)
            return false;

        var entries = BuildCommentHitEntries(pageIndex);
        var hit = CommentHitTester.HitTest(entries.Select(entry => entry.Info).ToArray(), point, 3d);
        if (hit is null)
        {
            _selectedCommentKey = null;
            RefreshCommentOverlay();
            RefreshCommentPropertiesPanel();
            UpdateCommentCommands();
            return false;
        }

        var matched = entries.First(entry => ReferenceEquals(entry.Info, hit));
        if (matched.Key is not CommentKey key)
        {
            _selectedCommentKey = null;
            StatusText.Text = "La anotación seleccionada se conserva como solo lectura en COMENTAR V1.";
            RefreshCommentOverlay();
            RefreshCommentPropertiesPanel();
            return false;
        }

        _selectedCommentKey = key;
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
        return true;
    }

    private bool BeginCommentSelectionGesture(CommentPoint point)
    {
        if (_commentWorkspace is null || _selectedCommentKey is not CommentKey key)
            return false;

        CommentState state;
        try { state = _commentWorkspace.GetState(key); }
        catch (KeyNotFoundException) { return false; }
        if (state.Deleted || state.Subtype is not (CommentSubtype.Text or CommentSubtype.Square or CommentSubtype.Circle))
            return false;

        _commentSelectionGestureKey = key;
        _commentSelectionGestureStartPoint = point;
        _commentSelectionGestureOriginalRect = state.Rect;
        _commentSelectionPreviewRect = state.Rect;
        _commentSelectionGestureKind = state.Subtype is CommentSubtype.Square or CommentSubtype.Circle &&
            Distance(point, new CommentPoint(state.Rect.Right, state.Rect.Top)) <= 6d
            ? CommentSelectionGestureKind.Resize
            : CommentSelectionGestureKind.Move;
        return true;
    }

    private bool UpdateCommentSelectionGesture(CommentPoint point)
    {
        if (_commentSelectionGestureKind == CommentSelectionGestureKind.None ||
            _commentSelectionGestureStartPoint is not CommentPoint start ||
            _commentSelectionGestureOriginalRect is not CommentRect original)
        {
            return false;
        }

        if (_commentSelectionGestureKind == CommentSelectionGestureKind.Move)
        {
            var dx = point.X - start.X;
            var dy = point.Y - start.Y;
            _commentSelectionPreviewRect = new CommentRect(
                original.Left + dx,
                original.Bottom + dy,
                original.Right + dx,
                original.Top + dy);
        }
        else
        {
            var right = Math.Max(original.Left + 0.5d, original.Right + point.X - start.X);
            var top = Math.Max(original.Bottom + 0.5d, original.Top + point.Y - start.Y);
            _commentSelectionPreviewRect = new CommentRect(original.Left, original.Bottom, right, top);
        }

        RefreshCommentOverlay();
        return true;
    }

    private bool CompleteCommentSelectionGesture(CommentPoint point)
    {
        if (_commentWorkspace is null || _commentSelectionGestureKey is not CommentKey key ||
            _commentSelectionGestureKind == CommentSelectionGestureKind.None)
        {
            return false;
        }

        UpdateCommentSelectionGesture(point);
        var rect = _commentSelectionPreviewRect;
        var kind = _commentSelectionGestureKind;
        CancelCommentSelectionGesture(refresh: false);
        if (rect is null)
        {
            RefreshCommentOverlay();
            return false;
        }

        var candidate = kind == CommentSelectionGestureKind.Resize
            ? _commentWorkspace.PrepareResize(key, rect.Value)
            : _commentWorkspace.PrepareMove(key, rect.Value);
        _commentWorkspace.CommitCandidate(candidate);
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
        return true;
    }

    private void CancelCommentCreationGesture(bool refresh = true)
    {
        _commentGesturePageIndex = -1;
        _commentGestureStartPoint = null;
        _commentInkPreviewPoints = null;
        _commentShapePreviewRect = null;
        if (refresh)
            RefreshCommentOverlay();
    }

    private void CancelCommentSelectionGesture(bool refresh = true)
    {
        _commentSelectionGestureKind = CommentSelectionGestureKind.None;
        _commentSelectionGestureKey = null;
        _commentSelectionGestureStartPoint = null;
        _commentSelectionGestureOriginalRect = null;
        _commentSelectionPreviewRect = null;
        if (refresh)
            RefreshCommentOverlay();
    }

    private void LoadCommentPageIfNeeded(int pageIndex)
    {
        if (_commentWorkspace is null || _session is null || _commentLoadedPages.Contains(pageIndex))
            return;

        var comments = _getCommentsForUi(_session, pageIndex, CancellationToken.None);
        var identities = CommentIdentity.CreateForPage(comments);
        var readOnly = new List<PdfCommentInfo>();
        for (var index = 0; index < comments.Count; index++)
        {
            var info = comments[index];
            if (!CanPromoteCommentForEditing(info))
            {
                readOnly.Add(info);
                continue;
            }

            var state = new CommentState(
                CommentKey.New(),
                info.PageIndex,
                info.Subtype!.Value,
                info.Rect!.Value,
                info.Color!.Value,
                info.Quads,
                info.Contents,
                info.Strokes,
                info.BorderWidth,
                isNew: false,
                deleted: false);
            _commentWorkspace.EnsureBaseline(state, identities[index]);
            _commentKnownKeys.Add(state.Key);
            _commentSourceInfoByKey[state.Key] = info;
        }

        _commentReadOnlyByPage[pageIndex] = readOnly;
        _commentLoadedPages.Add(pageIndex);
    }

    private static bool CanPromoteCommentForEditing(PdfCommentInfo info)
    {
        if (!info.IsEditable || info.Subtype is null || info.Rect is null || info.Color is null)
            return false;

        return info.Subtype.Value switch
        {
            CommentSubtype.Highlight or CommentSubtype.Underline or CommentSubtype.Strikeout => info.Quads.Count > 0,
            CommentSubtype.Ink => info.Strokes.Count > 0,
            CommentSubtype.Square or CommentSubtype.Circle => info.BorderWidth is > 0d,
            CommentSubtype.Text => true,
            _ => false
        };
    }

    private IReadOnlyList<CommentHitEntry> BuildCommentHitEntries(int pageIndex)
    {
        var entries = new List<CommentHitEntry>();
        if (_commentWorkspace is not null)
        {
            var syntheticIndex = int.MaxValue;
            foreach (var key in _commentKnownKeys)
            {
                CommentState state;
                try { state = _commentWorkspace.GetState(key); }
                catch (KeyNotFoundException) { continue; }
                if (state.Deleted || state.PageIndex != pageIndex)
                    continue;

                var index = _commentSourceInfoByKey.TryGetValue(key, out var source)
                    ? source.AnnotationIndex
                    : syntheticIndex--;
                entries.Add(new CommentHitEntry(key, SnapshotForHit(state, index)));
            }
        }

        if (_commentReadOnlyByPage.TryGetValue(pageIndex, out var readOnly))
            entries.AddRange(readOnly.Select(info => new CommentHitEntry(null, info)));
        return entries;
    }

    private static PdfCommentInfo SnapshotForHit(CommentState state, int annotationIndex)
        => new(
            state.PageIndex,
            annotationIndex,
            0,
            state.Subtype,
            true,
            state.Rect,
            state.Color,
            state.QuadPoints,
            state.Contents ?? string.Empty,
            state.InkStrokes,
            state.BorderWidth);

    private CommentSubtype? SelectedCommentSubtype()
    {
        if (_commentWorkspace is null || _selectedCommentKey is not CommentKey key)
            return null;
        try { return _commentWorkspace.GetState(key).Subtype; }
        catch (KeyNotFoundException) { return null; }
    }

    private bool TryGetCommentPdfPoint(Point viewportPoint, out int pageIndex, out CommentPoint point)
    {
        pageIndex = -1;
        point = default;
        if (_readerContinuousSurface?.Visibility != Visibility.Visible || _readerGeometry.Count == 0)
            return false;

        var viewport = GetReaderViewportSize();
        for (var index = 0; index < _readerPages.Count; index++)
        {
            var item = _readerPages[index];
            var geometry = item.Geometry;
            var pageTop = geometry.Top - _readerVerticalOffset;
            var pageLeft = Math.Max(0d, (viewport.Width - geometry.DisplayWidth) / 2d);
            if (viewportPoint.X < pageLeft || viewportPoint.X > pageLeft + geometry.DisplayWidth ||
                viewportPoint.Y < pageTop || viewportPoint.Y > pageTop + geometry.DisplayHeight ||
                item.DeviceTransform is not PdfPageDeviceTransform transform)
            {
                continue;
            }

            var deviceX = ((viewportPoint.X - pageLeft) / geometry.DisplayWidth) * transform.DeviceWidth;
            var deviceY = ((viewportPoint.Y - pageTop) / geometry.DisplayHeight) * transform.DeviceHeight;
            pageIndex = index;
            point = new CommentPoint(
                transform.OriginX + (deviceX * transform.XAxisX) + (deviceY * transform.YAxisX),
                transform.OriginY + (deviceX * transform.XAxisY) + (deviceY * transform.YAxisY));
            return true;
        }

        return false;
    }

    private bool TryPdfToCommentViewport(int pageIndex, CommentPoint pdfPoint, out Point viewportPoint)
    {
        viewportPoint = default;
        if (pageIndex < 0 || pageIndex >= _readerPages.Count)
            return false;

        var item = _readerPages[pageIndex];
        var geometry = item.Geometry;
        var viewport = GetReaderViewportSize();
        var pageLeft = Math.Max(0d, (viewport.Width - geometry.DisplayWidth) / 2d);
        var pageTop = geometry.Top - _readerVerticalOffset;
        if (item.DeviceTransform is PdfPageDeviceTransform transform)
        {
            var det = (transform.XAxisX * transform.YAxisY) - (transform.YAxisX * transform.XAxisY);
            if (Math.Abs(det) > 1e-12d)
            {
                var dx = pdfPoint.X - transform.OriginX;
                var dy = pdfPoint.Y - transform.OriginY;
                var deviceX = ((dx * transform.YAxisY) - (transform.YAxisX * dy)) / det;
                var deviceY = ((transform.XAxisX * dy) - (dx * transform.XAxisY)) / det;
                viewportPoint = new Point(
                    pageLeft + (deviceX / transform.DeviceWidth * geometry.DisplayWidth),
                    pageTop + (deviceY / transform.DeviceHeight * geometry.DisplayHeight));
                return true;
            }
        }

        if (geometry.PdfWidthPoints <= 0d || geometry.PdfHeightPoints <= 0d)
            return false;
        viewportPoint = new Point(
            pageLeft + (pdfPoint.X / geometry.PdfWidthPoints * geometry.DisplayWidth),
            pageTop + ((geometry.PdfHeightPoints - pdfPoint.Y) / geometry.PdfHeightPoints * geometry.DisplayHeight));
        return true;
    }

    private Rect? CommentRectToViewport(int pageIndex, CommentRect rect)
    {
        if (!TryPdfToCommentViewport(pageIndex, new CommentPoint(rect.Left, rect.Bottom), out var first) ||
            !TryPdfToCommentViewport(pageIndex, new CommentPoint(rect.Right, rect.Top), out var second))
        {
            return null;
        }

        var left = Math.Min(first.X, second.X);
        var top = Math.Min(first.Y, second.Y);
        var width = Math.Abs(second.X - first.X);
        var height = Math.Abs(second.Y - first.Y);
        return width > 0d && height > 0d ? new Rect(left, top, width, height) : null;
    }

    private void RefreshCommentOverlay()
    {
        if (_commentOverlayCanvas is null)
            return;

        var editor = _commentInlineNoteEditor;
        _commentOverlayCanvas.Children.Clear();
        if (!_commentModeActive || _commentWorkspace is null || _readerContinuousSurface?.Visibility != Visibility.Visible)
            return;

        foreach (var key in _commentKnownKeys)
        {
            CommentState state;
            try { state = _commentWorkspace.GetState(key); }
            catch (KeyNotFoundException) { continue; }
            if (state.Deleted)
                continue;

            var renderState = state;
            if (_commentSelectionGestureKey == key && _commentSelectionPreviewRect is CommentRect preview)
                renderState = state with { Rect = preview };
            RenderCommentState(renderState, _selectedCommentKey == key);
        }

        foreach (var pair in _commentReadOnlyByPage)
        {
            foreach (var info in pair.Value)
                RenderReadOnlyComment(info);
        }

        RenderCommentCreationPreview();
        if (editor is not null && _commentInlineEditingKey is CommentKey editingKey)
        {
            try
            {
                var state = _commentWorkspace.GetState(editingKey);
                if (TryPdfToCommentViewport(state.PageIndex, new CommentPoint(state.Rect.Right, state.Rect.Top), out var location))
                {
                    Canvas.SetLeft(editor, location.X + 6d);
                    Canvas.SetTop(editor, Math.Max(0d, location.Y - 4d));
                }
                _commentOverlayCanvas.Children.Add(editor);
            }
            catch (KeyNotFoundException)
            {
                _commentInlineNoteEditor = null;
                _commentInlineEditingKey = null;
            }
        }
    }

    private void RenderCommentState(CommentState state, bool selected)
    {
        if (_commentOverlayCanvas is null)
            return;
        var brush = BrushFor(state.Color);
        var selectedBrush = Brushes.DodgerBlue;

        if (state.Subtype is CommentSubtype.Highlight or CommentSubtype.Underline or CommentSubtype.Strikeout)
        {
            foreach (var quad in state.QuadPoints)
            {
                var bounds = QuadBounds(quad);
                var rect = CommentRectToViewport(state.PageIndex, bounds);
                if (rect is null)
                    continue;
                if (state.Subtype == CommentSubtype.Highlight)
                {
                    var visual = new Rectangle
                    {
                        Width = rect.Value.Width,
                        Height = rect.Value.Height,
                        Fill = brush,
                        Opacity = Math.Max(0.15d, state.Color.Alpha / 255d),
                        Stroke = selected ? selectedBrush : null,
                        StrokeThickness = selected ? 1d : 0d,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(visual, rect.Value.Left);
                    Canvas.SetTop(visual, rect.Value.Top);
                    _commentOverlayCanvas.Children.Add(visual);
                }
                else
                {
                    var y = state.Subtype == CommentSubtype.Underline
                        ? rect.Value.Bottom - 1d
                        : rect.Value.Top + rect.Value.Height / 2d;
                    var line = new Line
                    {
                        X1 = rect.Value.Left,
                        X2 = rect.Value.Right,
                        Y1 = y,
                        Y2 = y,
                        Stroke = selected ? selectedBrush : brush,
                        StrokeThickness = selected ? 2.5d : 2d,
                        IsHitTestVisible = false
                    };
                    _commentOverlayCanvas.Children.Add(line);
                }
            }
            return;
        }

        if (state.Subtype == CommentSubtype.Ink)
        {
            foreach (var stroke in state.InkStrokes)
            {
                var polyline = new Polyline
                {
                    Stroke = selected ? selectedBrush : brush,
                    StrokeThickness = selected ? 3d : 2d,
                    IsHitTestVisible = false
                };
                foreach (var point in stroke.Points)
                {
                    if (TryPdfToCommentViewport(state.PageIndex, point, out var viewport))
                        polyline.Points.Add(viewport);
                }
                if (polyline.Points.Count >= 2)
                    _commentOverlayCanvas.Children.Add(polyline);
            }
            return;
        }

        var deviceRect = CommentRectToViewport(state.PageIndex, state.Rect);
        if (deviceRect is null)
            return;
        if (state.Subtype == CommentSubtype.Text)
        {
            var note = new Border
            {
                Width = Math.Max(16d, deviceRect.Value.Width),
                Height = Math.Max(16d, deviceRect.Value.Height),
                Background = brush,
                BorderBrush = selected ? selectedBrush : Brushes.DimGray,
                BorderThickness = new Thickness(selected ? 2d : 1d),
                CornerRadius = new CornerRadius(2d),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = "✎",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Black,
                    IsHitTestVisible = false
                }
            };
            Canvas.SetLeft(note, deviceRect.Value.Left);
            Canvas.SetTop(note, deviceRect.Value.Top);
            _commentOverlayCanvas.Children.Add(note);
            return;
        }

        Shape shape = state.Subtype == CommentSubtype.Circle ? new Ellipse() : new Rectangle();
        shape.Width = deviceRect.Value.Width;
        shape.Height = deviceRect.Value.Height;
        shape.Fill = Brushes.Transparent;
        shape.Stroke = selected ? selectedBrush : brush;
        shape.StrokeThickness = selected ? Math.Max(2d, state.BorderWidth ?? 2d) : state.BorderWidth ?? 2d;
        shape.IsHitTestVisible = false;
        Canvas.SetLeft(shape, deviceRect.Value.Left);
        Canvas.SetTop(shape, deviceRect.Value.Top);
        _commentOverlayCanvas.Children.Add(shape);

        if (selected)
        {
            var handle = new Rectangle
            {
                Width = 8d,
                Height = 8d,
                Fill = Brushes.White,
                Stroke = selectedBrush,
                StrokeThickness = 1.5d,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(handle, deviceRect.Value.Right - 4d);
            Canvas.SetTop(handle, deviceRect.Value.Top - 4d);
            _commentOverlayCanvas.Children.Add(handle);
        }
    }

    private void RenderReadOnlyComment(PdfCommentInfo info)
    {
        if (_commentOverlayCanvas is null || info.Rect is not CommentRect rect)
            return;
        var deviceRect = CommentRectToViewport(info.PageIndex, rect);
        if (deviceRect is null)
            return;
        var outline = new Rectangle
        {
            Width = deviceRect.Value.Width,
            Height = deviceRect.Value.Height,
            Fill = Brushes.Transparent,
            Stroke = Brushes.Gray,
            StrokeThickness = 1d,
            StrokeDashArray = new DoubleCollection { 3d, 3d },
            Opacity = 0.65d,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(outline, deviceRect.Value.Left);
        Canvas.SetTop(outline, deviceRect.Value.Top);
        _commentOverlayCanvas.Children.Add(outline);
    }

    private void RenderCommentCreationPreview()
    {
        if (_commentOverlayCanvas is null || _commentGesturePageIndex < 0)
            return;
        var brush = BrushFor(_commentDefaultColor);
        if (_commentInkPreviewPoints is { Count: >= 2 } ink)
        {
            var line = new Polyline
            {
                Stroke = brush,
                StrokeThickness = 2d,
                StrokeDashArray = new DoubleCollection { 2d, 2d },
                IsHitTestVisible = false
            };
            foreach (var point in ink)
            {
                if (TryPdfToCommentViewport(_commentGesturePageIndex, point, out var viewport))
                    line.Points.Add(viewport);
            }
            if (line.Points.Count >= 2)
                _commentOverlayCanvas.Children.Add(line);
        }
        else if (_commentShapePreviewRect is CommentRect rect)
        {
            var deviceRect = CommentRectToViewport(_commentGesturePageIndex, rect);
            if (deviceRect is null)
                return;
            Shape shape = _commentTool == CommentTool.Ellipse ? new Ellipse() : new Rectangle();
            shape.Width = deviceRect.Value.Width;
            shape.Height = deviceRect.Value.Height;
            shape.Fill = Brushes.Transparent;
            shape.Stroke = brush;
            shape.StrokeThickness = _commentDefaultBorderWidth;
            shape.StrokeDashArray = new DoubleCollection { 4d, 3d };
            shape.IsHitTestVisible = false;
            Canvas.SetLeft(shape, deviceRect.Value.Left);
            Canvas.SetTop(shape, deviceRect.Value.Top);
            _commentOverlayCanvas.Children.Add(shape);
        }
    }

    private static CommentRect QuadBounds(CommentQuad quad)
        => new(
            new[] { quad.P1.X, quad.P2.X, quad.P3.X, quad.P4.X }.Min(),
            new[] { quad.P1.Y, quad.P2.Y, quad.P3.Y, quad.P4.Y }.Min(),
            new[] { quad.P1.X, quad.P2.X, quad.P3.X, quad.P4.X }.Max(),
            new[] { quad.P1.Y, quad.P2.Y, quad.P3.Y, quad.P4.Y }.Max());

    private static SolidColorBrush BrushFor(CommentColor color)
        => new(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));

    private static double Distance(CommentPoint left, CommentPoint right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private void OpenInlineNoteEditor(CommentKey key)
    {
        if (_commentWorkspace is null || _commentOverlayCanvas is null)
            return;

        CommentState state;
        try { state = _commentWorkspace.GetState(key); }
        catch (KeyNotFoundException) { return; }
        if (state.Subtype != CommentSubtype.Text || state.Deleted)
            return;

        CloseInlineNoteEditor();
        _commentInlineEditingKey = key;
        var editor = new TextBox
        {
            Width = 220d,
            MinHeight = 72d,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Text = state.Contents ?? string.Empty,
            Padding = new Thickness(6d),
            Background = Brushes.White,
            BorderBrush = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1.5d)
        };
        editor.PreviewKeyDown += InlineCommentNoteEditor_PreviewKeyDown;
        editor.LostKeyboardFocus += InlineCommentNoteEditor_LostKeyboardFocus;
        _commentInlineNoteEditor = editor;
        RefreshCommentOverlay();
        editor.Focus();
        editor.CaretIndex = editor.Text.Length;
    }

    private void InlineCommentNoteEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelInlineNoteEditor();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            CommitInlineNoteEditor();
            e.Handled = true;
        }
    }

    private void InlineCommentNoteEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        => CommitInlineNoteEditor();

    private void CommitInlineNoteEditor(bool commitOnlyIfActiveMode = true)
    {
        if (_commentInlineNoteEditor is null || _commentInlineEditingKey is not CommentKey key)
            return;
        if (commitOnlyIfActiveMode && !_commentModeActive)
        {
            CloseInlineNoteEditor();
            return;
        }

        var text = _commentInlineNoteEditor.Text;
        CloseInlineNoteEditor();
        if (_commentWorkspace is null)
            return;
        try
        {
            var state = _commentWorkspace.GetState(key);
            if (!string.Equals(state.Contents, text, StringComparison.Ordinal))
                _commentWorkspace.CommitCandidate(_commentWorkspace.PrepareNoteText(key, text));
        }
        catch (KeyNotFoundException)
        {
        }
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
    }

    private void CancelInlineNoteEditor()
    {
        if (_commentInlineEditingKey is not CommentKey key || _commentWorkspace is null)
        {
            CloseInlineNoteEditor();
            return;
        }

        var shouldDelete = false;
        try
        {
            var state = _commentWorkspace.GetState(key);
            shouldDelete = state.IsNew && string.IsNullOrWhiteSpace(state.Contents) &&
                string.IsNullOrWhiteSpace(_commentInlineNoteEditor?.Text);
        }
        catch (KeyNotFoundException)
        {
        }

        CloseInlineNoteEditor();
        if (shouldDelete)
        {
            try { _commentWorkspace.CommitCandidate(_commentWorkspace.PrepareDelete(key)); }
            catch (KeyNotFoundException) { }
            _selectedCommentKey = null;
        }
        RefreshCommentOverlay();
        RefreshCommentPropertiesPanel();
        UpdateCommentCommands();
    }

    private void CloseInlineNoteEditor()
    {
        if (_commentInlineNoteEditor is not null)
        {
            _commentInlineNoteEditor.PreviewKeyDown -= InlineCommentNoteEditor_PreviewKeyDown;
            _commentInlineNoteEditor.LostKeyboardFocus -= InlineCommentNoteEditor_LostKeyboardFocus;
            if (_commentOverlayCanvas?.Children.Contains(_commentInlineNoteEditor) == true)
                _commentOverlayCanvas.Children.Remove(_commentInlineNoteEditor);
        }
        _commentInlineNoteEditor = null;
        _commentInlineEditingKey = null;
    }

    private void CommentNoteProperties_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            CommitSelectedNoteFromProperties();
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void CommitSelectedNoteFromProperties()
    {
        if (_updatingCommentProperties || _commentWorkspace is null || _commentNoteTextBox is null || _selectedCommentKey is not CommentKey key)
            return;
        try
        {
            var state = _commentWorkspace.GetState(key);
            if (state.Subtype != CommentSubtype.Text || state.Deleted || string.Equals(state.Contents, _commentNoteTextBox.Text, StringComparison.Ordinal))
                return;
            _commentWorkspace.CommitCandidate(_commentWorkspace.PrepareNoteText(key, _commentNoteTextBox.Text));
            RefreshCommentOverlay();
            UpdateCommentCommands();
        }
        catch (KeyNotFoundException)
        {
            _selectedCommentKey = null;
        }
    }

    private void CommentColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingCommentProperties || _commentColorComboBox?.SelectedItem is not CommentPaletteItem item)
            return;
        var alpha = _selectedCommentKey is null ? _commentDefaultColor.Alpha : CurrentSelectedColor()?.Alpha ?? _commentDefaultColor.Alpha;
        ApplyCommentColor(new CommentColor(item.Color.Red, item.Color.Green, item.Color.Blue, alpha));
    }

    private void CommentOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingCommentProperties)
            return;
        var current = CurrentSelectedColor() ?? _commentDefaultColor;
        var alpha = (byte)Math.Clamp((int)Math.Round(e.NewValue / 100d * 255d), 0, 255);
        ApplyCommentColor(new CommentColor(current.Red, current.Green, current.Blue, alpha));
    }

    private CommentColor? CurrentSelectedColor()
    {
        if (_commentWorkspace is null || _selectedCommentKey is not CommentKey key)
            return null;
        try { return _commentWorkspace.GetState(key).Color; }
        catch (KeyNotFoundException) { return null; }
    }

    private void ApplyCommentColor(CommentColor color)
    {
        if (_selectedCommentKey is null || _commentWorkspace is null)
        {
            _commentDefaultColor = color;
            RefreshCommentOverlay();
            return;
        }
        try
        {
            var key = _selectedCommentKey.Value;
            var current = _commentWorkspace.GetState(key);
            if (current.Color == color)
                return;
            _commentWorkspace.CommitCandidate(_commentWorkspace.PrepareUpdate(
                CommentOperationKind.Update,
                current with { Color = color }));
            RefreshCommentOverlay();
            UpdateCommentCommands();
        }
        catch (KeyNotFoundException)
        {
            _selectedCommentKey = null;
        }
    }

    private void CommentBorderWidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingCommentProperties || _commentBorderWidthComboBox?.SelectedItem is not double width)
            return;
        _commentDefaultBorderWidth = width;
        if (_commentWorkspace is null || _selectedCommentKey is not CommentKey key)
            return;
        try
        {
            var current = _commentWorkspace.GetState(key);
            if (current.Subtype is not (CommentSubtype.Square or CommentSubtype.Circle) || current.BorderWidth == width)
                return;
            _commentWorkspace.CommitCandidate(_commentWorkspace.PrepareUpdate(
                CommentOperationKind.Update,
                current with { BorderWidth = width }));
            RefreshCommentOverlay();
            UpdateCommentCommands();
        }
        catch (KeyNotFoundException)
        {
            _selectedCommentKey = null;
        }
    }

    private void RefreshCommentPropertiesPanel()
    {
        if (_commentPropertiesPanel is null || _commentPropertyTypeText is null ||
            _commentNoteTextBox is null || _commentBorderWidthComboBox is null ||
            _commentColorComboBox is null || _commentOpacitySlider is null || _commentDeleteButton is null)
        {
            return;
        }

        _updatingCommentProperties = true;
        try
        {
            CommentState? state = null;
            if (_commentWorkspace is not null && _selectedCommentKey is CommentKey key)
            {
                try { state = _commentWorkspace.GetState(key); }
                catch (KeyNotFoundException) { _selectedCommentKey = null; }
            }

            _commentPropertyTypeText.Text = state is null
                ? $"Herramienta activa: {CommentToolLabel(_commentTool)}"
                : $"Seleccionado: {CommentSubtypeLabel(state.Subtype)}";
            _commentDeleteButton.IsEnabled = state is not null && !state.Deleted;

            var noteVisible = state?.Subtype == CommentSubtype.Text;
            _commentNoteTextBox.Visibility = noteVisible ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("CommentNoteLabel") is TextBlock noteLabel)
                noteLabel.Visibility = _commentNoteTextBox.Visibility;
            _commentNoteTextBox.Text = noteVisible ? state!.Contents ?? string.Empty : string.Empty;

            var shapeContext = state?.Subtype is CommentSubtype.Square or CommentSubtype.Circle ||
                state is null && _commentTool is CommentTool.Rectangle or CommentTool.Ellipse;
            _commentBorderWidthComboBox.Visibility = shapeContext ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("CommentBorderWidthLabel") is TextBlock borderLabel)
                borderLabel.Visibility = _commentBorderWidthComboBox.Visibility;
            if (shapeContext)
            {
                var width = state?.BorderWidth ?? _commentDefaultBorderWidth;
                _commentBorderWidthComboBox.SelectedItem = new[] { 1d, 2d, 3d, 5d }
                    .OrderBy(candidate => Math.Abs(candidate - width))
                    .First();
            }

            var color = state?.Color ?? _commentDefaultColor;
            var palette = ((IEnumerable<CommentPaletteItem>)_commentColorComboBox.ItemsSource).ToArray();
            _commentColorComboBox.SelectedItem = palette
                .OrderBy(item => ColorDistance(item.Color, color))
                .First();
            _commentOpacitySlider.Value = Math.Clamp(color.Alpha / 255d * 100d, 20d, 100d);
        }
        finally
        {
            _updatingCommentProperties = false;
        }
    }

    private static int ColorDistance(CommentColor left, CommentColor right)
        => Math.Abs(left.Red - right.Red) + Math.Abs(left.Green - right.Green) + Math.Abs(left.Blue - right.Blue);

    private static string CommentToolLabel(CommentTool tool) => tool switch
    {
        CommentTool.Select => "Seleccionar",
        CommentTool.Highlight => "Resaltar",
        CommentTool.Underline => "Subrayar",
        CommentTool.Strikeout => "Tachado",
        CommentTool.Note => "Nota",
        CommentTool.Ink => "Lápiz",
        CommentTool.Rectangle => "Rectángulo",
        CommentTool.Ellipse => "Óvalo",
        _ => tool.ToString()
    };

    private static string CommentSubtypeLabel(CommentSubtype subtype) => subtype switch
    {
        CommentSubtype.Text => "Nota",
        CommentSubtype.Square => "Rectángulo",
        CommentSubtype.Circle => "Óvalo",
        CommentSubtype.Highlight => "Resaltado",
        CommentSubtype.Underline => "Subrayado",
        CommentSubtype.Strikeout => "Tachado",
        CommentSubtype.Ink => "Lápiz",
        _ => subtype.ToString()
    };

    private void UpdateCommentCommands()
    {
        if (_commentUndoButton is not null)
            _commentUndoButton.IsEnabled = _commentModeActive && _commentWorkspace?.CanUndo == true && !_commentMaterializing;
        if (_commentRedoButton is not null)
            _commentRedoButton.IsEnabled = _commentModeActive && _commentWorkspace?.CanRedo == true && !_commentMaterializing;
        if (_commentSaveAsButton is not null)
            _commentSaveAsButton.IsEnabled = _commentModeActive && _commentWorkspace?.IsDirty == true && !_commentMaterializing;
    }
}
