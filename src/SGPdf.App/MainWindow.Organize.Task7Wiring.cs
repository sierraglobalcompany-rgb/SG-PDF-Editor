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
        var surface = _organizeSurface
            ?? throw new InvalidOperationException("La superficie ORGANIZAR no está inicializada.");

        insertButton.Click += (_, _) =>
        {
            TryInsertOrganizePdf();
            UpdateOrganizeTask7CommandAvailability();
        };
        mergeButton.Click += (_, _) =>
        {
            TryMergeOrganizePdf();
            UpdateOrganizeTask7CommandAvailability();
        };
        saveButton.Click += (_, _) => UpdateOrganizeTask7CommandAvailability();

        var visibilityDescriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.VisibilityProperty,
            typeof(UIElement));
        visibilityDescriptor?.AddValueChanged(surface, (_, _) => UpdateOrganizeTask7CommandAvailability());

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
