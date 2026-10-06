using SGPdf.App.Navigation;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PageNavigationStateTests
{
    [Fact]
    public void ThreePageDocument_NavigatesWithinBounds()
    {
        var state = new PageNavigationState(3);

        Assert.Equal(3, state.PageCount);
        Assert.Equal(0, state.CurrentPageIndex);
        Assert.Equal(1, state.CurrentPageNumber);
        Assert.False(state.CanMovePrevious);
        Assert.True(state.CanMoveNext);

        state = state.Previous();
        Assert.Equal(0, state.CurrentPageIndex);

        state = state.Next();
        Assert.Equal(1, state.CurrentPageIndex);
        Assert.True(state.CanMovePrevious);
        Assert.True(state.CanMoveNext);

        state = state.Next();
        Assert.Equal(2, state.CurrentPageIndex);
        Assert.Equal(3, state.CurrentPageNumber);
        Assert.True(state.CanMovePrevious);
        Assert.False(state.CanMoveNext);

        state = state.Next();
        Assert.Equal(2, state.CurrentPageIndex);
    }

    [Fact]
    public void OnePageDocument_DisablesBothDirections()
    {
        var state = new PageNavigationState(1);

        Assert.Equal(1, state.CurrentPageNumber);
        Assert.False(state.CanMovePrevious);
        Assert.False(state.CanMoveNext);
        Assert.Equal(0, state.Previous().CurrentPageIndex);
        Assert.Equal(0, state.Next().CurrentPageIndex);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(3, -1)]
    [InlineData(3, 3)]
    public void InvalidConstructorArguments_AreRejected(int pageCount, int currentPageIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PageNavigationState(pageCount, currentPageIndex));
    }
}
