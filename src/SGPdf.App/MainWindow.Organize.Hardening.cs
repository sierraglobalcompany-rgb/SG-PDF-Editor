using System.Windows;
using System.Windows.Input;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool OrganizeHardeningLoadedHookRegistered = RegisterOrganizeHardeningLoadedHook();

    private bool _organizeHardeningUiInitialized;
    private bool _organizePlanDirty;

    private Func<bool> _confirmDiscardOrganizeChanges = static () =>
        MessageBox.Show(
            "Hay cambios de organización sin guardar.\n\n¿Deseas descartarlos y continuar?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private static bool RegisterOrganizeHardeningLoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OrganizeHardeningHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void OrganizeHardeningHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeOrganizeHardeningUi();
    }

    private void InitializeOrganizeHardeningUi()
    {
        if (_organizeHardeningUiInitialized)
            return;

        _organizeHardeningUiInitialized = true;
        _ = OrganizeHardeningLoadedHookRegistered;
        InitializeOrganizeUi();

        if (_readModeButton is not null)
        {
            _readModeButton.PreviewMouseLeftButtonDown -= ExistingModeButton_PreviewMouseLeftButtonDown;
            _readModeButton.PreviewMouseLeftButtonDown += GuardedExistingModeButton_PreviewMouseLeftButtonDown;
        }

        if (_signModeButton is not null)
        {
            _signModeButton.PreviewMouseLeftButtonDown -= ExistingModeButton_PreviewMouseLeftButtonDown;
            _signModeButton.PreviewMouseLeftButtonDown += GuardedExistingModeButton_PreviewMouseLeftButtonDown;
        }

        OpenZplMenuItem.Click -= OpenZpl_Click;
        OpenZplMenuItem.Click += GuardedOpenZpl_Click;
    }

    private void GuardedExistingModeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_organizeModeActive && !TryLeaveOrganizeModeWithGuard())
            e.Handled = true;
    }

    private void GuardedOpenZpl_Click(object sender, RoutedEventArgs e)
    {
        if (_organizeModeActive && !TryLeaveOrganizeModeWithGuard())
        {
            e.Handled = true;
            return;
        }

        OpenZpl_Click(sender, e);
    }

    private bool TryLeaveOrganizeModeWithGuard()
    {
        if (!_organizeModeActive)
            return true;
        if (_organizeMaterializing)
            return false;
        if (_organizePlanDirty && !_confirmDiscardOrganizeChanges())
            return false;

        LeaveOrganizeMode();
        _organizePlanDirty = false;
        return true;
    }
}
