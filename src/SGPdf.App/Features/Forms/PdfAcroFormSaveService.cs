using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Forms;

internal sealed class PdfAcroFormSaveService
{
    private readonly Action<string, string>? _beforeValidation;

    internal PdfAcroFormSaveService(Action<string, string>? beforeValidation)
    {
        _beforeValidation = beforeValidation;
    }

    internal void Save(
        string sourcePath,
        string destinationPath,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("La ruta del PDF fuente es obligatoria.", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("La ruta del PDF de destino es obligatoria.", nameof(destinationPath));
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
            throw new ArgumentException("Debe indicar al menos un campo de texto para guardar.", nameof(values));

        cancellationToken.ThrowIfCancellationRequested();

        string sourceFullPath = Path.GetFullPath(sourcePath);
        string destinationFullPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El formulario editado debe guardarse en una ruta distinta del archivo fuente.",
                nameof(destinationPath));
        }

        using (var session = PdfDocumentSession.Open(sourceFullPath))
        {
            PdfEditPreflightResult preflight = new PdfEditPreflightInspector().Inspect(session, cancellationToken);
            if (!preflight.CanProceed)
            {
                string reason = preflight.Findings
                    .First(finding => finding.Severity == PdfEditFindingSeverity.Block)
                    .Message;
                throw new InvalidOperationException($"El PDF fuente está bloqueado para formularios: {reason}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        EnsureRequestedFieldsAreSupported(sourceFullPath, values);
        PdfEditSourceFingerprint sourceFingerprint = PdfEditSourceFingerprint.Capture(sourceFullPath);

        string destinationDirectory = Path.GetDirectoryName(destinationFullPath)
            ?? throw new InvalidOperationException("No se pudo resolver el directorio de destino.");
        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException($"No existe el directorio de destino: {destinationDirectory}");

        string tempPath = Path.Combine(
            destinationDirectory,
            $".sgpdf-form-{Guid.NewGuid():N}.tmp.pdf");

        try
        {
            WriteTextFieldValues(sourceFullPath, tempPath, values, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _beforeValidation?.Invoke(sourceFullPath, tempPath);
            cancellationToken.ThrowIfCancellationRequested();

            ValidateTemp(tempPath, values, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (!sourceFingerprint.MatchesCurrentFile(sourceFullPath))
                throw new IOException("El PDF fuente cambió durante la edición del formulario; no se publicará el resultado.");

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(destinationFullPath))
                File.Replace(tempPath, destinationFullPath, destinationBackupFileName: null);
            else
                File.Move(tempPath, destinationFullPath);
        }
        finally
        {
            TryDeleteTemp(tempPath);
        }
    }

    private static void EnsureRequestedFieldsAreSupported(
        string sourcePath,
        IReadOnlyDictionary<string, string> values)
    {
        var supportedNames = new HashSet<string>(
            PdfAcroFormInspector.InspectTextFields(sourcePath).Select(field => field.Name),
            StringComparer.Ordinal);

        foreach ((string name, string value) in values)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del campo de formulario no puede estar vacío.", nameof(values));
            if (value is null)
                throw new ArgumentException($"El valor del campo '{name}' no puede ser nulo.", nameof(values));
            if (!supportedNames.Contains(name))
            {
                throw new InvalidOperationException(
                    $"El campo '{name}' no existe o no es un campo de texto AcroForm soportado.");
            }
        }
    }

    private static void WriteTextFieldValues(
        string sourcePath,
        string destinationPath,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using PdfDocument document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
        PdfDictionary? acroForm = document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
        PdfArray? fields = acroForm?.Elements.GetArray("/Fields");
        if (fields is null)
            throw new InvalidOperationException("El PDF no contiene campos AcroForm editables.");

        var remaining = new HashSet<string>(values.Keys, StringComparer.Ordinal);
        for (var index = 0; index < fields.Elements.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PdfDictionary? field = fields.Elements.GetDictionary(index);
            if (field is not null)
            {
                WriteTextFieldValues(
                    field,
                    inheritedFieldType: null,
                    parentName: null,
                    values,
                    remaining,
                    cancellationToken);
            }
        }

        if (remaining.Count != 0)
        {
            throw new InvalidOperationException(
                $"No se pudieron materializar los campos: {string.Join(", ", remaining)}.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        document.Save(destinationPath);
    }

    private static void WriteTextFieldValues(
        PdfDictionary field,
        string? inheritedFieldType,
        string? parentName,
        IReadOnlyDictionary<string, string> values,
        HashSet<string> remaining,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string ownFieldType = field.Elements.GetName("/FT");
        string? fieldType = string.IsNullOrEmpty(ownFieldType)
            ? inheritedFieldType
            : ownFieldType;

        string ownName = field.Elements.GetString("/T");
        string? fullName = string.IsNullOrWhiteSpace(ownName)
            ? parentName
            : string.IsNullOrWhiteSpace(parentName)
                ? ownName
                : $"{parentName}.{ownName}";

        PdfArray? kids = field.Elements.GetArray("/Kids");
        if (kids is not null && kids.Elements.Count > 0)
        {
            for (var index = 0; index < kids.Elements.Count; index++)
            {
                PdfDictionary? child = kids.Elements.GetDictionary(index);
                if (child is not null)
                {
                    WriteTextFieldValues(
                        child,
                        fieldType,
                        fullName,
                        values,
                        remaining,
                        cancellationToken);
                }
            }

            return;
        }

        if (fieldType == "/Tx"
            && !string.IsNullOrWhiteSpace(fullName)
            && values.TryGetValue(fullName, out string? value))
        {
            field.Elements.SetString("/V", value);
            remaining.Remove(fullName);
        }
    }

    private static void ValidateTemp(
        string tempPath,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (var session = PdfDocumentSession.Open(tempPath))
        {
            if (session.GetOrganizeFormType(cancellationToken) == 0)
                throw new InvalidDataException("El PDF temporal perdió su formulario AcroForm.");
        }

        var actual = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (PdfAcroFormTextFieldInfo field in PdfAcroFormInspector.InspectTextFields(tempPath))
        {
            if (!actual.TryAdd(field.Name, field.Value))
                throw new InvalidDataException($"El PDF temporal contiene el campo duplicado '{field.Name}'.");
        }

        foreach ((string name, string expectedValue) in values)
        {
            if (!actual.TryGetValue(name, out string? actualValue)
                || !string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"El PDF temporal no conservó el valor esperado para el campo '{name}'.");
            }
        }
    }

    private static void TryDeleteTemp(string tempPath)
    {
        if (!File.Exists(tempPath))
            return;

        try
        {
            File.Delete(tempPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
