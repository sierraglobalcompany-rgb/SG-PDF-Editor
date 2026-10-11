using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Features.Comments;
using SGPdf.App.Features.Edit;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentWriterTransactionTests
{
    [Fact]
    public void SaveAs_StaleSource_BlocksBeforePublishingAndKeepsWorkspaceDirty()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        var workspace = CreateDirtyWorkspace(fixture.Path, sourceOpenedWithPassword: false);
        var destination = Path.Combine(fixture.DirectoryPath, "stale.pdf");
        File.AppendAllText(fixture.Path, "\n% changed after workspace capture\n");

        var error = Assert.ThrowsAny<Exception>(() => InvokeLegacySave(new PdfCommentWriter(), workspace, destination));

        Assert.IsType<System.IO.IOException>(Unwrap(error));
        Assert.False(File.Exists(destination));
        Assert.True(workspace.IsDirty);
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_PasswordOpenedSource_BlocksBeforePublishingAndKeepsWorkspaceDirty()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        var workspace = CreateDirtyWorkspace(fixture.Path, sourceOpenedWithPassword: true);
        var destination = Path.Combine(fixture.DirectoryPath, "password.pdf");

        var error = Assert.ThrowsAny<Exception>(() => InvokeLegacySave(new PdfCommentWriter(), workspace, destination));

        Assert.IsType<InvalidOperationException>(Unwrap(error));
        Assert.False(File.Exists(destination));
        Assert.True(workspace.IsDirty);
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_CryptoSignaturePreflight_BlocksBeforeTemporaryCreation()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        var workspace = CreateDirtyWorkspace(fixture.Path, sourceOpenedWithPassword: false);
        var destination = Path.Combine(fixture.DirectoryPath, "crypto.pdf");
        var block = new PdfEditPreflightResult(new[]
        {
            new PdfEditFinding(
                PdfEditFindingKind.CryptographicSignature,
                PdfEditFindingSeverity.Block,
                "COMENTAR bloquea firmas criptográficas.",
                PdfEditPreservationStatus.Unknown)
        });
        var writer = CreateInjectedWriter(
            validateOutput: (_, _, _) => { },
            inspectPreflight: (_, _) => block);

        var error = Assert.ThrowsAny<Exception>(() => InvokeConfirmedSave(writer, workspace, destination, warningsConfirmed: true));

        Assert.IsType<InvalidOperationException>(Unwrap(error));
        Assert.False(File.Exists(destination));
        Assert.True(workspace.IsDirty);
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_WarningsNotConfirmed_BlocksBeforeTemporaryCreation()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        var workspace = CreateDirtyWorkspace(fixture.Path, sourceOpenedWithPassword: false);
        var destination = Path.Combine(fixture.DirectoryPath, "warning.pdf");
        var warning = new PdfEditPreflightResult(new[]
        {
            new PdfEditFinding(
                PdfEditFindingKind.Bookmark,
                PdfEditFindingSeverity.Warning,
                "COMENTAR requiere confirmación explícita.",
                PdfEditPreservationStatus.Unknown)
        });
        var writer = CreateInjectedWriter(
            validateOutput: (_, _, _) => { },
            inspectPreflight: (_, _) => warning);

        var error = Assert.ThrowsAny<Exception>(() => InvokeConfirmedSave(writer, workspace, destination, warningsConfirmed: false));

        Assert.IsType<InvalidOperationException>(Unwrap(error));
        Assert.False(File.Exists(destination));
        Assert.True(workspace.IsDirty);
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_ValidatorFailure_PreservesExistingDestinationCleansTempAndKeepsDirtyState()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        var workspace = CreateDirtyWorkspace(fixture.Path, sourceOpenedWithPassword: false);
        var destination = Path.Combine(fixture.DirectoryPath, "validator-failure.pdf");
        var sentinel = new byte[] { 9, 4, 1, 7 };
        File.WriteAllBytes(destination, sentinel);
        var writer = CreateInjectedWriter(
            validateOutput: (_, _, _) => throw new InvalidDataException("synthetic validator failure"),
            inspectPreflight: (_, _) => CleanPreflight());

        var error = Assert.ThrowsAny<Exception>(() => InvokeConfirmedSave(writer, workspace, destination, warningsConfirmed: false));

        Assert.IsType<InvalidDataException>(Unwrap(error));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        Assert.True(workspace.IsDirty);
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_CancelledAfterValidatorRuns_PreservesExistingDestinationCleansTempAndKeepsDirtyState()
    {
        using var fixture = ImageEditPreservationFixture.CreatePlain();
        var workspace = CreateDirtyWorkspace(fixture.Path, sourceOpenedWithPassword: false);
        var destination = Path.Combine(fixture.DirectoryPath, "cancel-after-temp.pdf");
        var sentinel = new byte[] { 4, 2, 4, 2 };
        File.WriteAllBytes(destination, sentinel);
        using var cancellation = new CancellationTokenSource();
        var validatorRan = false;
        var writer = CreateInjectedWriter(
            validateOutput: (_, _, _) =>
            {
                validatorRan = true;
                cancellation.Cancel();
            },
            inspectPreflight: (_, _) => CleanPreflight());

        var error = Assert.ThrowsAny<Exception>(() =>
            InvokeConfirmedSave(writer, workspace, destination, warningsConfirmed: false, cancellation.Token));

        Assert.True(validatorRan);
        Assert.IsType<OperationCanceledException>(Unwrap(error));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        Assert.True(workspace.IsDirty);
        AssertNoTemps(fixture.DirectoryPath);
    }

    private static CommentWorkspace CreateDirtyWorkspace(string sourcePath, bool sourceOpenedWithPassword)
    {
        var workspace = CommentWorkspace.Create(sourcePath, sourceOpenedWithPassword);
        workspace.Create(new CommentState(
            CommentKey.New(),
            0,
            CommentSubtype.Text,
            new CommentRect(30d, 40d, 50d, 60d),
            new CommentColor(255, 220, 0, 255),
            Array.Empty<CommentQuad>(),
            "Task 9",
            Array.Empty<CommentStroke>(),
            null,
            true,
            false));
        Assert.True(workspace.IsDirty);
        return workspace;
    }

    private static PdfEditPreflightResult CleanPreflight()
        => new(Array.Empty<PdfEditFinding>());

    private static PdfCommentWriter CreateInjectedWriter(
        Action<string, CommentWorkspace, CancellationToken> validateOutput,
        Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult> inspectPreflight)
    {
        var constructor = typeof(PdfCommentWriter).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(Action<string, CommentWorkspace, CancellationToken>),
                typeof(Func<PdfDocumentSession, CancellationToken, PdfEditPreflightResult>)
            },
            modifiers: null);
        Assert.NotNull(constructor);
        return Assert.IsType<PdfCommentWriter>(constructor.Invoke(new object[] { validateOutput, inspectPreflight }));
    }

    private static void InvokeLegacySave(PdfCommentWriter writer, CommentWorkspace workspace, string destinationPath)
        => Invoke(
            writer,
            new[] { typeof(CommentWorkspace), typeof(string), typeof(CancellationToken) },
            new object[] { workspace, destinationPath, CancellationToken.None });

    private static void InvokeConfirmedSave(
        PdfCommentWriter writer,
        CommentWorkspace workspace,
        string destinationPath,
        bool warningsConfirmed,
        CancellationToken cancellationToken = default)
        => Invoke(
            writer,
            new[] { typeof(CommentWorkspace), typeof(string), typeof(bool), typeof(CancellationToken) },
            new object[] { workspace, destinationPath, warningsConfirmed, cancellationToken });

    private static void Invoke(PdfCommentWriter writer, Type[] parameterTypes, object[] arguments)
    {
        var method = typeof(PdfCommentWriter).GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: parameterTypes,
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            method.Invoke(writer, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static Exception Unwrap(Exception error)
        => error is TargetInvocationException wrapper && wrapper.InnerException is Exception inner
            ? inner
            : error;

    private static void AssertNoTemps(string directory)
        => Assert.Empty(Directory.GetFiles(directory, ".*.sgpdf.tmp", SearchOption.TopDirectoryOnly));
}
