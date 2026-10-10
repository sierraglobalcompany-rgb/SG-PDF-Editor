using System.Collections;
using System.Reflection;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class CommentIdentityTests
{
    [Fact]
    public void CreateForPage_DistinguishesSameSubtypeWithoutDependingOnAnnotationIndex()
    {
        var comments = new[]
        {
            CreateHighlight(2, new CommentRect(10, 10, 50, 30), new CommentColor(255, 220, 0, 160)),
            CreateHighlight(7, new CommentRect(60, 10, 100, 30), new CommentColor(255, 220, 0, 160))
        };

        var identities = CommentIdentityContractApi.CreateForPage(comments);

        Assert.Equal(2, identities.Count);
        Assert.NotEqual(
            CommentIdentityContractApi.Read<string>(identities[0], "StructuralFingerprint"),
            CommentIdentityContractApi.Read<string>(identities[1], "StructuralFingerprint"));
        Assert.Equal(0, CommentIdentityContractApi.Read<int>(identities[0], "DuplicateOrdinal"));
        Assert.Equal(0, CommentIdentityContractApi.Read<int>(identities[1], "DuplicateOrdinal"));
    }

    [Fact]
    public void Resolve_RebindsAcrossFreshReopenWhenNativeIndicesShift()
    {
        var original = new[]
        {
            CreateHighlight(3, new CommentRect(10, 10, 50, 30), new CommentColor(255, 220, 0, 160)),
            CreateHighlight(4, new CommentRect(60, 10, 100, 30), new CommentColor(255, 220, 0, 160))
        };
        var identity = CommentIdentityContractApi.CreateForPage(original)[1];

        var reopened = new[]
        {
            CreateNote(0, new CommentRect(5, 5, 15, 15), "otra"),
            CreateHighlight(8, new CommentRect(10, 10, 50, 30), new CommentColor(255, 220, 0, 160)),
            CreateHighlight(9, new CommentRect(60, 10, 100, 30), new CommentColor(255, 220, 0, 160))
        };

        var resolved = CommentIdentityContractApi.Resolve(identity, reopened);

        Assert.NotNull(resolved);
        Assert.Equal(9, resolved!.AnnotationIndex);
    }

    [Fact]
    public void Resolve_UsesDuplicateOrdinalButFailsClosedWhenDuplicateCardinalityChanges()
    {
        var duplicateA = CreateHighlight(1, new CommentRect(10, 10, 50, 30), new CommentColor(255, 220, 0, 160));
        var duplicateB = duplicateA with { AnnotationIndex = 2 };
        var identities = CommentIdentityContractApi.CreateForPage(new[] { duplicateA, duplicateB });

        Assert.Equal(0, CommentIdentityContractApi.Read<int>(identities[0], "DuplicateOrdinal"));
        Assert.Equal(1, CommentIdentityContractApi.Read<int>(identities[1], "DuplicateOrdinal"));
        Assert.Equal(2, CommentIdentityContractApi.Read<int>(identities[0], "DuplicateCount"));

        var reopenedSame = new[]
        {
            duplicateA with { AnnotationIndex = 5 },
            duplicateB with { AnnotationIndex = 6 }
        };
        Assert.Equal(6, CommentIdentityContractApi.Resolve(identities[1], reopenedSame)!.AnnotationIndex);

        var reopenedMissingDuplicate = new[] { duplicateA with { AnnotationIndex = 9 } };
        Assert.Null(CommentIdentityContractApi.Resolve(identities[1], reopenedMissingDuplicate));
    }

    [Fact]
    public void Resolve_FailsClosedWhenStructuralFingerprintNoLongerMatches()
    {
        var original = CreateNote(4, new CommentRect(20, 20, 40, 40), "Texto inicial");
        var identity = CommentIdentityContractApi.CreateForPage(new[] { original }).Single();
        var changed = original with { AnnotationIndex = 11, Contents = "Texto alterado externamente" };

        Assert.Null(CommentIdentityContractApi.Resolve(identity, new[] { changed }));
    }

    [Fact]
    public void FreshPdfReopen_ProducesEquivalentStructuralIdentities()
    {
        using var fixture = CommentNativeCharacterizationHarness.CreateFixture();
        CommentNativeCharacterizationHarness.CreateSevenAnnotationsAndSave(fixture.SourcePath, fixture.OutputPath);

        IReadOnlyList<object> firstIds;
        using (var firstSession = PdfDocumentSession.Open(fixture.OutputPath))
            firstIds = CommentIdentityContractApi.CreateForPage(firstSession.GetCommentsOnPage(0));

        IReadOnlyList<object> secondIds;
        using (var secondSession = PdfDocumentSession.Open(fixture.OutputPath))
            secondIds = CommentIdentityContractApi.CreateForPage(secondSession.GetCommentsOnPage(0));

        Assert.Equal(firstIds.Count, secondIds.Count);
        for (var index = 0; index < firstIds.Count; index++)
        {
            Assert.Equal(
                CommentIdentityContractApi.Read<string>(firstIds[index], "StructuralFingerprint"),
                CommentIdentityContractApi.Read<string>(secondIds[index], "StructuralFingerprint"));
            Assert.Equal(
                CommentIdentityContractApi.Read<int>(firstIds[index], "DuplicateOrdinal"),
                CommentIdentityContractApi.Read<int>(secondIds[index], "DuplicateOrdinal"));
        }
    }

    private static PdfCommentInfo CreateHighlight(int annotationIndex, CommentRect rect, CommentColor color)
    {
        var quad = new CommentQuad(
            new CommentPoint(rect.Left, rect.Top),
            new CommentPoint(rect.Right, rect.Top),
            new CommentPoint(rect.Left, rect.Bottom),
            new CommentPoint(rect.Right, rect.Bottom));
        return new PdfCommentInfo(
            0, annotationIndex, PdfiumNative.FPDF_ANNOT_HIGHLIGHT, CommentSubtype.Highlight, true,
            rect, color, new[] { quad }, string.Empty, Array.Empty<CommentStroke>(), null);
    }

    private static PdfCommentInfo CreateNote(int annotationIndex, CommentRect rect, string contents) =>
        new(
            0, annotationIndex, PdfiumNative.FPDF_ANNOT_TEXT, CommentSubtype.Text, true,
            rect, new CommentColor(255, 210, 0, 255), Array.Empty<CommentQuad>(), contents,
            Array.Empty<CommentStroke>(), null);
}

internal static class CommentIdentityContractApi
{
    private const string TypeName = "SGPdf.App.Features.Comments.CommentIdentity";

    internal static IReadOnlyList<object> CreateForPage(IReadOnlyList<PdfCommentInfo> comments)
    {
        var type = RequiredType();
        var method = type.GetMethod(
            "CreateForPage",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(TypeName, "CreateForPage");
        var result = method.Invoke(null, new object[] { comments })
            ?? throw new InvalidOperationException("CommentIdentity.CreateForPage devolvió null.");
        return ((IEnumerable)result).Cast<object>().ToArray();
    }

    internal static PdfCommentInfo? Resolve(object identity, IReadOnlyList<PdfCommentInfo> comments)
    {
        var type = RequiredType();
        var method = type.GetMethod(
            "Resolve",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(TypeName, "Resolve");
        return (PdfCommentInfo?)method.Invoke(null, new object[] { identity, comments });
    }

    internal static T Read<T>(object target, string propertyName) =>
        (T)(target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target)
            ?? throw new InvalidOperationException($"No existe {target.GetType().Name}.{propertyName}."));

    private static Type RequiredType() =>
        typeof(CommentWorkspace).Assembly.GetType(TypeName, throwOnError: true)!;
}
