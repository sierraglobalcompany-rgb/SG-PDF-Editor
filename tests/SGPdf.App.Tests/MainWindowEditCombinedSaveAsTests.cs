using SGPdf.App.Features.Edit;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowEditCombinedSaveAsTests
{
    [Fact]
    public void SaveAs_WithTextWorkspace_PassesBothAndMarksBothBaselinesAfterSuccess()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = PdfEditWriterTextFixtureFactory.CreateImageThenText();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 5d)!);
                Assert.True(prepared.Workspace.IsDirty);

                using var source = PdfDocumentSession.Open(fixture.Path);
                var text = Assert.Single(source.GetTextObjects(0));
                var textWorkspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
                textWorkspace.EnsureObject(text);
                var candidate = textWorkspace.PrepareCandidate(
                    text.Key,
                    "CASA 321",
                    text.FontSize,
                    text.FillColor);
                Assert.True(candidate.IsValid);
                textWorkspace.CommitCandidate(candidate.Candidate!);
                Assert.True(textWorkspace.IsDirty);

                OrganizeWindowTestHost.SetField(window, "_textEditWorkspace", textWorkspace);
                var destination = Path.Combine(fixture.DirectoryPath, "saved.pdf");
                OrganizeWindowTestHost.SetField(window, "_selectEditPdfDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectEditPreflight",
                    (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) =>
                        new PdfEditPreflightResult(Array.Empty<PdfEditFinding>())));

                var writerCalls = 0;
                var imageDirtyDuringWriter = false;
                var textDirtyDuringWriter = false;
                OrganizeWindowTestHost.SetField(
                    window,
                    "_saveCombinedEditCopy",
                    (Action<ImageEditWorkspace, TextEditWorkspace, string, bool, CancellationToken>)
                    ((imageWorkspace, passedTextWorkspace, path, confirmed, _) =>
                    {
                        writerCalls++;
                        Assert.Same(prepared.Workspace, imageWorkspace);
                        Assert.Same(textWorkspace, passedTextWorkspace);
                        Assert.Equal(destination, path);
                        Assert.False(confirmed);
                        imageDirtyDuringWriter = imageWorkspace.IsDirty;
                        textDirtyDuringWriter = passedTextWorkspace.IsDirty;
                    }));

                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);

                Assert.Equal(1, writerCalls);
                Assert.True(imageDirtyDuringWriter);
                Assert.True(textDirtyDuringWriter);
                Assert.False(prepared.Workspace.IsDirty);
                Assert.False(textWorkspace.IsDirty);
            }
            finally
            {
                ImageEditCommandTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SaveAs_CombinedWriterFailure_PreservesBothDirtyBaselines()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = PdfEditWriterTextFixtureFactory.CreateImageThenText();
            var window = new MainWindow();
            try
            {
                var prepared = await ImageEditCommandTestHost.PrepareAsync(window, fixture.Path);
                Assert.True((bool)OrganizeWindowTestHost.Invoke(window, "RotateSelectedImage", 5d)!);

                using var source = PdfDocumentSession.Open(fixture.Path);
                var text = Assert.Single(source.GetTextObjects(0));
                var textWorkspace = TextEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
                textWorkspace.EnsureObject(text);
                var candidate = textWorkspace.PrepareCandidate(text.Key, "CASA 321", text.FontSize, text.FillColor);
                textWorkspace.CommitCandidate(candidate.Candidate!);
                OrganizeWindowTestHost.SetField(window, "_textEditWorkspace", textWorkspace);

                OrganizeWindowTestHost.SetField(
                    window,
                    "_selectEditPdfDestination",
                    (Func<string?>)(() => Path.Combine(fixture.DirectoryPath, "saved.pdf")));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_inspectEditPreflight",
                    (Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)((_, _) =>
                        new PdfEditPreflightResult(Array.Empty<PdfEditFinding>())));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_saveCombinedEditCopy",
                    (Action<ImageEditWorkspace, TextEditWorkspace, string, bool, CancellationToken>)
                    ((_, _, _, _, _) => throw new IOException("forced combined save failure")));

                Assert.False((bool)OrganizeWindowTestHost.Invoke(window, "TrySaveEditWorkspace")!);
                Assert.True(prepared.Workspace.IsDirty);
                Assert.True(textWorkspace.IsDirty);
            }
            finally
            {
                ImageEditCommandTestHost.CloseClean(window);
            }
        });
    }
}
