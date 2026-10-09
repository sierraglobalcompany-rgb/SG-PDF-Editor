using System.Collections.ObjectModel;

namespace SGPdf.App.Features.Organize;

internal sealed record OrganizePlan
{
    internal OrganizePlan(
        IReadOnlyList<OrganizeSource> sources,
        IReadOnlyList<OrganizePage> pages)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(pages);

        if (sources.Count == 0)
            throw new ArgumentException("El plan debe tener al menos un origen.", nameof(sources));
        if (pages.Count == 0)
            throw new ArgumentException("El plan debe tener al menos una página.", nameof(pages));

        var sourceArray = sources.ToArray();
        var duplicateSourceId = sourceArray
            .GroupBy(source => source.SourceId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSourceId is not null)
            throw new ArgumentException("El plan contiene orígenes duplicados.", nameof(sources));

        var sourceMap = sourceArray.ToDictionary(source => source.SourceId);
        var pageArray = pages.ToArray();
        var seenItemIds = new HashSet<Guid>();

        foreach (var page in pageArray)
        {
            if (!seenItemIds.Add(page.ItemId))
                throw new ArgumentException("El plan contiene identificadores de página duplicados.", nameof(pages));

            if (!sourceMap.TryGetValue(page.SourceId, out var source))
                throw new ArgumentException("Una página referencia un origen desconocido.", nameof(pages));

            if (page.SourcePageIndex < 0 || page.SourcePageIndex >= source.OriginalPageCount)
                throw new ArgumentOutOfRangeException(nameof(pages), "Una página referencia un índice fuera del origen.");
        }

        Sources = new ReadOnlyCollection<OrganizeSource>(sourceArray);
        Pages = new ReadOnlyCollection<OrganizePage>(pageArray);
    }

    internal IReadOnlyList<OrganizeSource> Sources { get; }
    internal IReadOnlyList<OrganizePage> Pages { get; }

    internal static OrganizePlan FromPrimarySource(OrganizeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var pages = Enumerable.Range(0, source.OriginalPageCount)
            .Select(pageIndex => new OrganizePage(Guid.NewGuid(), source.SourceId, pageIndex, 0))
            .ToArray();

        return new OrganizePlan(new[] { source }, pages);
    }
}
