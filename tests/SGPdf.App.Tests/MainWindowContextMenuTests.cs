using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using SGPdf.App.Shell;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowContextMenuTests
{
    [Fact]
    public void ReaderContextMenu_IsSharedAndCopiesOnlyTheActiveDocumentSelection()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var first = OrganizePdfFixtureFactory.CreatePlain();
            using var second = OrganizePdfFixtureFactory.CreatePlain();
            var window = new MainWindow();
            try
            {
                var viewer = OrganizeWindowTestHost.Element<ScrollViewer>(window, "PdfScrollViewer");
                var menu = Assert.IsType<ContextMenu>(viewer.ContextMenu);
                var pageList = Assert.IsType<ListBox>(OrganizeWindowTestHost.GetField(window, "_readerPageList"));
                Assert.Same(menu, pageList.ContextMenu);
                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "CanOpenPdfWorkspaceContextMenu")!);

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, first.Path));
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "CanOpenPdfWorkspaceContextMenu")!);
                menu.PlacementTarget = pageList;
                OrganizeWindowTestHost.Invoke(window, "UpdatePdfWorkspaceContextMenuState");

                var copy = Assert.IsType<MenuItem>(menu.Items[0]);
                var separator = Assert.IsType<Separator>(menu.Items[1]);
                var fitPage = Assert.IsType<MenuItem>(menu.Items[2]);
                var fitWidth = Assert.IsType<MenuItem>(menu.Items[3]);
                var print = Assert.IsType<MenuItem>(menu.Items[4]);
                Assert.Equal(Visibility.Collapsed, copy.Visibility);
                Assert.Equal(Visibility.Collapsed, separator.Visibility);
                Assert.True(fitPage.IsEnabled);
                Assert.True(fitWidth.IsEnabled);
                Assert.True(print.IsEnabled);

                string? copiedText = null;
                OrganizeWindowTestHost.SetField(window, "_getReaderTextRange",
                    (Func<PdfDocumentSession, int, int, int, CancellationToken, string>)((_, _, _, _, _) => "texto de PDF-A"));
                OrganizeWindowTestHost.SetField(window, "_setReaderClipboardText", (Action<string>)(value => copiedText = value));
                var selection = new ReaderTextSelection(0, 3, 5, Array.Empty<PdfTextRect>());
                OrganizeWindowTestHost.SetField(window, "_readerTextSelection", selection);
                OrganizeWindowTestHost.Invoke(window, "UpdatePdfWorkspaceContextMenuState");

                Assert.Equal(Visibility.Visible, copy.Visibility);
                Assert.Equal(Visibility.Visible, separator.Visibility);
                copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, copy));
                Assert.Equal("texto de PDF-A", copiedText);

                OrganizeWindowTestHost.SetField(window, "_editModeActive", true);
                OrganizeWindowTestHost.Invoke(window, "UpdatePdfWorkspaceContextMenuState");
                Assert.False(fitPage.IsEnabled);
                Assert.False(fitWidth.IsEnabled);
                Assert.False(print.IsEnabled);
                copiedText = null;
                copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, copy));
                Assert.Null(copiedText);

                OrganizeWindowTestHost.SetField(window, "_editModeActive", false);
                OrganizeWindowTestHost.SetField(window, "_readerTextSelection", selection);
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, second.Path));
                Assert.Null(OrganizeWindowTestHost.GetField(window, "_readerTextSelection"));
                OrganizeWindowTestHost.Invoke(window, "UpdatePdfWorkspaceContextMenuState");
                Assert.Equal(Visibility.Collapsed, copy.Visibility);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SelectedImageContextMenu_ReusesImageOwnersAndRejectsStaleOrBusyActions()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                var overlay = Assert.IsType<Canvas>(OrganizeWindowTestHost.GetField(window, "_editOverlayCanvas"));
                var originalOutline = Assert.Single(overlay.Children.OfType<Polygon>());
                var menu = Assert.IsType<ContextMenu>(originalOutline.ContextMenu);
                Assert.Null(overlay.ContextMenu);
                Assert.Equal(
                    new[] { "Rotar 90°", "Reemplazar...", "Extraer PNG...", "Eliminar imagen" },
                    menu.Items.OfType<MenuItem>().Select(item => item.Header as string));

                menu.PlacementTarget = originalOutline;
                OrganizeWindowTestHost.Invoke(window, "UpdateSelectedImageContextMenuState");
                var rotate = Assert.IsType<MenuItem>(menu.Items[0]);
                Assert.True(rotate.IsEnabled);

                var originalMatrix = prepared.Workspace.GetState(prepared.Key).CurrentMatrix;
                OrganizeWindowTestHost.SetField(window, "_isBusy", true);
                OrganizeWindowTestHost.Invoke(window, "UpdateSelectedImageContextMenuState");
                Assert.False(rotate.IsEnabled);
                rotate.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, rotate));
                Assert.Equal(originalMatrix, prepared.Workspace.GetState(prepared.Key).CurrentMatrix);
                Assert.False(prepared.Workspace.CanUndo);

                OrganizeWindowTestHost.SetField(window, "_isBusy", false);
                OrganizeWindowTestHost.Invoke(window, "UpdateSelectedImageContextMenuState");
                Assert.True(rotate.IsEnabled);
                rotate.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, rotate));
                var rotatedMatrix = prepared.Workspace.GetState(prepared.Key).CurrentMatrix;
                Assert.NotEqual(originalMatrix, rotatedMatrix);
                Assert.True(prepared.Workspace.CanUndo);

                var currentOutline = Assert.Single(overlay.Children.OfType<Polygon>());
                Assert.NotSame(originalOutline, currentOutline);
                Assert.Same(menu, currentOutline.ContextMenu);
                menu.PlacementTarget = originalOutline;
                OrganizeWindowTestHost.Invoke(window, "UpdateSelectedImageContextMenuState");
                Assert.False(rotate.IsEnabled);
                rotate.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, rotate));
                Assert.Equal(rotatedMatrix, prepared.Workspace.GetState(prepared.Key).CurrentMatrix);

                OrganizeWindowTestHost.Invoke(window, "HandleImageEditEscape");
                Assert.Empty(overlay.Children.OfType<Polygon>());
            }
            finally
            {
                ImageEditCommandTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SelectedImageContextMenu_DoesNotRetainImageOwnerAcrossDocumentTabs()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var first = ImageEditPdfFixtureFactory.CreateSingleImage();
            using var second = ImageEditPdfFixtureFactory.CreateSingleImage();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, first.Path);
                var firstDocument = Assert.IsType<PdfDocumentWorkspace>(window.ActiveDocument);
                var overlay = Assert.IsType<Canvas>(OrganizeWindowTestHost.GetField(window, "_editOverlayCanvas"));
                var outline = Assert.Single(overlay.Children.OfType<Polygon>());
                var menu = Assert.IsType<ContextMenu>(outline.ContextMenu);
                menu.PlacementTarget = outline;
                OrganizeWindowTestHost.Invoke(window, "UpdateSelectedImageContextMenuState");
                var rotate = Assert.IsType<MenuItem>(menu.Items[0]);
                rotate.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, rotate));
                var rotated = prepared.Workspace.GetState(prepared.Key).CurrentMatrix;

                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, second.Path));
                Assert.NotSame(firstDocument, window.ActiveDocument);
                Assert.False((bool)OrganizeWindowTestHost.GetField(window, "_editModeActive")!);

                menu.PlacementTarget = outline;
                OrganizeWindowTestHost.Invoke(window, "UpdateSelectedImageContextMenuState");
                Assert.False(rotate.IsEnabled);
                rotate.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, rotate));
                Assert.Equal(rotated, prepared.Workspace.GetState(prepared.Key).CurrentMatrix);

                await (Task)OrganizeWindowTestHost.Invoke(window, "ActivateDocumentAsync", firstDocument)!;
                Assert.Same(firstDocument, window.ActiveDocument);
                Assert.True((bool)OrganizeWindowTestHost.GetField(window, "_editModeActive")!);
                var restoredOutline = Assert.Single(overlay.Children.OfType<Polygon>());
                Assert.NotSame(outline, restoredOutline);
                Assert.Same(menu, restoredOutline.ContextMenu);
                menu.PlacementTarget = restoredOutline;
                OrganizeWindowTestHost.Invoke(window, "UpdateSelectedImageContextMenuState");
                Assert.True(rotate.IsEnabled);

                var secondDocument = window.OpenDocuments.Single(document => !ReferenceEquals(document, firstDocument));
                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryCloseDocumentWithGuard", secondDocument));
            }
            finally
            {
                ImageEditCommandTestHost.CloseClean(window);
            }
        });
    }
}
