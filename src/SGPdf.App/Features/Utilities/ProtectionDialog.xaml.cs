using System.Windows;

namespace SGPdf.App.Features.Utilities;

public partial class ProtectionDialog : Window
{
    private readonly bool _sourceOpenedWithPassword;

    internal ProtectionDialog(bool sourceOpenedWithPassword)
    {
        _sourceOpenedWithPassword = sourceOpenedWithPassword;
        InitializeComponent();
        CurrentPasswordPanel.Visibility = sourceOpenedWithPassword
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    internal PdfProtectionRequest? Request { get; private set; }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;

        var request = new PdfProtectionRequest(
            OpenPasswordBox.Password,
            ConfirmPasswordBox.Password,
            _sourceOpenedWithPassword ? CurrentPasswordBox.Password : null,
            AuthorizationCheckBox.IsChecked == true);

        if (!request.TryBuild(_sourceOpenedWithPassword, out _, out var error))
        {
            ValidationText.Text = error ?? "Los datos de protección no son válidos.";
            return;
        }

        Request = request;
        DialogResult = true;
    }
}
