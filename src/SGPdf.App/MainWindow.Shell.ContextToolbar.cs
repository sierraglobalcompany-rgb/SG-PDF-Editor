using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool ContextToolbarLoadedHandlerRegistered = RegisterContextToolbarLoadedHandler();

    private bool _contextToolbarUiInitialized;
    private Grid? _contextToolbarHost;

    private Grid ContextToolbarHost => _contextToolbarHost
        ?? throw new InvalidOperationException("No se encontró el host de la toolbar contextual.");

    private static bool RegisterContextToolbarLoadedHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ContextToolbarHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void ContextToolbarHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeContextToolbarHost();
    }

    private void InitializeContextToolbarHost()
    {
        _ = ContextToolbarLoadedHandlerRegistered;
        if (_contextToolbarUiInitialized)
            return;

        InitializeCommentUi();

        var workspace = PdfScrollViewer.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el workspace central para la toolbar contextual.");

        workspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        workspace.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });

        foreach (UIElement child in workspace.Children.Cast<UIElement>().ToArray())
            Grid.SetRow(child, 1);

        var host = new Grid
        {
            Name = "ContextToolbarHost"
        };
        Grid.SetRow(host, 0);
        workspace.Children.Add(host);
        RegisterName(host.Name, host);
        _contextToolbarHost = host;

        AttachCommentToolbarToContextHost();
        AttachEditToolbarToContextHost();
        AttachCommentPropertiesToContextColumn();
        AttachSignaturePropertiesToContextColumn();
        AttachEditTextPropertiesToContextColumn();
        _contextToolbarUiInitialized = true;
    }

    private void AttachCommentToolbarToContextHost()
    {
        if (_commentToolbar is null)
            return;

        if (_commentToolbar.Parent is Panel legacyParent)
            legacyParent.Children.Remove(_commentToolbar);

        _commentToolbar.ClearValue(Border.BorderThicknessProperty);
        _commentToolbar.ClearValue(Border.BorderBrushProperty);
        _commentToolbar.ClearValue(Border.BackgroundProperty);
        _commentToolbar.SetResourceReference(FrameworkElement.StyleProperty, "SgToolbarBorderStyle");

        if (_commentToolbar.Child is Panel buttons)
        {
            foreach (var button in buttons.Children.OfType<Button>())
                button.SetResourceReference(FrameworkElement.StyleProperty, "SgToolbarButtonStyle");
        }

        ContextToolbarHost.Children.Add(_commentToolbar);
    }

    private void AttachEditToolbarToContextHost()
    {
        if (_editSaveAsButton is null)
            return;

        if (_editSaveAsButton.Parent is Panel legacyParent)
            legacyParent.Children.Remove(_editSaveAsButton);

        _editSaveAsButton.SetResourceReference(FrameworkElement.StyleProperty, "SgToolbarButtonStyle");
        ContextToolbarHost.Children.Add(_editSaveAsButton);
    }

    private void AttachCommentPropertiesToContextColumn()
    {
        if (_commentPropertiesPanel is null)
            return;

        _commentPropertiesPanel.IsVisibleChanged += CommentPropertiesPanel_IsVisibleChanged;
        SyncContextPropertiesColumn();
    }

    private void CommentPropertiesPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SyncContextPropertiesColumn();
    }

    private void AttachSignaturePropertiesToContextColumn()
    {
        if (_signaturePropertiesPanel is null)
            return;

        _signaturePropertiesPanel.IsVisibleChanged += SignaturePropertiesPanel_IsVisibleChanged;
        SyncContextPropertiesColumn();
    }

    private void SignaturePropertiesPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SyncContextPropertiesColumn();
    }

    private void AttachEditTextPropertiesToContextColumn()
    {
        if (_editTextPropertiesScrollViewer is null)
            return;

        _editTextPropertiesScrollViewer.IsVisibleChanged += EditTextPropertiesScrollViewer_IsVisibleChanged;
        SyncContextPropertiesColumn();
    }

    private void EditTextPropertiesScrollViewer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SyncContextPropertiesColumn();
    }

    private void SyncContextPropertiesColumn()
    {
        var hasContext = LabelPropertiesPanel.Visibility == Visibility.Visible ||
                         _commentPropertiesPanel?.Visibility == Visibility.Visible ||
                         _signaturePropertiesPanel?.Visibility == Visibility.Visible ||
                         _editTextPropertiesScrollViewer?.Visibility == Visibility.Visible;

        ContextPropertiesColumn.Width = hasContext
            ? new GridLength(320d)
            : new GridLength(0d);
    }
}
