using System.IO;

namespace SGPdf.App.Features.Organize;

internal static class OrganizeOutputNaming
{
    internal static string BuildSplitPath(
        string basePath,
        int ordinal,
        int firstPageNumber,
        int lastPageNumber)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("La ruta base es obligatoria.", nameof(basePath));
        if (ordinal <= 0)
            throw new ArgumentOutOfRangeException(nameof(ordinal), "El ordinal debe ser mayor que cero.");
        if (firstPageNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(firstPageNumber), "La primera página debe ser mayor que cero.");
        if (lastPageNumber < firstPageNumber)
            throw new ArgumentOutOfRangeException(nameof(lastPageNumber), "La última página no puede ser menor que la primera.");

        var fullBasePath = Path.GetFullPath(basePath);
        var directory = Path.GetDirectoryName(fullBasePath);
        var baseName = Path.GetFileNameWithoutExtension(fullBasePath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("La ruta base debe incluir un nombre de archivo válido.", nameof(basePath));

        var rangeSuffix = firstPageNumber == lastPageNumber
            ? $"p{firstPageNumber}"
            : $"p{firstPageNumber}-{lastPageNumber}";
        return Path.Combine(
            directory,
            $"{baseName}_parte-{ordinal:D3}_{rangeSuffix}.pdf");
    }
}
