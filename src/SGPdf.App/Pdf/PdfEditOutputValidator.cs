using System.IO;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Pdf;

internal sealed class PdfEditOutputValidator
{
    private const double PageSizeTolerancePoints = 0.05d;
    private const double MatrixTolerance = 0.05d;
    private const double FontSizeTolerance = 0.01d;
    private const double ValidationDpi = 36d;
    private readonly Action<int, double>? _renderObserver;

    internal PdfEditOutputValidator(Action<int, double>? renderObserver = null)
    {
        _renderObserver = renderObserver;
    }

    internal void Validate(
        string outputPath,
        ImageEditWorkspace workspace,
        CancellationToken cancellationToken = default)
        => ValidateCore(outputPath, workspace, textWorkspace: null, cancellationToken);

    internal void Validate(
        string outputPath,
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(textWorkspace);
        EnsureCombinedWorkspaceIdentity(imageWorkspace, textWorkspace);
        ValidateCore(outputPath, imageWorkspace, textWorkspace, cancellationToken);
    }

    private void ValidateCore(
        string outputPath,
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace? textWorkspace,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(imageWorkspace);
        cancellationToken.ThrowIfCancellationRequested();

        using var source = PdfDocumentSession.Open(imageWorkspace.SourcePath);
        PdfDocumentSession output;
        try
        {
            output = PdfDocumentSession.Open(outputPath);
        }
        catch (Exception ex) when (ex is PdfDocumentOpenException or FileNotFoundException or IOException)
        {
            throw new InvalidDataException("El PDF temporal no se pudo reabrir para validación.", ex);
        }

        using (output)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateDocumentStructure(source, output, cancellationToken);
            ValidateEditedImages(source, output, imageWorkspace, cancellationToken);
            if (textWorkspace is not null)
                ValidateEditedText(output, textWorkspace, cancellationToken);

            var editedPages = imageWorkspace.EditedStates
                .Select(state => state.ObjectRef.Key.PageIndex)
                .Concat(textWorkspace?.EditedStates.Select(state => state.Key.PageIndex) ?? Array.Empty<int>())
                .Distinct()
                .OrderBy(pageIndex => pageIndex)
                .ToArray();

            foreach (var pageIndex in editedPages)
                ValidateEditedPageRender(output, pageIndex, cancellationToken);
        }
    }

    private static void EnsureCombinedWorkspaceIdentity(
        ImageEditWorkspace imageWorkspace,
        TextEditWorkspace textWorkspace)
    {
        ArgumentNullException.ThrowIfNull(imageWorkspace);
        var imagePath = Path.GetFullPath(imageWorkspace.SourcePath);
        var textPath = Path.GetFullPath(textWorkspace.SourcePath);
        if (!string.Equals(imagePath, textPath, StringComparison.OrdinalIgnoreCase) ||
            imageWorkspace.SourceFingerprint != textWorkspace.SourceFingerprint)
        {
            throw new InvalidOperationException(
                "Los workspaces de imagen y texto deben pertenecer al mismo PDF fuente y al mismo fingerprint.");
        }
    }

    private static void ValidateDocumentStructure(
        PdfDocumentSession source,
        PdfDocumentSession output,
        CancellationToken cancellationToken)
    {
        if (output.PageCount != source.PageCount)
        {
            throw new InvalidDataException(
                $"El PDF materializado contiene {output.PageCount} páginas; se esperaban {source.PageCount}.");
        }

        for (var pageIndex = 0; pageIndex < source.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceSize = source.GetPageSize(pageIndex, cancellationToken);
            var outputSize = output.GetPageSize(pageIndex, cancellationToken);
            if (!NearlyEqual(sourceSize.Width, outputSize.Width, PageSizeTolerancePoints) ||
                !NearlyEqual(sourceSize.Height, outputSize.Height, PageSizeTolerancePoints))
            {
                throw new InvalidDataException($"La página {pageIndex + 1} cambió de tamaño durante la edición.");
            }

            var sourceRotation = source.GetPageRotation(pageIndex, cancellationToken);
            var outputRotation = output.GetPageRotation(pageIndex, cancellationToken);
            if (sourceRotation != outputRotation)
                throw new InvalidDataException($"La página {pageIndex + 1} cambió de rotación durante la edición.");
        }
    }

    private void ValidateEditedPageRender(
        PdfDocumentSession output,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rendered = output.RenderPage(pageIndex, ValidationDpi, cancellationToken);
        if (Math.Abs(rendered.Dpi - ValidationDpi) > double.Epsilon ||
            rendered.PixelWidth <= 0 ||
            rendered.PixelHeight <= 0 ||
            rendered.Pixels.Length == 0)
        {
            throw new InvalidDataException($"La página {pageIndex + 1} no produjo un render válido a 36 DPI.");
        }

        _renderObserver?.Invoke(pageIndex, rendered.Dpi);
    }

    private static void ValidateEditedImages(
        PdfDocumentSession source,
        PdfDocumentSession output,
        ImageEditWorkspace workspace,
        CancellationToken cancellationToken)
    {
        foreach (var pageGroup in workspace.EditedStates.GroupBy(state => state.ObjectRef.Key.PageIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var states = pageGroup.ToArray();
            var sourceImages = source.GetImageObjects(pageGroup.Key, cancellationToken);
            var outputImages = output.GetImageObjects(pageGroup.Key, cancellationToken);
            var expectedImageCount = sourceImages.Count - states.Count(state => state.Deleted);
            if (outputImages.Count != expectedImageCount)
            {
                throw new InvalidDataException(
                    $"La página {pageGroup.Key + 1} contiene {outputImages.Count} imágenes; se esperaban {expectedImageCount}.");
            }

            var ordinalsAreStable = states.All(state => !state.Deleted && state.TargetObjectIndex is null);
            if (!ordinalsAreStable)
                continue;

            foreach (var state in states)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var actual = outputImages.SingleOrDefault(
                    image => image.PageObjectIndex == state.ObjectRef.Key.PageObjectIndex);
                if (actual is null)
                    throw new InvalidDataException("No se encontró una imagen editada esperada en el PDF temporal.");

                if (!NearlyEqual(actual.Matrix, state.CurrentMatrix))
                    throw new InvalidDataException("La matriz de una imagen editada no coincide con el estado lógico guardado.");

                if (!IsValidBounds(actual.Bounds))
                    throw new InvalidDataException("Los límites de una imagen editada no son válidos después del guardado.");

                if (state.ReplacementAsset is not null &&
                    (actual.Metadata.PixelWidth == 0 || actual.Metadata.PixelHeight == 0))
                {
                    throw new InvalidDataException("Los metadatos de la imagen reemplazada no son válidos después del guardado.");
                }
            }
        }
    }

    private static void ValidateEditedText(
        PdfDocumentSession output,
        TextEditWorkspace workspace,
        CancellationToken cancellationToken)
    {
        foreach (var pageGroup in workspace.EditedStates.GroupBy(state => state.Key.PageIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputText = output.GetTextObjects(pageGroup.Key, cancellationToken);

            foreach (var state in pageGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var actual = outputText.SingleOrDefault(text => text.Key.PageObjectIndex == state.Key.PageObjectIndex);
                if (actual is null)
                {
                    throw new InvalidDataException(
                        $"No se encontró el objeto TEXT esperado en el ordinal {state.Key.PageObjectIndex} de la página {state.Key.PageIndex + 1}.");
                }

                if (!string.Equals(actual.Text, state.Text, StringComparison.Ordinal))
                    throw new InvalidDataException("El Unicode de un texto editado no coincide exactamente con el estado lógico guardado.");
                if (!NearlyEqual(actual.Matrix, state.Original.Matrix))
                    throw new InvalidDataException("La matriz de un texto editado no coincide con el estado lógico guardado.");
                if (!NearlyEqual(actual.FontSize, state.FontSize, FontSizeTolerance))
                    throw new InvalidDataException("El tamaño de un texto editado no coincide con el estado lógico guardado.");
                if (actual.FillColor != state.FillColor)
                    throw new InvalidDataException("El color de un texto editado no coincide con el estado lógico guardado.");

                ValidateFontRoute(actual.FontName, state);
            }
        }
    }

    private static void ValidateFontRoute(string actualFontName, TextEditState state)
    {
        if (state.FontStrategy == TextFontStrategy.OriginalFont)
        {
            if (!FontNamesMatch(actualFontName, state.Original.FontName))
                throw new InvalidDataException("El texto editado no conservó la ruta de fuente original esperada.");
            return;
        }

        var expectedFallback = Path.GetFileNameWithoutExtension(FallbackFontAsset.FontFileName);
        if (!FontNamesMatch(actualFontName, expectedFallback))
            throw new InvalidDataException("El texto editado no usa la fuente fallback pinneada esperada.");
    }

    private static bool FontNamesMatch(string actual, string expected)
    {
        static string Normalize(string value)
        {
            var subsetSeparator = value.IndexOf('+');
            if (subsetSeparator == 6 && value.Take(6).All(char.IsLetter))
                value = value[(subsetSeparator + 1)..];

            return new string(value.Where(char.IsLetterOrDigit).ToArray());
        }

        return string.Equals(Normalize(actual), Normalize(expected), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidBounds(PdfObjectBounds bounds)
        => double.IsFinite(bounds.Left) && double.IsFinite(bounds.Bottom) &&
           double.IsFinite(bounds.Right) && double.IsFinite(bounds.Top) &&
           bounds.Right > bounds.Left && bounds.Top > bounds.Bottom;

    private static bool NearlyEqual(PdfObjectMatrix left, PdfObjectMatrix right)
        => NearlyEqual(left.A, right.A, MatrixTolerance) &&
           NearlyEqual(left.B, right.B, MatrixTolerance) &&
           NearlyEqual(left.C, right.C, MatrixTolerance) &&
           NearlyEqual(left.D, right.D, MatrixTolerance) &&
           NearlyEqual(left.E, right.E, MatrixTolerance) &&
           NearlyEqual(left.F, right.F, MatrixTolerance);

    private static bool NearlyEqual(double left, double right, double tolerance)
        => double.IsFinite(left) && double.IsFinite(right) && Math.Abs(left - right) <= tolerance;
}