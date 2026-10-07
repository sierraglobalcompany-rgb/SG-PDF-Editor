namespace SGPdf.App.Navigation;

public sealed class PageNavigationState
{
    public PageNavigationState(int pageCount, int currentPageIndex = 0)
    {
        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount), "El documento debe tener al menos una página.");

        if (currentPageIndex < 0 || currentPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(currentPageIndex));

        PageCount = pageCount;
        CurrentPageIndex = currentPageIndex;
    }

    public int PageCount { get; }

    public int CurrentPageIndex { get; }

    public int CurrentPageNumber => CurrentPageIndex + 1;

    public bool CanMovePrevious => CurrentPageIndex > 0;

    public bool CanMoveNext => CurrentPageIndex < PageCount - 1;

    public PageNavigationState Previous()
    {
        return CanMovePrevious
            ? new PageNavigationState(PageCount, CurrentPageIndex - 1)
            : this;
    }

    public PageNavigationState Next()
    {
        return CanMoveNext
            ? new PageNavigationState(PageCount, CurrentPageIndex + 1)
            : this;
    }

    public PageNavigationState GoToPageNumber(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        var pageIndex = pageNumber - 1;
        return pageIndex == CurrentPageIndex
            ? this
            : new PageNavigationState(PageCount, pageIndex);
    }
}
