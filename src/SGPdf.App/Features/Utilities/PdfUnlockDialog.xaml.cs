using System.Windows;

namespace SGPdf.App.Features.Utilities;

internal sealed class PdfUnlockRequest(string password, bool authorizationConfirmed)
{
    internal string Password { get; private set; } = password;
    internal bool AuthorizationConfirmed { get; } = authorizationConfirmed;
    internal void Clear() => Password = string.Empty;
}

public partial class PdfUnlockDialog : Window
{
    internal PdfUnlockDialog()
    {
        InitializeComponent();
    }

    internal PdfUnlockRequest? Request { get; private set; }

    private void Window_Loaded(object sender, RoutedEventArgs e)
        => CurrentPasswordBox.Focus();

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        if (string.IsNullOrEmpty(CurrentPasswordBox.Password) ||
            CurrentPasswordBox.Password.Contains('\r') || CurrentPasswordBox.Password.Contains('\n'))
        {
            ValidationText.Text = "Escribe la contraseña actual del PDF.";
            return;
        }

        if (AuthorizationCheckBox.IsChecked != true)
        {
            ValidationText.Text = "Confirma que tienes autorización para crear la copia sin contraseña.";
            return;
        }

        Request = new PdfUnlockRequest(CurrentPasswordBox.Password, true);
        CurrentPasswordBox.Clear();
        DialogResult = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        CurrentPasswordBox.Clear();
        base.OnClosed(e);
    }
}
