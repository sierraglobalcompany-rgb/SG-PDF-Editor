using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizePlanOperationsTests
{
    [Fact]
    public void MoveSelection_NonContiguousForwardBackward_PreservesRelativeOrderWithoutLoss()
    {
        using var fixture = PlanFixture.Create(6);
        var originalPages = OrganizeContractApi.Pages(fixture.Plan);
        var selected = new[]
        {
            OrganizeContractApi.Read<Guid>(originalPages[3], "ItemId"),
            OrganizeContractApi.Read<Guid>(originalPages[1], "ItemId")
        };

        var movedForward = OrganizeContractApi.MoveSelection(fixture.Plan, selected, 6);
        Assert.Equal(new[] { 0, 2, 4, 5, 1, 3 }, SourceIndexes(movedForward));
        Assert.Equal(6, ItemIds(movedForward).Distinct().Count());
        Assert.Equal(ItemIds(fixture.Plan).OrderBy(id => id), ItemIds(movedForward).OrderBy(id => id));

        var movedBackward = OrganizeContractApi.MoveSelection(movedForward, selected, 0);
        Assert.Equal(new[] { 1, 3, 0, 2, 4, 5 }, SourceIndexes(movedBackward));
        Assert.Equal(ItemIds(fixture.Plan).OrderBy(id => id), ItemIds(movedBackward).OrderBy(id => id));
    }

    [Fact]
    public void MoveSelection_DropInsideSelectedSpan_IsNoOp()
    {
        using var fixture = PlanFixture.Create(6);
        var pages = OrganizeContractApi.Pages(fixture.Plan);
        var selected = new[]
        {
            OrganizeContractApi.Read<Guid>(pages[1], "ItemId"),
            OrganizeContractApi.Read<Guid>(pages[3], "ItemId")
        };

        var result = OrganizeContractApi.MoveSelection(fixture.Plan, selected, 2);

        Assert.Equal(ItemIds(fixture.Plan), ItemIds(result));
    }

    [Fact]
    public void Rotate_ComposesModuloFour()
    {
        using var fixture = PlanFixture.Create(4);
        var pages = OrganizeContractApi.Pages(fixture.Plan);
        var selected = new[]
        {
            OrganizeContractApi.Read<Guid>(pages[0], "ItemId"),
            OrganizeContractApi.Read<Guid>(pages[2], "ItemId")
        };

        var first = OrganizeContractApi.Rotate(fixture.Plan, selected, 1);
        Assert.Equal(new[] { 1, 0, 1, 0 }, Rotations(first));

        var second = OrganizeContractApi.Rotate(first, selected, 3);
        Assert.Equal(new[] { 0, 0, 0, 0 }, Rotations(second));

        var third = OrganizeContractApi.Rotate(second, selected, -1);
        Assert.Equal(new[] { 3, 0, 3, 0 }, Rotations(third));
    }

    [Fact]
    public void Delete_SubsetKeepsOrder_ButDeleteAllIsBlocked()
    {
        using var fixture = PlanFixture.Create(4);
        var pages = OrganizeContractApi.Pages(fixture.Plan);
        var subset = new[]
        {
            OrganizeContractApi.Read<Guid>(pages[1], "ItemId"),
            OrganizeContractApi.Read<Guid>(pages[3], "ItemId")
        };

        var result = OrganizeContractApi.Delete(fixture.Plan, subset);
        Assert.Equal(new[] { 0, 2 }, SourceIndexes(result));

        Assert.Throws<InvalidOperationException>(() =>
            OrganizeContractApi.Delete(fixture.Plan, ItemIds(fixture.Plan)));
    }

    [Fact]
    public void Duplicate_CreatesNewIdsAndInsertsAfterSelectedBlock()
    {
        using var fixture = PlanFixture.Create(6);
        var pages = OrganizeContractApi.Pages(fixture.Plan);
        var selected = new[]
        {
            OrganizeContractApi.Read<Guid>(pages[3], "ItemId"),
            OrganizeContractApi.Read<Guid>(pages[1], "ItemId")
        };

        var result = OrganizeContractApi.Duplicate(fixture.Plan, selected);
        var resultPages = OrganizeContractApi.Pages(result);

        Assert.Equal(new[] { 0, 1, 2, 3, 1, 3, 4, 5 }, SourceIndexes(result));
        Assert.Equal(8, ItemIds(result).Distinct().Count());
        Assert.NotEqual(OrganizeContractApi.Read<Guid>(pages[1], "ItemId"), OrganizeContractApi.Read<Guid>(resultPages[4], "ItemId"));
        Assert.NotEqual(OrganizeContractApi.Read<Guid>(pages[3], "ItemId"), OrganizeContractApi.Read<Guid>(resultPages[5], "ItemId"));
    }

    [Fact]
    public void InsertSourcePages_BeginningMiddleEnd_PreservesRequestedPageOrder()
    {
        using var primary = PlanFixture.Create(3);
        using var secondaryFile = TempSourceFile.Create("secondary");
        var secondary = OrganizeContractApi.CaptureSource(secondaryFile.Path, 3);
        var primaryId = OrganizeContractApi.Read<Guid>(primary.Source, "SourceId");
        var secondaryId = OrganizeContractApi.Read<Guid>(secondary, "SourceId");

        var beginning = OrganizeContractApi.InsertSourcePages(primary.Plan, secondary, new[] { 1 }, 0);
        Assert.Equal(
            new[] { (secondaryId, 1), (primaryId, 0), (primaryId, 1), (primaryId, 2) },
            PageKeys(beginning));

        var middle = OrganizeContractApi.InsertSourcePages(primary.Plan, secondary, new[] { 2, 0 }, 1);
        Assert.Equal(
            new[] { (primaryId, 0), (secondaryId, 2), (secondaryId, 0), (primaryId, 1), (primaryId, 2) },
            PageKeys(middle));

        var end = OrganizeContractApi.InsertSourcePages(primary.Plan, secondary, new[] { 2 }, 3);
        Assert.Equal(
            new[] { (primaryId, 0), (primaryId, 1), (primaryId, 2), (secondaryId, 2) },
            PageKeys(end));

        Assert.Equal(2, OrganizeContractApi.Sources(middle).Count);
    }

    [Fact]
    public void Operations_InvalidIdsOrIndices_AreRejected()
    {
        using var fixture = PlanFixture.Create(3);
        var unknown = new[] { Guid.NewGuid() };

        Assert.Throws<ArgumentException>(() => OrganizeContractApi.MoveSelection(fixture.Plan, unknown, 0));
        Assert.Throws<ArgumentException>(() => OrganizeContractApi.Rotate(fixture.Plan, unknown, 1));
        Assert.Throws<ArgumentException>(() => OrganizeContractApi.Delete(fixture.Plan, unknown));
        Assert.Throws<ArgumentException>(() => OrganizeContractApi.Duplicate(fixture.Plan, unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrganizeContractApi.MoveSelection(fixture.Plan, ItemIds(fixture.Plan).Take(1).ToArray(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrganizeContractApi.MoveSelection(fixture.Plan, ItemIds(fixture.Plan).Take(1).ToArray(), 4));

        using var secondaryFile = TempSourceFile.Create("secondary");
        var secondary = OrganizeContractApi.CaptureSource(secondaryFile.Path, 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => OrganizeContractApi.InsertSourcePages(fixture.Plan, secondary, new[] { 2 }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrganizeContractApi.InsertSourcePages(fixture.Plan, secondary, new[] { 0 }, 4));
    }

    private static Guid[] ItemIds(object plan) => OrganizeContractApi.Pages(plan)
        .Select(page => OrganizeContractApi.Read<Guid>(page, "ItemId"))
        .ToArray();

    private static int[] SourceIndexes(object plan) => OrganizeContractApi.Pages(plan)
        .Select(page => OrganizeContractApi.Read<int>(page, "SourcePageIndex"))
        .ToArray();

    private static int[] Rotations(object plan) => OrganizeContractApi.Pages(plan)
        .Select(page => OrganizeContractApi.Read<int>(page, "RotationDeltaQuarterTurns"))
        .ToArray();

    private static (Guid SourceId, int SourcePageIndex)[] PageKeys(object plan) => OrganizeContractApi.Pages(plan)
        .Select(page => (
            OrganizeContractApi.Read<Guid>(page, "SourceId"),
            OrganizeContractApi.Read<int>(page, "SourcePageIndex")))
        .ToArray();

    private sealed class PlanFixture : IDisposable
    {
        private PlanFixture(TempSourceFile file, object source, object plan)
        {
            File = file;
            Source = source;
            Plan = plan;
        }

        internal TempSourceFile File { get; }
        internal object Source { get; }
        internal object Plan { get; }

        internal static PlanFixture Create(int pageCount)
        {
            var file = TempSourceFile.Create("primary");
            var source = OrganizeContractApi.CaptureSource(file.Path, pageCount);
            var plan = OrganizeContractApi.FromPrimarySource(source);
            return new PlanFixture(file, source, plan);
        }

        public void Dispose() => File.Dispose();
    }

    private sealed class TempSourceFile : IDisposable
    {
        private TempSourceFile(string root, string path)
        {
            Root = root;
            Path = path;
        }

        internal string Root { get; }
        internal string Path { get; }

        internal static TempSourceFile Create(string content)
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-organize-ops-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = System.IO.Path.Combine(root, "source.pdf");
            File.WriteAllText(path, content);
            return new TempSourceFile(root, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
