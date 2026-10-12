using System.IO;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal sealed class OcrWorkflow
{
    private readonly string _sourcePath;
    private readonly string _destinationPath;
    private readonly Func<PdfDocumentSession, CancellationToken, OcrPreflightResult> _preflight;
    private readonly Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult> _eligibility;
    private readonly Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage> _rasterize;
    private readonly Func<PdfRenderedPage, int, OcrLanguage, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>> _recognizeAndPlan;
    private readonly Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult> _writer;

    internal OcrWorkflow(
        string sourcePath,
        string destinationPath,
        Func<PdfDocumentSession, CancellationToken, OcrPreflightResult> preflight,
        Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult> eligibility,
        Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage> rasterize,
        Func<PdfRenderedPage, int, OcrLanguage, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>> recognizeAndPlan,
        Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult> writer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        _sourcePath = Path.GetFullPath(sourcePath);
        _destinationPath = Path.GetFullPath(destinationPath);
        _preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
        _eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
        _rasterize = rasterize ?? throw new ArgumentNullException(nameof(rasterize));
        _recognizeAndPlan = recognizeAndPlan ?? throw new ArgumentNullException(nameof(recognizeAndPlan));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    internal async Task<OcrResult> RunAsync(
        OcrRequest request,
        IProgress<OcrProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var fingerprint = PdfEditSourceFingerprint.Capture(_sourcePath);
        cancellationToken.ThrowIfCancellationRequested();

        using var session = PdfDocumentSession.Open(_sourcePath);
        var preflight = _preflight(session, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!preflight.CanProceed)
        {
            throw new InvalidOperationException(
                preflight.Reason ?? "El PDF no puede procesarse con OCR de forma segura.");
        }

        var eligibility = _eligibility(session, request.Scope, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var candidates = eligibility.CandidatePageIndices.ToArray();
        var skipped = eligibility.SkippedPageIndices.ToArray();
        if (candidates.Length == 0)
            return OcrResult.NoOp(skipped);

        var placements = new List<OcrTextLinePlacement>();
        for (var index = 0; index < candidates.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageIndex = candidates[index];
            var ordinal = index + 1;

            progress?.Report(new OcrProgress(
                ordinal,
                candidates.Length,
                pageIndex,
                OcrProgressStage.Rendering));
            var raster = _rasterize(session, pageIndex, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new OcrProgress(
                ordinal,
                candidates.Length,
                pageIndex,
                OcrProgressStage.Recognizing));
            var pagePlacements = await _recognizeAndPlan(
                raster,
                pageIndex,
                request.Language,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (pagePlacements is null)
                throw new InvalidOperationException("El reconocimiento OCR devolvió un resultado nulo.");

            placements.AddRange(pagePlacements);
        }

        if (placements.Count == 0)
            throw new InvalidOperationException("OCR no produjo texto utilizable para las páginas elegibles.");

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new OcrProgress(
            candidates.Length,
            candidates.Length,
            candidates[^1],
            OcrProgressStage.Writing));

        var writeResult = _writer(
            _sourcePath,
            fingerprint,
            placements,
            _destinationPath,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!writeResult.Succeeded)
        {
            throw new InvalidOperationException(
                writeResult.Error ?? "No se pudo publicar el PDF OCR.");
        }

        return OcrResult.Success(candidates, skipped);
    }
}
