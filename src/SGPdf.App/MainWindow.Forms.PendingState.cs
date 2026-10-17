using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App;

public partial class MainWindow
{
    private IReadOnlyDictionary<string, string> CaptureAcroFormCurrentValues()
        => _acroFormEditors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Text,
            StringComparer.Ordinal);

    private void DetachAcroFormUiForDocumentSwitch()
    {
        _acroFormEditors.Clear();
        _acroFormOriginalValues.Clear();
        _acroFormFieldsPanel?.Children.Clear();

        if (_acroFormPropertiesPanel is not null)
            _acroFormPropertiesPanel.Visibility = Visibility.Collapsed;
        if (_acroFormPropertiesScrollViewer is not null)
            _acroFormPropertiesScrollViewer.Visibility = Visibility.Collapsed;
        if (_saveAcroFormButton is not null)
            _saveAcroFormButton.IsEnabled = false;
    }

    private void RestoreAcroFormPendingState(
        IReadOnlyDictionary<string, string> originalValues,
        IReadOnlyDictionary<string, string> currentValues)
    {
        ArgumentNullException.ThrowIfNull(originalValues);
        ArgumentNullException.ThrowIfNull(currentValues);
        InitializeFormsUi();

        if (_acroFormFieldsPanel is null ||
            _acroFormPropertiesPanel is null ||
            _acroFormPropertiesScrollViewer is null ||
            _saveAcroFormButton is null)
        {
            return;
        }

        _acroFormEditors.Clear();
        _acroFormOriginalValues.Clear();
        _acroFormFieldsPanel.Children.Clear();

        foreach (var pair in currentValues.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var original = originalValues.TryGetValue(pair.Key, out var originalValue)
                ? originalValue
                : pair.Value;
            _acroFormOriginalValues[pair.Key] = original;

            var editor = new TextBox
            {
                Text = pair.Value,
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 4, 0, 10)
            };
            _acroFormEditors[pair.Key] = editor;

            _acroFormFieldsPanel.Children.Add(new TextBlock
            {
                Text = pair.Key,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            _acroFormFieldsPanel.Children.Add(editor);
        }

        if (currentValues.Count == 0)
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
        _acroFormPropertiesScrollViewer.Visibility = Visibility.Visible;
        _saveAcroFormButton.IsEnabled = currentValues.Count > 0;
    }
}
