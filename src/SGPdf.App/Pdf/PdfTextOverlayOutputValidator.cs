using SGPdf.App.Features.Utilities;

namespace SGPdf.App.Pdf;

internal sealed record PdfTextOverlayValidationResult(bool IsValid, string? Error)
{
    internal static PdfTextOverlayValidationResult Valid { get; } = new(true, null);

    internal static PdfTextOverlayValidationResult Invalid(string error) => new(false, error);
}

internal sealed class PdfTextOverlayOutputValidator
{
    private const double MatrixTolerance = 0.001d;
    private const double FontSizeTolerance = 0.01d;

    internal PdfTextOverlayValidationResult Validate(
        string outputPath,
        IReadOnlyList<TextOverlayPlacement> placements,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(placements);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var session = PdfDocumentSession.Open(outputPath);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var placement in placements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (placement.PageIndex < 0 || placement.PageIndex >= session.PageCount)
                    return PdfTextOverlayValidationResult.Invalid(
                        $"Overlay page {placement.PageIndex} is outside the saved document.");

                var expectedAlpha = ToAlpha(placement.FillColor.Alpha, placement.Opacity);
                var matched = session.GetTextObjects(placement.PageIndex, cancellationToken)
                    .Any(candidate =>
                        string.Equals(candidate.Text, placement.Text, StringComparison.Ordinal) &&
                        Math.Abs(candidate.FontSize - placement.FontSize) <= FontSizeTolerance &&
                        candidate.FillColor.Red == placement.FillColor.Red &&
                        candidate.FillColor.Green == placement.FillColor.Green &&
                        candidate.FillColor.Blue == placement.FillColor.Blue &&
                        candidate.FillColor.Alpha == expectedAlpha &&
                        MatrixMatches(candidate.Matrix, placement.Matrix));

                if (!matched)
                {
                    return PdfTextOverlayValidationResult.Invalid(
                        $"Saved output did not preserve the expected text overlay on page {placement.PageIndex}.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return PdfTextOverlayValidationResult.Valid;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PdfTextOverlayValidationResult.Invalid(
                $"Could not validate the saved overlay output: {ex.Message}");
        }
    }

    private static uint ToAlpha(uint baseAlpha, double opacity) =>
        checked((uint)Math.Round(baseAlpha * opacity, MidpointRounding.AwayFromZero));

    private static bool MatrixMatches(PdfObjectMatrix actual, PdfObjectMatrix expected) =>
        Math.Abs(actual.A - expected.A) <= MatrixTolerance &&
        Math.Abs(actual.B - expected.B) <= MatrixTolerance &&
        Math.Abs(actual.C - expected.C) <= MatrixTolerance &&
        Math.Abs(actual.D - expected.D) <= MatrixTolerance &&
        Math.Abs(actual.E - expected.E) <= MatrixTolerance &&
        Math.Abs(actual.F - expected.F) <= MatrixTolerance;
}
