namespace SGPdf.App.Features.Utilities;

internal sealed record UtilityPageRange
{
    private enum RangeKind
    {
        All,
        Current,
        Explicit
    }

    private readonly RangeKind _kind;
    private readonly int _startPageIndex;
    private readonly int _endPageIndex;

    private UtilityPageRange(RangeKind kind, int startPageIndex, int endPageIndex)
    {
        _kind = kind;
        _startPageIndex = startPageIndex;
        _endPageIndex = endPageIndex;
    }

    internal static UtilityPageRange All { get; } = new(RangeKind.All, 0, 0);

    internal static UtilityPageRange Current(int pageIndex) =>
        new(RangeKind.Current, pageIndex, pageIndex);

    internal static UtilityPageRange Explicit(int startPageIndex, int endPageIndex) =>
        new(RangeKind.Explicit, startPageIndex, endPageIndex);

    internal IReadOnlyList<int> Resolve(int pageCount)
    {
        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount), "Page count must be greater than zero.");

        return _kind switch
        {
            RangeKind.All => CreateReadOnlyRange(0, pageCount),
            RangeKind.Current => ResolveCurrent(pageCount),
            RangeKind.Explicit => ResolveExplicit(pageCount),
            _ => throw new InvalidOperationException("Unknown page-range kind.")
        };
    }

    private IReadOnlyList<int> ResolveCurrent(int pageCount)
    {
        if (_startPageIndex < 0 || _startPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(_startPageIndex), "Current page is outside the document.");

        return CreateReadOnlyRange(_startPageIndex, 1);
    }

    private IReadOnlyList<int> ResolveExplicit(int pageCount)
    {
        if (_startPageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(_startPageIndex), "Range start cannot be negative.");
        if (_startPageIndex > _endPageIndex)
            throw new ArgumentException("Range start cannot be greater than range end.");
        if (_endPageIndex >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(_endPageIndex), "Range end is outside the document.");

        var count = checked(_endPageIndex - _startPageIndex + 1);
        return CreateReadOnlyRange(_startPageIndex, count);
    }

    private static IReadOnlyList<int> CreateReadOnlyRange(int start, int count) =>
        Array.AsReadOnly(Enumerable.Range(start, count).ToArray());
}
