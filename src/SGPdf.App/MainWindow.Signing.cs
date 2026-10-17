using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SGPdf.App.Features.Signing;

namespace SGPdf.App;

public partial class MainWindow
{
    private static readonly bool CryptographicSigningLoadedHookRegistered = RegisterCryptographicSigningLoadedHook();

    private bool _cryptographicSigningUiInitialized;
    private MenuItem? _cryptographicSignMenuItem;

    private Func<string?> _selectCryptographicPfx = static () =>
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar certificado PFX/P12",
            Filter = "Certificados PFX/P12 (*.pfx;*.p12)|*.pfx;*.p12|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private Func<Window, string?> _requestCryptographicPfxPassword =
        static owner => RequestCryptographicPfxPassword(owner);

    private Func<string, string, (string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter)>
        _inspectCryptographicCertificate = static (path, password) =>
        {
            using var certificate = PfxCertificateLoader.Load(path, password, DateTimeOffset.UtcNow);
            return (certificate.Subject, certificate.Issuer, certificate.NotBefore, certificate.NotAfter);
        };

    private Func<string, string, DateTime, DateTime, bool> _confirmCryptographicCertificate =
        static (subject, issuer, notBefore, notAfter) =>
            MessageBox.Show(
                $"Firmante: {subject}\nEmisor: {issuer}\nVálido desde: {notBefore:g}\nVálido hasta: {notAfter:g}\n\n¿Deseas usar este certificado?",
                "Firma criptográfica",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information) == MessageBoxResult.Yes;

    private Func<string?> _selectCryptographicDestination = static () =>
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar PDF firmado como",
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            FileName = "firmado.pdf",
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    private Action<string, string, string, string, CancellationToken> _signCryptographicPdf =
        static (source, destination, pfx, password, token) =>
            new PdfCryptographicSigningService(beforeValidation: null)
                .Sign(source, destination, pfx, password, token);

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

        if (Content is not DockPanel)
            throw new InvalidOperationException("No se encontró el contenedor principal para firma criptográfica.");

        var fileMenu = ItemsControl.ItemsControlFromItemContainer(OpenPdfMenuItem) as MenuItem
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
        if (!CanStartCryptographicSigning() || _session is not { } sourceSession)
            return;

        string? password = null;
        try
        {
            string? pfxPath = _selectCryptographicPfx();
            if (string.IsNullOrWhiteSpace(pfxPath))
            {
                SetCryptographicSigningCancelledStatus();
                return;
            }

            password = _requestCryptographicPfxPassword(this);
            if (password is null)
            {
                SetCryptographicSigningCancelledStatus();
                return;
            }

            (string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter) certificate;
            try
            {
                certificate = _inspectCryptographicCertificate(pfxPath, password);
            }
            catch (Exception ex)
            {
                ShowCryptographicSigningFailure(
                    "No se pudo preparar la firma criptográfica. No se modificó el PDF actual.",
                    ex);
                return;
            }

            if (!_confirmCryptographicCertificate(
                    certificate.Subject,
                    certificate.Issuer,
                    certificate.NotBefore,
                    certificate.NotAfter))
            {
                SetCryptographicSigningCancelledStatus();
                return;
            }

            string? destinationPath = _selectCryptographicDestination();
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                SetCryptographicSigningCancelledStatus();
                return;
            }

            SetBusy(true);
            StatusText.Text = "Creando y validando firma criptográfica...";
            try
            {
                _signCryptographicPdf(
                    sourceSession.FilePath,
                    destinationPath,
                    pfxPath,
                    password,
                    CancellationToken.None);

                StatusText.Text = "Firma criptográfica creada y validada localmente.";
            }
            catch (OperationCanceledException)
            {
                SetCryptographicSigningCancelledStatus();
            }
            catch (Exception ex)
            {
                ShowCryptographicSigningFailure(
                    "No se pudo crear la firma criptográfica. No se modificó el PDF actual.",
                    ex);
            }
            finally
            {
                SetBusy(false);
                UpdateCryptographicSigningCommandAvailability();
            }
        }
        finally
        {
            password = null;
        }
    }

    private void SetCryptographicSigningCancelledStatus()
        => StatusText.Text = "Firma criptográfica cancelada. No se modificó el PDF actual.";

    private void ShowCryptographicSigningFailure(string status, Exception exception)
    {
        StatusText.Text = status;
        if (IsVisible)
        {
            MessageBox.Show(
                this,
                $"{status}\n\n{exception.Message}",
                "SG PDF Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string? RequestCryptographicPfxPassword(Window owner)
    {
        var passwordBox = new PasswordBox
        {
            MinWidth = 280,
            Margin = new Thickness(0, 8, 0, 12)
        };
        var okButton = new Button
        {
            Content = "Continuar",
            IsDefault = true,
            MinWidth = 90,
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 8, 0)
        };
        var cancelButton = new Button
        {
            Content = "Cancelar",
            IsCancel = true,
            MinWidth = 90,
            Padding = new Thickness(10, 4, 10, 4)
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        var content = new StackPanel { Margin = new Thickness(18) };
        content.Children.Add(new TextBlock
        {
            Text = "Escribe la contraseña del certificado PFX/P12.",
            TextWrapping = TextWrapping.Wrap
        });
        content.Children.Add(passwordBox);
        content.Children.Add(buttons);

        var dialog = new Window
        {
            Title = "Contraseña del certificado",
            Owner = owner,
            Content = content,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        okButton.Click += (_, _) => dialog.DialogResult = true;
        dialog.Loaded += (_, _) => passwordBox.Focus();

        return dialog.ShowDialog() == true ? passwordBox.Password : null;
    }
}
