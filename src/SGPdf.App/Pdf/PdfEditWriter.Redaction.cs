using System.IO;
using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Redaction;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Pdf;

internal static class PdfEditWriterRedactionExtensions
{
    private const double MatrixTolerance = 0.05d;
    private const double BoundsTolerance = 0.10d;
    private const double FontSizeTolerance = 0.01d;

    internal static void SaveAsCopy(
        this PdfEditWriter writer,
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace,
        RedactionWorkspace redactionWorkspace,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(imageWorkspace);
        ArgumentNullException.ThrowIfNull(textWorkspace);
        ArgumentNullException.ThrowIfNull(redactionWorkspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureWorkspaceIdentity(imageWorkspace, textWorkspace, redactionWorkspace);
        if (redactionWorkspace.SourceOpenedWithPassword)
            throw new InvalidOperationException("Los PDF abiertos con contraseña no se pueden materializar en EDITAR.");
        if (!redactionWorkspace.IsDirty)
        {
            writer.SaveAsCopy(imageWorkspace, textWorkspace, destinationPath, warningsConfirmed, cancellationToken);
            return;
        }

        var sourcePath = Path.GetFullPath(redactionWorkspace.SourcePath);
        var destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourcePath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Guardar como requiere una ruta diferente al PDF fuente.", nameof(destinationPath));
        if (!redactionWorkspace.SourceFingerprint.MatchesCurrentFile(sourcePath))
            throw new IOException("El PDF fuente cambió o ya no está disponible.");

        var editedTextKeys = textWorkspace.EditedStates.Select(state => state.Key).ToHashSet();
        if (redactionWorkspace.Targets.Any(target => editedTextKeys.Contains(target.Original.Key)))
            throw new InvalidOperationException("Un mismo objeto no puede editarse y redactarse en la misma operación.");

        ValidateSourceSnapshots(redactionWorkspace, cancellationToken);

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath)
            ?? throw new ArgumentException("La ruta de destino no tiene directorio.", nameof(destinationPath));
        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException(destinationDirectory);

        var fileName = Path.GetFileName(destinationFullPath);
        var stagingPath = Path.Combine(
            destinationDirectory,
            $".{fileName}.{Guid.NewGuid():N}.edit-stage.sgpdf.tmp");
        var redactedPath = Path.Combine(
            destinationDirectory,
            $".{fileName}.{Guid.NewGuid():N}.redaction-stage.sgpdf.tmp");

        try
        {
            writer.SaveAsCopy(
                imageWorkspace,
                textWorkspace,
                stagingPath,
                warningsConfirmed,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            var stagedTargets = ResolveTargetsInStaging(
                stagingPath,
                redactionWorkspace.Targets,
                cancellationToken);

            new PdfRedactionMaterializer().Materialize(
                stagingPath,
                stagedTargets,
                redactedPath,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            new PdfRedactionOutputValidator().Validate(
                redactedPath,
                redactionWorkspace,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (!redactionWorkspace.SourceFingerprint.MatchesCurrentFile(sourcePath))
                throw new IOException("El PDF fuente cambió o ya no está disponible.");

            if (File.Exists(destinationFullPath))
                File.Replace(redactedPath, destinationFullPath, null, ignoreMetadataErrors: true);
            else
                File.Move(redactedPath, destinationFullPath);
        }
        finally
        {
            TryDelete(stagingPath);
            TryDelete(redactedPath);
        }
    }

    private static void EnsureWorkspaceIdentity(
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace,
        RedactionWorkspace redactionWorkspace)
    {
        var imagePath = Path.GetFullPath(imageWorkspace.SourcePath);
        var textPath = Path.GetFullPath(textWorkspace.SourcePath);
        var redactionPath = Path.GetFullPath(redactionWorkspace.SourcePath);
        if (!string.Equals(imagePath, textPath, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(imagePath, redactionPath, StringComparison.OrdinalIgnoreCase) ||
            imageWorkspace.SourceFingerprint != textWorkspace.SourceFingerprint ||
            imageWorkspace.SourceFingerprint != redactionWorkspace.SourceFingerprint)
        {
            throw new InvalidOperationException(
                "Los workspaces de imagen, texto y redacción deben pertenecer al mismo PDF fuente y al mismo fingerprint.");
        }
    }

    private static void ValidateSourceSnapshots(
        RedactionWorkspace workspace,
        CancellationToken cancellationToken)
    {
        using var source = PdfDocumentSession.Open(workspace.SourcePath);
        var preflight = new PdfEditPreflightInspector().Inspect(source, cancellationToken);

        foreach (var group in workspace.Targets.GroupBy(target => target.Original.Key.PageIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targets = group.ToArray();
            var pageObjects = source.GetTextObjects(group.Key, cancellationToken);
            var policy = RedactionPolicy.Evaluate(
                pageObjects,
                targets.Select(target => target.Original.Key).ToArray(),
                preflight);
            if (!policy.Allowed)
            {
                throw new InvalidOperationException(
                    policy.Reason ?? "La selección no superó la política de redacción segura.");
            }

            var pageObjectsByKey = pageObjects.ToDictionary(item => item.Key);
            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!pageObjectsByKey.TryGetValue(target.Original.Key, out var actual) ||
                    !EquivalentSnapshot(actual, target.Original))
                {
                    throw new InvalidOperationException(
                        "El texto fuente ya no coincide con el snapshot del plan de redacción.");
                }
            }
        }
    }

    private static IReadOnlyList<PdfTextObjectInfo> ResolveTargetsInStaging(
        string stagingPath,
        IReadOnlyList<RedactionTarget> targets,
        CancellationToken cancellationToken)
    {
        using var staging = PdfDocumentSession.Open(stagingPath);
        var resolved = new List<PdfTextObjectInfo>(targets.Count);

        foreach (var group in targets.GroupBy(target => target.Original.Key.PageIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidates = staging.GetTextObjects(group.Key, cancellationToken);
            foreach (var target in group)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var matches = candidates
                    .Where(candidate => EquivalentContent(candidate, target.Original))
                    .ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidOperationException(
                        "La copia staging ya no coincide de forma inequívoca con el snapshot de redacción.");
                }
                resolved.Add(matches[0]);
            }
        }

        return resolved
            .OrderBy(item => item.Key.PageIndex)
            .ThenBy(item => item.Key.PageObjectIndex)
            .ToArray();
    }

    private static bool EquivalentSnapshot(PdfTextObjectInfo actual, PdfTextObjectInfo expected)
        => actual.Key == expected.Key && EquivalentContent(actual, expected);

    private static bool EquivalentContent(PdfTextObjectInfo actual, PdfTextObjectInfo expected)
        => string.Equals(actual.Text, expected.Text, StringComparison.Ordinal) &&
           NearlyEqual(actual.Matrix.A, expected.Matrix.A, MatrixTolerance) &&
           NearlyEqual(actual.Matrix.B, expected.Matrix.B, MatrixTolerance) &&
           NearlyEqual(actual.Matrix.C, expected.Matrix.C, MatrixTolerance) &&
           NearlyEqual(actual.Matrix.D, expected.Matrix.D, MatrixTolerance) &&
           NearlyEqual(actual.Matrix.E, expected.Matrix.E, MatrixTolerance) &&
           NearlyEqual(actual.Matrix.F, expected.Matrix.F, MatrixTolerance) &&
           NearlyEqual(actual.Bounds.Left, expected.Bounds.Left, BoundsTolerance) &&
           NearlyEqual(actual.Bounds.Bottom, expected.Bounds.Bottom, BoundsTolerance) &&
           NearlyEqual(actual.Bounds.Right, expected.Bounds.Right, BoundsTolerance) &&
           NearlyEqual(actual.Bounds.Top, expected.Bounds.Top, BoundsTolerance) &&
           string.Equals(actual.FontName, expected.FontName, StringComparison.Ordinal) &&
           NearlyEqual(actual.FontSize, expected.FontSize, FontSizeTolerance) &&
           actual.FillColor == expected.FillColor &&
           actual.TextRenderMode == expected.TextRenderMode;

    private static bool NearlyEqual(double left, double right, double tolerance)
        => double.IsFinite(left) && double.IsFinite(right) && Math.Abs(left - right) <= tolerance;

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
