using System.IO;
using SGPdf.App.Features.Edit.Images;

namespace SGPdf.App.Pdf;

internal sealed class PdfEditOutputValidator
{
    private const double PageSizeTolerancePoints = 0.05d;
    private const double MatrixTolerance = 0.05d;
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
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(workspace);
        cancellationToken.ThrowIfCancellationRequested();

        using var source = PdfDocumentSession.Open(workspace.SourcePath);
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

                var rendered = output.RenderPage(pageIndex, ValidationDpi, cancellationToken);
                if (Math.Abs(rendered.Dpi - ValidationDpi) > double.Epsilon)
                    throw new InvalidDataException($"La página {pageIndex + 1} no se validó a 36 DPI.");
                _renderObserver?.Invoke(pageIndex, rendered.Dpi);
            }

            ValidateEditedObjects(source, output, workspace, cancellationToken);
        }
    }

    private static void ValidateEditedObjects(
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
