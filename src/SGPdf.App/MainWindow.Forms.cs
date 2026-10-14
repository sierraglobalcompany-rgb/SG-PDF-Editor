using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SGPdf.App.Features.Forms;

namespace SGPdf.App;

public partial class MainWindow
{
    private readonly Dictionary<string, TextBox> _acroFormEditors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _acroFormOriginalValues = new(StringComparer.Ordinal);
    private MenuItem? _editAcroFormMenuItem;
    private StackPanel? _acroFormPropertiesPanel;
    private StackPanel? _acroFormFieldsPanel;
    private Button? _saveAcroFormButton;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        InitializeFormsUi();
        InitializeCompareUi();
    }

    private void InitializeFormsUi()
    {
        if (_editAcroFormMenuItem is not null)
            return;

        if (Content is not DockPanel root)
            throw new InvalidOperationException("No se encontró el contenedor principal de la ventana.");

        Menu? menu = root.Children.OfType<Menu>().FirstOrDefault();
        MenuItem? editMenu = menu?.Items
            .OfType<MenuItem>()
            .FirstOrDefault(item => string.Equals(item.Header?.ToString(), "_Editar", StringComparison.Ordinal));
        if (editMenu is null)
            throw new InvalidOperationException("No se encontró el menú Editar.");

        var editAcroFormMenuItem = new MenuItem
        {
            Name = "EditAcroFormMenuItem",
            Header = "_Campos de formulario..."
        };
        editAcroFormMenuItem.Click += EditAcroForm_Click;
        editMenu.Items.Add(editAcroFormMenuItem);

        Grid propertiesHost = PropertiesPlaceholderText.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el panel de propiedades.");

        var propertiesPanel = new StackPanel
        {
            Name = "AcroFormPropertiesPanel",
            Margin = new Thickness(16),
            Visibility = Visibility.Collapsed
        };
        propertiesPanel.Children.Add(new TextBlock
        {
            Text = "Campos de formulario",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var fieldsPanel = new StackPanel
        {
            Name = "AcroFormFieldsPanel"
        };
        propertiesPanel.Children.Add(fieldsPanel);

        var saveButton = new Button
        {
            Name = "SaveAcroFormButton",
            Content = "Guardar como...",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 14, 0, 0),
            IsEnabled = false
        };
        saveButton.Click += SaveAcroForm_Click;
        propertiesPanel.Children.Add(saveButton);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = propertiesPanel,
            Visibility = Visibility.Visible
        };
        propertiesHost.Children.Add(scrollViewer);

        _editAcroFormMenuItem = editAcroFormMenuItem;
        _acroFormPropertiesPanel = propertiesPanel;
        _acroFormFieldsPanel = fieldsPanel;
        _saveAcroFormButton = saveButton;
    }

    private void EditAcroForm_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || _acroFormPropertiesPanel is null || _acroFormFieldsPanel is null || _saveAcroFormButton is null)
        {
            StatusText.Text = "Abre un PDF para editar sus campos de formulario.";
            return;
        }

        if (_editModeActive && !TryLeaveEditModeWithGuard())
            return;
        if (_organizeModeActive && !TryLeaveOrganizeModeWithGuard())
            return;
        if (_signatureEditState?.IsDirty == true &&
            !TryResolvePendingSignatureEdits(SignatureGuardReason.LeaveSignMode))
        {
            return;
        }

        _signatureModeActive = false;
        if (_signatureOverlayCanvas is not null)
            _signatureOverlayCanvas.Visibility = Visibility.Collapsed;
        if (_signaturePropertiesPanel is not null)
            _signaturePropertiesPanel.Visibility = Visibility.Collapsed;

        try
        {
            IReadOnlyList<PdfAcroFormTextFieldInfo> fields = PdfAcroFormInspector.InspectTextFields(_session.FilePath);
            _acroFormEditors.Clear();
            _acroFormOriginalValues.Clear();
            _acroFormFieldsPanel.Children.Clear();

            foreach (PdfAcroFormTextFieldInfo field in fields)
            {
                var editor = new TextBox
                {
                    Text = field.Value,
                    Padding = new Thickness(6, 3, 6, 3),
                    Margin = new Thickness(0, 4, 0, 10)
                };
                _acroFormEditors.Add(field.Name, editor);
                _acroFormOriginalValues.Add(field.Name, field.Value);

                _acroFormFieldsPanel.Children.Add(new TextBlock
                {
                    Text = field.Name,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                });
                _acroFormFieldsPanel.Children.Add(editor);
            }

            if (fields.Count == 0)
            {
                _acroFormFieldsPanel.Children.Add(new TextBlock
                {
                    Text = "No se encontraron campos de texto AcroForm compatibles.",
                    TextWrapping = TextWrapping.Wrap
                });
            }

            LabelPropertiesPanel.Visibility = Visibility.Collapsed;
            PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
            _acroFormPropertiesPanel.Visibility = Visibility.Visible;
            _saveAcroFormButton.IsEnabled = fields.Count > 0;
            StatusText.Text = fields.Count == 0
                ? "El PDF no contiene campos de texto AcroForm compatibles."
                : $"Campos de formulario: {fields.Count}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudieron leer los campos de formulario.";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    $"No se pudieron leer los campos de formulario.\n\n{ex.Message}",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private IReadOnlyDictionary<string, string> GetChangedAcroFormValues()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string name, TextBox editor) in _acroFormEditors)
        {
            if (!_acroFormOriginalValues.TryGetValue(name, out string? originalValue) ||
                !string.Equals(editor.Text, originalValue, StringComparison.Ordinal))
            {
                values.Add(name, editor.Text);
            }
        }

        return values;
    }

    private void ResetAcroFormUi()
    {
        _acroFormEditors.Clear();
        _acroFormOriginalValues.Clear();
        _acroFormFieldsPanel?.Children.Clear();

        if (_acroFormPropertiesPanel is not null)
            _acroFormPropertiesPanel.Visibility = Visibility.Collapsed;
        if (_saveAcroFormButton is not null)
            _saveAcroFormButton.IsEnabled = false;

        if (_zplDocument is null)
        {
            LabelPropertiesPanel.Visibility = Visibility.Collapsed;
            PropertiesPlaceholderText.Visibility = Visibility.Visible;
        }
    }

    private async void SaveAcroForm_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || _acroFormEditors.Count == 0)
            return;

        IReadOnlyDictionary<string, string> values = GetChangedAcroFormValues();
        if (values.Count == 0)
        {
            StatusText.Text = "No hay cambios de formulario para guardar.";
            return;
        }

        string sourcePath = _session.FilePath;
        var dialog = new SaveFileDialog
        {
            Title = "Guardar formulario PDF como",
            Filter = "Archivos PDF (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            InitialDirectory = Path.GetDirectoryName(sourcePath),
            FileName = $"{Path.GetFileNameWithoutExtension(sourcePath)}-formulario.pdf"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        SetBusy(true);
        StatusText.Text = "Guardando formulario...";
        try
        {
            await Task.Run(() =>
                new PdfAcroFormSaveService(beforeValidation: null).Save(
                    sourcePath,
                    dialog.FileName,
                    values,
                    CancellationToken.None));
            StatusText.Text = $"Formulario guardado: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "No se pudo guardar el formulario.";
            if (IsVisible)
            {
                MessageBox.Show(
                    this,
                    $"No se pudo guardar el formulario.\n\n{ex.Message}",
                    "SG PDF Editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            SetBusy(false);
        }
    }
}
