using System.IO;
using SGPdf.App.Features.Ocr;

namespace SGPdf.App.Pdf;

internal sealed record OcrValidationExpectation(
    int ExpectedPageCount,
    IReadOnlyList<OcrTextLinePlacement> Placements,
    IReadOnlyDictionary<int, string> SkippedPageTexts,
    IReadOnlyList<PdfRenderedPage> SourceRenders);

internal sealed record PdfOcrValidationResult(bool Succeeded, string? Error)
{
    internal static PdfOcrValidationResult Success { get; } = new(true, null);

    internal static PdfOcrValidationResult Failure(string error) => new(false, error);
}

internal static class PdfOcrOutputValidator
{
    internal static PdfOcrValidationResult Validate(
        string outputPath,
        OcrValidationExpectation expectation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(expectation);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var output = PdfDocumentSession.Open(outputPath);
            cancellationToken.ThrowIfCancellationRequested();

            if (output.PageCount != expectation.ExpectedPageCount)
            {
                return PdfOcrValidationResult.Failure(
                    $"OCR output page count changed: expected {expectation.ExpectedPageCount}, found {output.PageCount}.");
            }

            var placementFailure = ValidatePlacements(output, expectation, cancellationToken);
            if (placementFailure is not null)
                return placementFailure;

            var skippedFailure = ValidateSkippedPages(output, expectation, cancellationToken);
            if (skippedFailure is not null)
                return skippedFailure;

            var renderFailure = ValidateRenders(output, expectation, cancellationToken);
            if (renderFailure is not null)
                return renderFailure;

            return PdfOcrValidationResult.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PdfOcrValidationResult.Failure(ex.Message);
        }
    }

    private static PdfOcrValidationResult? ValidatePlacements(
        PdfDocumentSession output,
        OcrValidationExpectation expectation,
        CancellationToken cancellationToken)
    {
        foreach (var placement in expectation.Placements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (placement.PageIndex < 0 || placement.PageIndex >= expectation.ExpectedPageCount)
                return PdfOcrValidationResult.Failure("An OCR placement targets a page outside the saved document.");

            var placementGeometryIntersects = false;
            foreach (var word in placement.Words)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(word.Text))
                    return PdfOcrValidationResult.Failure("An OCR placement contains an empty search token.");

                var matches = output.FindTextOnPage(placement.PageIndex, word.Text, cancellationToken);
                var exactTokenFound = false;
                foreach (var match in matches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var extracted = output.GetTextRange(
                        placement.PageIndex,
                        match.StartIndex,
                        match.CharacterCount,
                        cancellationToken);
                    if (!string.Equals(extracted, word.Text, StringComparison.Ordinal))
                        continue;

                    exactTokenFound = true;
                    if (match.Rects.Any(rect => Intersects(rect, placement.Bounds)))
                        placementGeometryIntersects = true;
                }

                if (!exactTokenFound)
                {
                    return PdfOcrValidationResult.Failure(
                        $"OCR token '{word.Text}' was not found with exact Unicode text after reopen.");
                }
            }

            if (!placementGeometryIntersects)
            {
                return PdfOcrValidationResult.Failure(
                    "The reopened OCR text geometry does not intersect its expected source region.");
            }
        }

        return null;
    }

    private static PdfOcrValidationResult? ValidateSkippedPages(
        PdfDocumentSession output,
        OcrValidationExpectation expectation,
        CancellationToken cancellationToken)
    {
        foreach (var (pageIndex, expectedText) in expectation.SkippedPageTexts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageIndex < 0 || pageIndex >= expectation.ExpectedPageCount)
                return PdfOcrValidationResult.Failure("A skipped-page expectation targets a page outside the saved document.");

            var actualText = output.GetPageText(pageIndex, cancellationToken);
            if (!string.Equals(actualText, expectedText, StringComparison.Ordinal))
            {
                return PdfOcrValidationResult.Failure(
                    $"Searchable text changed unexpectedly on skipped page {pageIndex + 1}.");
            }
        }

        return null;
    }

    private static PdfOcrValidationResult? ValidateRenders(
        PdfDocumentSession output,
        OcrValidationExpectation expectation,
        CancellationToken cancellationToken)
    {
        foreach (var reference in expectation.SourceRenders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reference.PageIndex < 0 || reference.PageIndex >= expectation.ExpectedPageCount)
                return PdfOcrValidationResult.Failure("A render expectation targets a page outside the saved document.");

            var actual = output.RenderPage(reference.PageIndex, reference.Dpi, cancellationToken);
            if (actual.PixelWidth != reference.PixelWidth ||
                actual.PixelHeight != reference.PixelHeight ||
                actual.Stride != reference.Stride ||
                !actual.Pixels.AsSpan().SequenceEqual(reference.Pixels))
            {
                return PdfOcrValidationResult.Failure(
                    $"Visible page pixels changed unexpectedly on page {reference.PageIndex + 1}.");
            }
        }

        return null;
    }

    private static bool Intersects(PdfTextRect actual, PdfOcrRect expected) =>
        actual.Right > expected.Left &&
        actual.Left < expected.Right &&
        actual.Top > expected.Bottom &&
        actual.Bottom < expected.Top;
}
