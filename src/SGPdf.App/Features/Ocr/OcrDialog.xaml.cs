using System.Windows;
using System.Windows.Controls;

namespace SGPdf.App.Features.Ocr;

public partial class OcrDialog : Window
{
    private readonly int _pageCount;
    private readonly int _currentPageIndex;
    private bool _running;

    internal OcrDialog(int pageCount, int currentPageIndex)
    {
        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        if (currentPageIndex < 0 || currentPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(currentPageIndex));

        _pageCount = pageCount;
        _currentPageIndex = currentPageIndex;
        InitializeComponent();

        OcrRangeStartTextBox.Text = (_currentPageIndex + 1).ToString();
        OcrRangeEndTextBox.Text = _pageCount.ToString();
        OcrScopeComboBox.SelectionChanged += OcrScopeComboBox_SelectionChanged;
        UpdateRangeUi();
    }

    internal OcrRequest? Request { get; private set; }

    internal event EventHandler? RunRequested;
    internal event EventHandler? CancelRequested;

    private void OcrScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateRangeUi();

    private void OcrStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
            return;

        if (!TryBuildRequest(out var request, out var error))
        {
            OcrValidationText.Text = error ?? "Los valores de OCR no son válidos.";
            OcrValidationText.Visibility = Visibility.Visible;
            return;
        }

        Request = request;
        OcrValidationText.Visibility = Visibility.Collapsed;
        SetRunning(true);

        if (RunRequested is null)
        {
            DialogResult = true;
            return;
        }

        RunRequested.Invoke(this, EventArgs.Empty);
    }

    private void OcrCancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_running)
        {
            DialogResult = false;
            return;
        }

        OcrCancelButton.IsEnabled = false;
        OcrCancelButton.Content = "Cancelando…";
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool TryBuildRequest(out OcrRequest? request, out string? error)
    {
        request = null;
        error = null;

        var language = OcrLanguageComboBox.SelectedIndex switch
        {
            0 => OcrLanguage.Spanish,
            1 => OcrLanguage.English,
            2 => OcrLanguage.SpanishEnglish,
            _ => (OcrLanguage?)null
        };
        if (language is null)
        {
            error = "Selecciona un idioma para OCR.";
            return false;
        }

        OcrPageScope scope;
        switch (OcrScopeComboBox.SelectedIndex)
        {
            case 0:
                scope = OcrPageScope.Automatic;
                break;
            case 1:
                scope = OcrPageScope.Current(_currentPageIndex);
                break;
            case 2:
                if (!int.TryParse(OcrRangeStartTextBox.Text, out var startPage) ||
                    !int.TryParse(OcrRangeEndTextBox.Text, out var endPage) ||
                    startPage < 1 || endPage < startPage || endPage > _pageCount)
                {
                    error = $"El rango debe estar entre 1 y {_pageCount}, y la página inicial no puede superar la final.";
                    return false;
                }

                scope = OcrPageScope.Range(startPage - 1, endPage - 1);
                break;
            default:
                error = "Selecciona qué páginas procesar.";
                return false;
        }

        request = OcrRequest.Create(scope, language.Value);
        return true;
    }

    internal void UpdateProgress(OcrProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var total = Math.Max(1, progress.TotalEligiblePages);
        var current = Math.Clamp(progress.CurrentOrdinal, 0, total);
        OcrProgressBar.Maximum = total;
        OcrProgressBar.Value = current;
        OcrProgressText.Text = $"Página {current} de {total} — {StageText(progress.Stage)}";
    }

    internal void ResetAfterRun()
    {
        _running = false;
        OcrLanguageComboBox.IsEnabled = true;
        OcrScopeComboBox.IsEnabled = true;
        OcrStartButton.IsEnabled = true;
        OcrCancelButton.IsEnabled = true;
        OcrCancelButton.Content = "Cancelar";
        UpdateRangeUi();
    }

    internal void CompleteAndClose()
    {
        _running = false;
        DialogResult = true;
    }

    private void SetRunning(bool running)
    {
        _running = running;
        OcrLanguageComboBox.IsEnabled = !running;
        OcrScopeComboBox.IsEnabled = !running;
        OcrRangeStartTextBox.IsEnabled = !running && OcrScopeComboBox.SelectedIndex == 2;
        OcrRangeEndTextBox.IsEnabled = !running && OcrScopeComboBox.SelectedIndex == 2;
        OcrStartButton.IsEnabled = !running;
        OcrCancelButton.IsEnabled = true;
        OcrCancelButton.Content = "Cancelar";
        if (running)
            OcrProgressText.Text = "Preparando OCR…";
    }

    private void UpdateRangeUi()
    {
        var enabled = !_running && OcrScopeComboBox.SelectedIndex == 2;
        OcrRangeStartTextBox.IsEnabled = enabled;
        OcrRangeEndTextBox.IsEnabled = enabled;
    }

    private static string StageText(OcrProgressStage stage) => stage switch
    {
        OcrProgressStage.Rendering => "preparando imagen",
        OcrProgressStage.Recognizing => "reconociendo texto",
        OcrProgressStage.Writing => "guardando PDF",
        OcrProgressStage.Validating => "validando resultado",
        _ => "procesando"
    };
}
