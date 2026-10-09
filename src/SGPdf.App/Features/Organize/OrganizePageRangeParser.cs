namespace SGPdf.App.Features.Organize;

internal static class OrganizePageRangeParser
{
    internal static IReadOnlyList<int> Parse(string expression, int pageCount)
    {
        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount), "El PDF debe tener al menos una página.");
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Debes indicar al menos una página o rango.", nameof(expression));

        var result = new List<int>();
        var seen = new HashSet<int>();
        var entries = expression.Split(',', StringSplitOptions.None);

        foreach (var rawEntry in entries)
        {
            var entry = rawEntry.Trim();
            if (entry.Length == 0)
                throw new ArgumentException("La expresión contiene una entrada vacía.", nameof(expression));

            var dashIndex = entry.IndexOf('-');
            if (dashIndex < 0)
            {
                AddPage(ParseOneBasedPage(entry, pageCount));
                continue;
            }

            if (dashIndex == 0 ||
                dashIndex == entry.Length - 1 ||
                entry.IndexOf('-', dashIndex + 1) >= 0)
            {
                throw new ArgumentException($"Rango inválido: {entry}.", nameof(expression));
            }

            var start = ParseOneBasedPage(entry[..dashIndex].Trim(), pageCount);
            var end = ParseOneBasedPage(entry[(dashIndex + 1)..].Trim(), pageCount);
            if (start > end)
                throw new ArgumentException($"El rango descendente {entry} no es válido.", nameof(expression));

            for (var page = start; page <= end; page++)
                AddPage(page);
        }

        return result.AsReadOnly();

        void AddPage(int zeroBasedPage)
        {
            if (!seen.Add(zeroBasedPage))
                throw new ArgumentException($"La página {zeroBasedPage + 1} está repetida.", nameof(expression));
            result.Add(zeroBasedPage);
        }
    }

    private static int ParseOneBasedPage(string value, int pageCount)
    {
        if (!int.TryParse(value, out var pageNumber) || pageNumber <= 0)
            throw new ArgumentException($"Página inválida: {value}.", nameof(value));
        if (pageNumber > pageCount)
            throw new ArgumentException($"La página {pageNumber} excede las {pageCount} páginas del PDF.", nameof(value));
        return pageNumber - 1;
    }
}
