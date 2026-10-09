using System.Windows.Input;
using SGPdf.App.Features.Organize;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowOrganizeHardeningTests
{
    [Fact]
    public void DirtyPlan_OpenPdfCancel_PreservesCurrentSessionAndPlan()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var first = OrganizeWindowPdfFixture.Create(3);
            using var second = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, first.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                MakeDirty(window);

                var sourceSession = Assert.IsType<PdfDocumentSession>(OrganizeWindowTestHost.GetField(window, "_session"));
                var sourcePlan = Assert.IsType<OrganizePlan>(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardOrganizeChanges", (Func<bool>)(() => false));

                Assert.False(await OrganizeWindowTestHost.OpenReaderAsync(window, second.Path));
                Assert.Same(sourceSession, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Same(sourcePlan, OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_organizeModeActive")!);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_organizePlanDirty")!);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtyPlan_OpenPdfDiscard_DiscardsPlanThenOpensReplacement()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var first = OrganizeWindowPdfFixture.Create(3);
            using var second = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, first.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                MakeDirty(window);
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardOrganizeChanges", (Func<bool>)(() => true));

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, second.Path));

                var session = Assert.IsType<PdfDocumentSession>(OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Equal(second.Path, session.FilePath, ignoreCase: true);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_organizeModeActive")!);
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_organizePlanDirty")!);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void DirtyPlan_ModeSwitchCancel_KeepsOrganizeStateUntouched()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                MakeDirty(window);
                var sourcePlan = Assert.IsType<OrganizePlan>(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                OrganizeWindowTestHost.SetField(window, "_confirmDiscardOrganizeChanges", (Func<bool>)(() => false));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TryLeaveOrganizeModeWithGuard")!);

                Assert.Same(sourcePlan, OrganizeWindowTestHost.GetField(window, "_organizePlan"));
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_organizeModeActive")!);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_organizePlanDirty")!);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void SuccessfulSave_ClearsDirtyState_AndCleanLeaveDoesNotPrompt()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(3);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryEnterOrganizeMode")!);
                MakeDirty(window);

                var destination = Path.Combine(fixture.DirectoryPath, "saved.pdf");
                OrganizeWindowTestHost.SetField(window, "_selectOrganizePdfDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(window, "_inspectCurrentOrganizePreflight",
                    (Func<PdfDocumentSession, CancellationToken, OrganizePreflightResult>)((_, _) =>
                        new OrganizePreflightResult(Array.Empty<OrganizeFinding>())));
                OrganizeWindowTestHost.SetField(window, "_saveOrganizeCopy",
                    (Action<OrganizePlan, string, string, bool, CancellationToken>)((_, _, _, _, _) => { }));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveOrganizePlan")!);
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_organizePlanDirty")!);

                OrganizeWindowTestHost.SetField(window, "_confirmDiscardOrganizeChanges",
                    (Func<bool>)(() => throw new InvalidOperationException("clean plan must not prompt")));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TryLeaveOrganizeModeWithGuard")!);
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_organizeModeActive")!);
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_organizePlan"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    private static void MakeDirty(MainWindow window)
    {
        var firstItem = OrganizeWindowTestHost.GetOrganizeItems(window)[0];
        var firstId = OrganizeWindowTestHost.GetProperty<Guid>(firstItem, "ItemId");
        OrganizeWindowTestHost.Invoke(window, "ApplyOrganizeSelectionClick", firstId, ModifierKeys.None);
        Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateOrganizeSelection", 1)!);
    }
}
