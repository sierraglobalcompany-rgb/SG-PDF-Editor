using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Organize;

internal sealed class OrganizePageItem : INotifyPropertyChanged
{
    internal const double ThumbnailWidthPixels = 132d;

    private BitmapSource? _bitmap;
    private bool _isLoading;
    private bool _hasError;
    private string? _errorMessage;
    private int _pageNumber;
    private int _rotationDeltaQuarterTurns;
    private double _displayHeight;

    internal OrganizePageItem(OrganizePage page, int pageNumber, PdfPageSize sourcePageSize)
    {
        ArgumentNullException.ThrowIfNull(page);
        ValidatePageNumber(pageNumber);
        ValidatePageSize(sourcePageSize);

        ItemId = page.ItemId;
        SourceId = page.SourceId;
        SourcePageIndex = page.SourcePageIndex;
        SourcePageSize = sourcePageSize;
        _pageNumber = pageNumber;
        _rotationDeltaQuarterTurns = NormalizeRotation(page.RotationDeltaQuarterTurns);
        _displayHeight = ResolveDisplayHeight(sourcePageSize, _rotationDeltaQuarterTurns);
    }

    public Guid ItemId { get; }
    public Guid SourceId { get; }
    public int SourcePageIndex { get; }
    public int PageNumber => _pageNumber;
    public int RotationDeltaQuarterTurns => _rotationDeltaQuarterTurns;
    public PdfPageSize SourcePageSize { get; }
    public double DisplayWidth => ThumbnailWidthPixels;
    public double DisplayHeight => _displayHeight;
    public BitmapSource? Bitmap => _bitmap;
    public bool IsLoading => _isLoading;
    public bool HasError => _hasError;
    public string? ErrorMessage => _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal double ResolveThumbnailDpi()
    {
        var effectiveWidth = (_rotationDeltaQuarterTurns & 1) == 0
            ? SourcePageSize.WidthPoints
            : SourcePageSize.HeightPoints;
        return ThumbnailWidthPixels * 72d / effectiveWidth;
    }

    internal bool MatchesLogicalPage(OrganizePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return ItemId == page.ItemId &&
               SourceId == page.SourceId &&
               SourcePageIndex == page.SourcePageIndex;
    }

    internal void UpdatePlanState(OrganizePage page, int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(page);
        ValidatePageNumber(pageNumber);
        if (!MatchesLogicalPage(page))
            throw new ArgumentException("La página lógica no corresponde a este tile.", nameof(page));

        if (_pageNumber != pageNumber)
        {
            _pageNumber = pageNumber;
            OnPropertyChanged(nameof(PageNumber));
        }

        var rotation = NormalizeRotation(page.RotationDeltaQuarterTurns);
        if (_rotationDeltaQuarterTurns == rotation)
            return;

        _rotationDeltaQuarterTurns = rotation;
        _displayHeight = ResolveDisplayHeight(SourcePageSize, rotation);
        ReleaseBitmap();
        OnPropertyChanged(nameof(RotationDeltaQuarterTurns));
        OnPropertyChanged(nameof(DisplayHeight));
    }

    internal void MarkLoading()
    {
        _isLoading = true;
        _hasError = false;
        _errorMessage = null;
        NotifyRenderStateChanged();
    }

    internal void Publish(BitmapSource bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        _bitmap = bitmap;
        _isLoading = false;
        _hasError = false;
        _errorMessage = null;
        OnPropertyChanged(nameof(Bitmap));
        NotifyRenderStateChanged();
    }

    internal void MarkError(string? message = null)
    {
        _bitmap = null;
        _isLoading = false;
        _hasError = true;
        _errorMessage = string.IsNullOrWhiteSpace(message)
            ? "No se pudo mostrar la miniatura."
            : message;
        OnPropertyChanged(nameof(Bitmap));
        NotifyRenderStateChanged();
    }

    internal void ReleaseBitmap()
    {
        _bitmap = null;
        _isLoading = false;
        _hasError = false;
        _errorMessage = null;
        OnPropertyChanged(nameof(Bitmap));
        NotifyRenderStateChanged();
    }

    private void NotifyRenderStateChanged()
    {
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    private static int NormalizeRotation(int quarterTurns)
    {
        var normalized = quarterTurns % 4;
        return normalized < 0 ? normalized + 4 : normalized;
    }

    private static double ResolveDisplayHeight(PdfPageSize pageSize, int rotation)
    {
        var width = (rotation & 1) == 0 ? pageSize.WidthPoints : pageSize.HeightPoints;
        var height = (rotation & 1) == 0 ? pageSize.HeightPoints : pageSize.WidthPoints;
        return ThumbnailWidthPixels * height / width;
    }

    private static void ValidatePageNumber(int pageNumber)
    {
        if (pageNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
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
