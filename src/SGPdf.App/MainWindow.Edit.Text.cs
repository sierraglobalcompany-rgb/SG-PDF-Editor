using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Features.Edit.Text.Layout;
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
    private TextBlock? _editTextRedactionStateText;
    private Button? _editTextRedactButton;
    private RedactionWorkspace? _redactionWorkspace;
    private Button? _editTextApplyButton;
    private TextBox? _editTextReflowTextBox;
    private TextBlock? _editTextReflowStateText;
    private Button? _editTextReflowApplyButton;
    private TextLayoutPage? _selectedTextLayoutPage;
    private TextParagraph? _selectedTextParagraph;
    private PdfEditPreflightResult? _selectedTextReflowPreflight;

    private Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfTextObjectInfo>> _getTextObjects =
        static (session, pageIndex, token) => session.GetTextObjects(pageIndex, token);

    private Func<PdfDocumentSession, int, CancellationToken, TextLayoutPage> _analyzeTextLayout =
        static (session, pageIndex, token) => session.AnalyzeTextLayout(pageIndex, token);

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
            Foreground = Brushes.Firebrick,
            Margin = new Thickness(0, 0, 0, 16)
        };
        panel.Children.Add(validation);

        var redactionState = new TextBlock
        {
            Name = "EditTextRedactionStateText",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 8)
        };
        panel.Children.Add(redactionState);

        var redact = new Button
        {
            Name = "EditTextRedactButton",
            Content = "Redactar",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 0, 0, 16),
            IsEnabled = false
        };
        redact.Click += (_, _) => TryStageSelectedTextRedaction();
        panel.Children.Add(redact);

        panel.Children.Add(new Separator
        {
            Margin = new Thickness(0, 0, 0, 14)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Reflow seguro (Texto V2)",
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var reflowState = new TextBlock
        {
            Name = "EditTextReflowStateText",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(reflowState);

        panel.Children.Add(FieldLabel("Párrafo reconocido"));
        var reflowText = new TextBox
        {
            Name = "EditTextReflowTextBox",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 96,
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 0, 10),
            IsReadOnly = true
        };
        panel.Children.Add(reflowText);

        var reflowApply = new Button
        {
            Name = "EditTextReflowApplyButton",
            Content = "Aplicar reflow seguro",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 5, 12, 5),
            IsEnabled = false
        };
        reflowApply.Click += (_, _) => TryApplySelectedTextReflow();
        panel.Children.Add(reflowApply);

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
        RegisterName(redactionState.Name, redactionState);
        RegisterName(redact.Name, redact);
        RegisterName(reflowState.Name, reflowState);
        RegisterName(reflowText.Name, reflowText);
        RegisterName(reflowApply.Name, reflowApply);

        _editTextPropertiesScrollViewer = scroll;
        _editTextPropertiesPanel = panel;
        _editTextContentTextBox = content;
        _editTextFontValueText = font;
        _editTextStrategyValueText = strategy;
        _editTextSizeTextBox = size;
        _editTextColorTextBox = color;
        _editTextApplyButton = apply;
        _editTextValidationText = validation;
        _editTextRedactionStateText = redactionState;
        _editTextRedactButton = redact;
        _editTextReflowStateText = reflowState;
        _editTextReflowTextBox = reflowText;
        _editTextReflowApplyButton = reflowApply;
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

        RefreshTextReflowProperties(key);
        RefreshTextRedactionState(key);
    }

    private void RefreshTextRedactionState(TextObjectKey key)
    {
        if (_editTextRedactionStateText is null || _editTextRedactButton is null)
            return;

        var workspace = EnsureRedactionWorkspace();
        if (!_editModeActive || _session is null || workspace is null)
        {
            SetTextRedactionUnavailable("No hay una página de edición segura activa.");
            return;
        }

        if (workspace.Targets.Any(target => target.Original.Key == key))
        {
            _editTextRedactButton.IsEnabled = false;
            _editTextRedactionStateText.Foreground = Brushes.DarkGreen;
            _editTextRedactionStateText.Text = "Redacción preparada en el borrador. Usa Guardar como... para publicar los cambios.";
            return;
        }

        try
        {
            var preflight = _inspectEditPreflight(_session, CancellationToken.None);
            var decision = RedactionPolicy.Evaluate(_activeTextObjects, new[] { key }, preflight);
            _editTextRedactButton.IsEnabled = decision.Allowed;
            _editTextRedactionStateText.Foreground = decision.Allowed
                ? Brushes.DarkGreen
                : Brushes.Firebrick;
            _editTextRedactionStateText.Text = FormatTextRedactionAvailability(decision.Allowed, decision.Reason);
        }
        catch (OperationCanceledException)
        {
            SetTextRedactionUnavailable("La verificación de redacción fue cancelada.");
        }
        catch (Exception)
        {
            SetTextRedactionUnavailable("No se pudo verificar la elegibilidad de redacción.");
        }
    }

    private RedactionWorkspace? EnsureRedactionWorkspace()
    {
        if (_redactionWorkspace is not null)
            return _redactionWorkspace;
        if (!_editModeActive || _session is null || _imageEditWorkspace is null)
            return null;

        var candidate = RedactionWorkspace.Create(_session.FilePath, _session.OpenedWithPassword);
        if (!string.Equals(
                candidate.SourcePath,
                _imageEditWorkspace.SourcePath,
                StringComparison.OrdinalIgnoreCase) ||
            candidate.SourceFingerprint != _imageEditWorkspace.SourceFingerprint)
        {
            return null;
        }

        _redactionWorkspace = candidate;
        return _redactionWorkspace;
    }

    private bool TryStageSelectedTextRedaction()
    {
        if (!_editModeActive ||
            _session is null ||
            _selectedTextKey is not TextObjectKey key ||
            _editTextRedactionStateText is null ||
            _editTextRedactButton is null)
        {
            return false;
        }

        var workspace = EnsureRedactionWorkspace();
        if (workspace is null)
        {
            SetTextRedactionUnavailable("El PDF fuente cambió y la redacción ya no es segura.");
            return false;
        }

        if (workspace.Targets.Any(target => target.Original.Key == key))
        {
            RefreshTextRedactionState(key);
            return false;
        }

        try
        {
            var preflight = _inspectEditPreflight(_session, CancellationToken.None);
            var decision = RedactionPolicy.Evaluate(_activeTextObjects, new[] { key }, preflight);
            if (!decision.Allowed || decision.Plan is null)
            {
                SetTextRedactionUnavailable(
                    decision.Reason ?? "El objeto seleccionado no cumple los criterios de seguridad.");
                return false;
            }

            workspace.Stage(decision.Plan);
            RefreshTextRedactionState(key);
            UpdateEditSaveCommandAvailability();
            StatusText.Text = "Redacción preparada en el borrador. Usa Guardar como... para crear el PDF.";
            return true;
        }
        catch (OperationCanceledException)
        {
            SetTextRedactionUnavailable("La preparación de la redacción fue cancelada.");
            return false;
        }
        catch (Exception)
        {
            SetTextRedactionUnavailable("No se pudo preparar la redacción de forma segura.");
            return false;
        }
    }

    private static string FormatTextRedactionAvailability(bool allowed, string? reason)
    {
        if (allowed)
            return "Redacción segura disponible para este objeto de texto.";

        var detail = string.IsNullOrWhiteSpace(reason)
            ? "el objeto seleccionado no cumple los criterios de seguridad."
            : reason.Trim();
        return $"Redacción segura no disponible: {detail}";
    }

    private void SetTextRedactionUnavailable(string reason)
    {
        if (_editTextRedactionStateText is null)
            return;

        if (_editTextRedactButton is not null)
            _editTextRedactButton.IsEnabled = false;
        _editTextRedactionStateText.Foreground = Brushes.Firebrick;
        _editTextRedactionStateText.Text = FormatTextRedactionAvailability(false, reason);
    }

    private void RefreshTextReflowProperties(TextObjectKey key)
    {
        if (_editTextReflowTextBox is null ||
            _editTextReflowStateText is null ||
            _editTextReflowApplyButton is null)
        {
            return;
        }

        ResetTextReflowSelection(clearText: true);

        if (!_editModeActive ||
            _session is null ||
            _navigation is null ||
            _textEditWorkspace is null)
        {
            SetTextReflowUnavailable("No hay una página de edición activa.");
            return;
        }

        var pageIndex = _navigation.CurrentPageIndex;
        if (key.PageIndex != pageIndex)
        {
            SetTextReflowUnavailable("El objeto seleccionado no pertenece a la página activa.");
            return;
        }

        try
        {
            var layout = _analyzeTextLayout(_session, pageIndex, CancellationToken.None);
            var paragraph = FindSelectedTextParagraph(layout, key);
            var preflight = _inspectEditPreflight(_session, CancellationToken.None);
            var decision = TextReflowPolicy.Evaluate(layout, paragraph, preflight);

            _selectedTextLayoutPage = layout;
            _selectedTextParagraph = paragraph;
            _selectedTextReflowPreflight = preflight;

            if (paragraph is not null)
                _editTextReflowTextBox.Text = BuildCurrentParagraphText(paragraph);

            _editTextReflowTextBox.IsReadOnly = !decision.Allowed;
            _editTextReflowApplyButton.IsEnabled = decision.Allowed;
            _editTextReflowStateText.Foreground = decision.Allowed
                ? Brushes.DarkGreen
                : Brushes.Firebrick;
            _editTextReflowStateText.Text = FormatTextReflowAvailability(decision.Allowed, decision.Reason);
        }
        catch (OperationCanceledException)
        {
            SetTextReflowUnavailable("El análisis del párrafo fue cancelado.");
        }
        catch (Exception ex)
        {
            SetTextReflowUnavailable($"No se pudo analizar el párrafo: {ex.Message}");
        }
    }

    private static TextParagraph? FindSelectedTextParagraph(TextLayoutPage page, TextObjectKey key)
    {
        ArgumentNullException.ThrowIfNull(page);

        return page.Columns
            .SelectMany(column => column.Paragraphs)
            .FirstOrDefault(paragraph => paragraph.Lines
                .SelectMany(line => line.Runs)
                .Any(run => run.SourceKey == key));
    }

    private string BuildCurrentParagraphText(TextParagraph paragraph)
    {
        if (_textEditWorkspace is null)
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            paragraph.Lines.Select(line => string.Join(
                " ",
                line.Runs.Select(run => _textEditWorkspace.GetState(run.SourceKey).Text))));
    }

    private static string FormatTextReflowAvailability(bool allowed, string? reason)
    {
        if (allowed)
            return "Texto V2: Simple · Reflow seguro disponible para este párrafo.";

        var detail = string.IsNullOrWhiteSpace(reason)
            ? "el bloque no cumple los criterios de seguridad."
            : reason.Trim();
        return $"Texto V2: no seguro · Reflow seguro no disponible: {detail}";
    }

    private void SetTextReflowUnavailable(string reason)
    {
        ResetTextReflowSelection(clearText: true);
        if (_editTextReflowStateText is null)
            return;

        _editTextReflowStateText.Foreground = Brushes.Firebrick;
        _editTextReflowStateText.Text = FormatTextReflowAvailability(false, reason);
    }

    private void ResetTextReflowSelection(bool clearText)
    {
        _selectedTextLayoutPage = null;
        _selectedTextParagraph = null;
        _selectedTextReflowPreflight = null;

        if (_editTextReflowTextBox is not null)
        {
            if (clearText)
                _editTextReflowTextBox.Text = string.Empty;
            _editTextReflowTextBox.IsReadOnly = true;
        }

        if (_editTextReflowApplyButton is not null)
            _editTextReflowApplyButton.IsEnabled = false;

        if (_editTextReflowStateText is not null)
            _editTextReflowStateText.Text = string.Empty;
    }

    private void HideEditTextProperties(bool showPlaceholder)
    {
        if (_editTextPropertiesScrollViewer is not null)
            _editTextPropertiesScrollViewer.Visibility = Visibility.Collapsed;
        if (_editTextPropertiesPanel is not null)
            _editTextPropertiesPanel.Visibility = Visibility.Collapsed;
        if (_editTextValidationText is not null)
            _editTextValidationText.Text = string.Empty;
        if (_editTextRedactionStateText is not null)
            _editTextRedactionStateText.Text = string.Empty;
        if (_editTextRedactButton is not null)
            _editTextRedactButton.IsEnabled = false;
        ResetTextReflowSelection(clearText: true);
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

    private bool TryApplySelectedTextReflow()
    {
        if (!_editModeActive ||
            _textEditWorkspace is null ||
            _selectedTextKey is null ||
            _selectedTextLayoutPage is null ||
            _selectedTextParagraph is null ||
            _selectedTextReflowPreflight is null ||
            _editTextReflowTextBox is null ||
            _editTextReflowStateText is null ||
            _editTextReflowApplyButton is null)
        {
            return false;
        }

        try
        {
            var plan = TextReflowPlanner.Plan(
                _selectedTextLayoutPage,
                _selectedTextParagraph,
                _selectedTextReflowPreflight,
                _editTextReflowTextBox.Text);
            if (!plan.Allowed)
            {
                SetTextReflowUnavailable(plan.Reason ?? "El párrafo no admite reflow seguro.");
                return false;
            }

            var result = _textEditWorkspace.ApplyReflowPlan(plan);
            if (!result.Applied)
            {
                SetTextReflowUnavailable(result.Reason ?? "No se pudo aplicar el reflow seguro.");
                return false;
            }

            RefreshTextEditProperties();
            UpdateEditSaveCommandAvailability();
            StatusText.Text = "Reflow seguro aplicado al borrador. Usa Guardar como... para crear el PDF.";
            return true;
        }
        catch (OperationCanceledException)
        {
            SetTextReflowUnavailable("La aplicación del reflow fue cancelada.");
            return false;
        }
        catch (Exception ex)
        {
            SetTextReflowUnavailable($"No se pudo aplicar el reflow seguro: {ex.Message}");
            return false;
        }
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
        => _imageEditWorkspace?.IsDirty == true ||
           _textEditWorkspace?.IsDirty == true ||
           _redactionWorkspace?.IsDirty == true;

    private void ResetTextEditState()
    {
        _activeTextObjects = Array.Empty<PdfTextObjectInfo>();
        _selectedTextKey = null;
        _redactionWorkspace = null;
        HideEditTextProperties(showPlaceholder: false);
    }
}
