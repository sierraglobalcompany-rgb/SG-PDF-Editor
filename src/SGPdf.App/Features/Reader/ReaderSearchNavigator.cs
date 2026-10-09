using SGPdf.App.Pdf;

namespace SGPdf.App.Features.Reader;

internal enum ReaderSearchDirection
{
    Forward,
    Backward
}

internal sealed record ReaderSearchCursor(
    string Query,
    int PageIndex,
    int MatchIndexWithinPage);

internal sealed record ReaderSearchResult(
    PdfTextMatch Match,
    ReaderSearchCursor Cursor);

internal static class ReaderSearchNavigator
{
    internal static ReaderSearchResult? Find(
        string query,
        int startPageIndex,
        ReaderSearchCursor? current,
        ReaderSearchDirection direction,
        int pageCount,
        Func<int, string, CancellationToken, IReadOnlyList<PdfTextMatch>> findOnPage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(findOnPage);
        if (pageCount <= 0)
            return null;
        if (startPageIndex < 0 || startPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(startPageIndex));
        if (query.Length == 0)
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var sameQuery = current is not null && string.Equals(current.Query, query, StringComparison.Ordinal);
        var firstPage = sameQuery ? current!.PageIndex : startPageIndex;
        if (firstPage < 0 || firstPage >= pageCount)
            firstPage = startPageIndex;

        for (var visited = 0; visited < pageCount; visited++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageIndex = direction == ReaderSearchDirection.Forward
                ? (firstPage + visited) % pageCount
                : (firstPage - visited + pageCount) % pageCount;
            var matches = findOnPage(pageIndex, query, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (matches.Count == 0)
                continue;

            int matchIndex;
            if (visited == 0 && sameQuery && current!.PageIndex == pageIndex)
            {
                matchIndex = direction == ReaderSearchDirection.Forward
                    ? current.MatchIndexWithinPage + 1
                    : current.MatchIndexWithinPage - 1;

                if (matchIndex < 0 || matchIndex >= matches.Count)
                    continue;
            }
            else
            {
                matchIndex = direction == ReaderSearchDirection.Forward
                    ? 0
                    : matches.Count - 1;
            }

            return new ReaderSearchResult(
                matches[matchIndex],
                new ReaderSearchCursor(query, pageIndex, matchIndex));
        }

        return null;
    }
}
