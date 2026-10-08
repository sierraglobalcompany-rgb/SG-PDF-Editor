using System.ComponentModel;
using System.Windows;

namespace SGPdf.App;

public partial class SignatureNameDialog : Window
{
    private bool _accepted;

    internal SignatureNameDialog(string title, string? initialValue = null)
    {
        InitializeComponent();
        Title = string.IsNullOrWhiteSpace(title) ? "Nombre de firma" : title;
        SignatureNameTextBox.Text = initialValue ?? string.Empty;
        SignatureNameTextBox.SelectAll();

        SignatureNameAcceptButton.Click += (_, _) => Accept();
        SignatureNameCancelButton.Click += (_, _) => Cancel();
    }

    internal string? EnteredName { get; private set; }

    internal static string? Prompt(Window owner, string title, string? initialValue = null)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var dialog = new SignatureNameDialog(title, initialValue) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.EnteredName : null;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_accepted)
            EnteredName = null;
        base.OnClosing(e);
    }

    private void Accept()
    {
        EnteredName = SignatureNameTextBox.Text;
        _accepted = true;
        if (IsVisible)
            DialogResult = true;
    }

    private void Cancel()
    {
        _accepted = false;
        EnteredName = null;
        if (IsVisible)
            DialogResult = false;
    }
}
