using System.IO;
using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Organize;

internal sealed record OrganizePlannedOutput(
    OrganizePlan Plan,
    string DestinationPath);

internal sealed class OrganizeBatchPublisher
{
    private readonly Action<OrganizePlan, string, string, bool, CancellationToken> _writer;

    internal OrganizeBatchPublisher(
        Action<OrganizePlan, string, string, bool, CancellationToken>? writerOverride = null)
    {
        _writer = writerOverride ?? Write;
    }

    internal void Publish(
        IReadOnlyList<OrganizePlannedOutput> outputs,
        bool warningsConfirmed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        if (outputs.Count == 0)
            throw new ArgumentException("Debe existir al menos una salida planificada.", nameof(outputs));

        var normalized = new List<OrganizePlannedOutput>(outputs.Count);
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var output in outputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(output);
            ArgumentNullException.ThrowIfNull(output.Plan);
            if (string.IsNullOrWhiteSpace(output.DestinationPath))
                throw new ArgumentException("Cada salida requiere una ruta de destino.", nameof(outputs));

            var destination = Path.GetFullPath(output.DestinationPath);
            if (!destinations.Add(destination))
                throw new ArgumentException("El lote contiene rutas de destino duplicadas.", nameof(outputs));

            if (File.Exists(destination))
                throw new IOException($"Ya existe un archivo de salida: {destination}");

            if (output.Plan.Sources.Any(source =>
                    string.Equals(source.Path, destination, StringComparison.OrdinalIgnoreCase)))
            {
                throw new IOException("Una salida del lote coincide con un PDF de origen.");
            }

            var directory = Path.GetDirectoryName(destination)
                ?? throw new ArgumentException("La ruta de salida no tiene directorio.", nameof(outputs));
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            normalized.Add(new OrganizePlannedOutput(output.Plan, destination));
        }

        foreach (var output in normalized)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var activeSourcePath = output.Plan.Sources[0].Path;
            _writer(
                output.Plan,
                activeSourcePath,
                output.DestinationPath,
                warningsConfirmed,
                cancellationToken);
        }
    }

    private static void Write(
        OrganizePlan plan,
        string activeSourcePath,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken)
        => new PdfOrganizeWriter().SaveAsCopy(
            plan,
            activeSourcePath,
            destinationPath,
            warningsConfirmed,
            cancellationToken);
}
