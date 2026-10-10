using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Edit.Images;

internal enum ImageResizeCorner
{
    BottomLeft,
    BottomRight,
    TopRight,
    TopLeft
}

internal static class ImageTransformMath
{
    private const double Epsilon = 1e-9d;

    internal static PdfObjectMatrix Translate(PdfObjectMatrix matrix, double dx, double dy)
    {
        ValidateMatrix(matrix);
        if (!double.IsFinite(dx) || !double.IsFinite(dy))
            throw new ArgumentException("El desplazamiento debe ser finito.");

        return matrix with { E = matrix.E + dx, F = matrix.F + dy };
    }

    internal static PdfObjectMatrix RotateAroundCenter(PdfObjectMatrix matrix, double degrees)
    {
        ValidateMatrix(matrix);
        if (!double.IsFinite(degrees))
            throw new ArgumentException("El ángulo debe ser finito.", nameof(degrees));

        var radians = degrees * Math.PI / 180d;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        var center = PointAt(matrix, .5d, .5d);

        var a = (cosine * matrix.A) - (sine * matrix.B);
        var b = (sine * matrix.A) + (cosine * matrix.B);
        var c = (cosine * matrix.C) - (sine * matrix.D);
        var d = (sine * matrix.C) + (cosine * matrix.D);
        var e = center.X - ((a + c) * .5d);
        var f = center.Y - ((b + d) * .5d);

        var result = new PdfObjectMatrix(a, b, c, d, e, f);
        ValidateMatrix(result);
        return result;
    }

    internal static PdfObjectMatrix ResizeFromCorner(
        PdfObjectMatrix matrix,
        ImageResizeCorner corner,
        PdfPoint targetPagePoint,
        bool preserveAspectRatio,
        double minimumSizePdf)
    {
        ValidateMatrix(matrix);
        if (!double.IsFinite(targetPagePoint.X) || !double.IsFinite(targetPagePoint.Y))
            throw new ArgumentException("El punto destino debe ser finito.", nameof(targetPagePoint));
        if (!double.IsFinite(minimumSizePdf) || minimumSizePdf <= 0d)
            throw new ArgumentOutOfRangeException(nameof(minimumSizePdf));

        var (cornerU, cornerV) = CornerCoordinates(corner);
        var anchorU = 1d - cornerU;
        var anchorV = 1d - cornerV;
        var anchor = PointAt(matrix, anchorU, anchorV);
        var targetX = targetPagePoint.X - anchor.X;
        var targetY = targetPagePoint.Y - anchor.Y;

        double scaleU;
        double scaleV;
        if (preserveAspectRatio)
        {
            var currentCorner = PointAt(matrix, cornerU, cornerV);
            var currentX = currentCorner.X - anchor.X;
            var currentY = currentCorner.Y - anchor.Y;
            var lengthSquared = (currentX * currentX) + (currentY * currentY);
            if (!double.IsFinite(lengthSquared) || lengthSquared <= Epsilon)
                throw new ArgumentException("La geometría de la imagen es degenerada.", nameof(matrix));

            var scale = ((targetX * currentX) + (targetY * currentY)) / lengthSquared;
            if (!double.IsFinite(scale) || scale <= Epsilon)
                throw new ArgumentOutOfRangeException(nameof(targetPagePoint), "El resize cruzaría la esquina ancla.");

            scaleU = scale;
            scaleV = scale;
        }
        else
        {
            var determinant = Determinant(matrix);
            var localU = ((targetX * matrix.D) - (targetY * matrix.C)) / determinant;
            var localV = ((matrix.A * targetY) - (matrix.B * targetX)) / determinant;
            var signU = cornerU - anchorU;
            var signV = cornerV - anchorV;
            scaleU = localU / signU;
            scaleV = localV / signV;

            if (!double.IsFinite(scaleU) || !double.IsFinite(scaleV) ||
                scaleU <= Epsilon || scaleV <= Epsilon)
            {
                throw new ArgumentOutOfRangeException(nameof(targetPagePoint), "El resize cruzaría la esquina ancla.");
            }
        }

        var a = matrix.A * scaleU;
        var b = matrix.B * scaleU;
        var c = matrix.C * scaleV;
        var d = matrix.D * scaleV;
        var e = anchor.X - (a * anchorU) - (c * anchorV);
        var f = anchor.Y - (b * anchorU) - (d * anchorV);
        var result = new PdfObjectMatrix(a, b, c, d, e, f);

        ValidateMatrix(result);
        var width = Math.Sqrt((a * a) + (b * b));
        var height = Math.Sqrt((c * c) + (d * d));
        if (width < minimumSizePdf || height < minimumSizePdf)
            throw new ArgumentOutOfRangeException(nameof(targetPagePoint), "La imagen quedaría por debajo del tamaño mínimo permitido.");

        return result;
    }

    private static (double U, double V) CornerCoordinates(ImageResizeCorner corner)
        => corner switch
        {
            ImageResizeCorner.BottomLeft => (0d, 0d),
            ImageResizeCorner.BottomRight => (1d, 0d),
            ImageResizeCorner.TopRight => (1d, 1d),
            ImageResizeCorner.TopLeft => (0d, 1d),
            _ => throw new ArgumentOutOfRangeException(nameof(corner))
        };

    private static PdfPoint PointAt(PdfObjectMatrix matrix, double u, double v)
        => new(
            (matrix.A * u) + (matrix.C * v) + matrix.E,
            (matrix.B * u) + (matrix.D * v) + matrix.F);

    private static double Determinant(PdfObjectMatrix matrix)
        => (matrix.A * matrix.D) - (matrix.B * matrix.C);

    private static void ValidateMatrix(PdfObjectMatrix matrix)
    {
        if (!double.IsFinite(matrix.A) || !double.IsFinite(matrix.B) ||
            !double.IsFinite(matrix.C) || !double.IsFinite(matrix.D) ||
            !double.IsFinite(matrix.E) || !double.IsFinite(matrix.F))
        {
            throw new ArgumentException("La matriz de imagen debe contener valores finitos.", nameof(matrix));
        }

        var determinant = Determinant(matrix);
        if (!double.IsFinite(determinant) || Math.Abs(determinant) <= Epsilon)
            throw new ArgumentException("La matriz de imagen no puede ser singular.", nameof(matrix));
    }
}
