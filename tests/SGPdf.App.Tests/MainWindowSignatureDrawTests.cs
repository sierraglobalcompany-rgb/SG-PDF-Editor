using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using PdfSharp.Pdf;
using SGPdf.App.Features.Sign;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowSignatureDrawTests
{
    [Fact]
    public void DrawSignature_ActionExistsAndIsVisibleOnlyWithFirmarPanel()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                var panel = Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel"));
                var button = Assert.IsType<Button>(window.FindName("DrawSignatureButton"));
                Assert.Equal(Visibility.Collapsed, panel.Visibility);
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                Assert.Equal(Visibility.Visible, panel.Visibility);
                Assert.True(button.IsEnabled);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void DrawCancel_KeepsPriorPlacementSelectionAndDirtyState()
    {
        RunInSta(() =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out var state);
            using (fixture)
            try
            {
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;
                SetField(window, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => null));

                Assert.False((bool)Invoke(window, "TryDrawSignature")!);
                Assert.Equal(before, state.Placements);
                Assert.Equal(selected, state.SelectedId);
                Assert.Equal(dirty, state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void DrawFailure_KeepsPriorPlacementSelectionAndDirtyState()
    {
        RunInSta(() =>
        {
            var window = CreateWindowWithExistingSignature(out var fixture, out var state);
            using (fixture)
            try
            {
                var before = state.Placements.ToArray();
                var selected = state.SelectedId;
                var dirty = state.IsDirty;
                SetField(window, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => throw new InvalidOperationException("forced")));

                Assert.False((bool)Invoke(window, "TryDrawSignature")!);
                Assert.Equal(before, state.Placements);
                Assert.Equal(selected, state.SelectedId);
                Assert.Equal(dirty, state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void DrawApply_AddsExactlyOneNormalSelectedDirtyPlacement()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(window, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
                var drawn = CreateAsset("drawn-signature");
                SetField(window, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => drawn));

                Assert.True((bool)Invoke(window, "TryDrawSignature")!);
                var state = GetState(window)!;
                var placement = Assert.Single(state.Placements);
                Assert.Same(drawn, placement.Asset);
                Assert.Equal(placement.Id, state.SelectedId);
                Assert.True(state.IsDirty);
            }
            finally
            {
                ClearDirtyState(window);
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void DrawApply_UsesSameAddSignatureAssetFlowAsOtherSources()
    {
        RunInSta(() =>
        {
            var drawnWindow = new MainWindow();
            var directWindow = new MainWindow();
            using var fixture = PdfFixture.Create();
            try
            {
                AttachPdfWorkspace(drawnWindow, fixture.SourcePath);
                AttachPdfWorkspace(directWindow, fixture.SourcePath);
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(drawnWindow, "TryEnterSignModeAsync"));
                Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(directWindow, "TryEnterSignModeAsync"));
                var asset = CreateAsset("same.png");

                SetField(drawnWindow, "_drawSignature", (Func<Window, SignatureAsset?>)(_ => asset));
                Assert.True((bool)Invoke(drawnWindow, "TryDrawSignature")!);
                Invoke(directWindow, "AddSignatureAsset", asset);

                var drawnPlacement = Assert.Single(GetState(drawnWindow)!.Placements);
                var directPlacement = Assert.Single(GetState(directWindow)!.Placements);
                Assert.Equal(directPlacement.Bounds, drawnPlacement.Bounds);
                Assert.Equal(directPlacement.PageIndex, drawnPlacement.PageIndex);
                Assert.Same(asset, drawnPlacement.Asset);
            }
            finally
            {
                ClearDirtyState(drawnWindow);
                ClearDirtyState(directWindow);
                OrganizeWindowTestHost.CloseClean(drawnWindow);
                OrganizeWindowTestHost.CloseClean(directWindow);
            }
        });
    }

    [Fact]
    public void ExistingPngAndPhotoSourceActionsRemainAvailable()
    {
        RunInSta(() =>
        {
            var window = new MainWindow();
            try
            {
                var panel = Assert.IsType<StackPanel>(window.FindName("SignaturePropertiesPanel"));
                Assert.Contains(panel.Children.OfType<Button>(), button => Equals(button.Content, "Cargar PNG transparente..."));
                Assert.IsType<Button>(window.FindName("CreateSignatureFromPhotoButton"));
                Assert.IsType<Button>(window.FindName("DrawSignatureButton"));
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    private static MainWindow CreateWindowWithExistingSignature(out PdfFixture fixture, out SignatureEditState state)
    {
        var window = new MainWindow();
        fixture = PdfFixture.Create();
        AttachPdfWorkspace(window, fixture.SourcePath);
        Assert.True(OrganizeWindowTestHost.InvokeTaskSync<bool>(window, "TryEnterSignModeAsync"));
        Invoke(window, "AddSignatureAsset", CreateAsset("existing.png"));
        state = GetState(window)!;
        return window;
    }

    private static void AttachPdfWorkspace(MainWindow window, string path)
    {
        Assert.True(OrganizeWindowTestHost.OpenReader(window, path));
        SetField(window, "_currentPdfDeviceTransform",
            new PdfPageDeviceTransform(0d, 300d, 1d, 0d, 0d, -1d, 300, 300));
    }

    private static SignatureAsset CreateAsset(string name)
    {
        var pixels = new byte[200 * 100 * 4];
        for (var index = 3; index < pixels.Length; index += 4)
            pixels[index] = 255;
        pixels[3] = 0;
        return new SignatureAsset(200, 100, 800, pixels, name);
    }

    private static SignatureEditState? GetState(MainWindow window)
        => (SignatureEditState?)GetField(window, "_signatureEditState");

    private static void ClearDirtyState(MainWindow window) => GetState(window)?.DiscardAll();

    private static object? Invoke(MainWindow window, string method, params object?[] args)
    {
        var candidates = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(item => item.Name == method && item.GetParameters().Length == args.Length)
            .ToArray();
        Assert.Single(candidates);
        return candidates[0].Invoke(window, args);
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

        internal static PdfFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-sign-draw-ui-{Guid.NewGuid():N}");
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
