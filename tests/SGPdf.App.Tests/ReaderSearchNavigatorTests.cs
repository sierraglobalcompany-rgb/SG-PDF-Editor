using SGPdf.App.Features.Reader;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ReaderSearchNavigatorTests
{
    [Fact]
    public void Forward_CurrentPage_ReturnsFirstMatch()
    {
        var result = ReaderSearchNavigator.Find(
            "needle", 1, null, ReaderSearchDirection.Forward, 3,
            Finder((1, Matches(1, 4, 12))), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Match.PageIndex);
        Assert.Equal(0, result.Cursor.MatchIndexWithinPage);
    }

    [Fact]
    public void Forward_CurrentPage_ReturnsNextMatchBeforeLaterPage()
    {
        var current = new ReaderSearchCursor("needle", 1, 0);
        var result = ReaderSearchNavigator.Find(
            "needle", 1, current, ReaderSearchDirection.Forward, 3,
            Finder((1, Matches(1, 4, 12)), (2, Matches(2, 2))), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Match.PageIndex);
        Assert.Equal(12, result.Match.StartIndex);
        Assert.Equal(1, result.Cursor.MatchIndexWithinPage);
    }

    [Fact]
    public void Forward_LaterPage_IsVisitedAfterCurrentPageExhausted()
    {
        var current = new ReaderSearchCursor("needle", 1, 0);
        var result = ReaderSearchNavigator.Find(
            "needle", 1, current, ReaderSearchDirection.Forward, 4,
            Finder((1, Matches(1, 4)), (3, Matches(3, 9))), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Match.PageIndex);
        Assert.Equal(0, result.Cursor.MatchIndexWithinPage);
    }

    [Fact]
    public void Forward_WrapsAtMostOnce()
    {
        var visits = new List<int>();
        var current = new ReaderSearchCursor("needle", 2, 0);
        var result = ReaderSearchNavigator.Find(
            "needle", 2, current, ReaderSearchDirection.Forward, 3,
            (page, _, _) =>
            {
                visits.Add(page);
                return page switch
                {
                    2 => Matches(2, 8),
                    0 => Matches(0, 3),
                    _ => Array.Empty<PdfTextMatch>()
                };
            }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result!.Match.PageIndex);
        Assert.Equal(new[] { 2, 0 }, visits);
        Assert.Equal(visits.Count, visits.Distinct().Count());
    }

    [Fact]
    public void Backward_ReturnsPreviousThenEarlierPageAndWraps()
    {
        var current = new ReaderSearchCursor("needle", 1, 1);
        var first = ReaderSearchNavigator.Find(
            "needle", 1, current, ReaderSearchDirection.Backward, 3,
            Finder((1, Matches(1, 4, 12)), (0, Matches(0, 7)), (2, Matches(2, 5))), CancellationToken.None);

        Assert.NotNull(first);
        Assert.Equal(1, first!.Match.PageIndex);
        Assert.Equal(4, first.Match.StartIndex);
        Assert.Equal(0, first.Cursor.MatchIndexWithinPage);

        var second = ReaderSearchNavigator.Find(
            "needle", 1, first.Cursor, ReaderSearchDirection.Backward, 3,
            Finder((1, Matches(1, 4, 12)), (0, Matches(0, 7)), (2, Matches(2, 5))), CancellationToken.None);

        Assert.NotNull(second);
        Assert.Equal(0, second!.Match.PageIndex);

        var wrapped = ReaderSearchNavigator.Find(
            "needle", 0, second.Cursor, ReaderSearchDirection.Backward, 3,
            Finder((1, Matches(1, 4, 12)), (0, Matches(0, 7)), (2, Matches(2, 5))), CancellationToken.None);

        Assert.NotNull(wrapped);
        Assert.Equal(2, wrapped!.Match.PageIndex);
    }

    [Fact]
    public void NoResult_VisitsEachPageAtMostOnce()
    {
        var visits = new List<int>();

        var result = ReaderSearchNavigator.Find(
            "missing", 2, null, ReaderSearchDirection.Forward, 5,
            (page, _, _) =>
            {
                visits.Add(page);
                return Array.Empty<PdfTextMatch>();
            }, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(5, visits.Count);
        Assert.Equal(5, visits.Distinct().Count());
        Assert.Equal(new[] { 2, 3, 4, 0, 1 }, visits);
    }

    [Fact]
    public void QueryChange_ResetsCursorToStartPage()
    {
        var current = new ReaderSearchCursor("old", 0, 5);

        var result = ReaderSearchNavigator.Find(
            "new", 2, current, ReaderSearchDirection.Forward, 4,
            Finder((0, Matches(0, 1)), (2, Matches(2, 20, 30))), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Match.PageIndex);
        Assert.Equal(20, result.Match.StartIndex);
        Assert.Equal(0, result.Cursor.MatchIndexWithinPage);
        Assert.Equal("new", result.Cursor.Query);
    }

    [Fact]
    public void Cancellation_StopsBetweenPages()
    {
        using var cancellation = new CancellationTokenSource();
        var visits = new List<int>();

        Assert.Throws<OperationCanceledException>(() => ReaderSearchNavigator.Find(
            "needle", 0, null, ReaderSearchDirection.Forward, 4,
            (page, _, _) =>
            {
                visits.Add(page);
                if (page == 0)
                    cancellation.Cancel();
                return Array.Empty<PdfTextMatch>();
            }, cancellation.Token));

        Assert.Equal(new[] { 0 }, visits);
    }

    private static Func<int, string, CancellationToken, IReadOnlyList<PdfTextMatch>> Finder(
        params (int Page, IReadOnlyList<PdfTextMatch> Matches)[] pages)
    {
        var map = pages.ToDictionary(item => item.Page, item => item.Matches);
        return (page, _, _) => map.TryGetValue(page, out var matches)
            ? matches
            : Array.Empty<PdfTextMatch>();
    }

    private static IReadOnlyList<PdfTextMatch> Matches(int pageIndex, params int[] starts)
        => starts.Select(start => new PdfTextMatch(
            pageIndex,
            start,
            6,
            new[] { new PdfTextRect(start, 10d, start + 5d, 20d) }))
            .ToArray();
}
