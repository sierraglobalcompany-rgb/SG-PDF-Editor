using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SGPdf.App;

public partial class PdfPasswordDialog : Window
{
    public PdfPasswordDialog(string fileName)
    {
        InitializeComponent();
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "PDF" : Path.GetFileName(fileName);
        PdfPasswordMessageText.Text = $"El archivo {safeName} está protegido. Escribe la contraseña para abrirlo.";
        Loaded += (_, _) => PdfPasswordBox.Focus();
    }

    internal string? PasswordValue { get; private set; }

    internal static string? Request(Window owner, string fileName)
    {
        var dialog = new PdfPasswordDialog(fileName) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.PasswordValue : null;
    }

    private void PdfPasswordOkButton_Click(object sender, RoutedEventArgs e)
        => AcceptPassword();

    private void PdfPasswordCancelButton_Click(object sender, RoutedEventArgs e)
    {
        PasswordValue = null;
        Close();
    }

    private void PdfPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            PasswordValue = null;
            Close();
        }
    }

    private void AcceptPassword()
    {
        PasswordValue = PdfPasswordBox.Password;
        DialogResult = true;
    }
}
