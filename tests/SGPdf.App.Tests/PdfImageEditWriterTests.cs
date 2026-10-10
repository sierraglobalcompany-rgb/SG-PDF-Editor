using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using SkiaSharp;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfImageEditWriterTests
{
    [Fact]
    public void SaveAs_DestinationEqualsSource_BlocksAndPreservesSource()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var before = File.ReadAllBytes(fixture.Path);
        var workspace = CreateWorkspace(fixture.Path);

        var ex = Assert.Throws<ArgumentException>(() => Save(CreateWriter(), workspace, fixture.Path));

        Assert.Contains("diferente", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, File.ReadAllBytes(fixture.Path));
    }

    [Fact]
    public void SaveAs_MissingDestinationDirectory_BlocksBeforeWrite()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path);
        var missingDirectory = Path.Combine(fixture.DirectoryPath, "missing", "nested");
        var destination = Path.Combine(missingDirectory, "out.pdf");

        Assert.Throws<DirectoryNotFoundException>(() => Save(CreateWriter(), workspace, destination));
        Assert.False(File.Exists(destination));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveAs_StaleOrMissingSource_PreservesExistingDestination(bool deleteSource)
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 9, 8, 7, 6, 5 };
        File.WriteAllBytes(destination, sentinel);

        if (deleteSource)
        {
            File.Delete(fixture.Path);
        }
        else
        {
            using (var stream = new FileStream(fixture.Path, FileMode.Append, FileAccess.Write, FileShare.Read))
                stream.WriteByte(0x20);
            File.SetLastWriteTimeUtc(fixture.Path, DateTime.UtcNow.AddSeconds(5));
        }

        Assert.Throws<IOException>(() => Save(CreateWriter(), workspace, destination));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_PasswordWorkspace_BlocksAndPreservesExistingDestination()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path, sourceOpenedWithPassword: true);
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 4, 3, 2, 1 };
        File.WriteAllBytes(destination, sentinel);

        Assert.Throws<InvalidOperationException>(() => Save(CreateWriter(), workspace, destination));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_SignedSource_BlocksAndPreservesExistingDestination()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 1, 3, 3, 7 };
        File.WriteAllBytes(destination, sentinel);
        var writer = CreateWriter(signatureCountOverride: _ => 1);

        Assert.Throws<InvalidOperationException>(() => Save(writer, workspace, destination));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_NativeMutationFailure_PreservesExistingDestinationAndCleansTemp()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path);
        var state = EnsureSingleState(workspace, fixture.Path);
        workspace.Commit(ImageEditOperationKind.Move, state with
        {
            CurrentMatrix = state.CurrentMatrix with { E = state.CurrentMatrix.E + 15d }
        });

        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 5, 4, 3, 2, 1 };
        File.WriteAllBytes(destination, sentinel);
        var writer = CreateWriter(setMatrixOverride: (_, _) => 0);

        Assert.Throws<InvalidOperationException>(() => Save(writer, workspace, destination));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_NativeSaveFailure_PreservesExistingDestinationAndCleansTemp()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path);
        EnsureSingleState(workspace, fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 7, 7, 7 };
        File.WriteAllBytes(destination, sentinel);
        var writer = CreateWriter(saveAsCopyOverride: (_, _, _) => 0);

        Assert.Throws<InvalidOperationException>(() => Save(writer, workspace, destination));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_Cancellation_PreservesExistingDestinationAndCleansTemp()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path);
        EnsureSingleState(workspace, fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 2, 4, 6, 8 };
        File.WriteAllBytes(destination, sentinel);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => Save(CreateWriter(), workspace, destination, cts.Token));
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void SaveAs_ValidationFailure_WithExistingDestination_PreservesDestinationAndCleansTemp()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var workspace = CreateWorkspace(fixture.Path);
        EnsureSingleState(workspace, fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "existing.pdf");
        var sentinel = new byte[] { 6, 6, 6, 6 };
        File.WriteAllBytes(destination, sentinel);
        var writer = CreateWriter(validateOutputOverride: (_, _, _) => throw new InvalidDataException("forced validation failure"));

        var ex = Assert.Throws<InvalidDataException>(() => Save(writer, workspace, destination));

        Assert.Contains("forced", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(sentinel, File.ReadAllBytes(destination));
        AssertNoTemps(fixture.DirectoryPath);
    }

    [Fact]
    public void Writer_TwoEditedImagesSamePage_DeleteAndReorder_ResolvesAllOriginalHandlesBeforeMutation()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateOverlappingImagesAndVector();
        using var sourceSession = PdfDocumentSession.Open(fixture.Path);
        var sourceImages = sourceSession.GetImageObjects(0).OrderBy(image => image.PageObjectIndex).ToArray();
        Assert.Equal(2, sourceImages.Length);

        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var first = workspace.EnsureObject(sourceImages[0]);
        var second = workspace.EnsureObject(sourceImages[1]);
        workspace.Commit(ImageEditOperationKind.Delete, first with { Deleted = true });
        workspace.Commit(ImageEditOperationKind.ChangeZOrder, second with { TargetObjectIndex = 0 });

        var destination = Path.Combine(fixture.DirectoryPath, "delete-reorder.pdf");
        Save(CreateWriter(), workspace, destination);

        using var saved = PdfDocumentSession.Open(destination);
        var remaining = Assert.Single(saved.GetImageObjects(0));
        Assert.Equal(0, remaining.PageObjectIndex);
        using var bitmap = SKBitmap.Decode(saved.GetImagePng(0, remaining.PageObjectIndex));
        Assert.NotNull(bitmap);
        var center = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        Assert.True(center.Blue > center.Red);
    }

    [Fact]
    public void SaveAs_Identity_WritesValidatedCopyAndLeavesSourceUntouched()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        var sourceBytes = File.ReadAllBytes(fixture.Path);
        var workspace = CreateWorkspace(fixture.Path);
        EnsureSingleState(workspace, fixture.Path);
        var destination = Path.Combine(fixture.DirectoryPath, "identity.pdf");

        Save(CreateWriter(), workspace, destination);

        Assert.True(File.Exists(destination));
        Assert.Equal(sourceBytes, File.ReadAllBytes(fixture.Path));
        using var saved = PdfDocumentSession.Open(destination);
        Assert.Equal(1, saved.PageCount);
        Assert.Single(saved.GetImageObjects(0));
        Assert.True(saved.RenderPage(0, 36d).PixelWidth > 0);
    }

    [Theory]
    [InlineData("move")]
    [InlineData("resize")]
    [InlineData("rotate")]
    public void SaveAs_Transform_SaveReopen_PersistsExpectedMatrix(string transform)
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var state = workspace.EnsureObject(image);

        var expected = transform switch
        {
            "move" => state.CurrentMatrix with
            {
                E = state.CurrentMatrix.E + 23d,
                F = state.CurrentMatrix.F + 11d
            },
            "resize" => state.CurrentMatrix with
            {
                A = state.CurrentMatrix.A * 1.35d,
                B = state.CurrentMatrix.B * 1.35d,
                C = state.CurrentMatrix.C * 0.80d,
                D = state.CurrentMatrix.D * 0.80d
            },
            "rotate" => ImageTransformMath.RotateAroundCenter(state.CurrentMatrix, 27d),
            _ => throw new ArgumentOutOfRangeException(nameof(transform))
        };
        var kind = transform switch
        {
            "move" => ImageEditOperationKind.Move,
            "resize" => ImageEditOperationKind.Resize,
            _ => ImageEditOperationKind.Rotate
        };
        workspace.Commit(kind, state with { CurrentMatrix = expected });

        var destination = Path.Combine(fixture.DirectoryPath, $"{transform}.pdf");
        Save(CreateWriter(), workspace, destination);

        using var saved = PdfDocumentSession.Open(destination);
        var actual = Assert.Single(saved.GetImageObjects(0));
        AssertMatrix(expected, actual.Matrix);
        Assert.True(actual.Bounds.Right > actual.Bounds.Left);
        Assert.True(actual.Bounds.Top > actual.Bounds.Bottom);
    }

    [Fact]
    public void SaveAs_ReplacementAfterSourceAssetDeleted_UsesCapturedBytes()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var replacementPath = Path.Combine(directory.Path, "replacement.png");
        Task5ImageTestFixture.WritePng(replacementPath, transparent: false);
        var replacement = ImageReplacementAssetLoader.Load(replacementPath);
        File.Delete(replacementPath);

        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var state = workspace.EnsureObject(image);
        workspace.Commit(ImageEditOperationKind.Replace, state with { ReplacementAsset = replacement });
        var destination = Path.Combine(fixture.DirectoryPath, "replacement.pdf");

        Save(CreateWriter(), workspace, destination);

        using var saved = PdfDocumentSession.Open(destination);
        var savedImage = Assert.Single(saved.GetImageObjects(0));
        AssertMatrix(state.CurrentMatrix, savedImage.Matrix);
        using var bitmap = SKBitmap.Decode(saved.GetImagePng(0, savedImage.PageObjectIndex));
        Assert.NotNull(bitmap);
        var center = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        Assert.True(center.Blue > center.Red);
        Assert.True(center.Blue > center.Green);
    }

    [Fact]
    public void SaveAs_TransparentReplacement_SaveReopen_PreservesRepresentativeAlpha()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        using var directory = Task5ImageTestFixture.CreateDirectory();
        var replacementPath = Path.Combine(directory.Path, "alpha.png");
        Task5ImageTestFixture.WritePng(replacementPath, transparent: true);
        var replacement = ImageReplacementAssetLoader.Load(replacementPath);
        Assert.True(replacement.HasAlpha);

        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var state = workspace.EnsureObject(image);
        workspace.Commit(ImageEditOperationKind.Replace, state with { ReplacementAsset = replacement });
        var destination = Path.Combine(fixture.DirectoryPath, "alpha-replacement.pdf");

        Save(CreateWriter(), workspace, destination);

        using var saved = PdfDocumentSession.Open(destination);
        var savedImage = Assert.Single(saved.GetImageObjects(0));
        using var bitmap = SKBitmap.Decode(saved.GetImagePng(0, savedImage.PageObjectIndex));
        Assert.NotNull(bitmap);
        var alphas = Enumerable.Range(0, bitmap.Height)
            .SelectMany(y => Enumerable.Range(0, bitmap.Width).Select(x => bitmap.GetPixel(x, y).Alpha))
            .ToArray();
        Assert.Contains(alphas, alpha => alpha < 255);
        Assert.Contains(alphas, alpha => alpha == 255);
    }

    [Fact]
    public void SaveBaseline_SecondSaveFromOriginalReappliesPreviouslySavedLogicalEdits()
    {
        using var fixture = ImageEditPdfFixtureFactory.CreateSingleImage();
        using var source = PdfDocumentSession.Open(fixture.Path);
        var image = Assert.Single(source.GetImageObjects(0));
        var workspace = ImageEditWorkspace.Create(fixture.Path, sourceOpenedWithPassword: false);
        var original = workspace.EnsureObject(image);

        var firstMatrix = original.CurrentMatrix with
        {
            E = original.CurrentMatrix.E + 20d,
            F = original.CurrentMatrix.F + 5d
        };
        workspace.Commit(ImageEditOperationKind.Move, original with { CurrentMatrix = firstMatrix });
        var firstDestination = Path.Combine(fixture.DirectoryPath, "first-save.pdf");
        Save(CreateWriter(), workspace, firstDestination);
        workspace.MarkSavedBaseline();
        Assert.False(workspace.IsDirty);

        var afterBaseline = workspace.GetState(original.ObjectRef.Key);
        var secondMatrix = ImageTransformMath.RotateAroundCenter(
            afterBaseline.CurrentMatrix with
            {
                E = afterBaseline.CurrentMatrix.E + 13d,
                F = afterBaseline.CurrentMatrix.F + 9d
            },
            18d);
        workspace.Commit(ImageEditOperationKind.Rotate, afterBaseline with { CurrentMatrix = secondMatrix });
        var secondDestination = Path.Combine(fixture.DirectoryPath, "second-save.pdf");
        Save(CreateWriter(), workspace, secondDestination);

        using var firstSaved = PdfDocumentSession.Open(firstDestination);
        AssertMatrix(firstMatrix, Assert.Single(firstSaved.GetImageObjects(0)).Matrix);
        using var secondSaved = PdfDocumentSession.Open(secondDestination);
        AssertMatrix(secondMatrix, Assert.Single(secondSaved.GetImageObjects(0)).Matrix);
    }

    private static ImageEditWorkspace CreateWorkspace(string sourcePath, bool sourceOpenedWithPassword = false)
        => ImageEditWorkspace.Create(sourcePath, sourceOpenedWithPassword);

    private static ImageEditState EnsureSingleState(ImageEditWorkspace workspace, string sourcePath)
    {
        using var session = PdfDocumentSession.Open(sourcePath);
        return workspace.EnsureObject(Assert.Single(session.GetImageObjects(0)));
    }

    private static object CreateWriter(
        Func<IntPtr, IntPtr, uint, int>? saveAsCopyOverride = null,
        Action<string, ImageEditWorkspace, CancellationToken>? validateOutputOverride = null,
        Func<string, int>? signatureCountOverride = null,
        Func<IntPtr, PdfObjectMatrix, int>? setMatrixOverride = null)
    {
        var type = typeof(PdfDocumentSession).Assembly.GetType(
            "SGPdf.App.Pdf.PdfEditWriter",
            throwOnError: true)!;
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(info => info.GetParameters().Length == 4);
        return constructor.Invoke(new object?[]
        {
            saveAsCopyOverride,
            validateOutputOverride,
            signatureCountOverride,
            setMatrixOverride
        });
    }

    private static void Save(
        object writer,
        ImageEditWorkspace workspace,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var method = writer.GetType().GetMethod(
            "SaveAsCopy",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(ImageEditWorkspace),
                typeof(string),
                typeof(bool),
                typeof(CancellationToken)
            },
            modifiers: null);
        Assert.NotNull(method);
        try
        {
            method.Invoke(writer, new object?[] { workspace, destinationPath, false, cancellationToken });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static void AssertNoTemps(string directory)
        => Assert.Empty(Directory.GetFiles(directory, ".*.sgpdf.tmp", SearchOption.TopDirectoryOnly));

    private static void AssertMatrix(PdfObjectMatrix expected, PdfObjectMatrix actual)
    {
        Assert.Equal(expected.A, actual.A, precision: 2);
        Assert.Equal(expected.B, actual.B, precision: 2);
        Assert.Equal(expected.C, actual.C, precision: 2);
        Assert.Equal(expected.D, actual.D, precision: 2);
        Assert.Equal(expected.E, actual.E, precision: 2);
        Assert.Equal(expected.F, actual.F, precision: 2);
    }
}
