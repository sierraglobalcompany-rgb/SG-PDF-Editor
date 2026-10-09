using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Reader;

internal enum ReaderPageRenderState
{
    Placeholder,
    Loading,
    Ready,
    Error
}

internal sealed class ReaderPageItem : INotifyPropertyChanged
{
    private ReaderPageGeometry _geometry;
    private ReaderPageRenderState _renderState;
    private BitmapSource? _bitmap;
    private PdfPageDeviceTransform? _deviceTransform;
    private string? _errorMessage;

    internal ReaderPageItem(ReaderPageGeometry geometry)
    {
        PageIndex = geometry.PageIndex;
        _geometry = geometry;
        _renderState = ReaderPageRenderState.Placeholder;
    }

    public int PageIndex { get; }
    public ReaderPageGeometry Geometry => _geometry;
    public ReaderPageRenderState RenderState => _renderState;
    public BitmapSource? Bitmap => _bitmap;
    public PdfPageDeviceTransform? DeviceTransform => _deviceTransform;
    public string? ErrorMessage => _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void ApplyGeometry(ReaderPageGeometry geometry)
    {
        if (geometry.PageIndex != PageIndex)
            throw new ArgumentException("La geometría no corresponde a esta página.", nameof(geometry));

        _geometry = geometry;
        OnPropertyChanged(nameof(Geometry));
    }

    internal void MarkLoading()
    {
        _renderState = ReaderPageRenderState.Loading;
        _errorMessage = null;
        OnPropertyChanged(nameof(RenderState));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    internal void Publish(PdfRenderedPage rendered, BitmapSource bitmap)
    {
        ArgumentNullException.ThrowIfNull(rendered);
        ArgumentNullException.ThrowIfNull(bitmap);
        if (rendered.PageIndex != PageIndex)
            throw new ArgumentException("El render no corresponde a esta página.", nameof(rendered));

        _bitmap = bitmap;
        _deviceTransform = rendered.DeviceTransform;
        _errorMessage = null;
        _renderState = ReaderPageRenderState.Ready;
        OnPropertyChanged(nameof(Bitmap));
        OnPropertyChanged(nameof(DeviceTransform));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(RenderState));
    }

    internal void MarkError(string message)
    {
        _bitmap = null;
        _deviceTransform = null;
        _errorMessage = string.IsNullOrWhiteSpace(message) ? "No se pudo renderizar esta página." : message;
        _renderState = ReaderPageRenderState.Error;
        OnPropertyChanged(nameof(Bitmap));
        OnPropertyChanged(nameof(DeviceTransform));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(RenderState));
    }

    internal void ReleaseBitmap()
    {
        _bitmap = null;
        _deviceTransform = null;
        _errorMessage = null;
        _renderState = ReaderPageRenderState.Placeholder;
        OnPropertyChanged(nameof(Bitmap));
        OnPropertyChanged(nameof(DeviceTransform));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(RenderState));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
