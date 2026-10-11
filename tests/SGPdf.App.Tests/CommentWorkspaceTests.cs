using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class CommentWorkspaceTests
{
    [Fact]
    public void CommentTool_ContainsExactlyF8V1Tools()
    {
        Assert.Equal(
            new[] { "Select", "Highlight", "Underline", "Strikeout", "Note", "Ink", "Rectangle", "Ellipse" },
            CommentContractApi.EnumNames("CommentTool"));
    }

    [Fact]
    public void Create_NormalizesPath_CapturesFingerprintAndPasswordFlag()
    {
        using var file = TempPdfLikeFile.Create("comment-source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, sourceOpenedWithPassword: true);
        var fingerprint = CommentContractApi.ReadObject(workspace, "SourceFingerprint");

        Assert.Equal(Path.GetFullPath(file.Path), CommentContractApi.Read<string>(workspace, "SourcePath"));
        Assert.True(CommentContractApi.Read<bool>(workspace, "SourceOpenedWithPassword"));
        Assert.True(CommentContractApi.MatchesCurrentFile(fingerprint, file.Path));

        File.AppendAllText(file.Path, "changed");
        Assert.False(CommentContractApi.MatchesCurrentFile(fingerprint, file.Path));
    }

    [Fact]
    public void EnsureBaseline_IsIdempotent_AndStartsClean()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var baseline = CommentContractApi.CreateState(isNew: false, subtype: "Text", contents: "Nota inicial");

        var first = CommentContractApi.EnsureBaseline(workspace, baseline);
        var second = CommentContractApi.EnsureBaseline(workspace, baseline);

        Assert.Same(first, second);
        Assert.False(CommentContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.False(CommentContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(CommentContractApi.Read<bool>(workspace, "CanRedo"));
        Assert.Empty(CommentContractApi.EditedStates(workspace));
    }

    [Fact]
    public void Create_IsCandidateFirst_CommitMarksDirty_AndUndoRedoAreExact()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var created = CommentContractApi.CreateState(isNew: true, subtype: "Highlight");
        var key = CommentContractApi.ReadObject(created, "Key");

        var candidate = CommentContractApi.PrepareCreate(workspace, created);
        Assert.Throws<KeyNotFoundException>(() => CommentContractApi.GetState(workspace, key));
        Assert.False(CommentContractApi.Read<bool>(workspace, "IsDirty"));

        CommentContractApi.CommitCandidate(workspace, candidate);
        Assert.Same(created, CommentContractApi.GetState(workspace, key));
        Assert.True(CommentContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.True(CommentContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.Single(CommentContractApi.EditedStates(workspace));

        Assert.True(CommentContractApi.Undo(workspace));
        Assert.Throws<KeyNotFoundException>(() => CommentContractApi.GetState(workspace, key));
        Assert.False(CommentContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.True(CommentContractApi.Read<bool>(workspace, "CanRedo"));

        Assert.True(CommentContractApi.Redo(workspace));
        Assert.Same(created, CommentContractApi.GetState(workspace, key));
        Assert.True(CommentContractApi.Read<bool>(workspace, "IsDirty"));
    }

    [Fact]
    public void MoveResizeNoteTextAndDelete_AreCandidateFirstAndUndoable()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var baseline = CommentContractApi.CreateState(isNew: false, subtype: "Text", contents: "A");
        var key = CommentContractApi.ReadObject(baseline, "Key");
        CommentContractApi.EnsureBaseline(workspace, baseline);

        var movedRect = CommentContractApi.CreateRect(20, 30, 80, 90);
        var move = CommentContractApi.PrepareMove(workspace, key, movedRect);
        Assert.Same(baseline, CommentContractApi.GetState(workspace, key));
        CommentContractApi.CommitCandidate(workspace, move);
        Assert.Equal(movedRect, CommentContractApi.ReadObject(CommentContractApi.GetState(workspace, key), "Rect"));

        var resizedRect = CommentContractApi.CreateRect(20, 30, 110, 140);
        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareResize(workspace, key, resizedRect));
        Assert.Equal(resizedRect, CommentContractApi.ReadObject(CommentContractApi.GetState(workspace, key), "Rect"));

        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareNoteText(workspace, key, "NIÑO áé"));
        Assert.Equal("NIÑO áé", CommentContractApi.Read<string?>(CommentContractApi.GetState(workspace, key), "Contents"));

        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareDelete(workspace, key));
        Assert.True(CommentContractApi.Read<bool>(CommentContractApi.GetState(workspace, key), "Deleted"));
        Assert.True(CommentContractApi.Read<bool>(workspace, "IsDirty"));

        Assert.True(CommentContractApi.Undo(workspace));
        Assert.False(CommentContractApi.Read<bool>(CommentContractApi.GetState(workspace, key), "Deleted"));
        Assert.Equal("NIÑO áé", CommentContractApi.Read<string?>(CommentContractApi.GetState(workspace, key), "Contents"));
    }

    [Fact]
    public void CreateThenDeleteBeforeSave_ReturnsToCleanBaseline()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var created = CommentContractApi.CreateState(isNew: true, subtype: "Square");
        var key = CommentContractApi.ReadObject(created, "Key");

        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareCreate(workspace, created));
        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareDelete(workspace, key));

        Assert.Throws<KeyNotFoundException>(() => CommentContractApi.GetState(workspace, key));
        Assert.False(CommentContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.Empty(CommentContractApi.EditedStates(workspace));
        Assert.True(CommentContractApi.Read<bool>(workspace, "CanUndo"));
    }

    [Fact]
    public void NoOpCommit_DoesNotCreateHistoryOrDirtyState()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var baseline = CommentContractApi.CreateState(isNew: false, subtype: "Circle");
        CommentContractApi.EnsureBaseline(workspace, baseline);

        var candidate = CommentContractApi.PrepareUpdate(workspace, "Update", baseline);
        CommentContractApi.CommitCandidate(workspace, candidate);

        Assert.False(CommentContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.False(CommentContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(CommentContractApi.Read<bool>(workspace, "CanRedo"));
        Assert.Empty(CommentContractApi.EditedStates(workspace));
    }

    [Fact]
    public void NewEditAfterUndo_ClearsRedo()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var baseline = CommentContractApi.CreateState(isNew: false, subtype: "Square");
        var key = CommentContractApi.ReadObject(baseline, "Key");
        CommentContractApi.EnsureBaseline(workspace, baseline);

        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareMove(workspace, key, CommentContractApi.CreateRect(10, 10, 60, 60)));
        Assert.True(CommentContractApi.Undo(workspace));
        Assert.True(CommentContractApi.Read<bool>(workspace, "CanRedo"));

        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareResize(workspace, key, CommentContractApi.CreateRect(0, 0, 90, 90)));

        Assert.False(CommentContractApi.Read<bool>(workspace, "CanRedo"));
        Assert.False(CommentContractApi.Redo(workspace));
    }

    [Fact]
    public void StaleCandidate_IsRejectedWithoutOverwritingNewerState()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var baseline = CommentContractApi.CreateState(isNew: false, subtype: "Square");
        var key = CommentContractApi.ReadObject(baseline, "Key");
        CommentContractApi.EnsureBaseline(workspace, baseline);

        var stale = CommentContractApi.PrepareMove(workspace, key, CommentContractApi.CreateRect(10, 10, 70, 70));
        var fresh = CommentContractApi.PrepareResize(workspace, key, CommentContractApi.CreateRect(0, 0, 100, 100));
        CommentContractApi.CommitCandidate(workspace, fresh);

        Assert.Throws<InvalidOperationException>(() => CommentContractApi.CommitCandidate(workspace, stale));
        Assert.Equal(CommentContractApi.CreateRect(0, 0, 100, 100), CommentContractApi.ReadObject(CommentContractApi.GetState(workspace, key), "Rect"));
    }

    [Fact]
    public void MarkSaved_MakesCurrentStateCleanAndClearsHistory_ButPreservesSourceDelta()
    {
        using var file = TempPdfLikeFile.Create("source");
        var workspace = CommentContractApi.CreateWorkspace(file.Path, false);
        var baseline = CommentContractApi.CreateState(isNew: false, subtype: "Square");
        var key = CommentContractApi.ReadObject(baseline, "Key");
        CommentContractApi.EnsureBaseline(workspace, baseline);
        CommentContractApi.CommitCandidate(workspace, CommentContractApi.PrepareMove(workspace, key, CommentContractApi.CreateRect(15, 25, 75, 85)));

        CommentContractApi.MarkSaved(workspace);

        Assert.False(CommentContractApi.Read<bool>(workspace, "IsDirty"));
        Assert.False(CommentContractApi.Read<bool>(workspace, "CanUndo"));
        Assert.False(CommentContractApi.Read<bool>(workspace, "CanRedo"));
        Assert.Single(CommentContractApi.EditedStates(workspace));
    }

    [Theory]
    [InlineData(double.NaN, 0, 10, 10)]
    [InlineData(double.PositiveInfinity, 0, 10, 10)]
    [InlineData(10, 0, 5, 10)]
    [InlineData(0, 10, 10, 5)]
    public void InvalidRectGeometry_IsRejected(double left, double bottom, double right, double top)
    {
        Assert.ThrowsAny<ArgumentException>(() => CommentContractApi.CreateRect(left, bottom, right, top));
    }

    [Fact]
    public void CommentsDomain_DoesNotPersistNativeHandles()
    {
        var commentTypes = typeof(PdfDocumentSession).Assembly.GetTypes()
            .Where(type => type.Namespace == "SGPdf.App.Features.Comments")
            .ToArray();

        Assert.NotEmpty(commentTypes);
        foreach (var type in commentTypes)
        {
            var nativeMembers = type
                .GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(member => member switch
                {
                    FieldInfo field => field.FieldType == typeof(IntPtr) || field.FieldType == typeof(UIntPtr),
                    PropertyInfo property => property.PropertyType == typeof(IntPtr) || property.PropertyType == typeof(UIntPtr),
                    _ => false
                })
                .Select(member => $"{type.FullName}.{member.Name}")
                .ToArray();

            Assert.True(nativeMembers.Length == 0, $"El dominio de comentarios no puede persistir handles nativos: {string.Join(", ", nativeMembers)}");
        }
    }

    private sealed class TempPdfLikeFile : IDisposable
    {
        private TempPdfLikeFile(string root, string path)
        {
            Root = root;
            Path = path;
        }

        private string Root { get; }
        internal string Path { get; }

        internal static TempPdfLikeFile Create(string content)
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-comment-workspace-{Guid.NewGuid():N}");
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

internal static class CommentContractApi
{
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;

    private static Type RequiredType(string simpleName) =>
        AppAssembly.GetType($"SGPdf.App.Features.Comments.{simpleName}", throwOnError: true)!;

    private static Type WorkspaceType => RequiredType("CommentWorkspace");
    private static Type StateType => RequiredType("CommentState");
    private static Type KeyType => RequiredType("CommentKey");
    private static Type RectType => RequiredType("CommentRect");
    private static Type ColorType => RequiredType("CommentColor");
    private static Type SubtypeType => RequiredType("CommentSubtype");
    private static Type OperationKindType => RequiredType("CommentOperationKind");

    internal static string[] EnumNames(string simpleName) => Enum.GetNames(RequiredType(simpleName));

    internal static object CreateWorkspace(string path, bool sourceOpenedWithPassword) =>
        InvokeStatic(WorkspaceType, "Create", path, sourceOpenedWithPassword)!;

    internal static object CreateRect(double left, double bottom, double right, double top) =>
        Create(RectType, left, bottom, right, top);

    internal static object CreateState(bool isNew, string subtype, string? contents = null)
    {
        var key = Create(KeyType, Guid.NewGuid());
        var rect = CreateRect(0, 0, 50, 50);
        var color = Create(ColorType, (byte)255, (byte)220, (byte)0, (byte)180);
        var subtypeValue = Enum.Parse(SubtypeType, subtype);
        var emptyQuads = Array.CreateInstance(RequiredType("CommentQuad"), 0);
        var emptyStrokes = Array.CreateInstance(RequiredType("CommentStroke"), 0);
        return Create(
            StateType,
            key,
            0,
            subtypeValue,
            rect,
            color,
            emptyQuads,
            contents,
            emptyStrokes,
            null,
            isNew,
            false);
    }

    internal static object EnsureBaseline(object workspace, object state) => InvokeInstance(workspace, "EnsureBaseline", state)!;
    internal static object GetState(object workspace, object key) => InvokeInstance(workspace, "GetState", key)!;
    internal static object PrepareCreate(object workspace, object state) => InvokeInstance(workspace, "PrepareCreate", state)!;

    internal static object PrepareUpdate(object workspace, string kind, object nextState)
    {
        var operationKind = Enum.Parse(OperationKindType, kind);
        return InvokeInstance(workspace, "PrepareUpdate", operationKind, nextState)!;
    }

    internal static object PrepareMove(object workspace, object key, object rect) => InvokeInstance(workspace, "PrepareMove", key, rect)!;
    internal static object PrepareResize(object workspace, object key, object rect) => InvokeInstance(workspace, "PrepareResize", key, rect)!;
    internal static object PrepareNoteText(object workspace, object key, string text) => InvokeInstance(workspace, "PrepareNoteText", key, text)!;
    internal static object PrepareDelete(object workspace, object key) => InvokeInstance(workspace, "PrepareDelete", key)!;
    internal static void CommitCandidate(object workspace, object candidate) => InvokeInstance(workspace, "CommitCandidate", candidate);
    internal static bool Undo(object workspace) => (bool)InvokeInstance(workspace, "Undo")!;
    internal static bool Redo(object workspace) => (bool)InvokeInstance(workspace, "Redo")!;
    internal static void MarkSaved(object workspace) => InvokeInstance(workspace, "MarkSaved");
    internal static IReadOnlyList<object> EditedStates(object workspace) => ReadSequence(workspace, "EditedStates");

    internal static bool MatchesCurrentFile(object fingerprint, string path) =>
        (bool)InvokeInstance(fingerprint, "MatchesCurrentFile", path)!;

    internal static T Read<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No existe {target.GetType().Name}.{propertyName}.");
        return (T?)property.GetValue(target)!;
    }

    internal static object ReadObject(object target, string propertyName) =>
        Read<object>(target, propertyName);

    private static IReadOnlyList<object> ReadSequence(object target, string propertyName)
    {
        var value = ReadObject(target, propertyName);
        return ((IEnumerable)value).Cast<object>().ToArray();
    }

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

    private static object? InvokeStatic(Type type, string methodName, params object?[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        return Invoke(method, null, args);
    }

    private static object? InvokeInstance(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == args.Length)
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
}
