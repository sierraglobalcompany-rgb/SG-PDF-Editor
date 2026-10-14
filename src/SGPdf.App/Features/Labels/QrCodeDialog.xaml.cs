using System.Windows;

namespace SGPdf.App.Features.Labels;

public partial class QrCodeDialog : Window
{
    public QrCodeDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => ValueTextBox.Focus();
    }

    public string? Value { get; private set; }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ValueTextBox.Text))
        {
            ValidationText.Text = "Escribe un texto o una URL para generar el código QR.";
            return;
        }

        Value = ValueTextBox.Text;
        DialogResult = true;
    }
}
