using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Pdf;

namespace SGPdf.App.Printing;

public sealed class PdfDocumentPaginator : DocumentPaginator
{
    private const double PrintRenderDpi = 200d;

    private readonly PdfDocumentSession _session;
    private readonly PdfPrintRange _range;
    private readonly Size _pageSize;

    public PdfDocumentPaginator(
        PdfDocumentSession session,
        PdfPrintRange range,
        double printableWidth,
        double printableHeight)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(range);

        if (!double.IsFinite(printableWidth) || printableWidth <= 0d)
            throw new ArgumentOutOfRangeException(nameof(printableWidth));

        if (!double.IsFinite(printableHeight) || printableHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(printableHeight));

        if (range.FirstPageIndex < 0 || range.LastPageIndex >= session.PageCount)
            throw new ArgumentOutOfRangeException(nameof(range));

        _session = session;
        _range = range;
        _pageSize = new Size(printableWidth, printableHeight);
    }

    public override bool IsPageCountValid => true;

    public override int PageCount => _range.PageCount;

    public override Size PageSize
    {
        get => _pageSize;
        set => throw new NotSupportedException("El tamaño de impresión lo define la impresora seleccionada.");
    }

    public override IDocumentPaginatorSource? Source => null;

    public override DocumentPage GetPage(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber >= PageCount)
            return DocumentPage.Missing;

        var pdfPageIndex = _range.FirstPageIndex + pageNumber;
        var rendered = _session.RenderPage(pdfPageIndex, PrintRenderDpi);
        var bitmap = CreateBitmapSource(rendered);
        var destination = FitInsidePage(rendered.PixelWidth, rendered.PixelHeight, _pageSize);

        var visual = new DrawingVisual();
        using (var drawingContext = visual.RenderOpen())
        {
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(new Point(0d, 0d), _pageSize));
            drawingContext.DrawImage(bitmap, destination);
        }

        var pageBox = new Rect(new Point(0d, 0d), _pageSize);
        return new DocumentPage(visual, _pageSize, pageBox, pageBox);
    }

    private static BitmapSource CreateBitmapSource(PdfRenderedPage rendered)
    {
        var bitmap = BitmapSource.Create(
            rendered.PixelWidth,
            rendered.PixelHeight,
            96d,
            96d,
            PixelFormats.Bgra32,
            null,
            rendered.Pixels,
            rendered.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static Rect FitInsidePage(int pixelWidth, int pixelHeight, Size pageSize)
    {
        var widthScale = pageSize.Width / pixelWidth;
        var heightScale = pageSize.Height / pixelHeight;
        var scale = Math.Min(widthScale, heightScale);

        var width = pixelWidth * scale;
        var height = pixelHeight * scale;
        var x = (pageSize.Width - width) / 2d;
        var y = (pageSize.Height - height) / 2d;
        return new Rect(x, y, width, height);
    }
}
