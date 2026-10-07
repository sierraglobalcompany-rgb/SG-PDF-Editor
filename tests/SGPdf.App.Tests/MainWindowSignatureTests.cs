using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf;
using SGPdf.App.Features.Sign;
using SGPdf.App.Navigation;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowSignatureTests
{
    [Fact]
    public void SignMode_IsDisabledWithoutPdf_AndEnabledForPdfWorkspace()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                var sign = Assert.IsType<Button>(window.FindName("SignModeButton"));
                var read = Assert.IsType<Button>(window.FindName("ReadModeButton"));
                Assert.False(sign.IsEnabled);
                Assert.True(read.IsEnabled);

                AttachPdfWorkspace(window, fixture.SourcePath);

                Assert.True(sign.IsEnabled);
                sign.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(Visibility.Visible, Assert.IsType<Canvas>(window.FindName("SignatureOverlayCanvas")).Visibility);
                Assert.Equal(Visibility.Visible, Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel")).Visibility);
                Assert.Equal(Visibility.Collapsed, Assert.IsType<StackPanel>(window.FindName("LabelPropertiesPanel")).Visibility);
            }
            finally
            {
                ClearDirtyState(window);
                window.Close();
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
                Click(window, "SignModeButton");

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
                window.Close();
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
                Click(window, "SignModeButton");
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
                window.Close();
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
                Click(window, "SignModeButton");
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
                window.Close();
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
                Click(window, "SignModeButton");
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
                window.Close();
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
            AttachPdfWorkspace(window, fixture.SourcePath);
            Click(window, "SignModeButton");
            Invoke(window, "AddSignatureAsset", CreateAsset());
            SetDecision(window, PendingSignatureDecision.Cancel);

            var args = new CancelEventArgs();
            Invoke(window, "Window_Closing", window, args);
            Assert.True(args.Cancel);

            ClearDirtyState(window);
            window.Close();
        });
    }

    private static void AttachPdfWorkspace(MainWindow window, string path)
    {
        var session = PdfDocumentSession.Open(path);
        SetField(window, "_session", session);
        SetField(window, "_navigation", new PageNavigationState(1));
        SetField(window, "_currentPdfDeviceTransform",
            new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 300, 300));

        var pixels = new byte[300 * 300 * 4];
        Array.Fill(pixels, (byte)255);
        var bitmap = BitmapSource.Create(300, 300, 96, 96, PixelFormats.Bgra32, null, pixels, 300 * 4);
        bitmap.Freeze();
        var image = Assert.IsType<Image>(window.FindName("PdfImage"));
        image.Source = bitmap;
        image.Width = 300;
        image.Height = 300;
        image.Visibility = Visibility.Visible;

        Invoke(window, "UpdateViewerControlsUi");
    }

    private static SignatureAsset CreateAsset()
    {
        var pixels = new byte[200 * 100 * 4];
        for (var index = 3; index < pixels.Length; index += 4)
            pixels[index] = 255;
        pixels[3] = 0;
        return new SignatureAsset(200, 100, 800, pixels, "test.png");
    }

    private static void Click(MainWindow window, string name)
        => Assert.IsType<Button>(window.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

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
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

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
