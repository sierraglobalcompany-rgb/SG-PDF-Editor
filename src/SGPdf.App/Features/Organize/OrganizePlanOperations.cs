namespace SGPdf.App.Features.Organize;

internal static class OrganizePlanOperations
{
    internal static OrganizePlan MoveSelection(
        OrganizePlan plan,
        IReadOnlyCollection<Guid> selectedIds,
        int insertionIndex)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (insertionIndex < 0 || insertionIndex > plan.Pages.Count)
            throw new ArgumentOutOfRangeException(nameof(insertionIndex));

        var selected = ResolveSelection(plan, selectedIds);
        if (selected.Count == 0)
            return plan;

        var selectedSet = selected.Select(page => page.ItemId).ToHashSet();
        var selectedIndices = plan.Pages
            .Select((page, index) => (page, index))
            .Where(entry => selectedSet.Contains(entry.page.ItemId))
            .Select(entry => entry.index)
            .ToArray();

        var firstSelectedIndex = selectedIndices[0];
        var lastSelectedIndex = selectedIndices[^1];
        if (insertionIndex >= firstSelectedIndex && insertionIndex <= lastSelectedIndex + 1)
            return plan;

        var remaining = plan.Pages.Where(page => !selectedSet.Contains(page.ItemId)).ToList();
        var selectedBeforeInsertion = selectedIndices.Count(index => index < insertionIndex);
        var adjustedInsertionIndex = insertionIndex - selectedBeforeInsertion;
        remaining.InsertRange(adjustedInsertionIndex, selected);

        return new OrganizePlan(plan.Sources, remaining);
    }

    internal static OrganizePlan Rotate(
        OrganizePlan plan,
        IReadOnlyCollection<Guid> selectedIds,
        int deltaQuarterTurns)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var selected = ResolveSelection(plan, selectedIds);
        if (selected.Count == 0)
            return plan;

        var selectedSet = selected.Select(page => page.ItemId).ToHashSet();
        var pages = plan.Pages
            .Select(page => selectedSet.Contains(page.ItemId)
                ? new OrganizePage(
                    page.ItemId,
                    page.SourceId,
                    page.SourcePageIndex,
                    page.RotationDeltaQuarterTurns + deltaQuarterTurns)
                : page)
            .ToArray();

        return new OrganizePlan(plan.Sources, pages);
    }

    internal static OrganizePlan Delete(
        OrganizePlan plan,
        IReadOnlyCollection<Guid> selectedIds)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var selected = ResolveSelection(plan, selectedIds);
        if (selected.Count == 0)
            return plan;
        if (selected.Count == plan.Pages.Count)
            throw new InvalidOperationException("No se pueden eliminar todas las páginas del documento.");

        var selectedSet = selected.Select(page => page.ItemId).ToHashSet();
        var pages = plan.Pages.Where(page => !selectedSet.Contains(page.ItemId)).ToArray();
        return new OrganizePlan(plan.Sources, pages);
    }

    internal static OrganizePlan Duplicate(
        OrganizePlan plan,
        IReadOnlyCollection<Guid> selectedIds)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var selected = ResolveSelection(plan, selectedIds);
        if (selected.Count == 0)
            return plan;

        var selectedSet = selected.Select(page => page.ItemId).ToHashSet();
        var lastSelectedIndex = plan.Pages
            .Select((page, index) => (page, index))
            .Where(entry => selectedSet.Contains(entry.page.ItemId))
            .Max(entry => entry.index);

        var pages = plan.Pages.ToList();
        var duplicates = selected
            .Select(page => new OrganizePage(
                Guid.NewGuid(),
                page.SourceId,
                page.SourcePageIndex,
                page.RotationDeltaQuarterTurns))
            .ToArray();
        pages.InsertRange(lastSelectedIndex + 1, duplicates);

        return new OrganizePlan(plan.Sources, pages);
    }

    internal static OrganizePlan InsertSourcePages(
        OrganizePlan plan,
        OrganizeSource source,
        IReadOnlyList<int> sourcePageIndices,
        int insertionIndex)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourcePageIndices);

        if (insertionIndex < 0 || insertionIndex > plan.Pages.Count)
            throw new ArgumentOutOfRangeException(nameof(insertionIndex));
        if (sourcePageIndices.Count == 0)
            throw new ArgumentException("Debe seleccionarse al menos una página para insertar.", nameof(sourcePageIndices));

        foreach (var pageIndex in sourcePageIndices)
        {
            if (pageIndex < 0 || pageIndex >= source.OriginalPageCount)
                throw new ArgumentOutOfRangeException(nameof(sourcePageIndices), "Una página a insertar está fuera del origen.");
        }

        var sources = plan.Sources.ToList();
        var existingSource = sources.FirstOrDefault(candidate => candidate.SourceId == source.SourceId);
        if (existingSource is null)
        {
            sources.Add(source);
        }
        else if (existingSource != source)
        {
            throw new ArgumentException("El identificador de origen ya existe con datos diferentes.", nameof(source));
        }

        var insertedPages = sourcePageIndices
            .Select(pageIndex => new OrganizePage(Guid.NewGuid(), source.SourceId, pageIndex, 0))
            .ToArray();
        var pages = plan.Pages.ToList();
        pages.InsertRange(insertionIndex, insertedPages);

        return new OrganizePlan(sources, pages);
    }

    private static IReadOnlyList<OrganizePage> ResolveSelection(
        OrganizePlan plan,
        IReadOnlyCollection<Guid> selectedIds)
    {
        ArgumentNullException.ThrowIfNull(selectedIds);
        if (selectedIds.Count == 0)
            return Array.Empty<OrganizePage>();

        var selectedSet = selectedIds.ToHashSet();
        var pages = plan.Pages.Where(page => selectedSet.Contains(page.ItemId)).ToArray();
        if (pages.Length != selectedSet.Count)
            throw new ArgumentException("La selección contiene páginas que no pertenecen al plan.", nameof(selectedIds));

        return pages;
    }
}
