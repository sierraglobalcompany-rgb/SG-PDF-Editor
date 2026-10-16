using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using PdfSharp.Pdf;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowSignatureTests
{
    [Fact]
    public void SignTool_IsHiddenWithoutPdf_AndRoutesThroughDocumentRailForPdfWorkspace()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                OrganizeWindowTestHost.EnsureLoaded(window);
                var rail = OrganizeWindowTestHost.Element<Border>(window, "DocumentToolRail");
                var sign = OrganizeWindowTestHost.ToolButton(window, "sign");
                var read = OrganizeWindowTestHost.ToolButton(window, "read");
                Assert.True(sign.IsEnabled);
                Assert.True(read.IsEnabled);
                Assert.Equal(Visibility.Collapsed, rail.Visibility);

                AttachPdfWorkspace(window, fixture.SourcePath);

                Assert.Equal(Visibility.Visible, rail.Visibility);
                sign.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                OrganizeWindowTestHost.PumpUntil(() =>
                    (bool)OrganizeWindowTestHost.GetField(window, "_signatureModeActive")!);
                Assert.Equal(Visibility.Visible, Assert.IsType<Canvas>(window.FindName("SignatureOverlayCanvas")).Visibility);
                Assert.Equal(Visibility.Visible, Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel")).Visibility);
                Assert.Equal(Visibility.Collapsed, Assert.IsType<StackPanel>(window.FindName("LabelPropertiesPanel")).Visibility);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SignMode_DoesNotLeaveAcroFormScrollViewerAsHitTestOverlay()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                Invoke(window, "InitializeFormsUi");
                var propertiesGrid = Assert.IsType<Grid>(Assert.IsType<TextBlock>(window.FindName("PropertiesPlaceholderText")).Parent);
                var acroFormScrollViewer = Assert.Single(propertiesGrid.Children.OfType<ScrollViewer>(),
                    scrollViewer => scrollViewer.Content is StackPanel { Name: "AcroFormPropertiesPanel" });

                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));

                Assert.Equal(Visibility.Collapsed, acroFormScrollViewer.Visibility);
                Assert.Equal(Visibility.Visible, Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel")).Visibility);

                Invoke(window, "EditAcroForm_Click", window, new RoutedEventArgs());
                Assert.Equal(Visibility.Visible, acroFormScrollViewer.Visibility);

                Invoke(window, "ResetAcroFormUi");
                Assert.Equal(Visibility.Collapsed, acroFormScrollViewer.Visibility);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void AddSignature_CreatesCenteredDirtyPlacement_AndZoomReprojectsWithoutChangingPdfBounds()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));

                var asset = CreateAsset();
                Invoke(window, "AddSignatureAsset", asset);
                var state = GetState(window);
                Assert.NotNull(state);
                Assert.True(state!.IsDirty);
                Assert.Single(state.Placements);
                var canonical = state.Placements[0].Bounds;

                Invoke(window, "RefreshSignatureOverlay");
                var canvas = Assert.IsType<Canvas>(window.FindName("SignatureOverlayCanvas"));
                Assert.Single(canvas.Children);
                var first = Assert.IsType<Border>(canvas.Children[0]);
                var firstLeft = Canvas.GetLeft(first);
                var firstWidth = first.Width;

                SetField(window, "_currentPdfDeviceTransform",
                    new PdfPageDeviceTransform(0d, 300d, 0.5d, 0d, 0d, -0.5d, 600, 600));
                Invoke(window, "RefreshSignatureOverlay");

                var second = Assert.IsType<Border>(canvas.Children[0]);
                Assert.InRange(Math.Abs(Canvas.GetLeft(second) - firstLeft * 2d), 0d, 0.05d);
                Assert.InRange(Math.Abs(second.Width - firstWidth * 2d), 0d, 0.05d);
                Assert.Equal(canonical, GetState(window)!.Placements[0].Bounds);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void InvalidPngLoad_KeepsPriorPlacementUnchanged()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Invoke(window, "AddSignatureAsset", CreateAsset());
                var before = GetState(window)!.Placements.ToArray();

                SetField(window, "_loadSignaturePng", (Func<string, SignatureAsset>)(_ => throw new InvalidDataException("opaque")));
                var result = (bool)Invoke(window, "TryLoadSignatureFromPath", "bad.png")!;

                Assert.False(result);
                Assert.Equal(before, GetState(window)!.Placements);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void DirtyGuard_CancelKeepsEdits_DiscardClears_AndSaveFailureBlocksTransition()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Invoke(window, "AddSignatureAsset", CreateAsset());

                SetDecision(window, PendingSignatureDecision.Cancel);
                Assert.False(ResolveGuard(window, SignatureGuardReason.PageNavigation));
                Assert.True(GetState(window)!.IsDirty);

                SetDecision(window, PendingSignatureDecision.Discard);
                Assert.True(ResolveGuard(window, SignatureGuardReason.PageNavigation));
                Assert.Empty(GetState(window)!.Placements);

                Invoke(window, "AddSignatureAsset", CreateAsset());
                SetDecision(window, PendingSignatureDecision.Save);
                SetField(window, "_selectSignaturePdfDestination", (Func<string?>)(() => fixture.PathFor("out.pdf")));
                SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 0));
                SetField(window, "_saveVisualSignatures",
                    (Action<string, string, IReadOnlyList<SignaturePlacement>, CancellationToken>)((_, _, _, _) => throw new IOException("forced")));

                Assert.False(ResolveGuard(window, SignatureGuardReason.PageNavigation));
                Assert.True(GetState(window)!.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void Save_SignedWarningCancelOrSignatureLookupFailure_LeavesWorkspaceDirty()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Invoke(window, "AddSignatureAsset", CreateAsset());
                SetField(window, "_selectSignaturePdfDestination", (Func<string?>)(() => fixture.PathFor("out.pdf")));

                var writes = 0;
                SetField(window, "_saveVisualSignatures",
                    (Action<string, string, IReadOnlyList<SignaturePlacement>, CancellationToken>)((_, _, _, _) => writes++));
                SetField(window, "_getCryptographicSignatureCount", (Func<PdfDocumentSession, int>)(_ => 1));
                SetField(window, "_confirmSignedPdfModification", (Func<int, bool>)(_ => false));

                Assert.False((bool)Invoke(window, "TrySavePendingVisualSignatures")!);
                Assert.Equal(0, writes);
                Assert.True(GetState(window)!.IsDirty);

                SetField(window, "_getCryptographicSignatureCount",
                    (Func<PdfDocumentSession, int>)(_ => throw new InvalidOperationException("signature probe failed")));
                Assert.False((bool)Invoke(window, "TrySavePendingVisualSignatures")!);
                Assert.Equal(0, writes);
                Assert.True(GetState(window)!.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void WindowClosing_CancelDecisionCancelsClose()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Invoke(window, "AddSignatureAsset", CreateAsset());
                SetDecision(window, PendingSignatureDecision.Cancel);

                var args = new CancelEventArgs();
                Invoke(window, "Window_Closing", window, args);
                Assert.True(args.Cancel);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    private static void AttachPdfWorkspace(MainWindow window, string path)
    {
        Assert.True(OrganizeWindowTestHost.OpenReader(window, path));
        SetField(window, "_currentPdfDeviceTransform",
            new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 300, 300));
    }

    private static SignatureAsset CreateAsset()
    {
        var pixels = new byte[200 * 100 * 4];
        for (var index = 3; index < pixels.Length; index += 4)
            pixels[index] = 255;
        pixels[3] = 0;
        return new SignatureAsset(200, 100, 800, pixels, "test.png");
    }

    private static SignatureEditState? GetState(MainWindow window)
        => (SignatureEditState?)GetField(window, "_signatureEditState");

    private static void ClearDirtyState(MainWindow window)
    {
        var state = GetState(window);
        state?.DiscardAll();
    }

    private static void SetDecision(MainWindow window, PendingSignatureDecision decision)
        => SetField(window, "_resolvePendingSignatureDecision", (Func<SignatureGuardReason, PendingSignatureDecision>)(_ => decision));

    private static bool ResolveGuard(MainWindow window, SignatureGuardReason reason)
        => (bool)Invoke(window, "TryResolvePendingSignatureEdits", reason)!;

    private static object? Invoke(MainWindow window, string method, params object?[] args)
    {
        var candidate = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == method && item.GetParameters().Length == args.Length);
        return candidate.Invoke(window, args);
    }

    private static object? GetField(MainWindow window, string name)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window);
    }

    private static void SetField(MainWindow window, string name, object value)
    {
        var field = typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(window, value);
    }

    private static void RunInSta(Action action)
        => OrganizeWindowTestHost.RunInSta(action);

    private sealed class PdfFixture : IDisposable
    {
        private PdfFixture(string directory, string sourcePath)
        {
            DirectoryPath = directory;
            SourcePath = sourcePath;
        }

        internal string DirectoryPath { get; }
        internal string SourcePath { get; }
        internal string PathFor(string name) => Path.Combine(DirectoryPath, name);

        internal static PdfFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-ui-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var source = Path.Combine(directory, "source.pdf");
            var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(300);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(300);
            document.Save(source);
            document.Close();
            return new PdfFixture(directory, source);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
    }
}
