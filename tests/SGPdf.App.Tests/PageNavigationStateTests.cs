using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PageNavigationStateTests
{
    private static Type NavigationType
    {
        get
        {
            var type = typeof(PdfDocumentSession).Assembly.GetType("SGPdf.App.Navigation.PageNavigationState");
            Assert.NotNull(type);
            return type!;
        }
    }

    [Fact]
    public void ThreePageDocument_NavigatesWithinBounds()
    {
        dynamic state = Activator.CreateInstance(NavigationType, 3, 0)!;

        Assert.Equal(3, (int)state.PageCount);
        Assert.Equal(0, (int)state.CurrentPageIndex);
        Assert.Equal(1, (int)state.CurrentPageNumber);
        Assert.False((bool)state.CanMovePrevious);
        Assert.True((bool)state.CanMoveNext);

        state = state.Previous();
        Assert.Equal(0, (int)state.CurrentPageIndex);

        state = state.Next();
        Assert.Equal(1, (int)state.CurrentPageIndex);
        Assert.True((bool)state.CanMovePrevious);
        Assert.True((bool)state.CanMoveNext);

        state = state.Next();
        Assert.Equal(2, (int)state.CurrentPageIndex);
        Assert.Equal(3, (int)state.CurrentPageNumber);
        Assert.True((bool)state.CanMovePrevious);
        Assert.False((bool)state.CanMoveNext);

        state = state.Next();
        Assert.Equal(2, (int)state.CurrentPageIndex);
    }

    [Fact]
    public void OnePageDocument_DisablesBothDirections()
    {
        dynamic state = Activator.CreateInstance(NavigationType, 1, 0)!;

        Assert.Equal(1, (int)state.CurrentPageNumber);
        Assert.False((bool)state.CanMovePrevious);
        Assert.False((bool)state.CanMoveNext);
        Assert.Equal(0, (int)state.Previous().CurrentPageIndex);
        Assert.Equal(0, (int)state.Next().CurrentPageIndex);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(3, -1)]
    [InlineData(3, 3)]
    public void InvalidConstructorArguments_AreRejected(int pageCount, int currentPageIndex)
    {
        var ex = Assert.Throws<TargetInvocationException>(() =>
            Activator.CreateInstance(NavigationType, pageCount, currentPageIndex));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }
}
