using System.Reflection;
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

    [Fact]
    public void GoToPageNumber_UsesOneBasedPageNumber()
    {
        var method = typeof(PageNavigationState).GetMethod(
            "GoToPageNumber",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null);
        Assert.NotNull(method);

        var state = new PageNavigationState(5, 1);
        var result = Assert.IsType<PageNavigationState>(method!.Invoke(state, new object[] { 4 }));

        Assert.Equal(3, result.CurrentPageIndex);
        Assert.Equal(4, result.CurrentPageNumber);
        Assert.Equal(5, result.PageCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void GoToPageNumber_RejectsOutOfRangePageNumber(int pageNumber)
    {
        var method = typeof(PageNavigationState).GetMethod(
            "GoToPageNumber",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null);
        Assert.NotNull(method);

        var state = new PageNavigationState(5);
        var ex = Assert.Throws<TargetInvocationException>(() => method!.Invoke(state, new object[] { pageNumber }));
        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
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
