using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOrganizeExtractSplitTests
{
    [Fact]
    public void EnterOrganize_EnablesSplit_AndExtractTracksSelection()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);

                var extract = OrganizeWindowTestHost.Element<Button>(window, "OrganizeExtractButton");
                var split = OrganizeWindowTestHost.Element<Button>(window, "OrganizeSplitButton");
                Assert.False(extract.IsEnabled);
                Assert.True(split.IsEnabled);

                var plan = CurrentPlan(window);
                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", plan.Pages[1].ItemId, ModifierKeys.None);

                Assert.True(extract.IsEnabled);
                Assert.True(split.IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Extract_SelectedPagesWritesDerivedPdf_WithoutChangingWorkspacePlan()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(4);
            var destination = Path.Combine(fixture.DirectoryPath, "extracted.pdf");
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => new OrganizePreflightResult(Array.Empty<OrganizeFinding>())));

                var original = CurrentPlan(window);
                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", original.Pages[1].ItemId, ModifierKeys.None);
                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", original.Pages[3].ItemId, ModifierKeys.Control);

                Assert.True(InvokeBool(window, "TryExtractOrganizeSelectionTo", destination));
                Assert.Same(original, CurrentPlan(window));
                Assert.True(File.Exists(destination));

                using var output = PdfDocumentSession.Open(destination);
                Assert.Equal(2, output.PageCount);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SplitEvery_PublishesAllDerivedOutputs_AndLeavesWorkspaceUnchanged()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(5);
            var basePath = Path.Combine(fixture.DirectoryPath, "split.pdf");
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => new OrganizePreflightResult(Array.Empty<OrganizeFinding>())));
                var original = CurrentPlan(window);

                Assert.True(InvokeBool(window, "TrySplitOrganizeEvery", 2, basePath));
                Assert.Same(original, CurrentPlan(window));

                var expected = new[]
                {
                    Path.Combine(fixture.DirectoryPath, "split_parte-001_p1-2.pdf"),
                    Path.Combine(fixture.DirectoryPath, "split_parte-002_p3-4.pdf"),
                    Path.Combine(fixture.DirectoryPath, "split_parte-003_p5.pdf")
                };
                Assert.All(expected, path => Assert.True(File.Exists(path), path));
                Assert.Equal(new[] { 2, 2, 1 }, expected.Select(path =>
                {
                    using var session = PdfDocumentSession.Open(path);
                    return session.PageCount;
                }));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SplitExplicitRanges_InvalidExpressionPublishesNothing()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(5);
            var basePath = Path.Combine(fixture.DirectoryPath, "ranges.pdf");
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var original = CurrentPlan(window);

                Assert.False(InvokeBool(window, "TrySplitOrganizeRanges", "1-3,3-5", basePath));
                Assert.Same(original, CurrentPlan(window));
                Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "ranges_parte-*.pdf"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    private static OrganizePlan CurrentPlan(MainWindow window)
        => (OrganizePlan)OrganizeWindowTestHost.GetField(window, "_organizePlan")!;

    private static bool InvokeBool(MainWindow window, string methodName, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == args.Length);
        Assert.NotNull(method);
        return (bool)method.Invoke(window, args)!;
    }
}
