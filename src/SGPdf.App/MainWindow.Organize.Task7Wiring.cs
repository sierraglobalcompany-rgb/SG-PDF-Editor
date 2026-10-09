using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool OrganizeTask7LoadedHookRegistered = RegisterOrganizeTask7LoadedHook();
    private bool _organizeTask7UiInitialized;

    private static bool RegisterOrganizeTask7LoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OrganizeTask7Host_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void OrganizeTask7Host_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        window.InitializeOrganizeUi();
        window.InitializeOrganizeTask6Ui();
        window.InitializeOrganizeTask7Ui();
    }

    private void InitializeOrganizeTask7Ui()
    {
        if (_organizeTask7UiInitialized)
            return;

        _organizeTask7UiInitialized = true;
        _ = OrganizeTask7LoadedHookRegistered;

        var insertButton = Button("OrganizeInsertButton");
        var mergeButton = Button("OrganizeMergeButton");
        var saveButton = Button("OrganizeSaveAsButton");

        insertButton.Click += (_, _) => TryInsertOrganizePdf();
        mergeButton.Click += (_, _) => TryMergeOrganizePdf();

        var enabledDescriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.IsEnabledProperty,
            typeof(UIElement));
        enabledDescriptor?.AddValueChanged(saveButton, (_, _) => UpdateOrganizeTask7CommandAvailability());

        if (_organizeSurface is not null)
            _organizeSurface.IsVisibleChanged += (_, _) => UpdateOrganizeTask7CommandAvailability();

        UpdateOrganizeTask7CommandAvailability();

        Button Button(string name)
            => FindName(name) as Button
               ?? throw new InvalidOperationException($"No se encontró el comando {name}.");
    }

    private void UpdateOrganizeTask7CommandAvailability()
    {
        if (!_organizeTask7UiInitialized)
            return;

        var enabled = _organizeModeActive && !_organizeMaterializing && _organizePlan is not null;
        if (FindName("OrganizeInsertButton") is Button insertButton)
            insertButton.IsEnabled = enabled;
        if (FindName("OrganizeMergeButton") is Button mergeButton)
            mergeButton.IsEnabled = enabled;
    }
}
