using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal sealed class OcrInvisibleTextCapabilitySnapshot : IDisposable
{
    internal required string DirectoryPath { get; init; }
    internal required string SourcePath { get; init; }
    internal required string OutputPath { get; init; }
    internal required byte[] SourceBytesBefore { get; init; }
    internal required byte[] SourceBytesAfter { get; init; }
    internal required PdfRenderedPage SourceRender { get; init; }
    internal required PdfRenderedPage OutputRender { get; init; }
    internal required string ExtractedText { get; init; }
    internal required int RenderMode { get; init; }
    internal required IReadOnlyList<PdfTextMatch> NinoMatches { get; init; }
    internal required IReadOnlyList<PdfTextMatch> BogotaMatches { get; init; }
    internal required IReadOnlyList<PdfTextMatch> NumberMatches { get; init; }
    internal required IReadOnlyList<PdfTextRect> TextRects { get; init; }
    internal required bool TextGeometryIntersectsExpectedVisibleRegion { get; init; }
    internal required bool SourceHadSearchableText { get; init; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal static class OcrInvisibleTextNativeHarness
{
    internal const string SearchableText =
        "Factura número 12345 — NIÑO Bogotá, información útil. Total: $98.765,43";

    internal static OcrInvisibleTextCapabilitySnapshot Characterize(bool rotate90) =>
        throw new NotImplementedException("F10 RED: invisible-text native characterization harness is not implemented yet.");
}
