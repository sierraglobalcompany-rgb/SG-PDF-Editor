using System.IO;

namespace SGPdf.App.Pdf;

internal readonly record struct OrganizeExpectedPage(
    PdfPageSize PageSize,
    int RotationQuarterTurns);

internal sealed class OrganizeOutputValidator
{
    private const double PageSizeTolerancePoints = 0.05d;
    private const double ValidationDpi = 36d;
    private readonly Action<int, double>? _renderObserver;

    internal OrganizeOutputValidator(Action<int, double>? renderObserver = null)
    {
        _renderObserver = renderObserver;
    }

    internal void Validate(
        string outputPath,
        IReadOnlyList<OrganizeExpectedPage> expectedPages,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(expectedPages);
        if (expectedPages.Count == 0)
            throw new ArgumentException("Se esperaba al menos una página de salida.", nameof(expectedPages));

        cancellationToken.ThrowIfCancellationRequested();
        using var session = PdfDocumentSession.Open(outputPath);
        cancellationToken.ThrowIfCancellationRequested();

        if (session.PageCount != expectedPages.Count)
        {
            throw new InvalidDataException(
                $"El PDF materializado contiene {session.PageCount} páginas; se esperaban {expectedPages.Count}.");
        }

        for (var pageIndex = 0; pageIndex < expectedPages.Count; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expected = expectedPages[pageIndex];
            ValidateExpectedDescriptor(expected, pageIndex);

            var actualSize = session.GetPageSize(pageIndex, cancellationToken);
            if (!NearlyEqual(actualSize.Width, expected.PageSize.WidthPoints) ||
                !NearlyEqual(actualSize.Height, expected.PageSize.HeightPoints))
            {
                throw new InvalidDataException(
                    $"La página {pageIndex + 1} cambió de tamaño durante la materialización.");
            }

            var actualRotation = session.GetPageRotation(pageIndex, cancellationToken);
            if (actualRotation != expected.RotationQuarterTurns)
            {
                throw new InvalidDataException(
                    $"La página {pageIndex + 1} cambió de rotación durante la materialización.");
            }

            var rendered = session.RenderPage(pageIndex, ValidationDpi, cancellationToken);
            if (Math.Abs(rendered.Dpi - ValidationDpi) > double.Epsilon)
                throw new InvalidDataException($"La página {pageIndex + 1} no se validó a 36 DPI.");

            _renderObserver?.Invoke(pageIndex, rendered.Dpi);
        }
    }

    private static void ValidateExpectedDescriptor(OrganizeExpectedPage expected, int pageIndex)
    {
        if (!double.IsFinite(expected.PageSize.WidthPoints) || expected.PageSize.WidthPoints <= 0d ||
            !double.IsFinite(expected.PageSize.HeightPoints) || expected.PageSize.HeightPoints <= 0d)
        {
            throw new ArgumentException($"El tamaño esperado de la página {pageIndex + 1} no es válido.");
        }

        if (expected.RotationQuarterTurns is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(expected), "La rotación esperada debe estar entre 0 y 3.");
    }

    private static bool NearlyEqual(double actual, double expected)
        => Math.Abs(actual - expected) <= PageSizeTolerancePoints;
}
