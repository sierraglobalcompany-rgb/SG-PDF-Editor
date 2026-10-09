using SGPdf.App.Features.Organize;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizeSelectionTests
{
    [Fact]
    public void ClickToggleSelectAllAndClear_FollowWindowsSelectionState()
    {
        var plan = CreatePlan(5);
        var selection = new OrganizeSelection();
        var second = plan.Pages[1].ItemId;
        var fourth = plan.Pages[3].ItemId;

        selection.SelectSingle(second);
        Assert.Equal(new[] { second }, selection.SelectedItemIds);
        Assert.Equal(second, selection.AnchorItemId);

        selection.Toggle(fourth);
        Assert.Equal(new HashSet<Guid> { second, fourth }, selection.SelectedItemIds);
        Assert.Equal(fourth, selection.AnchorItemId);

        selection.Toggle(fourth);
        Assert.Equal(new[] { second }, selection.SelectedItemIds);
        Assert.Equal(fourth, selection.AnchorItemId);

        selection.SelectAll(plan);
        Assert.Equal(plan.Pages.Select(page => page.ItemId).ToHashSet(), selection.SelectedItemIds);
        Assert.Equal(plan.Pages[0].ItemId, selection.AnchorItemId);

        selection.Clear();
        Assert.Empty(selection.SelectedItemIds);
        Assert.Null(selection.AnchorItemId);
    }

    [Fact]
    public void SelectRange_AfterReorder_UsesCurrentPlanOrderFromAnchor()
    {
        var plan = CreatePlan(5);
        var selection = new OrganizeSelection();
        var anchor = plan.Pages[1].ItemId;
        var moved = plan.Pages[3].ItemId;
        var target = plan.Pages[4].ItemId;

        selection.SelectSingle(anchor);
        var reordered = OrganizePlanOperations.MoveSelection(plan, new[] { moved }, 1);
        Assert.Equal(new[]
        {
            plan.Pages[0].ItemId,
            moved,
            anchor,
            plan.Pages[2].ItemId,
            target
        }, reordered.Pages.Select(page => page.ItemId));

        selection.SelectRange(reordered, target);

        Assert.Equal(new HashSet<Guid>
        {
            anchor,
            plan.Pages[2].ItemId,
            target
        }, selection.SelectedItemIds);
        Assert.Equal(anchor, selection.AnchorItemId);
    }

    [Fact]
    public void SelectRange_WhenAnchorBecameStale_CleansStaleIdsAndStartsNewAnchor()
    {
        var plan = CreatePlan(4);
        var selection = new OrganizeSelection();
        var stale = plan.Pages[1].ItemId;
        var target = plan.Pages[3].ItemId;

        selection.SelectSingle(stale);
        selection.Toggle(plan.Pages[2].ItemId);
        selection.Toggle(stale); // stale remains the anchor even after deselection.

        var reduced = OrganizePlanOperations.Delete(plan, new[] { stale });
        selection.SelectRange(reduced, target);

        Assert.Equal(new[] { target }, selection.SelectedItemIds);
        Assert.Equal(target, selection.AnchorItemId);
    }

    [Fact]
    public void SelectRange_RejectsTargetOutsideCurrentPlan()
    {
        var plan = CreatePlan(2);
        var selection = new OrganizeSelection();
        selection.SelectSingle(plan.Pages[0].ItemId);

        var exception = Record.Exception(() => selection.SelectRange(plan, Guid.NewGuid()));
        Assert.IsType<ArgumentException>(exception);
    }

    private static OrganizePlan CreatePlan(int pageCount)
    {
        var source = new OrganizeSource(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"selection-{Guid.NewGuid():N}.pdf"),
            pageCount,
            default);
        return OrganizePlan.FromPrimarySource(source);
    }
}
