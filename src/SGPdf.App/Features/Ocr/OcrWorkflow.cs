using System.IO;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Utilities;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Ocr;

internal sealed class OcrWorkflow
{
    private static readonly TimeSpan ProductionRecognitionTimeout = TimeSpan.FromMinutes(2);

    private readonly string _sourcePath;
    private readonly string _destinationPath;
    private readonly Func<string>? _workspacePathFactory;
    private readonly Func<PdfDocumentSession, CancellationToken, OcrPreflightResult> _preflight;
    private readonly Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult> _eligibility;
    private readonly Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage> _rasterize;
    private readonly Func<PdfRenderedPage, int, OcrLanguage, string?, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>> _recognizeAndPlan;
    private readonly Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult> _writer;

    internal OcrWorkflow(string sourcePath, string destinationPath)
        : this(
            sourcePath,
            destinationPath,
            CreateProductionWorkspacePath,
            new OcrPreflight().Inspect,
            new OcrPageEligibilityPolicy().Resolve,
            OcrRasterizer.Render,
            RecognizeAndPlanProductionAsync,
            new PdfOcrWriter().SaveAsCopy)
    {
    }

    internal OcrWorkflow(
        string sourcePath,
        string destinationPath,
        Func<PdfDocumentSession, CancellationToken, OcrPreflightResult> preflight,
        Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult> eligibility,
        Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage> rasterize,
        Func<PdfRenderedPage, int, OcrLanguage, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>> recognizeAndPlan,
        Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult> writer)
        : this(
            sourcePath,
            destinationPath,
            workspacePathFactory: null,
            preflight,
            eligibility,
            rasterize,
            recognizeAndPlan: (raster, pageIndex, language, _, cancellationToken) =>
                recognizeAndPlan(raster, pageIndex, language, cancellationToken),
            writer,
            useWorkspace: false)
    {
        ArgumentNullException.ThrowIfNull(recognizeAndPlan);
    }

    internal OcrWorkflow(
        string sourcePath,
        string destinationPath,
        Func<string> workspacePathFactory,
        Func<PdfDocumentSession, CancellationToken, OcrPreflightResult> preflight,
        Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult> eligibility,
        Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage> rasterize,
        Func<PdfRenderedPage, int, OcrLanguage, string, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>> recognizeAndPlan,
        Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult> writer)
        : this(
            sourcePath,
            destinationPath,
            workspacePathFactory,
            preflight,
            eligibility,
            rasterize,
            recognizeAndPlan: (raster, pageIndex, language, workspacePath, cancellationToken) =>
                recognizeAndPlan(
                    raster,
                    pageIndex,
                    language,
                    workspacePath ?? throw new InvalidOperationException("El workspace OCR no está disponible."),
                    cancellationToken),
            writer,
            useWorkspace: true)
    {
        ArgumentNullException.ThrowIfNull(workspacePathFactory);
        ArgumentNullException.ThrowIfNull(recognizeAndPlan);
    }

    private OcrWorkflow(
        string sourcePath,
        string destinationPath,
        Func<string>? workspacePathFactory,
        Func<PdfDocumentSession, CancellationToken, OcrPreflightResult> preflight,
        Func<PdfDocumentSession, OcrPageScope, CancellationToken, OcrPageEligibilityResult> eligibility,
        Func<PdfDocumentSession, int, CancellationToken, PdfRenderedPage> rasterize,
        Func<PdfRenderedPage, int, OcrLanguage, string?, CancellationToken, Task<IReadOnlyList<OcrTextLinePlacement>>> recognizeAndPlan,
        Func<string, PdfEditSourceFingerprint, IReadOnlyList<OcrTextLinePlacement>, string, CancellationToken, PdfOcrWriteResult> writer,
        bool useWorkspace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        _sourcePath = Path.GetFullPath(sourcePath);
        _destinationPath = Path.GetFullPath(destinationPath);
        _workspacePathFactory = useWorkspace ? workspacePathFactory : null;
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

        string? workspacePath = null;
        if (_workspacePathFactory is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            workspacePath = CreateWorkspace(_workspacePathFactory());
        }

        try
        {
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
                    workspacePath,
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
        finally
        {
            if (workspacePath is not null)
                DeleteWorkspace(workspacePath);
        }
    }

    private static string CreateProductionWorkspacePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"sgpdf-ocr-{Guid.NewGuid():N}");
    }

    private static async Task<IReadOnlyList<OcrTextLinePlacement>> RecognizeAndPlanProductionAsync(
        PdfRenderedPage raster,
        int pageIndex,
        OcrLanguage language,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(raster);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fileStem = $"page-{pageIndex:D6}";
        var imagePath = Path.Combine(workspacePath, fileStem + ".bmp");
        var outputBasePath = Path.Combine(workspacePath, fileStem);
        var tsvPath = outputBasePath + ".tsv";

        try
        {
            OcrRasterFileSerializer.WriteBmp(raster, imagePath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var recognizer = new TesseractProcessRecognizer();
            var processResult = await recognizer.RecognizeAsync(
                imagePath,
                outputBasePath,
                language,
                ProductionRecognitionTimeout,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var words = TesseractTsvParser.ParseFile(processResult.TsvPath);
            cancellationToken.ThrowIfCancellationRequested();
            return OcrTextLayerPlanner.Build(words, raster);
        }
        finally
        {
            DeletePageArtifact(imagePath);
            DeletePageArtifact(tsvPath);
        }
    }

    private static string CreateWorkspace(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
            throw new IOException("El workspace temporal OCR ya existe.");

        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    private static void DeletePageArtifact(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void DeleteWorkspace(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup. Publication remains owned by the transactional writer.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup. Publication remains owned by the transactional writer.
        }
    }
}
