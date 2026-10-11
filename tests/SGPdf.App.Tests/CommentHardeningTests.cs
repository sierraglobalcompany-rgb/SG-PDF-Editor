using System.Reflection;
using SGPdf.App.Features.Comments;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class CommentHardeningTests
{
    [Fact]
    public void DurableCommentStateGraph_ContainsNoNativeHandleTypes()
    {
        var roots = new[]
        {
            typeof(CommentWorkspace),
            typeof(CommentState),
            typeof(CommentCandidate),
            typeof(CommentMutation),
            typeof(CommentIdentity),
            typeof(CommentKey),
            typeof(CommentRect),
            typeof(CommentColor),
            typeof(CommentPoint),
            typeof(CommentQuad),
            typeof(CommentStroke)
        };

        var offenders = new List<string>();
        foreach (var root in roots)
        {
            foreach (var field in root.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (ContainsNativeHandleType(field.FieldType, new HashSet<Type>()))
                    offenders.Add($"{root.FullName}.{field.Name}: {field.FieldType}");
            }

            foreach (var property in root.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (ContainsNativeHandleType(property.PropertyType, new HashSet<Type>()))
                    offenders.Add($"{root.FullName}.{property.Name}: {property.PropertyType}");
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void Discovery_IsBoundedToTheRequestedPage()
    {
        using var fixture = F8PreservationFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.Path);

        var pageZero = session.GetCommentsOnPage(0);
        var pageOne = session.GetCommentsOnPage(1);

        Assert.NotEmpty(pageZero);
        Assert.All(pageZero, comment => Assert.Equal(0, comment.PageIndex));
        Assert.Contains(pageZero, comment =>
            comment.Subtype == CommentSubtype.Text &&
            string.Equals(comment.Contents, "Existing supported F8", StringComparison.Ordinal));
        Assert.Contains(pageZero, comment => comment.Subtype is null);
        Assert.Empty(pageOne);
    }

    [Fact]
    public void PreCanceledDiscovery_DoesNotPoisonSubsequentPageRead()
    {
        using var fixture = F8PreservationFixture.Create();
        using var session = PdfDocumentSession.Open(fixture.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            session.GetCommentsOnPage(0, cancellation.Token));

        var later = session.GetCommentsOnPage(0, CancellationToken.None);
        Assert.NotEmpty(later);
        Assert.All(later, comment => Assert.Equal(0, comment.PageIndex));
    }

    private static bool ContainsNativeHandleType(Type type, HashSet<Type> visited)
    {
        if (type == typeof(IntPtr) || type == typeof(UIntPtr) || type.IsPointer)
            return true;

        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
            return ContainsNativeHandleType(nullable, visited);

        if (type.IsArray)
            return ContainsNativeHandleType(type.GetElementType()!, visited);

        if (type.IsGenericType && type.GetGenericArguments().Any(argument => ContainsNativeHandleType(argument, visited)))
            return true;

        if (type.Assembly != typeof(CommentWorkspace).Assembly || !visited.Add(type))
            return false;

        return type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(field => ContainsNativeHandleType(field.FieldType, visited));
    }
}
