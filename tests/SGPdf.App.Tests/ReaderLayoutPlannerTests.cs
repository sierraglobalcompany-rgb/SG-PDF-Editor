using System.Collections;
using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderLayoutPlannerTests
{
    private static Assembly AppAssembly => typeof(PdfDocumentSession).Assembly;

    [Fact]
    public void Build_ManualZoom_UsesOneDpiAcrossMixedPageSizes()
    {
        var pages = CreatePages((612d, 792d), (792d, 612d));
        var result = InvokeBuild(pages, new PdfZoomState(), 1000d, 800d);
        var items = Enumerate(result);

        Assert.Equal(2, items.Length);
        Assert.All(items, item => Assert.Equal(96d, GetDouble(item, "ResolvedDpi"), 6));
        Assert.Equal(816d, GetDouble(items[0], "DisplayWidth"), 6);
        Assert.Equal(1056d, GetDouble(items[0], "DisplayHeight"), 6);
        Assert.Equal(1056d, GetDouble(items[1], "DisplayWidth"), 6);
        Assert.Equal(816d, GetDouble(items[1], "DisplayHeight"), 6);
    }

    [Fact]
    public void Build_FitWidth_ResolvesDpiPerPageWidth()
    {
        var zoom = new PdfZoomState().FitWidth();
        var pages = CreatePages((612d, 792d), (792d, 612d));
        var items = Enumerate(InvokeBuild(pages, zoom, 1000d, 800d));

        Assert.Equal(zoom.ResolveDpi(612d, 792d, 1000d, 800d, 48d), GetDouble(items[0], "ResolvedDpi"), 6);
        Assert.Equal(zoom.ResolveDpi(792d, 612d, 1000d, 800d, 48d), GetDouble(items[1], "ResolvedDpi"), 6);
        Assert.NotEqual(GetDouble(items[0], "ResolvedDpi"), GetDouble(items[1], "ResolvedDpi"));
    }

    [Fact]
    public void Build_FitPage_ResolvesDpiPerPageDimensions()
    {
        var zoom = new PdfZoomState().FitPage();
        var pages = CreatePages((612d, 792d), (792d, 612d));
        var items = Enumerate(InvokeBuild(pages, zoom, 1000d, 800d));

        Assert.Equal(zoom.ResolveDpi(612d, 792d, 1000d, 800d, 48d), GetDouble(items[0], "ResolvedDpi"), 6);
        Assert.Equal(zoom.ResolveDpi(792d, 612d, 1000d, 800d, 48d), GetDouble(items[1], "ResolvedDpi"), 6);
    }

    [Fact]
    public void FindCurrentPage_CenterInsidePage_SelectsThatPage()
    {
        var pages = InvokeBuild(CreatePages((72d, 72d), (72d, 72d), (72d, 72d)), new PdfZoomState(), 600d, 180d);

        var pageIndex = InvokeFindCurrentPage(pages, 120d, 180d);

        Assert.Equal(1, pageIndex);
    }

    [Fact]
    public void FindCurrentPage_CenterInGap_SelectsNearestPage()
    {
        var pages = InvokeBuild(CreatePages((72d, 72d), (72d, 72d)), new PdfZoomState(), 600d, 100d);

        var pageIndex = InvokeFindCurrentPage(pages, 50d, 100d);

        Assert.Equal(0, pageIndex);
    }

    [Fact]
    public void RenderWindow_VisiblePagesPlusOneNeighborEachSideOnly()
    {
        var pages = InvokeBuild(
            CreatePages((72d, 72d), (72d, 72d), (72d, 72d), (72d, 72d), (72d, 72d)),
            new PdfZoomState(),
            600d,
            180d);

        var window = InvokeRenderWindow(pages, 120d, 180d);

        Assert.Equal(1, GetInt(window, "FirstVisiblePageIndex"));
        Assert.Equal(2, GetInt(window, "LastVisiblePageIndex"));
        Assert.Equal(0, GetInt(window, "FirstRetainedPageIndex"));
        Assert.Equal(3, GetInt(window, "LastRetainedPageIndex"));
    }

    [Fact]
    public void RenderWindow_PrioritizesVisibleBeforeNeighbors()
    {
        var pages = InvokeBuild(
            CreatePages((72d, 72d), (72d, 72d), (72d, 72d), (72d, 72d), (72d, 72d)),
            new PdfZoomState(),
            600d,
            180d);

        var window = InvokeRenderWindow(pages, 120d, 180d);
        var priority = ((IEnumerable)GetProperty(window, "RenderPriority")).Cast<int>().ToArray();

        Assert.Equal(new[] { 1, 2, 0, 3 }, priority);
    }

    [Fact]
    public void InvalidViewportOrPageSize_IsRejected()
    {
        var validPages = CreatePages((612d, 792d));
        var viewportFailure = Assert.Throws<TargetInvocationException>(() =>
            InvokeBuild(validPages, new PdfZoomState(), 0d, 800d));
        Assert.IsType<ArgumentOutOfRangeException>(viewportFailure.InnerException);

        var invalidPages = CreatePages((0d, 792d));
        var pageFailure = Assert.Throws<TargetInvocationException>(() =>
            InvokeBuild(invalidPages, new PdfZoomState(), 1000d, 800d));
        Assert.IsType<ArgumentOutOfRangeException>(pageFailure.InnerException);
    }

    private static IList CreatePages(params (double Width, double Height)[] sizes)
    {
        var pageSizeType = RequireType("SGPdf.App.Pdf.PdfPageSize");
        var listType = typeof(List<>).MakeGenericType(pageSizeType);
        var list = Assert.IsAssignableFrom<IList>(Activator.CreateInstance(listType));

        foreach (var size in sizes)
            list.Add(Activator.CreateInstance(pageSizeType, size.Width, size.Height)!);

        return list;
    }

    private static object InvokeBuild(IList pages, PdfZoomState zoom, double viewportWidth, double viewportHeight)
    {
        var method = RequirePlannerMethod("Build");
        return method.Invoke(null, new object[] { pages, zoom, viewportWidth, viewportHeight, 48d, 16d })!;
    }

    private static int InvokeFindCurrentPage(object pages, double verticalOffset, double viewportHeight)
    {
        var method = RequirePlannerMethod("FindCurrentPageIndex");
        return (int)method.Invoke(null, new[] { pages, verticalOffset, viewportHeight })!;
    }

    private static object InvokeRenderWindow(object pages, double verticalOffset, double viewportHeight)
    {
        var method = RequirePlannerMethod("GetRenderWindow");
        return method.Invoke(null, new[] { pages, verticalOffset, viewportHeight })!;
    }

    private static MethodInfo RequirePlannerMethod(string name)
    {
        var type = RequireType("SGPdf.App.Features.Reader.ReaderLayoutPlanner");
        var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!;
    }

    private static Type RequireType(string fullName)
    {
        var type = AppAssembly.GetType(fullName);
        Assert.NotNull(type);
        return type!;
    }

    private static object[] Enumerate(object sequence)
        => ((IEnumerable)sequence).Cast<object>().ToArray();

    private static object GetProperty(object target, string name)
    {
        var property = target.GetType().GetProperty(name);
        Assert.NotNull(property);
        return property!.GetValue(target)!;
    }

    private static double GetDouble(object target, string name)
        => (double)GetProperty(target, name);

    private static int GetInt(object target, string name)
        => (int)GetProperty(target, name);
}
