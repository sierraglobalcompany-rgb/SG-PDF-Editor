namespace SGPdf.App.Features.Organize;

internal static class OrganizeSplitPlanner
{
    internal static OrganizePlan ExtractSelected(
        OrganizePlan plan,
        IReadOnlyCollection<Guid> selectedIds)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(selectedIds);
        if (selectedIds.Count == 0)
            throw new ArgumentException("Selecciona al menos una página para extraer.", nameof(selectedIds));

        var selected = selectedIds.ToHashSet();
        if (selected.Any(id => plan.Pages.All(page => page.ItemId != id)))
            throw new ArgumentException("La selección contiene una página que no pertenece al plan.", nameof(selectedIds));

        var pages = plan.Pages.Where(page => selected.Contains(page.ItemId)).ToArray();
        if (pages.Length == 0)
            throw new ArgumentException("La selección no contiene páginas válidas.", nameof(selectedIds));

        return new OrganizePlan(plan.Sources, pages);
    }

    internal static IReadOnlyList<OrganizePlan> SplitEvery(
        OrganizePlan plan,
        int pagesPerOutput)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (pagesPerOutput <= 0)
            throw new ArgumentOutOfRangeException(nameof(pagesPerOutput), "La cantidad de páginas por archivo debe ser mayor que cero.");

        var outputs = new List<OrganizePlan>();
        for (var start = 0; start < plan.Pages.Count; start += pagesPerOutput)
        {
            var count = Math.Min(pagesPerOutput, plan.Pages.Count - start);
            outputs.Add(new OrganizePlan(plan.Sources, plan.Pages.Skip(start).Take(count).ToArray()));
        }

        return outputs.AsReadOnly();
    }

    internal static IReadOnlyList<OrganizePlan> SplitExplicitRanges(
        OrganizePlan plan,
        string expression)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Debes indicar al menos un rango.", nameof(expression));

        var groups = expression.Split(',', StringSplitOptions.None);
        var used = new HashSet<int>();
        var outputs = new List<OrganizePlan>(groups.Length);

        foreach (var rawGroup in groups)
        {
            var group = rawGroup.Trim();
            if (group.Length == 0)
                throw new ArgumentException("La expresión contiene un rango vacío.", nameof(expression));

            var dashIndex = group.IndexOf('-');
            int first;
            int last;

            if (dashIndex < 0)
            {
                first = ParseOneBased(group, plan.Pages.Count, expression);
                last = first;
            }
            else
            {
                if (dashIndex == 0 || dashIndex == group.Length - 1 || group.IndexOf('-', dashIndex + 1) >= 0)
                    throw new ArgumentException($"Rango inválido: {group}.", nameof(expression));

                first = ParseOneBased(group[..dashIndex].Trim(), plan.Pages.Count, expression);
                last = ParseOneBased(group[(dashIndex + 1)..].Trim(), plan.Pages.Count, expression);
                if (first > last)
                    throw new ArgumentException($"El rango descendente {group} no es válido.", nameof(expression));
            }

            var indexes = Enumerable.Range(first, last - first + 1).ToArray();
            foreach (var index in indexes)
            {
                if (!used.Add(index))
                    throw new ArgumentException($"La página {index + 1} aparece en más de un rango.", nameof(expression));
            }

            outputs.Add(new OrganizePlan(
                plan.Sources,
                indexes.Select(index => plan.Pages[index]).ToArray()));
        }

        return outputs.AsReadOnly();
    }

    private static int ParseOneBased(string value, int pageCount, string expression)
    {
        if (!int.TryParse(value, out var pageNumber) || pageNumber <= 0)
            throw new ArgumentException($"Página inválida: {value}.", nameof(expression));
        if (pageNumber > pageCount)
            throw new ArgumentException($"La página {pageNumber} excede las {pageCount} páginas del plan.", nameof(expression));
        return pageNumber - 1;
    }
}
