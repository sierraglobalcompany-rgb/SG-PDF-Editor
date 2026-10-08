using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Reader;

internal sealed class ReaderThumbnailItem : INotifyPropertyChanged
{
    private BitmapSource? _bitmap;
    private bool _isLoading;
    private bool _hasError;
    private string? _errorMessage;

    internal ReaderThumbnailItem(int pageIndex, PdfPageSize pageSize)
    {
        if (pageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        ValidatePageSize(pageSize);

        PageIndex = pageIndex;
        PageSize = pageSize;
        DisplayHeight = 132d * pageSize.HeightPoints / pageSize.WidthPoints;
    }

    public int PageIndex { get; }
    public PdfPageSize PageSize { get; }
    public double DisplayWidth => 132d;
    public double DisplayHeight { get; }
    public BitmapSource? Bitmap => _bitmap;
    public bool IsLoading => _isLoading;
    public bool HasError => _hasError;
    public string? ErrorMessage => _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal static double ResolveThumbnailDpi(PdfPageSize size, double targetWidthPixels = 132d)
    {
        ValidatePageSize(size);
        if (!double.IsFinite(targetWidthPixels) || targetWidthPixels <= 0d)
            throw new ArgumentOutOfRangeException(nameof(targetWidthPixels));

        return targetWidthPixels * 72d / size.WidthPoints;
    }

    internal void MarkLoading()
    {
        _isLoading = true;
        _hasError = false;
        _errorMessage = null;
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    internal void Publish(BitmapSource bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        _bitmap = bitmap;
        _isLoading = false;
        _hasError = false;
        _errorMessage = null;
        OnPropertyChanged(nameof(Bitmap));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    internal void MarkError(string? message = null)
    {
        _bitmap = null;
        _isLoading = false;
        _hasError = true;
        _errorMessage = string.IsNullOrWhiteSpace(message) ? "No se pudo mostrar la miniatura." : message;
        OnPropertyChanged(nameof(Bitmap));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    internal void ReleaseBitmap()
    {
        _bitmap = null;
        _isLoading = false;
        _hasError = false;
        _errorMessage = null;
        OnPropertyChanged(nameof(Bitmap));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    private static void ValidatePageSize(PdfPageSize size)
    {
        if (!double.IsFinite(size.WidthPoints) || size.WidthPoints <= 0d ||
            !double.IsFinite(size.HeightPoints) || size.HeightPoints <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
