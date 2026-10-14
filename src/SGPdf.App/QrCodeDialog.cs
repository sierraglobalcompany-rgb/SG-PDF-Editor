using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App;

public sealed class QrCodeDialog : Window
{
    private readonly TextBox _valueBox;
    private readonly TextBlock _validationText;

    public QrCodeDialog()
    {
        Title = "Generar código QR";
        Width = 460;
        Height = 230;
        MinWidth = 380;
        MinHeight = 210;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResizeWithGrip;

        var panel = new StackPanel
        {
            Margin = new Thickness(18)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Contenido del código QR:",
            Margin = new Thickness(0, 0, 0, 8)
        });

        _valueBox = new TextBox
        {
            MinHeight = 72,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        panel.Children.Add(_valueBox);

        _validationText = new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(_validationText);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };

        var cancel = new Button
        {
            Content = "Cancelar",
            MinWidth = 88,
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        var generate = new Button
        {
            Content = "Guardar PNG",
            MinWidth = 100,
            IsDefault = true
        };
        generate.Click += Generate_Click;

        buttons.Children.Add(cancel);
        buttons.Children.Add(generate);
        panel.Children.Add(buttons);
        Content = panel;

        Loaded += (_, _) => _valueBox.Focus();
    }

    public string Value { get; private set; } = string.Empty;

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        string value = _valueBox.Text;
        if (string.IsNullOrWhiteSpace(value))
        {
            _validationText.Text = "Escribe el contenido que quieres convertir en QR.";
            _valueBox.Focus();
            return;
        }

        Value = value;
        DialogResult = true;
    }
}
