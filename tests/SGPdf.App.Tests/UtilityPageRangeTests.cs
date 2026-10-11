using System.Collections;
using System.Reflection;
using SGPdf.App.Features.Edit.Text;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class UtilityPageRangeTests
{
    private const string RangeTypeName = "SGPdf.App.Features.Utilities.UtilityPageRange";

    [Fact]
    public void All_ResolvesEveryPageInOrder()
    {
        var range = All();

        Assert.Equal(new[] { 0, 1, 2, 3 }, Resolve(range, 4));
    }

    [Fact]
    public void Current_ResolvesExactlyCurrentPage()
    {
        var range = Current(2);

        Assert.Equal(new[] { 2 }, Resolve(range, 5));
    }

    [Fact]
    public void Explicit_ResolvesInclusiveDeterministicRange()
    {
        var range = Explicit(1, 3);

        Assert.Equal(new[] { 1, 2, 3 }, Resolve(range, 6));
        Assert.Equal(new[] { 1, 2, 3 }, Resolve(range, 6));
    }

    [Fact]
    public void Current_OutOfBounds_Fails()
    {
        var range = Current(4);

        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Resolve(range, 4));
    }

    [Fact]
    public void Explicit_StartBeforeZero_Fails()
    {
        var range = Explicit(-1, 2);

        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Resolve(range, 4));
    }

    [Fact]
    public void Explicit_EndPastPageCount_Fails()
    {
        var range = Explicit(1, 4);

        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Resolve(range, 4));
    }

    [Fact]
    public void Explicit_StartAfterEnd_Fails()
    {
        var range = Explicit(3, 2);

        AssertInvocationThrows<ArgumentException>(() => Resolve(range, 5));
    }

    [Fact]
    public void InvalidPageCount_Fails()
    {
        AssertInvocationThrows<ArgumentOutOfRangeException>(() => Resolve(All(), 0));
    }

    [Fact]
    public void ReturnedCollection_CannotBeUsedToMutateModel()
    {
        var range = Explicit(0, 2);
        var resolved = ResolveRaw(range, 3);
        var list = Assert.IsAssignableFrom<IList<int>>(resolved);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = 99);
        Assert.Equal(new[] { 0, 1, 2 }, Resolve(range, 3));
    }

    private static object All()
    {
        var type = RequireType();
        var property = type.GetProperty("All", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var value = property!.GetValue(null);
        Assert.NotNull(value);
        return value!;
    }

    private static object Current(int pageIndex) => InvokeFactory("Current", pageIndex);

    private static object Explicit(int startPageIndex, int endPageIndex) =>
        InvokeFactory("Explicit", startPageIndex, endPageIndex);

    private static object InvokeFactory(string name, params object[] arguments)
    {
        var type = RequireType();
        var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var value = method!.Invoke(null, arguments);
        Assert.NotNull(value);
        return value!;
    }

    private static IReadOnlyList<int> Resolve(object range, int pageCount) =>
        Assert.IsAssignableFrom<IReadOnlyList<int>>(ResolveRaw(range, pageCount));

    private static object ResolveRaw(object range, int pageCount)
    {
        var method = range.GetType().GetMethod("Resolve", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var value = method!.Invoke(range, new object[] { pageCount });
        Assert.NotNull(value);
        return value!;
    }

    private static Type RequireType()
    {
        var type = typeof(FallbackFontAsset).Assembly.GetType(RangeTypeName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static TException AssertInvocationThrows<TException>(Action action)
        where TException : Exception
    {
        var wrapper = Assert.Throws<TargetInvocationException>(action);
        return Assert.IsType<TException>(wrapper.InnerException);
    }
}
