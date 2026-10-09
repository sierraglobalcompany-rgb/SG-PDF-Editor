using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOrganizeInsertTests
{
    [Fact]
    public void Candidate_InvalidProtectedOrBlockedSource_LeavesPlanUnchanged()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var primary = OrganizeWindowPdfFixture.Create(2);
            using var blocked = OrganizeWindowPdfFixture.Create(2);
            using var protectedSource = OrganizePdfFixtureFactory.CreateProtected();
            var invalidPath = Path.Combine(primary.DirectoryPath, "invalid.pdf");
            File.WriteAllText(invalidPath, "not a pdf");

            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, primary.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var baseline = CurrentPlan(window);

                Assert.False(InvokeBool(window, "TryInsertOrganizePdfCandidate", invalidPath, null, 0));
                Assert.Same(baseline, CurrentPlan(window));

                Assert.False(InvokeBool(window, "TryInsertOrganizePdfCandidate", protectedSource.Path, null, 0));
                Assert.Same(baseline, CurrentPlan(window));

                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((session, _) =>
                        string.Equals(session.FilePath, blocked.Path, StringComparison.OrdinalIgnoreCase)
                            ? BlockedPreflight()
                            : CleanPreflight()));

                Assert.False(InvokeBool(window, "TryInsertOrganizePdfCandidate", blocked.Path, null, 0));
                Assert.Same(baseline, CurrentPlan(window));
                Assert.Single(baseline.Sources);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Insert_AllPagesBeginningMiddleEnd_AndExactRange_PublishesOnlyAfterValidation()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var primary = OrganizeWindowPdfFixture.Create(2);
            using var secondary = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, primary.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => CleanPreflight()));

                Assert.True(InvokeBool(window, "TryInsertOrganizePdfCandidate", secondary.Path, null, 0));
                var afterBeginning = CurrentPlan(window);
                Assert.Equal(5, afterBeginning.Pages.Count);
                AssertSecondarySequence(afterBeginning, secondary.Path, 0, new[] { 0, 1, 2 });

                Assert.True(InvokeBool(window, "TryInsertOrganizePdfCandidate", secondary.Path, "1,3", 4));
                var afterMiddleRange = CurrentPlan(window);
                Assert.Equal(7, afterMiddleRange.Pages.Count);
                AssertSecondarySequence(afterMiddleRange, secondary.Path, 4, new[] { 0, 2 });

                var appendIndex = afterMiddleRange.Pages.Count;
                Assert.True(InvokeBool(window, "TryInsertOrganizePdfCandidate", secondary.Path, null, appendIndex));
                var afterAppend = CurrentPlan(window);
                Assert.Equal(10, afterAppend.Pages.Count);
                AssertSecondarySequence(afterAppend, secondary.Path, appendIndex, new[] { 0, 1, 2 });

                var beforeInvalidRange = afterAppend;
                Assert.False(InvokeBool(window, "TryInsertOrganizePdfCandidate", secondary.Path, "3-2", 1));
                Assert.Same(beforeInvalidRange, CurrentPlan(window));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Insert_CancelLeavesPlan_AndMergeAppendsAllPages()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var primary = OrganizeWindowPdfFixture.Create(2);
            using var secondary = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, primary.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var baseline = CurrentPlan(window);

                SetSourcePicker(window, () => null);
                Assert.False(InvokeBool(window, "TryInsertOrganizePdf"));
                Assert.Same(baseline, CurrentPlan(window));

                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => CleanPreflight()));
                Assert.True(InvokeBool(window, "TryMergeOrganizePdfCandidate", secondary.Path));

                var merged = CurrentPlan(window);
                Assert.Equal(5, merged.Pages.Count);
                AssertSecondarySequence(merged, secondary.Path, 2, new[] { 0, 1, 2 });
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void EnterOrganize_EnablesInsertMergeAndSplit_ButExtractNeedsSelection()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var primary = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, primary.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);

                Assert.True(OrganizeWindowTestHost.Element<Button>(window, "OrganizeInsertButton").IsEnabled);
                Assert.True(OrganizeWindowTestHost.Element<Button>(window, "OrganizeMergeButton").IsEnabled);
                Assert.False(OrganizeWindowTestHost.Element<Button>(window, "OrganizeExtractButton").IsEnabled);
                Assert.True(OrganizeWindowTestHost.Element<Button>(window, "OrganizeSplitButton").IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    private static OrganizePlan CurrentPlan(MainWindow window)
        => (OrganizePlan)OrganizeWindowTestHost.GetField(window, "_organizePlan")!;

    private static bool InvokeBool(MainWindow window, string methodName, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(method => method.Name == methodName && method.GetParameters().Length == args.Length);
        Assert.NotNull(method);
        return (bool)method.Invoke(window, args)!;
    }

    private static void SetSourcePicker(MainWindow window, Func<string?> picker)
    {
        var field = typeof(MainWindow).GetField("_selectOrganizePdfSource", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, picker);
    }

    private static void AssertSecondarySequence(
        OrganizePlan plan,
        string sourcePath,
        int startIndex,
        IReadOnlyList<int> expectedSourcePageIndices)
    {
        var matchingSourceIds = plan.Sources
            .Where(source => string.Equals(source.Path, Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
            .Select(source => source.SourceId)
            .ToHashSet();
        Assert.NotEmpty(matchingSourceIds);

        var slice = plan.Pages.Skip(startIndex).Take(expectedSourcePageIndices.Count).ToArray();
        Assert.Equal(expectedSourcePageIndices, slice.Select(page => page.SourcePageIndex));
        Assert.All(slice, page => Assert.Contains(page.SourceId, matchingSourceIds));
    }

    private static OrganizePreflightResult CleanPreflight()
        => new(Array.Empty<OrganizeFinding>());

    private static OrganizePreflightResult BlockedPreflight()
        => new(new[]
        {
            new OrganizeFinding(
                OrganizeFindingKind.CryptographicSignature,
                OrganizeFindingSeverity.Block,
                "signed secondary",
                OrganizePreservationStatus.Unknown)
        });
}
