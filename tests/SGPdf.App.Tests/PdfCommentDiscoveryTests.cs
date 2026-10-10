using System.Collections;
using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfCommentDiscoveryTests
{
    [Fact]
    public void GetCommentsOnPage_ReadsSevenSupportedAnnotationsIntoManagedSnapshots()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        using var session = PdfDocumentSession.Open(fixture.OutputPath);

        var comments = CommentDiscoveryContractApi.GetCommentsOnPage(session, 0, CancellationToken.None);

        Assert.Equal(7, comments.Count);
        Assert.Equal(
            new[] { "Highlight", "Underline", "Strikeout", "Text", "Ink", "Square", "Circle" },
            comments.Select(comment => CommentDiscoveryContractApi.Read(comment, "Subtype")?.ToString()).ToArray());
        Assert.All(comments, comment => Assert.True(CommentDiscoveryContractApi.Read<bool>(comment, "IsEditable")));
        Assert.All(comments, comment => Assert.Equal(0, CommentDiscoveryContractApi.Read<int>(comment, "PageIndex")));
        Assert.Equal(Enumerable.Range(0, 7), comments.Select(comment => CommentDiscoveryContractApi.Read<int>(comment, "AnnotationIndex")));

        var highlight = comments[0];
        Assert.Equal(9, CommentDiscoveryContractApi.Read<int>(highlight, "NativeSubtype"));
        AssertRect(CommentDiscoveryContractApi.ReadObject(highlight, "Rect"), 55d, 305d, 185d, 335d);
        AssertColor(CommentDiscoveryContractApi.ReadObject(highlight, "Color"), 255, 220, 0, 160);
        var highlightQuads = CommentDiscoveryContractApi.ReadSequence(highlight, "Quads");
        Assert.Single(highlightQuads);
        AssertQuad(highlightQuads[0], 60d, 330d, 180d, 330d, 60d, 310d, 180d, 310d);

        var note = comments[3];
        Assert.Equal(CommentNativeCharacterizationHarness.NoteContents, CommentDiscoveryContractApi.Read<string>(note, "Contents"));

        var ink = comments[4];
        var strokes = CommentDiscoveryContractApi.ReadSequence(ink, "Strokes");
        Assert.Single(strokes);
        var points = CommentDiscoveryContractApi.ReadSequence(strokes[0], "Points");
        Assert.Equal(3, points.Count);
        AssertPoint(points[0], 70d, 170d);
        AssertPoint(points[1], 105d, 205d);
        AssertPoint(points[2], 145d, 180d);

        Assert.Equal(2.5d, CommentDiscoveryContractApi.Read<double?>(comments[5], "BorderWidth"), 3);
        Assert.Equal(2.5d, CommentDiscoveryContractApi.Read<double?>(comments[6], "BorderWidth"), 3);
    }

    [Fact]
    public void GetCommentsOnPage_RepeatedCallsReturnEquivalentManagedSnapshots()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        using var session = PdfDocumentSession.Open(fixture.OutputPath);

        var first = CommentDiscoveryContractApi.GetCommentsOnPage(session, 0, CancellationToken.None);
        var second = CommentDiscoveryContractApi.GetCommentsOnPage(session, 0, CancellationToken.None);

        Assert.Equal(7, first.Count);
        Assert.Equal(7, second.Count);
        for (var index = 0; index < first.Count; index++)
        {
            Assert.NotSame(first[index], second[index]);
            Assert.Equal(CommentDiscoveryContractApi.Read<int>(first[index], "AnnotationIndex"), CommentDiscoveryContractApi.Read<int>(second[index], "AnnotationIndex"));
            Assert.Equal(CommentDiscoveryContractApi.Read<int>(first[index], "NativeSubtype"), CommentDiscoveryContractApi.Read<int>(second[index], "NativeSubtype"));
            Assert.Equal(CommentDiscoveryContractApi.Read(first[index], "Subtype")?.ToString(), CommentDiscoveryContractApi.Read(second[index], "Subtype")?.ToString());
        }
    }

    [Fact]
    public void GetCommentsOnPage_HonorsPreCanceledToken()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        using var session = PdfDocumentSession.Open(fixture.OutputPath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            CommentDiscoveryContractApi.GetCommentsOnPage(session, 0, cts.Token));
    }

    [Fact]
    public void GetCommentsOnPage_RejectsInvalidPageIndex()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);
        using var session = PdfDocumentSession.Open(fixture.OutputPath);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommentDiscoveryContractApi.GetCommentsOnPage(session, 1, CancellationToken.None));
    }

    [Fact]
    public void PdfCommentInfo_DoesNotPersistNativeHandles()
    {
        var type = CommentDiscoveryContractApi.RequiredType("SGPdf.App.Pdf.PdfCommentInfo");
        var members = type
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(member => member is FieldInfo or PropertyInfo)
            .ToArray();

        Assert.NotEmpty(members);
        Assert.DoesNotContain(members, member => MemberType(member) == typeof(IntPtr));
        Assert.DoesNotContain(members, member => MemberType(member) == typeof(UIntPtr));
    }

    private static Type? MemberType(MemberInfo member) => member switch
    {
        FieldInfo field => field.FieldType,
        PropertyInfo property => property.PropertyType,
        _ => null
    };

    private static void AssertRect(object rect, double left, double bottom, double right, double top)
    {
        Assert.Equal(left, CommentDiscoveryContractApi.Read<double>(rect, "Left"), 3);
        Assert.Equal(bottom, CommentDiscoveryContractApi.Read<double>(rect, "Bottom"), 3);
        Assert.Equal(right, CommentDiscoveryContractApi.Read<double>(rect, "Right"), 3);
        Assert.Equal(top, CommentDiscoveryContractApi.Read<double>(rect, "Top"), 3);
    }

    private static void AssertColor(object color, byte red, byte green, byte blue, byte alpha)
    {
        Assert.Equal(red, CommentDiscoveryContractApi.Read<byte>(color, "Red"));
        Assert.Equal(green, CommentDiscoveryContractApi.Read<byte>(color, "Green"));
        Assert.Equal(blue, CommentDiscoveryContractApi.Read<byte>(color, "Blue"));
        Assert.Equal(alpha, CommentDiscoveryContractApi.Read<byte>(color, "Alpha"));
    }

    private static void AssertPoint(object point, double x, double y)
    {
        Assert.Equal(x, CommentDiscoveryContractApi.Read<double>(point, "X"), 3);
        Assert.Equal(y, CommentDiscoveryContractApi.Read<double>(point, "Y"), 3);
    }

    private static void AssertQuad(object quad, params double[] expected)
    {
        var actual = new List<double>(8);
        foreach (var property in new[] { "P1", "P2", "P3", "P4" })
        {
            var point = CommentDiscoveryContractApi.ReadObject(quad, property);
            actual.Add(CommentDiscoveryContractApi.Read<double>(point, "X"));
            actual.Add(CommentDiscoveryContractApi.Read<double>(point, "Y"));
        }
        Assert.Equal(expected, actual, new DoubleToleranceComparer(0.001));
    }

    private sealed class DoubleToleranceComparer(double tolerance) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) <= tolerance;
        public int GetHashCode(double obj) => 0;
    }
}

internal static class CommentDiscoveryContractApi
{
    internal static Type RequiredType(string fullName) =>
        typeof(PdfDocumentSession).Assembly.GetType(fullName, throwOnError: true)!;

    internal static IReadOnlyList<object> GetCommentsOnPage(
        PdfDocumentSession session,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        var method = typeof(PdfDocumentSession).GetMethod(
            "GetCommentsOnPage",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int), typeof(CancellationToken) },
            modifiers: null) ?? throw new MissingMethodException(typeof(PdfDocumentSession).FullName, "GetCommentsOnPage");

        try
        {
            var result = method.Invoke(session, new object[] { pageIndex, cancellationToken });
            return ((IEnumerable)(result ?? throw new InvalidOperationException("GetCommentsOnPage devolvió null."))).Cast<object>().ToArray();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    internal static object? Read(object target, string propertyName) =>
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target)
        ?? throw new InvalidOperationException($"No existe {target.GetType().Name}.{propertyName}.");

    internal static object ReadObject(object target, string propertyName) =>
        Read(target, propertyName) ?? throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} es null.");

    internal static T Read<T>(object target, string propertyName) => (T)Read(target, propertyName)!;

    internal static IReadOnlyList<object> ReadSequence(object target, string propertyName) =>
        ((IEnumerable)ReadObject(target, propertyName)).Cast<object>().ToArray();
}
