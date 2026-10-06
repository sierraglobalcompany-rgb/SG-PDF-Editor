using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SGPdf.App.Pdf;

internal sealed class PdfDocumentPaginator : DocumentPaginator
{
    private const double RenderScale = 2.0; // 192 DPI efectivos sobre unidades WPF de 96 DPI.
    private readonly PdfDocumentSession _session;
    private readonly PdfPrintRange _range;
    private Size _pageSize;

    internal PdfDocumentPaginator(PdfDocumentSession session, PdfPrintRange range, Size pageSize)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _range = range ?? throw new ArgumentNullException(nameof(range));

        if (pageSize.Width <= 0 || pageSize.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        _pageSize = pageSize;
    }

    public override bool IsPageCountValid => true;

    public override int PageCount => _range.PageCount;

    public override Size PageSize
    {
        get => _pageSize;
        set
        {
            if (value.Width <= 0 || value.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _pageSize = value;
        }
    }

    public override IDocumentPaginatorSource Source => null!;

    public override DocumentPage GetPage(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber >= PageCount)
            return DocumentPage.Missing;

        var sourcePageIndex = _range.StartPageIndex + pageNumber;
        var sourceSize = _session.GetPageSize(sourcePageIndex);
        var layout = PdfPrintLayout.Fit(
            sourceSize.Width,
            sourceSize.Height,
            PageSize.Width,
            PageSize.Height);

        var pixelWidth = Math.Clamp((int)Math.Ceiling(layout.Width * RenderScale), 1, 8192);
        var pixelHeight = Math.Clamp((int)Math.Ceiling(layout.Height * RenderScale), 1, 8192);
        var rendered = PdfPageRenderer.Render(_session, sourcePageIndex, pixelWidth, pixelHeight);

        var bitmap = BitmapSource.Create(
            rendered.Width,
            rendered.Height,
            96.0 * RenderScale,
            96.0 * RenderScale,
            PixelFormats.Bgra32,
            palette: null,
            rendered.Pixels,
            rendered.Stride);
        bitmap.Freeze();

        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawImage(
                bitmap,
                new Rect(layout.X, layout.Y, layout.Width, layout.Height));
        }

        var pageRect = new Rect(PageSize);
        return new DocumentPage(visual, PageSize, pageRect, pageRect);
    }
}
