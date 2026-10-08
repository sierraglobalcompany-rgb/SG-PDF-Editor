using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using SGPdf.App.Features.Sign;

namespace SGPdf.App;

public partial class SignatureDrawDialog : Window
{
    private readonly SignatureInkHistory _history = new();
    private SignatureInkColor _selectedColor = SignatureInkColor.Black;
    private SignatureInkWidth _selectedWidth = SignatureInkWidth.Medium;
    private bool _accepted;
    private Func<StrokeCollection, SignatureAsset> _renderInk =
        static strokes => SignatureInkRenderer.Render(strokes);

    internal SignatureDrawDialog()
    {
        InitializeComponent();

        SignatureDrawWidthComboBox.ItemsSource = Enum.GetValues<SignatureInkWidth>();
        SignatureDrawWidthComboBox.SelectedItem = SignatureInkWidth.Medium;
        SignatureDrawInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
        SignatureDrawInkCanvas.StrokeCollected += (_, e) => RecordCollectedStroke(e.Stroke);
        SignatureDrawBlackButton.Click += (_, _) => SetColor(SignatureInkColor.Black);
        SignatureDrawBlueButton.Click += (_, _) => SetColor(SignatureInkColor.Blue);
        SignatureDrawWidthComboBox.SelectionChanged += (_, _) =>
        {
            if (SignatureDrawWidthComboBox.SelectedItem is SignatureInkWidth width)
            {
                _selectedWidth = width;
                ApplyDrawingAttributes();
            }
        };
        SignatureDrawUndoButton.Click += (_, _) =>
        {
            _history.Undo(SignatureDrawInkCanvas.Strokes);
            UpdateCommandState();
        };
        SignatureDrawRedoButton.Click += (_, _) =>
        {
            _history.Redo(SignatureDrawInkCanvas.Strokes);
            UpdateCommandState();
        };
        SignatureDrawClearButton.Click += (_, _) =>
        {
            _history.Clear(SignatureDrawInkCanvas.Strokes);
            PreparedAsset = null;
            SignatureDrawStatusText.Text = string.Empty;
            UpdateCommandState();
        };
        SignatureDrawApplyButton.Click += Apply_Click;
        SignatureDrawCancelButton.Click += Cancel_Click;

        ApplyDrawingAttributes();
        UpdateCommandState();
    }

    internal SignatureAsset? PreparedAsset { get; private set; }

    internal static SignatureAsset? Draw(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var dialog = new SignatureDrawDialog { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.PreparedAsset : null;
    }

    internal void AddStrokeForTest(Stroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        if (!SignatureDrawInkCanvas.Strokes.Contains(stroke))
            SignatureDrawInkCanvas.Strokes.Add(stroke);
        RecordCollectedStroke(stroke);
    }

    internal bool TryApplyCurrentInk()
    {
        try
        {
            if (!SignatureInkRenderer.HasUsefulInk(SignatureDrawInkCanvas.Strokes))
            {
                PreparedAsset = null;
                SignatureDrawStatusText.Text = "Dibuja una firma más grande antes de aplicar.";
                UpdateCommandState();
                return false;
            }

            PreparedAsset = _renderInk(SignatureDrawInkCanvas.Strokes);
            SignatureDrawStatusText.Text = "Firma preparada.";
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or OverflowException or OutOfMemoryException)
        {
            PreparedAsset = null;
            SignatureDrawStatusText.Text = ex.Message;
            UpdateCommandState();
            return false;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_accepted)
            PreparedAsset = null;
        base.OnClosing(e);
    }

    private void RecordCollectedStroke(Stroke stroke)
    {
        _history.RecordUserStroke(stroke);
        PreparedAsset = null;
        SignatureDrawStatusText.Text = string.Empty;
        UpdateCommandState();
    }

    private void SetColor(SignatureInkColor color)
    {
        _selectedColor = color;
        ApplyDrawingAttributes();
        UpdateSelectorVisuals();
    }

    private void ApplyDrawingAttributes()
        => SignatureDrawInkCanvas.DefaultDrawingAttributes =
            SignatureStrokeStyle.CreateDrawingAttributes(_selectedColor, _selectedWidth);

    private void UpdateSelectorVisuals()
    {
        SignatureDrawBlackButton.FontWeight = _selectedColor == SignatureInkColor.Black
            ? FontWeights.Bold
            : FontWeights.Normal;
        SignatureDrawBlueButton.FontWeight = _selectedColor == SignatureInkColor.Blue
            ? FontWeights.Bold
            : FontWeights.Normal;
    }

    private void UpdateCommandState()
    {
        SignatureDrawUndoButton.IsEnabled = _history.CanUndo;
        SignatureDrawRedoButton.IsEnabled = _history.CanRedo;
        SignatureDrawClearButton.IsEnabled = SignatureDrawInkCanvas.Strokes.Count > 0;
        SignatureDrawApplyButton.IsEnabled = SignatureInkRenderer.HasUsefulInk(SignatureDrawInkCanvas.Strokes);
        UpdateSelectorVisuals();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApplyCurrentInk())
            return;

        _accepted = true;
        if (IsVisible)
            DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _accepted = false;
        PreparedAsset = null;
        if (IsVisible)
            DialogResult = false;
    }
}
