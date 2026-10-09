using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool OrganizeLoadedHookRegistered = RegisterOrganizeLoadedHook();

    private readonly PdfRenderScheduler _organizeThumbnailRenderScheduler = new();
    private readonly ObservableCollection<OrganizePageItem> _organizePages = new();
    private readonly Dictionary<Guid, IReadOnlyList<PdfPageSize>> _organizeSourcePageSizes = new();

    private OrganizePlan? _organizePlan;
    private bool _organizeModeActive;
    private bool _organizeUiInitialized;
    private bool _organizeThumbnailRefreshQueued;
    private Button? _organizeModeButton;
    private Grid? _organizeSurface;
    private ListBox? _organizePageList;

    private Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult> _inspectCurrentOrganizePreflight =
        static (session, token) => new OrganizePreflightInspector().Inspect(session, token);
    private Func<PdfDocumentSession, int, double, CancellationToken, PdfRenderedPage> _renderOrganizeThumbnail =
        static (session, pageIndex, dpi, token) => session.RenderPage(pageIndex, dpi, token);
    private Func<(int First, int Last)?> _getOrganizeRealizedRange = static () => null;

    private static bool RegisterOrganizeLoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OrganizeHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void OrganizeHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeOrganizeUi();
    }

    private void InitializeOrganizeUi()
    {
        if (_organizeUiInitialized)
            return;

        _organizeUiInitialized = true;
        _ = OrganizeLoadedHookRegistered;

        var modePanel = FindModePanel();
        var organizeModeButton = new Button
        {
            Name = "OrganizeModeButton",
            Content = "ORGANIZAR",
            Padding = new Thickness(14, 4, 14, 4),
            Margin = new Thickness(4, 0, 0, 0),
            IsEnabled = false
        };
        organizeModeButton.SetBinding(
            IsEnabledProperty,
            new Binding(nameof(IsEnabled)) { Source = PrintPdfMenuItem });
        organizeModeButton.Click += OrganizeMode_Click;
        modePanel.Children.Add(organizeModeButton);
        RegisterName(organizeModeButton.Name, organizeModeButton);
        _organizeModeButton = organizeModeButton;

        if (_readModeButton is not null)
            _readModeButton.PreviewMouseLeftButtonDown += ExistingModeButton_PreviewMouseLeftButtonDown;
        if (_signModeButton is not null)
            _signModeButton.PreviewMouseLeftButtonDown += ExistingModeButton_PreviewMouseLeftButtonDown;

        var viewerHost = PdfScrollViewer.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el contenedor central para ORGANIZAR.");

        var surface = new Grid
        {
            Name = "OrganizeSurface",
            Visibility = Visibility.Collapsed,
            Background = new SolidColorBrush(Color.FromRgb(243, 243, 243))
        };
        surface.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        surface.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });

        var toolbar = BuildOrganizeToolbar();
        Grid.SetRow(toolbar, 0);
        surface.Children.Add(toolbar);

        var list = new ListBox
        {
            Name = "OrganizePageList",
            ItemsSource = _organizePages,
            ItemTemplate = BuildOrganizePageTemplate(),
            SelectionMode = SelectionMode.Extended,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(12)
        };
        VirtualizingPanel.SetIsVirtualizing(list, true);
        VirtualizingPanel.SetVirtualizationMode(list, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(list, ScrollUnit.Pixel);
        ScrollViewer.SetCanContentScroll(list, true);
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        list.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(OrganizePageList_ScrollChanged));

        Grid.SetRow(list, 1);
        surface.Children.Add(list);
        Panel.SetZIndex(surface, 20);
        viewerHost.Children.Add(surface);

        RegisterName(surface.Name, surface);
        RegisterName(list.Name, list);
        _organizeSurface = surface;
        _organizePageList = list;

        Closing += OrganizeWindow_Closing;
    }

    private StackPanel FindModePanel()
    {
        if (_readModeButton?.Parent is StackPanel panel)
            return panel;

        if (Content is DockPanel root)
        {
            foreach (var border in root.Children.OfType<Border>())
            {
                if (border.Child is StackPanel candidate &&
                    candidate.Children.OfType<Button>().Any(button => button.Name == "ReadModeButton"))
                {
                    return candidate;
                }
            }
        }

        throw new InvalidOperationException("No se encontró la barra de modos para agregar ORGANIZAR.");
    }

    private StackPanel BuildOrganizeToolbar()
    {
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(12, 10, 12, 8)
        };

        AddOrganizeToolbarButton(toolbar, "OrganizeRotateLeftButton", "⟲", "Rotar a la izquierda");
        AddOrganizeToolbarButton(toolbar, "OrganizeRotateRightButton", "⟳", "Rotar a la derecha");
        AddOrganizeToolbarButton(toolbar, "OrganizeDeleteButton", "Eliminar", "Eliminar páginas seleccionadas");
        AddOrganizeToolbarButton(toolbar, "OrganizeDuplicateButton", "Duplicar", "Duplicar páginas seleccionadas");
        AddOrganizeToolbarButton(toolbar, "OrganizeInsertButton", "Insertar...", "Insertar páginas desde otro PDF");
        AddOrganizeToolbarButton(toolbar, "OrganizeMergeButton", "Combinar...", "Combinar con otro PDF");
        AddOrganizeToolbarButton(toolbar, "OrganizeExtractButton", "Extraer...", "Extraer páginas a otro PDF");
        AddOrganizeToolbarButton(toolbar, "OrganizeSplitButton", "Dividir...", "Dividir el documento");
        AddOrganizeToolbarButton(toolbar, "OrganizeSaveAsButton", "Guardar como...", "Materializar el plan en una copia");
        return toolbar;
    }

    private Button AddOrganizeToolbarButton(
        Panel toolbar,
        string name,
        string content,
        string toolTip)
    {
        var button = new Button
        {
            Name = name,
            Content = content,
            ToolTip = toolTip,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(3, 0, 3, 0),
            IsEnabled = false
        };
        toolbar.Children.Add(button);
        RegisterName(name, button);
        return button;
    }

    private static DataTemplate BuildOrganizePageTemplate()
    {
        var outer = new FrameworkElementFactory(typeof(Border));
        outer.SetValue(Border.MarginProperty, new Thickness(8));
        outer.SetValue(Border.PaddingProperty, new Thickness(10));
        outer.SetValue(Border.BackgroundProperty, Brushes.White);
        outer.SetValue(Border.BorderBrushProperty, Brushes.LightGray);
        outer.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        var stack = new FrameworkElementFactory(typeof(StackPanel));
        stack.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        var preview = new FrameworkElementFactory(typeof(Grid));
        preview.SetValue(FrameworkElement.WidthProperty, OrganizePageItem.ThumbnailWidthPixels);
        preview.SetBinding(
            FrameworkElement.HeightProperty,
            new Binding(nameof(OrganizePageItem.DisplayHeight)));
        preview.SetValue(Panel.BackgroundProperty, Brushes.WhiteSmoke);

        var image = new FrameworkElementFactory(typeof(Image));
        image.SetValue(Image.StretchProperty, Stretch.Fill);
        image.SetBinding(Image.SourceProperty, new Binding(nameof(OrganizePageItem.Bitmap)));
        preview.AppendChild(image);

        var error = new FrameworkElementFactory(typeof(TextBlock));
        error.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        error.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        error.SetValue(TextBlock.ForegroundProperty, Brushes.DimGray);
        error.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        error.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        error.SetValue(TextBlock.MarginProperty, new Thickness(8));
        error.SetBinding(TextBlock.TextProperty, new Binding(nameof(OrganizePageItem.ErrorMessage)));
        preview.AppendChild(error);
        stack.AppendChild(preview);

        var pageNumber = new FrameworkElementFactory(typeof(TextBlock));
        pageNumber.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pageNumber.SetValue(TextBlock.MarginProperty, new Thickness(0, 6, 0, 0));
        pageNumber.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        pageNumber.SetBinding(
            TextBlock.TextProperty,
            new Binding(nameof(OrganizePageItem.PageNumber)) { StringFormat = "Página {0}" });
        stack.AppendChild(pageNumber);

        outer.AppendChild(stack);
        return new DataTemplate(typeof(OrganizePageItem)) { VisualTree = outer };
    }

    private async void OrganizeMode_Click(object sender, RoutedEventArgs e)
    {
        if (!TryEnterOrganizeMode())
            return;

        await RefreshOrganizeThumbnailRenderWindowAsync();
    }

    private void ExistingModeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_organizeModeActive)
            LeaveOrganizeMode();
    }

    private bool TryEnterOrganizeMode()
    {
        InitializeOrganizeUi();
        if (_session is null || _navigation is null || _isBusy)
            return false;
        if (_organizeModeActive)
            return true;

        if (_signatureEditState?.IsDirty == true &&
            !TryResolvePendingSignatureEdits(SignatureGuardReason.LeaveSignMode))
        {
            return false;
        }

        OrganizePreflightResult preflight;
        try
        {
            preflight = _inspectCurrentOrganizePreflight(_session, CancellationToken.None);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo comprobar si el PDF se puede organizar: {ex.Message}";
            return false;
        }

        if (!preflight.CanProceed)
        {
            StatusText.Text = "Este PDF no se puede organizar de forma segura en esta versión.";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    "Este PDF contiene una estructura bloqueada para ORGANIZAR (por ejemplo, una firma criptográfica o protección con contraseña).",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            return false;
        }

        try
        {
            var source = OrganizeSource.Capture(_session.FilePath, _session.PageCount);
            var plan = OrganizePlan.FromPrimarySource(source);
            var sizes = _readerPageSizes.Count == source.OriginalPageCount
                ? _readerPageSizes.ToArray()
                : _session.GetPageSizes().ToArray();

            if (sizes.Length != source.OriginalPageCount)
                throw new InvalidOperationException("No se pudo obtener la geometría completa del PDF.");

            _organizeSourcePageSizes.Clear();
            _organizeSourcePageSizes[source.SourceId] = sizes;
            ApplyOrganizePlanSnapshot(plan);

            _organizeModeActive = true;
            _signatureModeActive = false;
            _resizeRenderScheduler.CancelCurrent();
            _readerRenderScheduler.CancelCurrent();
            _thumbnailRenderScheduler.CancelCurrent();

            if (_signatureOverlayCanvas is not null)
                _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
            if (_signaturePropertiesPanel is not null)
                _signaturePropertiesPanel.Visibility = Visibility.Collapsed;
            LabelPropertiesPanel.Visibility = Visibility.Collapsed;
            PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
            LabelSheetCanvas.Visibility = Visibility.Collapsed;
            LabelNavigationBar.Visibility = Visibility.Collapsed;
            NavigationBar.Visibility = Visibility.Collapsed;
            if (_readerContinuousSurface is not null)
                _readerContinuousSurface.Visibility = Visibility.Collapsed;
            PdfScrollViewer.Visibility = Visibility.Collapsed;
            PdfImage.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Collapsed;
            _organizeSurface!.Visibility = Visibility.Visible;
            UpdateReaderNavigationVisibility();

            StatusText.Text = $"Organizando {plan.Pages.Count} página(s). Los cambios aún no se han guardado.";
            QueueOrganizeThumbnailRefresh();
            return true;
        }
        catch (Exception ex)
        {
            _organizePlan = null;
            _organizeSourcePageSizes.Clear();
            _organizePages.Clear();
            StatusText.Text = $"No se pudo iniciar ORGANIZAR: {ex.Message}";
            return false;
        }
    }

    private void LeaveOrganizeMode()
    {
        _organizeThumbnailRenderScheduler.CancelCurrent();
        _organizeThumbnailRefreshQueued = false;
        _organizeModeActive = false;

        if (_organizeSurface is not null)
            _organizeSurface.Visibility = Visibility.Collapsed;

        foreach (var item in _organizePages)
            item.ReleaseBitmap();
        _organizePages.Clear();
        _organizePlan = null;
        _organizeSourcePageSizes.Clear();

        if (_session is not null && _zplDocument is null)
        {
            ShowContinuousReaderSurface();
            PropertiesPlaceholderText.Visibility = Visibility.Visible;
            UpdateViewerControlsUi();
            UpdateCurrentPageStatus();
            UpdateReaderNavigationVisibility();
        }
        else if (_zplDocument is not null)
        {
            ShowLegacySurfaceForZpl();
            UpdateViewerControlsUi();
        }
    }

    private void ApplyOrganizePlanSnapshot(OrganizePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var existing = _organizePages.ToDictionary(item => item.ItemId);
        var retainedIds = new HashSet<Guid>();
        var ordered = new List<OrganizePageItem>(plan.Pages.Count);

        for (var index = 0; index < plan.Pages.Count; index++)
        {
            var page = plan.Pages[index];
            if (!_organizeSourcePageSizes.TryGetValue(page.SourceId, out var sourceSizes) ||
                page.SourcePageIndex < 0 || page.SourcePageIndex >= sourceSizes.Count)
            {
                throw new InvalidOperationException("Falta la geometría del origen de una página del plan.");
            }

            OrganizePageItem item;
            if (existing.TryGetValue(page.ItemId, out var candidate) && candidate.MatchesLogicalPage(page))
            {
                item = candidate;
                item.UpdatePlanState(page, index + 1);
            }
            else
            {
                item = new OrganizePageItem(page, index + 1, sourceSizes[page.SourcePageIndex]);
            }

            retainedIds.Add(item.ItemId);
            ordered.Add(item);
        }

        foreach (var item in _organizePages)
        {
            if (!retainedIds.Contains(item.ItemId))
                item.ReleaseBitmap();
        }

        _organizePages.Clear();
        foreach (var item in ordered)
            _organizePages.Add(item);

        _organizePlan = plan;
        if (_organizeModeActive)
            QueueOrganizeThumbnailRefresh();
    }

    private void OrganizePageList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => QueueOrganizeThumbnailRefresh();

    private void QueueOrganizeThumbnailRefresh()
    {
        if (!_organizeUiInitialized || !_organizeModeActive || _organizeThumbnailRefreshQueued)
            return;

        _organizeThumbnailRefreshQueued = true;
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            new Action(async () =>
            {
                _organizeThumbnailRefreshQueued = false;
                if (!_organizeModeActive || _session is null)
                    return;

                try
                {
                    await RefreshOrganizeThumbnailRenderWindowAsync();
                }
                catch (ObjectDisposedException)
                {
                    // Window shutdown raced a queued low-priority organize refresh.
                }
            }));
    }

    private async Task RefreshOrganizeThumbnailRenderWindowAsync()
    {
        if (!_organizeModeActive || _session is null || _organizePlan is null || _organizePages.Count == 0)
            return;

        var sourceSession = _session;
        var sourcePlan = _organizePlan;
        var request = _organizeThumbnailRenderScheduler.Begin();
        var requestedRange = _getOrganizeRealizedRange() ?? GetRealizedOrganizeRange();
        if (requestedRange is null)
            return;

        var first = Math.Clamp(requestedRange.Value.First, 0, _organizePages.Count - 1);
        var last = Math.Clamp(requestedRange.Value.Last, 0, _organizePages.Count - 1);
        if (first > last)
            (first, last) = (last, first);

        var retainedFirst = Math.Max(0, first - 1);
        var retainedLast = Math.Min(_organizePages.Count - 1, last + 1);
        for (var index = 0; index < _organizePages.Count; index++)
        {
            if (index < retainedFirst || index > retainedLast)
                _organizePages[index].ReleaseBitmap();
        }

        var priority = new List<int>(last - first + 3);
        for (var index = first; index <= last; index++)
            priority.Add(index);
        if (first > 0)
            priority.Add(first - 1);
        if (last + 1 < _organizePages.Count)
            priority.Add(last + 1);

        foreach (var itemIndex in priority)
        {
            if (!IsCurrentOrganizeRequest(request, sourceSession, sourcePlan))
                return;

            var item = _organizePages[itemIndex];
            if (item.Bitmap is not null || item.HasError)
                continue;

            var itemId = item.ItemId;
            var sourceId = item.SourceId;
            var sourcePageIndex = item.SourcePageIndex;
            var rotation = item.RotationDeltaQuarterTurns;
            item.MarkLoading();

            try
            {
                var dpi = item.ResolveThumbnailDpi();
                var rendered = await Task.Run(() =>
                    RenderOrganizeThumbnailFromSource(
                        sourceSession,
                        sourcePlan,
                        sourceId,
                        sourcePageIndex,
                        dpi,
                        request.CancellationToken));

                if (!IsCurrentOrganizeItem(request, sourceSession, sourcePlan, item, itemId, sourceId, sourcePageIndex, rotation))
                    return;

                var bitmap = CreateBitmapSource(rendered);
                bitmap = ApplyOrganizeDisplayRotation(bitmap, rotation);

                if (!IsCurrentOrganizeItem(request, sourceSession, sourcePlan, item, itemId, sourceId, sourcePageIndex, rotation))
                    return;

                item.Publish(bitmap);
            }
            catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (IsCurrentOrganizeItem(request, sourceSession, sourcePlan, item, itemId, sourceId, sourcePageIndex, rotation))
                    item.MarkError(ex.Message);
            }
        }
    }

    private PdfRenderedPage RenderOrganizeThumbnailFromSource(
        PdfDocumentSession primarySession,
        OrganizePlan plan,
        Guid sourceId,
        int sourcePageIndex,
        double dpi,
        CancellationToken cancellationToken)
    {
        if (sourceId == plan.Sources[0].SourceId)
            return _renderOrganizeThumbnail(primarySession, sourcePageIndex, dpi, cancellationToken);

        var source = plan.Sources.SingleOrDefault(candidate => candidate.SourceId == sourceId)
            ?? throw new InvalidOperationException("No se encontró el origen de la miniatura en el plan actual.");

        using var secondarySession = PdfDocumentSession.Open(source.Path);
        return _renderOrganizeThumbnail(secondarySession, sourcePageIndex, dpi, cancellationToken);
    }

    private bool IsCurrentOrganizeRequest(
        PdfRenderRequest request,
        PdfDocumentSession sourceSession,
        OrganizePlan sourcePlan)
        => _organizeModeActive &&
           _organizeThumbnailRenderScheduler.IsCurrent(request) &&
           ReferenceEquals(_session, sourceSession) &&
           ReferenceEquals(_organizePlan, sourcePlan);

    private bool IsCurrentOrganizeItem(
        PdfRenderRequest request,
        PdfDocumentSession sourceSession,
        OrganizePlan sourcePlan,
        OrganizePageItem item,
        Guid itemId,
        Guid sourceId,
        int sourcePageIndex,
        int rotation)
        => IsCurrentOrganizeRequest(request, sourceSession, sourcePlan) &&
           _organizePages.Contains(item) &&
           item.ItemId == itemId &&
           item.SourceId == sourceId &&
           item.SourcePageIndex == sourcePageIndex &&
           item.RotationDeltaQuarterTurns == rotation;

    private static BitmapSource ApplyOrganizeDisplayRotation(BitmapSource bitmap, int rotationQuarterTurns)
    {
        var normalized = ((rotationQuarterTurns % 4) + 4) % 4;
        if (normalized == 0)
            return bitmap;

        var transformed = new TransformedBitmap(bitmap, new RotateTransform(normalized * 90d));
        transformed.Freeze();
        return transformed;
    }

    private (int First, int Last)? GetRealizedOrganizeRange()
    {
        if (_organizePageList is null || _organizePages.Count == 0)
            return null;

        _organizePageList.ApplyTemplate();
        var panel = FindVisualDescendant<VirtualizingStackPanel>(_organizePageList);
        if (panel is not null)
        {
            var first = int.MaxValue;
            var last = -1;
            foreach (UIElement child in panel.Children)
            {
                var index = _organizePageList.ItemContainerGenerator.IndexFromContainer(child);
                if (index < 0)
                    continue;
                first = Math.Min(first, index);
                last = Math.Max(last, index);
            }

            if (last >= 0)
                return (first, last);
        }

        return (0, 0);
    }

    private void OrganizeWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (e.Cancel)
            return;

        _organizeThumbnailRenderScheduler.Dispose();
        foreach (var item in _organizePages)
            item.ReleaseBitmap();
        _organizePages.Clear();
        _organizePlan = null;
        _organizeSourcePageSizes.Clear();
    }
}
