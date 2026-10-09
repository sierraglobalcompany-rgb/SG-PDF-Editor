using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOrganizeTask6Tests
{
    [Fact]
    public void SelectionAndDrag_UseCurrentPlanOrderForwardBackwardAndInsideDropNoOp()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(5);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var initial = CurrentPlan(window);
                var ids = initial.Pages.Select(page => page.ItemId).ToArray();

                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", ids[1], ModifierKeys.None);
                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", ids[3], ModifierKeys.Shift);
                Assert.Equal(new HashSet<Guid> { ids[1], ids[2], ids[3] }, CurrentSelection(window).SelectedItemIds);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "MoveOrganizeSelection", 5)!);
                Assert.Equal(new[] { ids[0], ids[4], ids[1], ids[2], ids[3] }, CurrentPlan(window).Pages.Select(page => page.ItemId));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "MoveOrganizeSelection", 0)!);
                Assert.Equal(new[] { ids[1], ids[2], ids[3], ids[0], ids[4] }, CurrentPlan(window).Pages.Select(page => page.ItemId));

                var beforeInsideDrop = CurrentPlan(window);
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "MoveOrganizeSelection", 2)!);
                Assert.Same(beforeInsideDrop, CurrentPlan(window));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void RotateDeleteDuplicateAndKeyboard_AreScopedToOrganizeAndMaintainSelection()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(4);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "HandleOrganizeKey", Key.A, ModifierKeys.Control)!);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var ids = CurrentPlan(window).Pages.Select(page => page.ItemId).ToArray();

                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", ids[1], ModifierKeys.None);
                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", ids[3], ModifierKeys.Control);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateOrganizeSelection", 1)!);
                var rotated = CurrentPlan(window);
                Assert.Equal(1, rotated.Pages.Single(page => page.ItemId == ids[1]).RotationDeltaQuarterTurns);
                Assert.Equal(1, rotated.Pages.Single(page => page.ItemId == ids[3]).RotationDeltaQuarterTurns);
                Assert.Equal(0, rotated.Pages.Single(page => page.ItemId == ids[0]).RotationDeltaQuarterTurns);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "DuplicateOrganizeSelection")!);
                var duplicated = CurrentPlan(window);
                Assert.Equal(6, duplicated.Pages.Count);
                var duplicateIds = CurrentSelection(window).SelectedItemIds.ToArray();
                Assert.Equal(2, duplicateIds.Length);
                Assert.DoesNotContain(ids[1], duplicateIds);
                Assert.DoesNotContain(ids[3], duplicateIds);
                Assert.All(duplicateIds, duplicateId => Assert.Contains(duplicated.Pages, page => page.ItemId == duplicateId));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "HandleOrganizeKey", Key.A, ModifierKeys.Control)!);
                Assert.Equal(6, CurrentSelection(window).SelectedItemIds.Count);
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "DeleteOrganizeSelection")!);
                Assert.Equal(6, CurrentPlan(window).Pages.Count);

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "HandleOrganizeKey", Key.Escape, ModifierKeys.None)!);
                Assert.Empty(CurrentSelection(window).SelectedItemIds);

                OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", duplicateIds[0], ModifierKeys.None);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "HandleOrganizeKey", Key.Delete, ModifierKeys.None)!);
                Assert.Equal(5, CurrentPlan(window).Pages.Count);
                Assert.DoesNotContain(CurrentPlan(window).Pages, page => page.ItemId == duplicateIds[0]);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void EnterOrganize_EnablesImplementedCommandsThroughTask7()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);

                foreach (var name in new[]
                {
                    "OrganizeRotateLeftButton", "OrganizeRotateRightButton", "OrganizeDeleteButton",
                    "OrganizeDuplicateButton", "OrganizeInsertButton", "OrganizeMergeButton", "OrganizeSaveAsButton"
                })
                {
                    Assert.True(OrganizeWindowTestHost.Element<Button>(window, name).IsEnabled);
                }

                foreach (var name in new[] { "OrganizeExtractButton", "OrganizeSplitButton" })
                    Assert.False(OrganizeWindowTestHost.Element<Button>(window, name).IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SaveAs_CancelAndBlock_DoNotCallWriterOrReplaceActiveState()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var sourceSession = OrganizeWindowTestHost.GetField(window, "_session");
                var sourcePlan = CurrentPlan(window);
                var writerCalls = 0;
                SetSaveWriter(window, (_, _, _, _, _) => writerCalls++);
                OrganizeWindowTestHost.SetField(window, "_selectOrganizePdfDestination", (Func<string?>)(() => null));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveOrganizePlan")!);
                Assert.Equal(0, writerCalls);
                Assert.Same(sourceSession, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Same(sourcePlan, CurrentPlan(window));

                var destination = Path.Combine(fixture.DirectoryPath, "blocked.pdf");
                OrganizeWindowTestHost.SetField(window, "_selectOrganizePdfDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(window, "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => BlockedPreflight()));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveOrganizePlan")!);
                Assert.Equal(0, writerCalls);
                Assert.False(File.Exists(destination));
                Assert.Same(sourceSession, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Same(sourcePlan, CurrentPlan(window));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SaveAs_WarningRequiresConfirmation_AndPassesAuthorizationOnlyWhenConfirmed()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var destination = Path.Combine(fixture.DirectoryPath, "warning.pdf");
                OrganizeWindowTestHost.SetField(window, "_selectOrganizePdfDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(window, "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => WarningPreflight()));

                var writerCalls = 0;
                bool? passedWarningsConfirmed = null;
                SetSaveWriter(window, (_, _, _, confirmed, _) => { writerCalls++; passedWarningsConfirmed = confirmed; });
                OrganizeWindowTestHost.SetField(window, "_confirmOrganizeWarnings", (Func<OrganizePreflightResult, bool>)(_ => false));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveOrganizePlan")!);
                Assert.Equal(0, writerCalls);

                OrganizeWindowTestHost.SetField(window, "_confirmOrganizeWarnings", (Func<OrganizePreflightResult, bool>)(_ => true));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveOrganizePlan")!);
                Assert.Equal(1, writerCalls);
                Assert.True(passedWarningsConfirmed);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SaveAs_FailureKeepsPlanAndSession_SuccessReportsPathWithoutReplacingActiveSession()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                var sourceSession = OrganizeWindowTestHost.GetField(window, "_session");
                var sourcePlan = CurrentPlan(window);
                var destination = Path.Combine(fixture.DirectoryPath, "saved-copy.pdf");
                OrganizeWindowTestHost.SetField(window, "_selectOrganizePdfDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(window, "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) => new OrganizePreflightResult(Array.Empty<OrganizeFinding>())));
                OrganizeWindowTestHost.SetField(window, "_confirmOrganizeWarnings", (Func<OrganizePreflightResult, bool>)(_ => throw new InvalidOperationException("clean preflight must not confirm")));

                SetSaveWriter(window, (_, _, _, _, _) => throw new IOException("synthetic writer failure"));
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveOrganizePlan")!);
                Assert.Same(sourceSession, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Same(sourcePlan, CurrentPlan(window));
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_organizeMaterializing")!);

                string? capturedActive = null;
                string? capturedDestination = null;
                bool? capturedWarnings = null;
                var mutationBlockedDuringWrite = false;
                SetSaveWriter(window, (plan, active, output, confirmed, _) =>
                {
                    Assert.Same(sourcePlan, plan);
                    capturedActive = active;
                    capturedDestination = output;
                    capturedWarnings = confirmed;
                    mutationBlockedDuringWrite = !(bool)OrganizeWindowTestHost.Invoke(window, "RotateOrganizeSelection", 1)!;
                });

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveOrganizePlan")!);
                Assert.Equal(fixture.Path, capturedActive, ignoreCase: true);
                Assert.Equal(destination, capturedDestination, ignoreCase: true);
                Assert.False(capturedWarnings);
                Assert.True(mutationBlockedDuringWrite);
                Assert.Same(sourceSession, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Same(sourcePlan, CurrentPlan(window));
                Assert.Equal(fixture.Path, ((PdfDocumentSession)sourceSession!).FilePath, ignoreCase: true);
                Assert.Contains(destination, OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    private static OrganizePlan CurrentPlan(MainWindow window)
        => (OrganizePlan)OrganizeWindowTestHost.GetField(window, "_organizePlan")!;

    private static OrganizeSelection CurrentSelection(MainWindow window)
        => (OrganizeSelection)OrganizeWindowTestHost.GetField(window, "_organizeSelection")!;

    private static void SetSaveWriter(
        MainWindow window,
        Action<OrganizePlan, string, string, bool, CancellationToken> writer)
        => OrganizeWindowTestHost.SetField(window, "_saveOrganizeCopy", writer);

    private static OrganizePreflightResult BlockedPreflight()
        => new(new[]
        {
            new OrganizeFinding(
                OrganizeFindingKind.CryptographicSignature,
                OrganizeFindingSeverity.Block,
                "blocked",
                OrganizePreservationStatus.Unknown)
        });

    private static OrganizePreflightResult WarningPreflight()
        => new(new[]
        {
            new OrganizeFinding(
                OrganizeFindingKind.Metadata,
                OrganizeFindingSeverity.Warning,
                "warning",
                OrganizePreservationStatus.Unknown)
        });
}
