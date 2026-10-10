using System.Reflection;
using SGPdf.App.Features.Edit.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TextEditWorkspaceTests
{
    [Fact]
    public void Policy_ReorderingKnownRunesAndAddingWhitespace_UsesOriginalFont()
    {
        var original = Info("CASA 123");
        var result = Evaluate(original, " CASA 321 ", 18d, original.FillColor);

        Assert.True(Bool(result, "IsValid"));
        Assert.False(Bool(result, "IsReadOnly"));
        Assert.Equal("OriginalFont", StrategyName(result));
    }

    [Fact]
    public void Policy_NewRuneOrSizeChange_UsesFallbackTtf()
    {
        var original = Info("CASA");

        var newRune = Evaluate(original, "NIÑO", 18d, original.FillColor);
        Assert.True(Bool(newRune, "IsValid"));
        Assert.Equal("FallbackTtf", StrategyName(newRune));

        var sizeChange = Evaluate(original, "CASA", 19d, original.FillColor);
        Assert.True(Bool(sizeChange, "IsValid"));
        Assert.Equal("FallbackTtf", StrategyName(sizeChange));
    }

    [Fact]
    public void Policy_UnsupportedRenderMode_IsReadOnly()
    {
        var original = Info("CASA", renderMode: 1);
        var result = Evaluate(original, "CASA", 18d, original.FillColor);

        Assert.False(Bool(result, "IsValid"));
        Assert.True(Bool(result, "IsReadOnly"));
        Assert.Null(Property(result, "Candidate"));
    }

    [Theory]
    [InlineData("", 18d)]
    [InlineData("   ", 18d)]
    [InlineData("CASA", 0d)]
    [InlineData("CASA", -1d)]
    [InlineData("CASA", 1000.01d)]
    [InlineData("CASA", double.NaN)]
    [InlineData("CASA", double.PositiveInfinity)]
    public void Policy_InvalidTextOrSize_IsRejected(string text, double size)
    {
        var original = Info("CASA");
        var result = Evaluate(original, text, size, original.FillColor);

        Assert.False(Bool(result, "IsValid"));
        Assert.False(Bool(result, "IsReadOnly"));
        Assert.Null(Property(result, "Candidate"));
    }

    [Fact]
    public void Policy_UnsupportedFallbackRune_IsRejected()
    {
        var original = Info("CASA");
        var result = Evaluate(original, "CASA 😀", 18d, original.FillColor);

        Assert.False(Bool(result, "IsValid"));
        Assert.Contains("U+1F600", String(result, "Error"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Policy_UsesRunesRatherThanUtf16CodeUnits()
    {
        var original = Info("\U00010000\U00010401");
        var result = Evaluate(original, "\U00010001", 18d, original.FillColor);

        Assert.False(Bool(result, "IsValid"));
        Assert.Contains("U+10001", String(result, "Error"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Workspace_CandidateFirst_InvalidDoesNotDirty_ValidCommitDoes()
    {
        using var source = TempSource.Create();
        var workspace = CreateWorkspace(source.Path);
        var original = Info("CASA");
        EnsureObject(workspace, original);

        var invalid = Prepare(workspace, original.Key, "   ", 18d, original.FillColor);
        Assert.False(Bool(invalid, "IsValid"));
        Assert.False(Bool(workspace, "IsDirty"));

        var valid = Prepare(workspace, original.Key, "NIÑO", 18d, original.FillColor);
        Assert.True(Bool(valid, "IsValid"));
        Commit(workspace, Property(valid, "Candidate")!);

        Assert.True(Bool(workspace, "IsDirty"));
        var state = GetState(workspace, original.Key);
        Assert.Equal("NIÑO", String(state, "Text"));
        Assert.Equal("FallbackTtf", Property(state, "FontStrategy")!.ToString());
    }

    [Fact]
    public void Workspace_NoOpAndSavedBaseline_RemainClean()
    {
        using var source = TempSource.Create();
        var workspace = CreateWorkspace(source.Path);
        var original = Info("CASA");
        EnsureObject(workspace, original);

        var noOp = Prepare(workspace, original.Key, "CASA", 18d, original.FillColor);
        Commit(workspace, Property(noOp, "Candidate")!);
        Assert.False(Bool(workspace, "IsDirty"));

        var changed = Prepare(workspace, original.Key, "NIÑO", 18d, original.FillColor);
        Commit(workspace, Property(changed, "Candidate")!);
        Assert.True(Bool(workspace, "IsDirty"));

        Invoke(workspace, "MarkSavedBaseline");
        Assert.False(Bool(workspace, "IsDirty"));
    }

    [Fact]
    public void Workspace_RejectsStaleCandidateAndDetectsStaleSourceFingerprint()
    {
        using var source = TempSource.Create();
        var workspace = CreateWorkspace(source.Path);
        var original = Info("CASA");
        EnsureObject(workspace, original);

        var first = Prepare(workspace, original.Key, "NIÑO", 18d, original.FillColor);
        var stale = Prepare(workspace, original.Key, "CASA 1", 18d, original.FillColor);
        Commit(workspace, Property(first, "Candidate")!);

        Assert.Throws<TargetInvocationException>(() => Commit(workspace, Property(stale, "Candidate")!));
        Assert.True(Bool(workspace, "SourceMatchesCurrentFile"));

        File.AppendAllText(source.Path, "changed");
        Assert.False(Bool(workspace, "SourceMatchesCurrentFile"));
    }

    [Fact]
    public void ManagedTextEditTypes_DoNotPersistNativeHandles()
    {
        foreach (var typeName in new[]
                 {
                     "SGPdf.App.Features.Edit.Text.TextEditState",
                     "SGPdf.App.Features.Edit.Text.TextEditWorkspace",
                     "SGPdf.App.Features.Edit.Text.TextEditCandidate"
                 })
        {
            var type = RequireType(typeName);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.DoesNotContain(fields, field => field.FieldType == typeof(IntPtr) || field.FieldType == typeof(UIntPtr));
        }
    }

    private static PdfTextObjectInfo Info(string text, int renderMode = 0) => new(
        new TextObjectKey(0, 3),
        text,
        new PdfObjectMatrix(1d, 0d, 0d, 1d, 20d, 30d),
        new PdfObjectBounds(20d, 30d, 120d, 50d),
        new PdfTextObjectQuad(20d, 30d, 120d, 30d, 120d, 50d, 20d, 50d),
        "Helvetica",
        18d,
        new PdfTextFillColor(20u, 30u, 40u, 255u),
        renderMode);

    private static object Evaluate(PdfTextObjectInfo source, string text, double size, PdfTextFillColor color)
    {
        var type = RequireType("SGPdf.App.Features.Edit.Text.TextEditPolicy");
        var method = type.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.NonPublic)!;
        return method.Invoke(null, new object[] { source, text, size, color })!;
    }

    private static object CreateWorkspace(string path)
    {
        var type = RequireType("SGPdf.App.Features.Edit.Text.TextEditWorkspace");
        var method = type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
        return method.Invoke(null, new object[] { path, false })!;
    }

    private static void EnsureObject(object workspace, PdfTextObjectInfo source) =>
        Invoke(workspace, "EnsureObject", source);

    private static object Prepare(object workspace, TextObjectKey key, string text, double size, PdfTextFillColor color) =>
        Invoke(workspace, "PrepareCandidate", key, text, size, color)!;

    private static void Commit(object workspace, object candidate) =>
        Invoke(workspace, "CommitCandidate", candidate);

    private static object GetState(object workspace, TextObjectKey key) =>
        Invoke(workspace, "GetState", key)!;

    private static object? Invoke(object target, string name, params object[] args)
    {
        var method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == name && item.GetParameters().Length == args.Length);
        return method.Invoke(target, args);
    }

    private static bool Bool(object target, string property) => (bool)Property(target, property)!;
    private static string String(object target, string property) => (string)Property(target, property)!;
    private static object? Property(object target, string property) =>
        target.GetType()
            .GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(target);
    private static string StrategyName(object result) => Property(Property(result, "Candidate")!, "FontStrategy")!.ToString()!;

    private static Type RequireType(string fullName) => typeof(MainWindow).Assembly.GetType(fullName, throwOnError: true)!;

    private sealed class TempSource : IDisposable
    {
        private TempSource(string path) => Path = path;
        internal string Path { get; }

        internal static TempSource Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sgpdf-f7-workspace-{Guid.NewGuid():N}.pdf");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
            return new TempSource(path);
        }

        public void Dispose()
        {
            try { File.Delete(Path); }
            catch { }
        }
    }
}
