using SGPdf.App.Features.Utilities;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class UtilityPageRangeTests
{
    [Fact]
    public void All_ResolvesEveryPageInOrder()
    {
        Assert.Equal(new[] { 0, 1, 2, 3 }, UtilityPageRange.All.Resolve(4));
    }

    [Fact]
    public void Current_ResolvesExactlyCurrentPage()
    {
        Assert.Equal(new[] { 2 }, UtilityPageRange.Current(2).Resolve(5));
    }

    [Fact]
    public void Explicit_ResolvesInclusiveDeterministicRange()
    {
        var range = UtilityPageRange.Explicit(1, 3);

        Assert.Equal(new[] { 1, 2, 3 }, range.Resolve(6));
        Assert.Equal(new[] { 1, 2, 3 }, range.Resolve(6));
    }

    [Fact]
    public void Current_OutOfBounds_Fails()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UtilityPageRange.Current(4).Resolve(4));
    }

    [Fact]
    public void Explicit_StartBeforeZero_Fails()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UtilityPageRange.Explicit(-1, 2).Resolve(4));
    }

    [Fact]
    public void Explicit_EndPastPageCount_Fails()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UtilityPageRange.Explicit(1, 4).Resolve(4));
    }

    [Fact]
    public void Explicit_StartAfterEnd_Fails()
    {
        Assert.Throws<ArgumentException>(() => UtilityPageRange.Explicit(3, 2).Resolve(5));
    }

    [Fact]
    public void InvalidPageCount_Fails()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UtilityPageRange.All.Resolve(0));
    }

    [Fact]
    public void ReturnedCollection_CannotBeUsedToMutateModel()
    {
        var range = UtilityPageRange.Explicit(0, 2);
        var resolved = range.Resolve(3);
        var list = Assert.IsAssignableFrom<IList<int>>(resolved);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = 99);
        Assert.Equal(new[] { 0, 1, 2 }, range.Resolve(3));
    }
}
