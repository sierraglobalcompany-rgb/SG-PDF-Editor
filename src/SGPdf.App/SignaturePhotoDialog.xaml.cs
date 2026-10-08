using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Sign;

namespace SGPdf.App;

public partial class SignaturePhotoDialog : Window
{
    private readonly SignaturePhotoSource _source;
    private readonly SignaturePhotoSource _previewSource;
    private readonly SignaturePaperColor _paper;
    private readonly SignaturePhotoPreviewVersion _previewVersion = new();
    private bool _suppressPreview;

    internal SignaturePhotoDialog(SignaturePhotoSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _paper = SignatureImageProcessor.EstimatePaper(source);
        _previewSource = SignaturePhotoPreview.CreateReducedSource(source);

        InitializeComponent();
        SignaturePhotoInkComboBox.ItemsSource = Enum.GetValues<SignatureInkStyle>();

        SignaturePhotoAutomaticButton.Click += (_, _) => ApplyAutomaticSettings();
        SignaturePhotoResetButton.Click += (_, _) => ApplyAutomaticSettings();
        SignaturePhotoApplyButton.Click += Apply_Click;
        SignaturePhotoCancelButton.Click += Cancel_Click;
        SignaturePhotoBackgroundSlider.ValueChanged += SettingsChanged;
        SignaturePhotoBrightnessSlider.ValueChanged += SettingsChanged;
        SignaturePhotoContrastSlider.ValueChanged += SettingsChanged;
        SignaturePhotoInkComboBox.SelectionChanged += SettingsChanged;
        SignaturePhotoAutoCropCheckBox.Checked += SettingsChanged;
        SignaturePhotoAutoCropCheckBox.Unchecked += SettingsChanged;

        ApplyAutomaticSettings();
    }

    internal SignatureAsset? PreparedAsset { get; private set; }

    internal static SignatureAsset? Prepare(Window owner, string path)
    {
        var source = SignaturePhotoLoader.Load(path);
        var dialog = new SignaturePhotoDialog(source) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.PreparedAsset : null;
    }

    internal static SignatureAsset ProcessFullSource(
        SignaturePhotoSource source,
        SignatureImageProcessingSettings settings,
        SignaturePaperColor paper)
        => SignatureImageProcessor.Process(source, settings, paper);

    internal bool TryApplyCurrentSettings()
    {
        try
        {
            PreparedAsset = ProcessFullSource(_source, ReadSettings(), _paper);
            SignaturePhotoStatusText.Text = "Firma preparada.";
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or OverflowException)
        {
            PreparedAsset = null;
            SignaturePhotoStatusText.Text = ex.Message;
            return false;
        }
    }

    private void ApplyAutomaticSettings()
    {
        _suppressPreview = true;
        try
        {
            var settings = SignatureImageProcessingSettings.Automatic;
            SignaturePhotoBackgroundSlider.Value = settings.BackgroundRemoval;
            SignaturePhotoBrightnessSlider.Value = settings.Brightness;
            SignaturePhotoContrastSlider.Value = settings.Contrast;
            SignaturePhotoInkComboBox.SelectedItem = settings.InkStyle;
            SignaturePhotoAutoCropCheckBox.IsChecked = settings.AutoCrop;
        }
        finally
        {
            _suppressPreview = false;
        }

        QueuePreview();
    }

    private SignatureImageProcessingSettings ReadSettings()
    {
        var inkStyle = SignaturePhotoInkComboBox.SelectedItem is SignatureInkStyle selected
            ? selected
            : SignatureInkStyle.Original;
        return new SignatureImageProcessingSettings(
            (int)Math.Round(SignaturePhotoBackgroundSlider.Value),
            (int)Math.Round(SignaturePhotoBrightnessSlider.Value),
            (int)Math.Round(SignaturePhotoContrastSlider.Value),
            inkStyle,
            SignaturePhotoAutoCropCheckBox.IsChecked != false);
    }

    private void SettingsChanged(object? sender, RoutedEventArgs e)
    {
        if (!_suppressPreview)
            QueuePreview();
    }

    private void QueuePreview()
    {
        var version = _previewVersion.BeginRequest();
        var settings = ReadSettings();
        SignaturePhotoStatusText.Text = "Actualizando vista previa...";

        if (!IsVisible)
        {
            ProcessPreviewSynchronously(version, settings);
            return;
        }

        _ = ProcessPreviewAsync(version, settings);
    }

    private void ProcessPreviewSynchronously(long version, SignatureImageProcessingSettings settings)
    {
        try
        {
            var asset = SignatureImageProcessor.Process(_previewSource, settings, _paper);
            ApplyPreviewResult(version, asset, null);
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or OverflowException)
        {
            ApplyPreviewResult(version, null, ex.Message);
        }
    }

    private async Task ProcessPreviewAsync(long version, SignatureImageProcessingSettings settings)
    {
        SignatureAsset? asset = null;
        string? error = null;
        try
        {
            asset = await Task.Run(() => SignatureImageProcessor.Process(_previewSource, settings, _paper)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or OverflowException)
        {
            error = ex.Message;
        }

        if (!_previewVersion.IsCurrent(version))
            return;

        await Dispatcher.InvokeAsync(() => ApplyPreviewResult(version, asset, error));
    }

    private void ApplyPreviewResult(long version, SignatureAsset? asset, string? error)
    {
        if (!_previewVersion.IsCurrent(version))
            return;

        if (asset is null)
        {
            SignaturePhotoPreviewImage.Source = null;
            SignaturePhotoStatusText.Text = error ?? "No se pudo preparar la vista previa.";
            return;
        }

        SignaturePhotoPreviewImage.Source = CreateBitmap(asset);
        SignaturePhotoStatusText.Text = string.Empty;
    }

    private static BitmapSource CreateBitmap(SignatureAsset asset)
    {
        var bitmap = BitmapSource.Create(
            asset.PixelWidth,
            asset.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            asset.BgraPixels.ToArray(),
            asset.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApplyCurrentSettings())
            return;
        if (IsVisible)
            DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        PreparedAsset = null;
        if (IsVisible)
            DialogResult = false;
    }
}
