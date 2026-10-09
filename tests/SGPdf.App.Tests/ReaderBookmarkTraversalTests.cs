using SGPdf.App.Features.Reader;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderBookmarkTraversalTests
{
    [Fact]
    public void BookmarkTraversal_CycleDepthAndNodeLimitsAreBounded()
    {
        var guard = new ReaderBookmarkTraversalGuard();

        Assert.True(guard.TryEnter((nint)1, 0));
        Assert.False(guard.TryEnter((nint)1, 0));
        Assert.False(guard.TryEnter((nint)2, ReaderBookmarkTraversalGuard.MaxDepth + 1));

        for (var index = 2; index <= ReaderBookmarkTraversalGuard.MaxNodes; index++)
            Assert.True(guard.TryEnter((nint)index, 1));

        Assert.False(guard.TryEnter((nint)(ReaderBookmarkTraversalGuard.MaxNodes + 1), 1));
    }

    [Fact]
    public void RepeatedHandle_IsRejectedWithoutConsumingAnotherNodeSlot()
    {
        var guard = new ReaderBookmarkTraversalGuard();

        Assert.True(guard.TryEnter((nint)10, 0));
        Assert.False(guard.TryEnter((nint)10, 1));
        Assert.True(guard.TryEnter((nint)11, 1));
    }

    [Fact]
    public void Depth129_IsRejected()
    {
        var guard = new ReaderBookmarkTraversalGuard();

        Assert.True(guard.TryEnter((nint)1, ReaderBookmarkTraversalGuard.MaxDepth));
        Assert.False(guard.TryEnter((nint)2, ReaderBookmarkTraversalGuard.MaxDepth + 1));
    }

    [Fact]
    public void Node10001_IsRejected()
    {
        var guard = new ReaderBookmarkTraversalGuard();

        for (var index = 1; index <= ReaderBookmarkTraversalGuard.MaxNodes; index++)
            Assert.True(guard.TryEnter((nint)index, 0));

        Assert.False(guard.TryEnter((nint)(ReaderBookmarkTraversalGuard.MaxNodes + 1), 0));
    }

    [Fact]
    public void RejectingLaterCycle_DoesNotInvalidatePreviouslyAcceptedHandles()
    {
        var guard = new ReaderBookmarkTraversalGuard();

        Assert.True(guard.TryEnter((nint)100, 0));
        Assert.True(guard.TryEnter((nint)101, 1));
        Assert.False(guard.TryEnter((nint)100, 2));
        Assert.True(guard.TryEnter((nint)102, 2));
    }
}
