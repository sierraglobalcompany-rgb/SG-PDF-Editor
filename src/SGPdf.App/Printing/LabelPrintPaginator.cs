using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SGPdf.App.Features.Labels;

namespace SGPdf.App.Printing;

public sealed class LabelPrintPaginator : DocumentPaginator
{
    private const double WpfUnitsPerMillimeter = 96d / 25.4d;

    private readonly LabelLayoutPlan _plan;
    private readonly IReadOnlyList<ZplRenderedLabel> _renderedLabels;
    private readonly Size _pageSize;
    private readonly int _pageCount;

    public LabelPrintPaginator(
        LabelLayoutPlan plan,
        IReadOnlyList<ZplRenderedLabel> renderedLabels)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(renderedLabels);

        if (plan.LabelsPerPage != 1)
            throw new ArgumentException("Thermal printing requires exactly one label per page.", nameof(plan));

        if (renderedLabels.Count != plan.DesignCount)
        {
            throw new ArgumentException(
                "Rendered label count must match the printable design count in the layout plan.",
                nameof(renderedLabels));
        }

        if (plan.PageCount > int.MaxValue)
            throw new InvalidOperationException("La cantidad de etiquetas excede el máximo que Windows puede paginar para impresión.");

        _plan = plan;
        _renderedLabels = renderedLabels;
        _pageCount = checked((int)plan.PageCount);
        _pageSize = new Size(
            plan.PageWidthMm * WpfUnitsPerMillimeter,
            plan.PageHeightMm * WpfUnitsPerMillimeter);
    }

    public override bool IsPageCountValid => true;

    public override int PageCount => _pageCount;

    public override Size PageSize
    {
        get => _pageSize;
        set => throw new NotSupportedException("El tamaño físico de la etiqueta lo define el layout térmico aplicado.");
    }

    public override IDocumentPaginatorSource? Source => null;

    public override DocumentPage GetPage(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber >= PageCount)
            return DocumentPage.Missing;

        var pagePlan = _plan.GetPage(pageNumber);
        if (pagePlan.Placements.Count != 1)
            throw new InvalidOperationException("Thermal printing requires exactly one placement per page.");

        var placement = pagePlan.Placements[0];
        if (placement.DesignIndex < 0 || placement.DesignIndex >= _renderedLabels.Count)
            throw new InvalidOperationException("The layout references a rendered design that is not available.");

        var bitmap = CreateBitmapSource(_renderedLabels[placement.DesignIndex].PngBytes);
        ImageSource imageSource = bitmap;
        if (placement.Rotation == LabelRotation.Degrees90)
        {
            var rotated = new TransformedBitmap(bitmap, new RotateTransform(90));
            rotated.Freeze();
            imageSource = rotated;
        }

        var visual = new DrawingVisual();
        using (var drawingContext = visual.RenderOpen())
        {
            var pageRect = new Rect(new Point(0d, 0d), _pageSize);
            drawingContext.DrawRectangle(Brushes.White, null, pageRect);
            drawingContext.DrawImage(imageSource, pageRect);
        }

        var pageBox = new Rect(new Point(0d, 0d), _pageSize);
        return new DocumentPage(visual, _pageSize, pageBox, pageBox);
    }

    private static BitmapSource CreateBitmapSource(byte[] pngBytes)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);

        using var stream = new MemoryStream(pngBytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
