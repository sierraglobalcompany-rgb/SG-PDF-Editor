using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;

namespace SGPdf.App;

public partial class MainWindow
{
    private IReadOnlyList<PdfTextObjectInfo> _activeTextObjects = Array.Empty<PdfTextObjectInfo>();
    private TextObjectKey? _selectedTextKey;
    private ScrollViewer? _editTextPropertiesScrollViewer;
    private StackPanel? _editTextPropertiesPanel;
    private TextBox? _editTextContentTextBox;
    private TextBox? _editTextSizeTextBox;
    private TextBox? _editTextColorTextBox;
    private TextBlock? _editTextFontValueText;
    private TextBlock? _editTextStrategyValueText;
    private TextBlock? _editTextValidationText;
    private Button? _editTextApplyButton;

    private Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfTextObjectInfo>> _getTextObjects =
        static (session, pageIndex, token) => session.GetTextObjects(pageIndex, token);

    private void InitializeEditTextUi()
    {
        if (_editTextPropertiesPanel is not null)
            return;

        var propertiesHost = PropertiesPlaceholderText.Parent as Grid
            ?? throw new InvalidOperationException("No se encontró el contenedor de propiedades para Texto V1.");

        var panel = new StackPanel
        {
            Name = "EditTextPropertiesPanel",
            Margin = new Thickness(16),
            Visibility = Visibility.Visible
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Texto",
            FontWeight = FontWeights.SemiBold,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 16)
        });

        panel.Children.Add(FieldLabel("Contenido"));
        var content = new TextBox
        {
            Name = "EditTextContentTextBox",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 72,
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(content);

        panel.Children.Add(FieldLabel("Fuente"));
        var font = new TextBlock
        {
            Name = "EditTextFontValueText",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(font);

        panel.Children.Add(FieldLabel("Estrategia"));
        var strategy = new TextBlock
        {
            Name = "EditTextStrategyValueText",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(strategy);

        panel.Children.Add(FieldLabel("Tamaño (pt)"));
        var size = new TextBox
        {
            Name = "EditTextSizeTextBox",
            Padding = new Thickness(6, 3, 6, 3),
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(size);

        panel.Children.Add(FieldLabel("Color FILL (#RRGGBB)"));
        var color = new TextBox
        {
            Name = "EditTextColorTextBox",
            Padding = new Thickness(6, 3, 6, 3),
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(color);

        var apply = new Button
        {
            Name = "EditTextApplyButton",
            Content = "Aplicar",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 0, 0, 10)
        };
        apply.Click += (_, _) => TryApplySelectedTextEdit();
        panel.Children.Add(apply);

        var validation = new TextBlock
        {
            Name = "EditTextValidationText",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Firebrick
        };
        panel.Children.Add(validation);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Visibility = Visibility.Collapsed,
            Content = panel
        };
        Panel.SetZIndex(scroll, 20);
        propertiesHost.Children.Add(scroll);

        RegisterName(panel.Name, panel);
        RegisterName(content.Name, content);
        RegisterName(font.Name, font);
        RegisterName(strategy.Name, strategy);
        RegisterName(size.Name, size);
        RegisterName(color.Name, color);
        RegisterName(apply.Name, apply);
        RegisterName(validation.Name, validation);

        _editTextPropertiesScrollViewer = scroll;
        _editTextPropertiesPanel = panel;
        _editTextContentTextBox = content;
        _editTextFontValueText = font;
        _editTextStrategyValueText = strategy;
        _editTextSizeTextBox = size;
        _editTextColorTextBox = color;
        _editTextApplyButton = apply;
        _editTextValidationText = validation;
    }

    private static TextBlock FieldLabel(string text)
        => new()
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        };

    private bool SelectEditObjectAtDevicePoint(double deviceX, double deviceY)
    {
        if (!_editModeActive || _currentPdfDeviceTransform is not PdfPageDeviceTransform transform)
        {
            ClearEditSelection();
            return false;
        }

        var pdfPoint = ImageHitTester.DeviceToPdf(deviceX, deviceY, transform);
        var hit = EditObjectHitTester.HitTest(
            CurrentSelectableImageObjects(),
            CurrentSelectableTextObjects(),
            pdfPoint);

        CancelImageEditGesture();
        if (hit is null)
        {
            ClearEditSelection();
            return false;
        }

        if (hit.Value.Kind == EditObjectKind.Image)
        {
            _selectedImageKey = new ImageObjectKey(hit.Value.PageIndex, hit.Value.PageObjectIndex);
            _selectedTextKey = null;
            HideEditTextProperties(showPlaceholder: true);
        }
        else
        {
            _selectedImageKey = null;
            _selectedTextKey = new TextObjectKey(hit.Value.PageIndex, hit.Value.PageObjectIndex);
            RefreshTextEditProperties();
        }

        RefreshImageEditOverlay();
        return true;
    }

    private IReadOnlyList<PdfTextObjectInfo> CurrentSelectableTextObjects()
        => _activeTextObjects;

    private void ClearEditSelection()
    {
        CancelImageEditGesture();
        _selectedImageKey = null;
        _selectedTextKey = null;
        HideEditTextProperties(showPlaceholder: _editModeActive);
        RefreshImageEditOverlay();
    }

    private void RefreshTextEditProperties()
    {
        if (_editTextPropertiesPanel is null ||
            _editTextPropertiesScrollViewer is null ||
            _editTextContentTextBox is null ||
            _editTextFontValueText is null ||
            _editTextStrategyValueText is null ||
            _editTextSizeTextBox is null ||
            _editTextColorTextBox is null ||
            _editTextApplyButton is null ||
            _editTextValidationText is null)
        {
            return;
        }

        if (!_editModeActive || _textEditWorkspace is null || _selectedTextKey is not TextObjectKey key)
        {
            HideEditTextProperties(showPlaceholder: _editModeActive && _selectedImageKey is null);
            return;
        }

        TextEditState state;
        try
        {
            state = _textEditWorkspace.GetState(key);
        }
        catch (KeyNotFoundException)
        {
            _selectedTextKey = null;
            HideEditTextProperties(showPlaceholder: true);
            return;
        }

        LabelPropertiesPanel.Visibility = Visibility.Collapsed;
        PropertiesPlaceholderText.Visibility = Visibility.Collapsed;
        _editTextPropertiesScrollViewer.Visibility = Visibility.Visible;
        _editTextPropertiesPanel.Visibility = Visibility.Visible;

        _editTextContentTextBox.Text = state.Text;
        _editTextFontValueText.Text = state.Original.FontName;
        _editTextStrategyValueText.Text = state.FontStrategy == TextFontStrategy.OriginalFont
            ? "Fuente original"
            : "Fuente compatible";
        _editTextSizeTextBox.Text = state.FontSize.ToString("0.###", CultureInfo.InvariantCulture);
        _editTextColorTextBox.Text = FormatFillColor(state.FillColor);

        var editable = state.Original.TextRenderMode == 0;
        _editTextContentTextBox.IsReadOnly = !editable;
        _editTextSizeTextBox.IsReadOnly = !editable;
        _editTextColorTextBox.IsReadOnly = !editable;
        _editTextApplyButton.IsEnabled = editable;
        _editTextValidationText.Text = editable
            ? string.Empty
            : "Este modo de renderizado es de solo lectura en Texto V1.";
    }

    private void HideEditTextProperties(bool showPlaceholder)
    {
        if (_editTextPropertiesScrollViewer is not null)
            _editTextPropertiesScrollViewer.Visibility = Visibility.Collapsed;
        if (_editTextPropertiesPanel is not null)
            _editTextPropertiesPanel.Visibility = Visibility.Collapsed;
        if (_editTextValidationText is not null)
            _editTextValidationText.Text = string.Empty;
        if (showPlaceholder)
            PropertiesPlaceholderText.Visibility = Visibility.Visible;
    }

    private bool TryApplySelectedTextEdit()
    {
        if (!_editModeActive ||
            _textEditWorkspace is null ||
            _selectedTextKey is not TextObjectKey key ||
            _editTextContentTextBox is null ||
            _editTextSizeTextBox is null ||
            _editTextColorTextBox is null ||
            _editTextValidationText is null)
        {
            return false;
        }

        var current = _textEditWorkspace.GetState(key);
        if (!TryParseFontSize(_editTextSizeTextBox.Text, out var fontSize))
        {
            _editTextValidationText.Text = "El tamaño de fuente no es válido.";
            return false;
        }

        if (!TryParseFillColor(_editTextColorTextBox.Text, current.FillColor.Alpha, out var fillColor))
        {
            _editTextValidationText.Text = "El color debe usar formato #RRGGBB.";
            return false;
        }

        var result = _textEditWorkspace.PrepareCandidate(
            key,
            _editTextContentTextBox.Text,
            fontSize,
            fillColor);
        if (!result.IsValid || result.Candidate is null)
        {
            _editTextValidationText.Text = result.Error ?? "No se puede aplicar esta edición de texto.";
            return false;
        }

        _textEditWorkspace.CommitCandidate(result.Candidate);
        RefreshTextEditProperties();
        UpdateEditSaveCommandAvailability();
        StatusText.Text = "Edición de texto aplicada al borrador. Usa Guardar como... para crear el PDF.";
        return true;
    }

    private static bool TryParseFontSize(string text, out double fontSize)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out fontSize))
            return true;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out fontSize);
    }

    private static bool TryParseFillColor(string text, uint alpha, out PdfTextFillColor color)
    {
        color = default;
        var value = text.Trim();
        if (value.Length != 7 || value[0] != '#')
            return false;

        if (!uint.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) ||
            !uint.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) ||
            !uint.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return false;
        }

        color = new PdfTextFillColor(red, green, blue, alpha);
        return true;
    }

    private static string FormatFillColor(PdfTextFillColor color)
        => $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

    private void RenderSelectedTextOutline(PdfPageDeviceTransform transform)
    {
        if (_editOverlayCanvas is null || _selectedTextKey is not TextObjectKey key)
            return;

        var info = _activeTextObjects.FirstOrDefault(item => item.Key == key);
        if (info is null && _textEditWorkspace is not null)
        {
            try
            {
                info = _textEditWorkspace.GetState(key).Original;
            }
            catch (KeyNotFoundException)
            {
                _selectedTextKey = null;
                HideEditTextProperties(showPlaceholder: true);
                return;
            }
        }

        if (info is null)
            return;

        var quad = info.Quad;
        var points = new[]
        {
            new PdfPoint(quad.X1, quad.Y1),
            new PdfPoint(quad.X2, quad.Y2),
            new PdfPoint(quad.X3, quad.Y3),
            new PdfPoint(quad.X4, quad.Y4)
        };
        var devicePoints = points
            .Select(point => ImageHitTester.PdfToDevice(point, transform))
            .ToArray();

        var outline = new Polygon
        {
            Points = new PointCollection(devicePoints.Select(point => new Point(point.X, point.Y))),
            Fill = Brushes.Transparent,
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4d, 2d },
            IsHitTestVisible = false,
            Tag = "TextOutline"
        };
        _editOverlayCanvas.Children.Add(outline);
    }

    private bool HasUnsavedEditChanges()
        => _imageEditWorkspace?.IsDirty == true || _textEditWorkspace?.IsDirty == true;

    private void ResetTextEditState()
    {
        _activeTextObjects = Array.Empty<PdfTextObjectInfo>();
        _selectedTextKey = null;
        HideEditTextProperties(showPlaceholder: false);
    }
}
