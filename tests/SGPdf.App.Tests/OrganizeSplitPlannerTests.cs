using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Features.Organize;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizeSplitPlannerTests
{
    [Fact]
    public void ExtractSelected_PreservesCurrentPlanOrder_AndLeavesOriginalUntouched()
    {
        using var fixture = SplitPlanFixture.Create(5);
        var original = fixture.Plan;
        var moved = OrganizePlanOperations.MoveSelection(
            original,
            new[] { original.Pages[3].ItemId },
            0);
        var selected = new[] { moved.Pages[4].ItemId, moved.Pages[1].ItemId };

        var extracted = InvokePlan("ExtractSelected", moved, selected);

        Assert.Equal(new[] { moved.Pages[1].ItemId, moved.Pages[4].ItemId }, extracted.Pages.Select(page => page.ItemId));
        Assert.Equal(new[] { moved.Pages[1].SourcePageIndex, moved.Pages[4].SourcePageIndex }, extracted.Pages.Select(page => page.SourcePageIndex));
        Assert.Equal(moved.Sources.Select(source => source.SourceId), extracted.Sources.Select(source => source.SourceId));
        Assert.Equal(5, moved.Pages.Count);
        Assert.Same(original.Sources[0], moved.Sources[0]);
    }

    [Fact]
    public void ExtractSelected_EmptyOrUnknownSelection_IsRejected()
    {
        using var fixture = SplitPlanFixture.Create(3);

        Assert.Throws<ArgumentException>(() => InvokePlan("ExtractSelected", fixture.Plan, Array.Empty<Guid>()));
        Assert.Throws<ArgumentException>(() => InvokePlan("ExtractSelected", fixture.Plan, new[] { Guid.NewGuid() }));
    }

    [Fact]
    public void SplitEvery_FinalShortGroup_PreservesLogicalOrder_AndRejectsInvalidN()
    {
        using var fixture = SplitPlanFixture.Create(5);
        var moved = OrganizePlanOperations.MoveSelection(
            fixture.Plan,
            new[] { fixture.Plan.Pages[4].ItemId },
            0);

        var outputs = InvokePlans("SplitEvery", moved, 2);

        Assert.Equal(3, outputs.Count);
        Assert.Equal(new[] { 4, 0 }, outputs[0].Pages.Select(page => page.SourcePageIndex));
        Assert.Equal(new[] { 1, 2 }, outputs[1].Pages.Select(page => page.SourcePageIndex));
        Assert.Equal(new[] { 3 }, outputs[2].Pages.Select(page => page.SourcePageIndex));
        Assert.All(outputs, output => Assert.Equal(moved.Sources.Select(source => source.SourceId), output.Sources.Select(source => source.SourceId)));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokePlans("SplitEvery", moved, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokePlans("SplitEvery", moved, -1));
    }

    [Fact]
    public void SplitExplicitRanges_UsesOneBasedNonOverlappingGroups_AndRejectsInvalidInput()
    {
        using var fixture = SplitPlanFixture.Create(10);

        var outputs = InvokePlans("SplitExplicitRanges", fixture.Plan, "1-3, 4-7, 8-10");

        Assert.Equal(3, outputs.Count);
        Assert.Equal(new[] { 0, 1, 2 }, outputs[0].Pages.Select(page => page.SourcePageIndex));
        Assert.Equal(new[] { 3, 4, 5, 6 }, outputs[1].Pages.Select(page => page.SourcePageIndex));
        Assert.Equal(new[] { 7, 8, 9 }, outputs[2].Pages.Select(page => page.SourcePageIndex));

        Assert.Throws<ArgumentException>(() => InvokePlans("SplitExplicitRanges", fixture.Plan, "1-3,3-5"));
        Assert.Throws<ArgumentException>(() => InvokePlans("SplitExplicitRanges", fixture.Plan, "1-3,,5-6"));
        Assert.Throws<ArgumentException>(() => InvokePlans("SplitExplicitRanges", fixture.Plan, "4-2"));
        Assert.Throws<ArgumentException>(() => InvokePlans("SplitExplicitRanges", fixture.Plan, "1-11"));
        Assert.Throws<ArgumentException>(() => InvokePlans("SplitExplicitRanges", fixture.Plan, " "));
    }

    private static OrganizePlan InvokePlan(string methodName, params object[] args)
        => (OrganizePlan)Invoke(methodName, args)!;

    private static IReadOnlyList<OrganizePlan> InvokePlans(string methodName, params object[] args)
        => ((System.Collections.IEnumerable)Invoke(methodName, args)!).Cast<OrganizePlan>().ToArray();

    private static object? Invoke(string methodName, params object[] args)
    {
        var type = typeof(OrganizePlan).Assembly.GetType(
            "SGPdf.App.Features.Organize.OrganizeSplitPlanner",
            throwOnError: true)!;
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        try
        {
            return method.Invoke(null, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private sealed class SplitPlanFixture : IDisposable
    {
        private SplitPlanFixture(string root, OrganizePlan plan)
        {
            Root = root;
            Plan = plan;
        }

        internal string Root { get; }
        internal OrganizePlan Plan { get; }

        internal static SplitPlanFixture Create(int pageCount)
        {
            var root = Path.Combine(Path.GetTempPath(), $"sgpdf-split-plan-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "source.pdf");
            File.WriteAllText(path, "synthetic-plan-source");
            var source = OrganizeSource.Capture(path, pageCount);
            return new SplitPlanFixture(root, OrganizePlan.FromPrimarySource(source));
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
