using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace SGPdf.App.Features.Labels;

public sealed class LabelPdfExporter
{
    private const double PointsPerMillimeter = 72d / 25.4d;

    public void Export(
        string destinationPath,
        LabelLayoutPlan plan,
        IReadOnlyList<ZplRenderedLabel> renderedLabels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(renderedLabels);

        if (renderedLabels.Count != plan.DesignCount)
        {
            throw new ArgumentException(
                "Rendered label count must match the printable design count in the layout plan.",
                nameof(renderedLabels));
        }

        var orderedLabels = ValidateAndOrderRenderedLabels(plan.DesignCount, renderedLabels);
        var fullDestinationPath = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(fullDestinationPath)
            ?? throw new ArgumentException("Destination path must include a valid directory.", nameof(destinationPath));
        Directory.CreateDirectory(directory);

        var fileName = Path.GetFileName(fullDestinationPath);
        var temporaryPath = Path.Combine(
            directory,
            $".{fileName}.{Guid.NewGuid():N}.sgpdf.tmp");

        try
        {
            WritePdf(temporaryPath, plan, orderedLabels);
            File.Move(temporaryPath, fullDestinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static ZplRenderedLabel[] ValidateAndOrderRenderedLabels(
        int designCount,
        IReadOnlyList<ZplRenderedLabel> renderedLabels)
    {
        var ordered = new ZplRenderedLabel[designCount];
        var seen = new bool[designCount];

        foreach (var label in renderedLabels)
        {
            if (label.DesignIndex < 0 || label.DesignIndex >= designCount || seen[label.DesignIndex])
            {
                throw new ArgumentException(
                    "Rendered labels must contain exactly one image for each printable design.",
                    nameof(renderedLabels));
            }

            ordered[label.DesignIndex] = label;
            seen[label.DesignIndex] = true;
        }

        if (seen.Any(value => !value))
        {
            throw new ArgumentException(
                "Rendered labels must contain exactly one image for each printable design.",
                nameof(renderedLabels));
        }

        return ordered;
    }

    private static void WritePdf(
        string temporaryPath,
        LabelLayoutPlan plan,
        IReadOnlyList<ZplRenderedLabel> renderedLabels)
    {
        using var document = new PdfDocument();
        var loadedImages = new LoadedImage[renderedLabels.Count];

        try
        {
            for (var designIndex = 0; designIndex < renderedLabels.Count; designIndex++)
                loadedImages[designIndex] = LoadedImage.Create(renderedLabels[designIndex].PngBytes);

            for (long pageIndex = 0; pageIndex < plan.PageCount; pageIndex++)
            {
                var pagePlan = plan.GetPage(pageIndex);
                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(plan.PageWidthMm);
                page.Height = XUnit.FromMillimeter(plan.PageHeightMm);

                using var graphics = XGraphics.FromPdfPage(page);
                foreach (var placement in pagePlan.Placements)
                    DrawPlacement(graphics, loadedImages[placement.DesignIndex].Image, placement);
            }

            document.Save(temporaryPath);
        }
        finally
        {
            foreach (var loadedImage in loadedImages)
                loadedImage?.Dispose();
        }
    }

    private static void DrawPlacement(
        XGraphics graphics,
        XImage image,
        LabelPlacement placement)
    {
        var x = ToPoints(placement.XMm);
        var y = ToPoints(placement.YMm);
        var width = ToPoints(placement.WidthMm);
        var height = ToPoints(placement.HeightMm);

        if (placement.Rotation == LabelRotation.Degrees90)
        {
            var state = graphics.Save();
            graphics.TranslateTransform(x + width, y);
            graphics.RotateTransform(90);
            graphics.DrawImage(image, 0, 0, height, width);
            graphics.Restore(state);
            return;
        }

        graphics.DrawImage(image, x, y, width, height);
    }

    private static double ToPoints(double millimeters)
        => millimeters * PointsPerMillimeter;

    private sealed class LoadedImage : IDisposable
    {
        private LoadedImage(MemoryStream stream, XImage image)
        {
            Stream = stream;
            Image = image;
        }

        public MemoryStream Stream { get; }
        public XImage Image { get; }

        public static LoadedImage Create(byte[] pngBytes)
        {
            ArgumentNullException.ThrowIfNull(pngBytes);
            var stream = new MemoryStream(pngBytes, 0, pngBytes.Length, writable: false, publiclyVisible: true);
            try
            {
                return new LoadedImage(stream, XImage.FromStream(stream));
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Image.Dispose();
            Stream.Dispose();
        }
    }
}
