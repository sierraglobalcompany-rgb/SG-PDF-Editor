using SGPdf.App.Features.Reader;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderTextSelectionTests
{
    [Fact]
    public void Normalize_ForwardAndBackwardDragReturnSameRange()
    {
        var forward = ReaderTextSelectionRange.Normalize(4, 9);
        var backward = ReaderTextSelectionRange.Normalize(9, 4);

        Assert.Equal((4, 6), forward);
        Assert.Equal(forward, backward);
    }

    [Fact]
    public void Normalize_MissingHitReturnsNull()
    {
        Assert.Null(ReaderTextSelectionRange.Normalize(-1, 5));
        Assert.Null(ReaderTextSelectionRange.Normalize(5, -1));
    }

    [Fact]
    public void Normalize_SameCharacterReturnsOneCharacterRange()
    {
        Assert.Equal((7, 1), ReaderTextSelectionRange.Normalize(7, 7));
    }
}
