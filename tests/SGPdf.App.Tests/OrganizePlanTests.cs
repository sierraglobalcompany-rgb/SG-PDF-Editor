using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizePlanTests
{
    [Fact]
    public void Capture_NormalizesPathAndRecordsFingerprint()
    {
        using var file = TempPdfLikeFile.Create("abc");

        var source = OrganizeContractApi.CaptureSource(file.Path, 3);
        var fingerprint = OrganizeContractApi.ReadObject(source, "Fingerprint");

        Assert.Equal(System.IO.Path.GetFullPath(file.Path), OrganizeContractApi.Read<string>(source, "Path"));
        Assert.Equal(3, OrganizeContractApi.Read<int>(source, "OriginalPageCount"));
        Assert.Equal(new FileInfo(file.Path).Length, OrganizeContractApi.Read<long>(fingerprint, "FileLength"));
        Assert.Equal(new FileInfo(file.Path).LastWriteTimeUtc.Ticks, OrganizeContractApi.Read<long>(fingerprint, "LastWriteTimeUtcTicks"));
        Assert.True(OrganizeContractApi.MatchesCurrentFile(source));
    }

    [Fact]
    public void MatchesCurrentFile_ChangedOrMissingSource_ReturnsFalse()
    {
        using var file = TempPdfLikeFile.Create("abc");
        var source = OrganizeContractApi.CaptureSource(file.Path, 1);

        File.AppendAllText(file.Path, "changed");
        Assert.False(OrganizeContractApi.MatchesCurrentFile(source));

        File.Delete(file.Path);
        Assert.False(OrganizeContractApi.MatchesCurrentFile(source));
    }

    [Fact]
    public void FromPrimarySource_CreatesOrderedUniquePageIds()
    {
        using var file = TempPdfLikeFile.Create("source");
        var source = OrganizeContractApi.CaptureSource(file.Path, 4);
        var plan = OrganizeContractApi.FromPrimarySource(source);
        var pages = OrganizeContractApi.Pages(plan);
        var sourceId = OrganizeContractApi.Read<Guid>(source, "SourceId");

        Assert.Equal(4, pages.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, pages.Select(page => OrganizeContractApi.Read<int>(page, "SourcePageIndex")));
        Assert.All(pages, page => Assert.Equal(sourceId, OrganizeContractApi.Read<Guid>(page, "SourceId")));
        Assert.All(pages, page => Assert.Equal(0, OrganizeContractApi.Read<int>(page, "RotationDeltaQuarterTurns")));
        Assert.Equal(4, pages.Select(page => OrganizeContractApi.Read<Guid>(page, "ItemId")).Distinct().Count());
    }

    [Fact]
    public void Plan_RejectsUnknownSource()
    {
        using var file = TempPdfLikeFile.Create("source");
        var source = OrganizeContractApi.CaptureSource(file.Path, 2);
        var page = OrganizeContractApi.CreatePage(Guid.NewGuid(), Guid.NewGuid(), 0, 0);

        Assert.Throws<ArgumentException>(() => OrganizeContractApi.CreatePlan(new[] { source }, new[] { page }));
    }

    [Fact]
    public void Plan_RejectsOutOfRangeSourcePage()
    {
        using var file = TempPdfLikeFile.Create("source");
        var source = OrganizeContractApi.CaptureSource(file.Path, 2);
        var sourceId = OrganizeContractApi.Read<Guid>(source, "SourceId");
        var page = OrganizeContractApi.CreatePage(Guid.NewGuid(), sourceId, 2, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => OrganizeContractApi.CreatePlan(new[] { source }, new[] { page }));
    }

    [Fact]
    public void Plan_RejectsDuplicateItemId()
    {
        using var file = TempPdfLikeFile.Create("source");
        var source = OrganizeContractApi.CaptureSource(file.Path, 2);
        var sourceId = OrganizeContractApi.Read<Guid>(source, "SourceId");
        var itemId = Guid.NewGuid();
        var first = OrganizeContractApi.CreatePage(itemId, sourceId, 0, 0);
        var second = OrganizeContractApi.CreatePage(itemId, sourceId, 1, 0);

        Assert.Throws<ArgumentException>(() => OrganizeContractApi.CreatePlan(new[] { source }, new[] { first, second }));
    }

    [Theory]
    [InlineData(5, 1)]
    [InlineData(-1, 3)]
    [InlineData(8, 0)]
    public void OrganizePage_NormalizesRotationDeltaToZeroThroughThree(int input, int expected)
    {
        var page = OrganizeContractApi.CreatePage(Guid.NewGuid(), Guid.NewGuid(), 0, input);
        Assert.Equal(expected, OrganizeContractApi.Read<int>(page, "RotationDeltaQuarterTurns"));
    }

    private sealed class TempPdfLikeFile : IDisposable
    {
        private TempPdfLikeFile(string root, string path)
        {
            Root = root;
            Path = path;
        }

        internal string Root { get; }
        internal string Path { get; }

        internal static TempPdfLikeFile Create(string content)
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-organize-plan-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = System.IO.Path.Combine(root, "source.pdf");
            File.WriteAllText(path, content);
            return new TempPdfLikeFile(root, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}

internal static class OrganizeContractApi
{
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;

    internal static Type SourceType => RequiredType("SGPdf.App.Features.Organize.OrganizeSource");
    internal static Type PageType => RequiredType("SGPdf.App.Features.Organize.OrganizePage");
    internal static Type PlanType => RequiredType("SGPdf.App.Features.Organize.OrganizePlan");
    internal static Type OperationsType => RequiredType("SGPdf.App.Features.Organize.OrganizePlanOperations");

    internal static object CaptureSource(string path, int pageCount) =>
        InvokeStatic(SourceType, "Capture", path, pageCount);

    internal static bool MatchesCurrentFile(object source) =>
        (bool)InvokeInstance(source, "MatchesCurrentFile")!;

    internal static object FromPrimarySource(object source) =>
        InvokeStatic(PlanType, "FromPrimarySource", source);

    internal static object CreatePage(Guid itemId, Guid sourceId, int sourcePageIndex, int rotationDeltaQuarterTurns) =>
        Activator.CreateInstance(
            PageType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { itemId, sourceId, sourcePageIndex, rotationDeltaQuarterTurns },
            culture: null) ?? throw new InvalidOperationException("No se pudo crear OrganizePage.");

    internal static object CreatePlan(IEnumerable<object> sources, IEnumerable<object> pages)
    {
        var sourceArray = ToTypedArray(SourceType, sources);
        var pageArray = ToTypedArray(PageType, pages);
        try
        {
            return Activator.CreateInstance(
                PlanType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { sourceArray, pageArray },
                culture: null) ?? throw new InvalidOperationException("No se pudo crear OrganizePlan.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    internal static IReadOnlyList<object> Pages(object plan) => ReadSequence(plan, "Pages");
    internal static IReadOnlyList<object> Sources(object plan) => ReadSequence(plan, "Sources");

    internal static object MoveSelection(object plan, IReadOnlyCollection<Guid> selectedIds, int insertionIndex) =>
        InvokeStatic(OperationsType, "MoveSelection", plan, selectedIds, insertionIndex);

    internal static object Rotate(object plan, IReadOnlyCollection<Guid> selectedIds, int deltaQuarterTurns) =>
        InvokeStatic(OperationsType, "Rotate", plan, selectedIds, deltaQuarterTurns);

    internal static object Delete(object plan, IReadOnlyCollection<Guid> selectedIds) =>
        InvokeStatic(OperationsType, "Delete", plan, selectedIds);

    internal static object Duplicate(object plan, IReadOnlyCollection<Guid> selectedIds) =>
        InvokeStatic(OperationsType, "Duplicate", plan, selectedIds);

    internal static object InsertSourcePages(object plan, object source, IReadOnlyList<int> sourcePageIndices, int insertionIndex) =>
        InvokeStatic(OperationsType, "InsertSourcePages", plan, source, sourcePageIndices, insertionIndex);

    internal static T Read<T>(object target, string propertyName) => (T)ReadObject(target, propertyName);

    internal static object ReadObject(object target, string propertyName) =>
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target)
        ?? throw new InvalidOperationException($"No existe {target.GetType().Name}.{propertyName}.");

    private static Type RequiredType(string fullName) =>
        AppAssembly.GetType(fullName, throwOnError: true)!;

    private static object InvokeStatic(Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        return Invoke(method, null, args)!;
    }

    private static object? InvokeInstance(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        return Invoke(method, target, args);
    }

    private static object? Invoke(MethodInfo method, object? target, object[] args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static Array ToTypedArray(Type elementType, IEnumerable<object> items)
    {
        var materialized = items.ToArray();
        var array = Array.CreateInstance(elementType, materialized.Length);
        for (var index = 0; index < materialized.Length; index++)
            array.SetValue(materialized[index], index);
        return array;
    }

    private static IReadOnlyList<object> ReadSequence(object target, string propertyName)
    {
        var enumerable = (IEnumerable)ReadObject(target, propertyName);
        return enumerable.Cast<object>().ToArray();
    }
}
