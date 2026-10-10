using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditWorkspaceTests
{
    [Fact]
    public void Create_NormalizesPath_CapturesFingerprintAndPasswordFlag()
    {
        using var file = TempPdfLikeFile.Create("abc");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, sourceOpenedWithPassword: true);
        var fingerprint = ImageEditContractApi.ReadObject(workspace, "SourceFingerprint");

        Assert.Equal(Path.GetFullPath(file.Path), ImageEditContractApi.Read<string>(workspace, "SourcePath"));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "SourceOpenedWithPassword"));
        Assert.Equal(new FileInfo(file.Path).Length, ImageEditContractApi.Read<long>(fingerprint, "FileLength"));
        Assert.Equal(new FileInfo(file.Path).LastWriteTimeUtc.Ticks, ImageEditContractApi.Read<long>(fingerprint, "LastWriteTimeUtcTicks"));
        Assert.True(ImageEditContractApi.MatchesCurrentFile(fingerprint, file.Path));

        File.AppendAllText(file.Path, "changed");
        Assert.False(ImageEditContractApi.MatchesCurrentFile(fingerprint, file.Path));
    }

    [Fact]
    public void EnsureObject_IsIdempotent_AndKeepsOneStatePerKey()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var sourceObject = ImageEditContractApi.CreateImageInfo(pageIndex: 0, pageObjectIndex: 2, translateX: 10, translateY: 20);

        var first = ImageEditContractApi.EnsureObject(workspace, sourceObject);
        var second = ImageEditContractApi.EnsureObject(workspace, sourceObject);
        var key = ImageEditContractApi.ReadObject(ImageEditContractApi.ReadObject(first, "ObjectRef"), "Key");

        Assert.Equal(first, second);
        Assert.Equal(first, ImageEditContractApi.GetState(workspace, key));
        Assert.Empty(ImageEditContractApi.EditedStates(workspace));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
    }

    [Fact]
    public void Commit_RecordsBeforeAfterAndMarksDirty()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var original = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));
        var moved = ImageEditContractApi.WithMatrix(original, translateX: 15, translateY: 5);

        ImageEditContractApi.Commit(workspace, "Move", moved);

        Assert.True(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));
        Assert.Single(ImageEditContractApi.EditedStates(workspace));
        Assert.Equal(moved, ImageEditContractApi.StateFor(workspace, original));
    }

    [Fact]
    public void Undo_RestoresBefore_AndRedoRestoresAfter()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var original = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));
        var moved = ImageEditContractApi.WithMatrix(original, 12, 8);
        ImageEditContractApi.Commit(workspace, "Move", moved);

        Assert.True(ImageEditContractApi.Undo(workspace));
        Assert.Equal(original, ImageEditContractApi.StateFor(workspace, original));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));

        Assert.True(ImageEditContractApi.Redo(workspace));
        Assert.Equal(moved, ImageEditContractApi.StateFor(workspace, original));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));
    }

    [Fact]
    public void NewEditAfterUndo_ClearsRedo()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var original = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));
        var first = ImageEditContractApi.WithMatrix(original, 10, 0);
        ImageEditContractApi.Commit(workspace, "Move", first);
        Assert.True(ImageEditContractApi.Undo(workspace));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));

        var second = ImageEditContractApi.WithMatrix(original, 0, 20);
        ImageEditContractApi.Commit(workspace, "Move", second);

        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));
        Assert.False(ImageEditContractApi.Redo(workspace));
        Assert.Equal(second, ImageEditContractApi.StateFor(workspace, original));
    }

    [Fact]
    public void Commit_NoOp_DoesNotCreateHistory()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var state = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));

        ImageEditContractApi.Commit(workspace, "Move", state);

        Assert.False(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));
        Assert.Empty(ImageEditContractApi.EditedStates(workspace));
    }

    [Fact]
    public void Commit_UnknownObject_FailsWithoutCreatingHistory()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));
        var unknown = ImageEditContractApi.CreateDetachedState(ImageEditContractApi.CreateImageInfo(0, 9, 0, 0));

        Assert.Throws<KeyNotFoundException>(() => ImageEditContractApi.Commit(workspace, "Move", unknown));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));
    }

    [Fact]
    public void MarkSavedBaseline_PreservesStates_ClearsHistory_AndMarksClean()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var original = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));
        var moved = ImageEditContractApi.WithMatrix(original, 30, 40);
        ImageEditContractApi.Commit(workspace, "Move", moved);

        ImageEditContractApi.MarkSavedBaseline(workspace);

        Assert.Equal(moved, ImageEditContractApi.StateFor(workspace, original));
        Assert.Single(ImageEditContractApi.EditedStates(workspace));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));
    }

    [Fact]
    public void SaveBaseline_SecondSaveFromOriginalReappliesPreviouslySavedLogicalEdits()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var objectA = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));
        var objectB = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 3, 50, 50));
        var savedA = ImageEditContractApi.WithMatrix(objectA, 10, 20);
        ImageEditContractApi.Commit(workspace, "Move", savedA);
        ImageEditContractApi.MarkSavedBaseline(workspace);

        var laterB = ImageEditContractApi.WithMatrix(objectB, 70, 80);
        ImageEditContractApi.Commit(workspace, "Move", laterB);
        var edited = ImageEditContractApi.EditedStates(workspace);

        Assert.Equal(2, edited.Count);
        Assert.Contains(edited, state => state.Equals(savedA));
        Assert.Contains(edited, state => state.Equals(laterB));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
    }

    [Fact]
    public void UndoAfterSavedBaseline_ReturnsToCleanSavedState()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = ImageEditContractApi.CreateWorkspace(file.Path, false);
        var original = ImageEditContractApi.EnsureObject(workspace, ImageEditContractApi.CreateImageInfo(0, 1, 0, 0));
        var saved = ImageEditContractApi.WithMatrix(original, 10, 10);
        ImageEditContractApi.Commit(workspace, "Move", saved);
        ImageEditContractApi.MarkSavedBaseline(workspace);
        var later = ImageEditContractApi.WithMatrix(saved, 25, 35);
        ImageEditContractApi.Commit(workspace, "Move", later);

        Assert.True(ImageEditContractApi.Undo(workspace));

        Assert.Equal(saved, ImageEditContractApi.StateFor(workspace, original));
        Assert.False(ImageEditContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.True(ImageEditContractApi.Read<bool>(workspace, "CanRedo"));
    }

    private sealed class TempPdfLikeFile : IDisposable
    {
        private TempPdfLikeFile(string root, string path)
        {
            Root = root;
            Path = path;
        }

        internal string Root { get; }
        internal string Path { get; }

        internal static TempPdfLikeFile Create(string content)
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-image-workspace-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var path = System.IO.Path.Combine(root, ".", "source.pdf");
            File.WriteAllText(path, content);
            return new TempPdfLikeFile(root, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}

internal static class ImageEditContractApi
{
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;

    private static Type WorkspaceType => RequiredType("SGPdf.App.Features.Edit.Images.ImageEditWorkspace");
    private static Type FingerprintType => RequiredType("SGPdf.App.Features.Edit.Images.ImageEditSourceFingerprint");
    private static Type ObjectKeyType => RequiredType("SGPdf.App.Features.Edit.Images.ImageObjectKey");
    private static Type ObjectRefType => RequiredType("SGPdf.App.Features.Edit.Images.ImageObjectRef");
    private static Type StateType => RequiredType("SGPdf.App.Features.Edit.Images.ImageEditState");
    private static Type OperationKindType => RequiredType("SGPdf.App.Features.Edit.Images.ImageEditOperationKind");
    private static Type MatrixType => RequiredType("SGPdf.App.Pdf.PdfObjectMatrix");
    private static Type BoundsType => RequiredType("SGPdf.App.Pdf.PdfObjectBounds");
    private static Type MetadataType => RequiredType("SGPdf.App.Pdf.PdfImageObjectMetadata");
    private static Type ImageInfoType => RequiredType("SGPdf.App.Pdf.PdfImageObjectInfo");

    internal static object CreateWorkspace(string path, bool sourceOpenedWithPassword) =>
        InvokeStatic(WorkspaceType, "Create", path, sourceOpenedWithPassword);

    internal static object CreateImageInfo(int pageIndex, int pageObjectIndex, double translateX, double translateY)
    {
        var matrix = Create(MatrixType, 100d, 0d, 0d, 60d, translateX, translateY);
        var bounds = Create(BoundsType, translateX, translateY, translateX + 100d, translateY + 60d);
        var metadata = Create(MetadataType, 100u, 60u, 24u, 2);
        return Create(ImageInfoType, pageIndex, pageObjectIndex, matrix, bounds, metadata);
    }

    internal static object EnsureObject(object workspace, object sourceObject) =>
        InvokeInstance(workspace, "EnsureObject", sourceObject)!;

    internal static object GetState(object workspace, object key) =>
        InvokeInstance(workspace, "GetState", key)!;

    internal static object StateFor(object workspace, object knownState)
    {
        var objectRef = ReadObject(knownState, "ObjectRef");
        var key = ReadObject(objectRef, "Key");
        return GetState(workspace, key);
    }

    internal static object WithMatrix(object state, double translateX, double translateY)
    {
        var objectRef = ReadObject(state, "ObjectRef");
        var matrix = Create(MatrixType, 100d, 0d, 0d, 60d, translateX, translateY);
        return Create(
            StateType,
            objectRef,
            matrix,
            ReadNullableObject(state, "ReplacementAsset"),
            ReadNullableObject(state, "Opacity"),
            ReadNullableObject(state, "TargetObjectIndex"),
            Read<bool>(state, "Deleted"));
    }

    internal static object CreateDetachedState(object sourceObject)
    {
        var key = Create(ObjectKeyType, Read<int>(sourceObject, "PageIndex"), Read<int>(sourceObject, "PageObjectIndex"));
        var objectRef = Create(ObjectRefType, key, sourceObject);
        return Create(
            StateType,
            objectRef,
            ReadObject(sourceObject, "Matrix"),
            null,
            null,
            null,
            false);
    }

    internal static void Commit(object workspace, string operationKind, object nextState)
    {
        var kind = Enum.Parse(OperationKindType, operationKind);
        InvokeInstance(workspace, "Commit", kind, nextState);
    }

    internal static bool Undo(object workspace) => (bool)InvokeInstance(workspace, "Undo")!;
    internal static bool Redo(object workspace) => (bool)InvokeInstance(workspace, "Redo")!;
    internal static void MarkSavedBaseline(object workspace) => InvokeInstance(workspace, "MarkSavedBaseline");

    internal static IReadOnlyList<object> EditedStates(object workspace) => ReadSequence(workspace, "EditedStates");

    internal static bool MatchesCurrentFile(object fingerprint, string path) =>
        (bool)InvokeInstance(fingerprint, "MatchesCurrentFile", path)!;

    internal static T Read<T>(object target, string propertyName) => (T)ReadObject(target, propertyName);

    internal static object ReadObject(object target, string propertyName) =>
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target)
        ?? throw new InvalidOperationException($"No existe {target.GetType().Name}.{propertyName}.");

    private static object? ReadNullableObject(object target, string propertyName) =>
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);

    private static Type RequiredType(string fullName) => AppAssembly.GetType(fullName, throwOnError: true)!;

    private static object Create(Type type, params object?[] args)
    {
        try
        {
            return Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: args,
                culture: null) ?? throw new InvalidOperationException($"No se pudo crear {type.FullName}.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static object InvokeStatic(Type type, string methodName, params object?[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        return Invoke(method, null, args)!;
    }

    private static object? InvokeInstance(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        return Invoke(method, target, args);
    }

    private static object? Invoke(MethodInfo method, object? target, object?[] args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static IReadOnlyList<object> ReadSequence(object target, string propertyName)
    {
        var enumerable = (IEnumerable)ReadObject(target, propertyName);
        return enumerable.Cast<object>().ToArray();
    }
}