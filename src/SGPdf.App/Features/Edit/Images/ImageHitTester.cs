using SGPdf.App.Pdf;

namespace SGPdf.App.Pdf
{
    internal readonly record struct PdfPoint(double X, double Y);
}

namespace SGPdf.App.Features.Edit.Images
{
    internal static class ImageHitTester
    {
        private const double GeometryTolerance = 1e-9d;

        internal static ImageObjectKey? HitTest(
            IReadOnlyList<PdfImageObjectInfo> images,
            PdfPoint point)
        {
            ArgumentNullException.ThrowIfNull(images);
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                return null;

            PdfImageObjectInfo? selected = null;
            foreach (var image in images)
            {
                if (!Contains(image, point))
                    continue;

                if (selected is null || image.PageObjectIndex > selected.PageObjectIndex)
                    selected = image;
            }

            return selected is null
                ? null
                : new ImageObjectKey(selected.PageIndex, selected.PageObjectIndex);
        }

        internal static PdfPoint DeviceToPdf(
            double deviceX,
            double deviceY,
            PdfPageDeviceTransform transform)
        {
            if (!double.IsFinite(deviceX) || !double.IsFinite(deviceY))
                throw new ArgumentException("Invalid device point.");

            transform.Validate();
            return new PdfPoint(
                transform.OriginX + (deviceX * transform.XAxisX) + (deviceY * transform.YAxisX),
                transform.OriginY + (deviceX * transform.XAxisY) + (deviceY * transform.YAxisY));
        }

        internal static PdfPoint PdfToDevice(PdfPoint point, PdfPageDeviceTransform transform)
        {
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                throw new ArgumentException("Invalid PDF point.", nameof(point));

            transform.Validate();
            var determinant = (transform.XAxisX * transform.YAxisY) -
                              (transform.XAxisY * transform.YAxisX);
            var dx = point.X - transform.OriginX;
            var dy = point.Y - transform.OriginY;

            return new PdfPoint(
                ((dx * transform.YAxisY) - (dy * transform.YAxisX)) / determinant,
                ((transform.XAxisX * dy) - (transform.XAxisY * dx)) / determinant);
        }

        internal static IReadOnlyList<PdfPoint> GetQuad(PdfObjectMatrix matrix)
            => new[]
            {
                new PdfPoint(matrix.E, matrix.F),
                new PdfPoint(matrix.A + matrix.E, matrix.B + matrix.F),
                new PdfPoint(matrix.A + matrix.C + matrix.E, matrix.B + matrix.D + matrix.F),
                new PdfPoint(matrix.C + matrix.E, matrix.D + matrix.F)
            };

        private static bool Contains(PdfImageObjectInfo image, PdfPoint point)
        {
            var bounds = image.Bounds;
            if (point.X < bounds.Left - GeometryTolerance ||
                point.X > bounds.Right + GeometryTolerance ||
                point.Y < bounds.Bottom - GeometryTolerance ||
                point.Y > bounds.Top + GeometryTolerance)
            {
                return false;
            }

            var matrix = image.Matrix;
            var determinant = (matrix.A * matrix.D) - (matrix.B * matrix.C);
            if (!double.IsFinite(determinant) || Math.Abs(determinant) < GeometryTolerance)
                return false;

            var dx = point.X - matrix.E;
            var dy = point.Y - matrix.F;
            var unitX = ((dx * matrix.D) - (dy * matrix.C)) / determinant;
            var unitY = ((matrix.A * dy) - (matrix.B * dx)) / determinant;

            return unitX >= -GeometryTolerance && unitX <= 1d + GeometryTolerance &&
                   unitY >= -GeometryTolerance && unitY <= 1d + GeometryTolerance;
        }
    }
}
