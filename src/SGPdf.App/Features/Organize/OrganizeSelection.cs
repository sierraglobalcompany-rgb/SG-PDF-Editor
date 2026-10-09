namespace SGPdf.App.Features.Organize;

internal sealed class OrganizeSelection
{
    private readonly HashSet<Guid> _selectedItemIds = new();

    internal IReadOnlySet<Guid> SelectedItemIds => _selectedItemIds;
    internal Guid? AnchorItemId { get; private set; }

    internal void SelectSingle(Guid itemId)
    {
        ValidateItemId(itemId, nameof(itemId));
        _selectedItemIds.Clear();
        _selectedItemIds.Add(itemId);
        AnchorItemId = itemId;
    }

    internal void Toggle(Guid itemId)
    {
        ValidateItemId(itemId, nameof(itemId));
        if (!_selectedItemIds.Add(itemId))
            _selectedItemIds.Remove(itemId);
        AnchorItemId = itemId;
    }

    internal void SelectRange(OrganizePlan plan, Guid itemId)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ValidateItemId(itemId, nameof(itemId));

        var currentIds = plan.Pages.Select(page => page.ItemId).ToHashSet();
        if (!currentIds.Contains(itemId))
            throw new ArgumentException("La página no pertenece al plan actual.", nameof(itemId));

        RemoveStaleIds(currentIds);
        if (AnchorItemId is not Guid anchor || !currentIds.Contains(anchor))
        {
            SelectSingle(itemId);
            return;
        }

        var anchorIndex = FindPageIndex(plan, anchor);
        var targetIndex = FindPageIndex(plan, itemId);
        var first = Math.Min(anchorIndex, targetIndex);
        var last = Math.Max(anchorIndex, targetIndex);

        _selectedItemIds.Clear();
        for (var index = first; index <= last; index++)
            _selectedItemIds.Add(plan.Pages[index].ItemId);
    }

    internal void SelectAll(OrganizePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _selectedItemIds.Clear();
        foreach (var page in plan.Pages)
            _selectedItemIds.Add(page.ItemId);
        AnchorItemId = plan.Pages[0].ItemId;
    }

    internal void Clear()
    {
        _selectedItemIds.Clear();
        AnchorItemId = null;
    }

    internal void Reconcile(OrganizePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var currentIds = plan.Pages.Select(page => page.ItemId).ToHashSet();
        RemoveStaleIds(currentIds);
        if (AnchorItemId is Guid anchor && !currentIds.Contains(anchor))
            AnchorItemId = null;
    }

    private void RemoveStaleIds(IReadOnlySet<Guid> currentIds)
        => _selectedItemIds.RemoveWhere(itemId => !currentIds.Contains(itemId));

    private static int FindPageIndex(OrganizePlan plan, Guid itemId)
    {
        for (var index = 0; index < plan.Pages.Count; index++)
        {
            if (plan.Pages[index].ItemId == itemId)
                return index;
        }

        throw new ArgumentException("La página no pertenece al plan actual.", nameof(itemId));
    }

    private static void ValidateItemId(Guid itemId, string parameterName)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("El identificador de página es obligatorio.", parameterName);
    }
}
