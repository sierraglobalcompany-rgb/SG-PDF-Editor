using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Labels;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Utilities;

internal sealed record PdfCropMargins(double LeftMm, double RightMm, double TopMm, double BottomMm);

internal sealed record PdfCropResult(bool Succeeded, string? Error)
{
    internal static PdfCropResult Success { get; } = new(true, null);
    internal static PdfCropResult Failure(string error) => new(false, error);
}

internal sealed class PdfCropCoordinator
{
    private const double PointsPerMillimeter = 72d / 25.4d;
    private const double MinimumPageDimensionPoints = 20d;
    private const double PageSizeTolerancePoints = 0.5d;
    private static readonly TimeSpan QpdfTimeout = TimeSpan.FromSeconds(15);
    private static readonly HashSet<string> SupportedCatalogKeys = new(StringComparer.Ordinal)
    {
        "/Type", "/Pages", "/Version", "/Metadata"
    };
    private static readonly HashSet<string> SupportedPageKeys = new(StringComparer.Ordinal)
    {
        "/Type", "/Parent", "/Resources", "/MediaBox", "/CropBox", "/Rotate",
        "/Contents", "/BleedBox", "/TrimBox", "/ArtBox", "/UserUnit", "/Group"
    };
    private readonly string _qpdfPath;
    private readonly Func<ProcessStartInfo, TimeSpan, CancellationToken, Task<ChildProcessResult>> _runProcessAsync;

    internal PdfCropCoordinator()
        : this(
            Path.Combine(AppContext.BaseDirectory, "qpdf", "qpdf.exe"),
            (startInfo, timeout, token) => ChildProcessRunner.RunAsync(startInfo, timeout, token))
    {
    }

    internal PdfCropCoordinator(
        string qpdfPath,
        Func<ProcessStartInfo, TimeSpan, CancellationToken, Task<ChildProcessResult>> runProcessAsync)
    {
        _qpdfPath = qpdfPath ?? throw new ArgumentNullException(nameof(qpdfPath));
        _runProcessAsync = runProcessAsync ?? throw new ArgumentNullException(nameof(runProcessAsync));
    }

    internal async Task<PdfCropResult> CreateCroppedCopyAsync(
        PdfDocumentSession sourceSession,
        string destinationPath,
        PdfCropMargins margins,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceSession);
        string? temporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateMargins(margins);
            var sourcePath = Path.GetFullPath(sourceSession.FilePath);
            var destination = Path.GetFullPath(destinationPath);
            ValidatePaths(sourcePath, destination);
            if (sourceSession.OpenedWithPassword)
                return PdfCropResult.Failure("El recorte seguro de esta versión no admite PDF abierto con contraseña.");
            if (!File.Exists(_qpdfPath))
                return PdfCropResult.Failure("No está disponible el verificador local requerido para esta operación.");

            var sourceHash = await ComputeSha256Async(sourcePath, cancellationToken).ConfigureAwait(false);
            var encryptionCheck = await RunQpdfAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            if (encryptionCheck.ExitCode != 2)
                return PdfCropResult.Failure("El PDF está cifrado o no se pudo demostrar que no lo esté.");

            var sourceSnapshot = PdfOperationPreservationSnapshot.Capture(sourceSession, cancellationToken);
            var sourceBoxes = ReadAndValidateSource(
                sourcePath,
                sourceSession,
                cancellationToken,
                out var sourceWriterState);
            var destinationDirectory = Path.GetDirectoryName(destination)!;
            temporaryPath = Path.Combine(destinationDirectory, $".sgpdf-crop-{Guid.NewGuid():N}.pdf");

            using (var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify))
            {
                if (!HasOnlySupportedStructure(document))
                    return PdfCropResult.Failure("El documento contiene una estructura que el recorte seguro no puede validar.");
                var originalModificationDate = document.Info.ModificationDate;
                var originalCreationDate = document.Info.CreationDate;
                var hadModificationDate = !string.IsNullOrEmpty(
                    sourceSession.GetOrganizeMetadataText("ModDate", cancellationToken));

                for (var pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var box = sourceBoxes[pageIndex].CropBox;
                    var left = margins.LeftMm * PointsPerMillimeter;
                    var right = margins.RightMm * PointsPerMillimeter;
                    var top = margins.TopMm * PointsPerMillimeter;
                    var bottom = margins.BottomMm * PointsPerMillimeter;
                    var x1 = box.X1 + left;
                    var y1 = box.Y1 + bottom;
                    var x2 = box.X2 - right;
                    var y2 = box.Y2 - top;
                    ValidateCropBox(x1, y1, x2, y2, sourceBoxes[pageIndex].MediaBox);
                    document.Pages[pageIndex].CropBox = CreateRectangle(x1, y1, x2, y2);
                }

                if (hadModificationDate)
                    document.Info.ModificationDate = originalModificationDate;
                document.Info.CreationDate = originalCreationDate;
                document.Save(temporaryPath);
            }

            cancellationToken.ThrowIfCancellationRequested();
            ValidateOutput(
                temporaryPath,
                sourceSession,
                sourceSnapshot,
                sourceBoxes,
                sourceWriterState,
                margins,
                cancellationToken);
            var currentHash = await ComputeSha256Async(sourcePath, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(sourceHash, currentHash))
                return PdfCropResult.Failure("El PDF original cambió durante el recorte; no se publicó la copia.");

            File.Move(temporaryPath, destination, overwrite: false);
            temporaryPath = null;
            return PdfCropResult.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return PdfCropResult.Failure(ex.Message);
        }
        catch (IOException)
        {
            return PdfCropResult.Failure("No se pudo crear la copia; el destino puede existir o no estar disponible.");
        }
        catch (Exception)
        {
            return PdfCropResult.Failure("No se pudo validar el recorte seguro; no se publicó ninguna copia.");
        }
        finally
        {
            if (temporaryPath is not null)
                TryDelete(temporaryPath);
        }
    }

    private async Task<ChildProcessResult> RunQpdfAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(_qpdfPath);
        startInfo.ArgumentList.Add("--is-encrypted");
        startInfo.ArgumentList.Add(sourcePath);
        return await _runProcessAsync(startInfo, QpdfTimeout, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<PageBoxSnapshot> ReadAndValidateSource(
        string sourcePath,
        PdfDocumentSession session,
        CancellationToken cancellationToken,
        out string writerState)
    {
        if (session.PageCount <= 0)
            throw new InvalidOperationException("El PDF no contiene páginas legibles.");
        if (session.GetCryptographicSignatureCount() != 0)
            throw new InvalidOperationException("El PDF contiene una firma criptográfica o no se pudo verificar.");
        if (session.GetOrganizeFormType(cancellationToken) != 0 ||
            session.GetBookmarks(cancellationToken).Count != 0 ||
            session.HasOrganizeNamedDestinations(cancellationToken) ||
            session.IsOrganizeTagged(cancellationToken) ||
            session.HasOrganizePageLabels(cancellationToken) ||
            session.HasOrganizeAttachments(cancellationToken))
        {
            throw new InvalidOperationException("El documento contiene estructuras no compatibles con el recorte seguro.");
        }

        for (var pageIndex = 0; pageIndex < session.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session.GetPageRotation(pageIndex, cancellationToken) != 0)
                throw new InvalidOperationException("El recorte seguro de esta versión no admite páginas rotadas.");
            if (session.GetPageAnnotationCount(pageIndex, cancellationToken) != 0)
                throw new InvalidOperationException("El documento contiene anotaciones o enlaces no compatibles con el recorte seguro.");
        }

        using var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
        if (document.PageCount != session.PageCount || !HasOnlySupportedStructure(document))
            throw new InvalidOperationException("El documento contiene una estructura que el recorte seguro no puede validar.");
        writerState = CaptureWriterState(document);

        var boxes = new List<PageBoxSnapshot>(document.PageCount);
        foreach (var page in document.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var media = page.MediaBoxReadOnly;
            var crop = page.EffectiveCropBoxReadOnly;
            ValidateRectangle(media, "MediaBox");
            ValidateRectangle(crop, "CropBox efectivo");
            if (!Contains(media, crop))
                throw new InvalidOperationException("El CropBox efectivo queda fuera del MediaBox y no se puede recortar con seguridad.");
            boxes.Add(new PageBoxSnapshot(crop, media));
        }

        return boxes;
    }

    private static void ValidateOutput(
        string outputPath,
        PdfDocumentSession sourceSession,
        PdfOperationPreservationSnapshot sourceSnapshot,
        IReadOnlyList<PageBoxSnapshot> sourceBoxes,
        string sourceWriterState,
        PdfCropMargins margins,
        CancellationToken cancellationToken)
    {
        using var outputSession = PdfDocumentSession.Open(outputPath);
        var outputSnapshot = PdfOperationPreservationSnapshot.Capture(outputSession, cancellationToken);
        if (outputSnapshot.PageCount != sourceSnapshot.PageCount)
            throw new InvalidOperationException("La salida cambió el número de páginas.");
        if (!string.Equals(outputSnapshot.TextByPage, sourceSnapshot.TextByPage, StringComparison.Ordinal))
            throw new InvalidOperationException("La salida no preservó todo el texto del documento.");
        var changedMetadataTags = sourceSnapshot.GetChangedMetadataFields(outputSnapshot, includeModificationDate: false);
        if (changedMetadataTags.Count != 0)
        {
            var changedTags = string.Join(", ", changedMetadataTags);
            throw new InvalidOperationException(
                $"La salida no preservó los metadatos básicos: {changedTags}.");
        }
        if (!string.Equals(outputSnapshot.Structure, sourceSnapshot.Structure, StringComparison.Ordinal))
            throw new InvalidOperationException("La salida no preservó la estructura admitida del documento.");
        if (outputSession.GetCryptographicSignatureCount() != 0)
            throw new InvalidOperationException("La salida contiene una firma inesperada.");

        using var outputPdf = PdfReader.Open(outputPath, PdfDocumentOpenMode.Modify);
        var outputWriterState = CaptureWriterState(outputPdf);
        if (outputPdf.PageCount != sourceBoxes.Count || !HasOnlySupportedStructure(outputPdf))
            throw new InvalidOperationException("La estructura de la salida no coincide con la admitida.");
        if (!string.Equals(outputWriterState, sourceWriterState, StringComparison.Ordinal))
        {
            var changedPaths = string.Join(", ", GetChangedWriterStatePaths(sourceWriterState, outputWriterState));
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(changedPaths)
                    ? "La estructura de la salida no coincide con la admitida."
                    : $"La salida no preservó estas estructuras: {changedPaths}.");
        }

        var left = margins.LeftMm * PointsPerMillimeter;
        var right = margins.RightMm * PointsPerMillimeter;
        var top = margins.TopMm * PointsPerMillimeter;
        var bottom = margins.BottomMm * PointsPerMillimeter;
        for (var pageIndex = 0; pageIndex < sourceBoxes.Count; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expectedBase = sourceBoxes[pageIndex].CropBox;
            var expected = CreateRectangle(
                expectedBase.X1 + left,
                expectedBase.Y1 + bottom,
                expectedBase.X2 - right,
                expectedBase.Y2 - top);
            var actual = outputPdf.Pages[pageIndex].EffectiveCropBoxReadOnly;
            if (!NearlyEqual(actual, expected) ||
                !NearlyEqual(outputPdf.Pages[pageIndex].MediaBoxReadOnly, sourceBoxes[pageIndex].MediaBox))
            {
                throw new InvalidOperationException("La salida no conservó el MediaBox o el CropBox esperado.");
            }

            var size = outputSession.GetPageSize(pageIndex, cancellationToken);
            if (!NearlyEqual(size.Width, expected.X2 - expected.X1, PageSizeTolerancePoints) ||
                !NearlyEqual(size.Height, expected.Y2 - expected.Y1, PageSizeTolerancePoints))
            {
                throw new InvalidOperationException("PDFium no confirmó las dimensiones visibles del recorte.");
            }

            var rendered = outputSession.RenderPage(pageIndex, 72d);
            if (rendered.PixelWidth <= 0 || rendered.PixelHeight <= 0 || rendered.Pixels.Length == 0)
                throw new InvalidOperationException("PDFium no pudo renderizar la salida recortada.");
        }

        if (sourceSession.PageCount != outputSession.PageCount)
            throw new InvalidOperationException("La salida cambió el número de páginas.");
    }

    private static bool HasOnlySupportedStructure(PdfDocument document)
    {
        if (document.Internals.Catalog.Elements.Keys.Any(key => !SupportedCatalogKeys.Contains(key)))
            return false;
        foreach (var page in document.Pages)
        {
            if (page.Elements.Keys.Any(key => !SupportedPageKeys.Contains(key)))
                return false;
        }
        return true;
    }

    private static string CaptureWriterState(PdfDocument document)
    {
        var state = new List<string>();
        var catalog = document.Internals.Catalog;
        if (catalog.Elements.ContainsKey("/Metadata"))
            state.Add("catalog.metadata=" + CanonicalXmp(catalog.Elements["/Metadata"]));

        var pageIndex = 0;
        foreach (var page in document.Pages)
        {
            var keys = page.Elements.Keys
                .Where(static key => key is not "/Parent" and not "/MediaBox" and not "/CropBox")
                .OrderBy(static key => key, StringComparer.Ordinal);
            foreach (var key in keys)
            {
                state.Add($"page[{pageIndex}].{key}=" + CanonicalItem(
                    page.Elements[key],
                    new HashSet<PdfObject>(ReferenceEqualityComparer.Instance)));
            }
            pageIndex++;
        }
        return string.Join("\n", state);
    }

    private static string CanonicalXmp(PdfItem? item)
    {
        if (item is PdfReference reference)
            item = reference.Value;
        if (item is not PdfDictionary { Stream: not null } metadata)
            throw new InvalidOperationException("Los metadatos XMP no se pudieron validar.");

        var bytes = metadata.Stream.UnfilteredValue;
        if (bytes is null)
            throw new InvalidOperationException("Los metadatos XMP no se pudieron validar.");
        var document = XDocument.Parse(Encoding.UTF8.GetString(bytes));
        var root = document.Root ?? throw new InvalidOperationException("Los metadatos XMP no se pudieron validar.");
        return CanonicalXmpElement(root);
    }

    private static string CanonicalXmpElement(XElement element)
    {
        if (element.Name.LocalName is "CreateDate" or "ModifyDate" or "DocumentID" or "InstanceID")
            return string.Empty;

        var attributes = element.Attributes()
            .Where(static attribute => !attribute.IsNamespaceDeclaration)
            .OrderBy(static attribute => attribute.Name.NamespaceName, StringComparer.Ordinal)
            .ThenBy(static attribute => attribute.Name.LocalName, StringComparer.Ordinal)
            .Select(static attribute => $"@{attribute.Name.NamespaceName}|{attribute.Name.LocalName}={attribute.Value}");
        var children = element.Elements().Select(CanonicalXmpElement);
        var text = string.Concat(element.Nodes().OfType<XText>()
            .Select(static node => node.Value.Trim())
            .Where(static value => value.Length != 0));
        return $"<{element.Name.NamespaceName}|{element.Name.LocalName}[{string.Join(";", attributes)}]>{text}{string.Join("", children)}</>";
    }

    private static IReadOnlyList<string> GetChangedWriterStatePaths(string before, string after)
    {
        static Dictionary<string, string> Parse(string value)
            => value.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(static line => line.Split('=', 2))
                .ToDictionary(static parts => parts[0], static parts => parts.Length > 1 ? parts[1] : string.Empty, StringComparer.Ordinal);

        var beforeValues = Parse(before);
        var afterValues = Parse(after);
        return beforeValues.Keys.Union(afterValues.Keys, StringComparer.Ordinal)
            .Where(key => !string.Equals(beforeValues.GetValueOrDefault(key), afterValues.GetValueOrDefault(key), StringComparison.Ordinal))
            .ToArray();
    }

    private static string CanonicalItem(PdfItem? item, HashSet<PdfObject> visited)
    {
        if (item is null)
            return "null";
        if (item is PdfReference reference)
            return CanonicalItem(reference.Value, visited);
        if (item is PdfDictionary dictionary)
        {
            if (!visited.Add(dictionary))
                return "#cycle";

            var values = new List<string>();
            foreach (var key in dictionary.Elements.Keys.OrderBy(static key => key, StringComparer.Ordinal))
            {
                if (dictionary.Stream is not null && key is "/Length" or "/Filter" or "/DecodeParms")
                    continue;
                values.Add(key + "=" + CanonicalItem(dictionary.Elements[key], visited));
            }
            if (dictionary.Stream is not null)
            {
                var bytes = dictionary.Stream.UnfilteredValue;
                values.Add("stream=" + (bytes is null ? "null" : Convert.ToHexString(SHA256.HashData(bytes))));
            }
            return "{" + string.Join(";", values) + "}";
        }
        if (item is PdfArray array)
        {
            if (!visited.Add(array))
                return "#cycle";
            return "[" + string.Join(";", array.Elements.Select(value => CanonicalItem(value, visited))) + "]";
        }
        return item.ToString() ?? item.GetType().FullName ?? "unknown";
    }

    private static void ValidatePaths(string sourcePath, string destinationPath)
    {
        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El destino debe ser distinto del PDF original.");
        if (!string.Equals(Path.GetExtension(destinationPath), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El destino debe tener extensión .pdf.");
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("No se encontró el PDF original.");
        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
            throw new IOException("Ese destino ya existe. Elige otro nombre para conservarlo.");
        var directory = Path.GetDirectoryName(destinationPath);
        if (directory is null || !Directory.Exists(directory))
            throw new DirectoryNotFoundException("No existe la carpeta de destino.");
    }

    private static void ValidateMargins(PdfCropMargins margins)
    {
        if (!double.IsFinite(margins.LeftMm) || !double.IsFinite(margins.RightMm) ||
            !double.IsFinite(margins.TopMm) || !double.IsFinite(margins.BottomMm) ||
            margins.LeftMm < 0d || margins.RightMm < 0d ||
            margins.TopMm < 0d || margins.BottomMm < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(margins), "Los márgenes deben ser números finitos iguales o mayores que cero.");
        }
    }

    private static void ValidateRectangle(PdfRectangle rectangle, string name)
    {
        if (!double.IsFinite(rectangle.X1) || !double.IsFinite(rectangle.Y1) ||
            !double.IsFinite(rectangle.X2) || !double.IsFinite(rectangle.Y2) ||
            rectangle.X2 <= rectangle.X1 || rectangle.Y2 <= rectangle.Y1)
        {
            throw new InvalidOperationException($"El {name} del PDF no es válido.");
        }
    }

    private static void ValidateCropBox(double x1, double y1, double x2, double y2, PdfRectangle media)
    {
        if (!double.IsFinite(x1) || !double.IsFinite(y1) || !double.IsFinite(x2) || !double.IsFinite(y2) ||
            x1 < media.X1 || y1 < media.Y1 || x2 > media.X2 || y2 > media.Y2 ||
            x2 - x1 < MinimumPageDimensionPoints || y2 - y1 < MinimumPageDimensionPoints)
        {
            throw new InvalidOperationException("Los márgenes dejarían una página demasiado pequeña o inválida.");
        }
    }

    private static bool Contains(PdfRectangle outer, PdfRectangle inner)
        => inner.X1 >= outer.X1 && inner.Y1 >= outer.Y1 &&
           inner.X2 <= outer.X2 && inner.Y2 <= outer.Y2;

    private static PdfRectangle CreateRectangle(double x1, double y1, double x2, double y2)
        => new(new XPoint(x1, y1), new XPoint(x2, y2));

    private static bool NearlyEqual(PdfRectangle actual, PdfRectangle expected)
        => NearlyEqual(actual.X1, expected.X1, PageSizeTolerancePoints) &&
           NearlyEqual(actual.Y1, expected.Y1, PageSizeTolerancePoints) &&
           NearlyEqual(actual.X2, expected.X2, PageSizeTolerancePoints) &&
           NearlyEqual(actual.Y2, expected.Y2, PageSizeTolerancePoints);

    private static bool NearlyEqual(double actual, double expected, double tolerance)
        => double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance;

    private static async Task<byte[]> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed record PageBoxSnapshot(PdfRectangle CropBox, PdfRectangle MediaBox);
}
