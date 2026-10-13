using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool CryptographicSigningLoadedHookRegistered = RegisterCryptographicSigningLoadedHook();

    private bool _cryptographicSigningUiInitialized;
    private MenuItem? _cryptographicSignMenuItem;

    private static bool RegisterCryptographicSigningLoadedHook()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(CryptographicSigningHost_Loaded),
            handledEventsToo: true);
        return true;
    }

    private static void CryptographicSigningHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
            window.InitializeCryptographicSigningUi();
    }

    private void InitializeCryptographicSigningUi()
    {
        if (_cryptographicSigningUiInitialized)
            return;

        _cryptographicSigningUiInitialized = true;
        _ = CryptographicSigningLoadedHookRegistered;

        if (Content is not DockPanel root)
            throw new InvalidOperationException("No se encontró el contenedor principal para firma criptográfica.");

        var menu = root.Children.OfType<Menu>().FirstOrDefault()
            ?? throw new InvalidOperationException("No se encontró el menú principal para firma criptográfica.");
        var fileMenu = menu.Items.OfType<MenuItem>().FirstOrDefault()
            ?? throw new InvalidOperationException("No se encontró el menú Archivo para firma criptográfica.");

        var command = new MenuItem
        {
            Name = "CryptographicSignMenuItem",
            Header = "Firmar digitalmente",
            IsEnabled = false
        };
        command.Click += CryptographicSign_Click;
        fileMenu.SubmenuOpened += CryptographicSigningFileMenu_SubmenuOpened;

        var printIndex = fileMenu.Items.IndexOf(PrintPdfMenuItem);
        fileMenu.Items.Insert(printIndex >= 0 ? printIndex + 1 : fileMenu.Items.Count, command);
        RegisterName(command.Name, command);
        _cryptographicSignMenuItem = command;
        UpdateCryptographicSigningCommandAvailability();
    }

    private void CryptographicSigningFileMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        => UpdateCryptographicSigningCommandAvailability();

    private void UpdateCryptographicSigningCommandAvailability()
    {
        if (_cryptographicSignMenuItem is null)
            return;

        _cryptographicSignMenuItem.IsEnabled = CanStartCryptographicSigning();
    }

    private bool CanStartCryptographicSigning()
        => _session is not null &&
           _navigation is not null &&
           !_isBusy &&
           !_editModeActive &&
           !_organizeModeActive &&
           !_signatureModeActive;

    private void CryptographicSign_Click(object sender, RoutedEventArgs e)
    {
        if (!CanStartCryptographicSigning())
            return;
    }
}
