using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool EditOpenMenuGuardRegistered = RegisterEditOpenMenuGuard();

    private Func<bool> _confirmDiscardImageEditChanges = static () =>
        MessageBox.Show(
            "Hay cambios de imagen sin guardar.\n\n¿Deseas descartarlos y continuar?",
            "SG PDF Editor",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private static bool RegisterEditOpenMenuGuard()
    {
        EventManager.RegisterClassHandler(
            typeof(MenuItem),
            MenuItem.ClickEvent,
            new RoutedEventHandler(EditOpenMenuItem_Click),
            handledEventsToo: false);
        return true;
    }

    private static void EditOpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || Window.GetWindow(menuItem) is not MainWindow window)
            return;

        if (!ReferenceEquals(menuItem, window.OpenPdfMenuItem) &&
            !ReferenceEquals(menuItem, window.OpenZplMenuItem))
        {
            return;
        }

        if (window._editModeActive && !window.TryLeaveEditModeWithGuard())
            e.Handled = true;
    }

    private void WireEditGuards()
    {
        _ = EditOpenMenuGuardRegistered;

        if (_readModeButton is not null)
            _readModeButton.PreviewMouseLeftButtonDown += GuardedEditModeButton_PreviewMouseLeftButtonDown;
        if (_signModeButton is not null)
            _signModeButton.PreviewMouseLeftButtonDown += GuardedEditModeButton_PreviewMouseLeftButtonDown;
        if (_organizeModeButton is not null)
            _organizeModeButton.PreviewMouseLeftButtonDown += GuardedEditModeButton_PreviewMouseLeftButtonDown;

        Closing += EditWindow_Closing;
    }

    private void GuardedEditModeButton_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_editModeActive && !TryLeaveEditModeWithGuard())
            e.Handled = true;
    }

    private bool TryLeaveEditModeWithGuard()
    {
        if (!_editModeActive)
            return true;

        if (_imageEditWorkspace?.IsDirty == true && !_confirmDiscardImageEditChanges())
            return false;

        ResetEditState();
        return true;
    }

    private void EditWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!TryLeaveEditModeWithGuard())
            e.Cancel = true;
    }
}
