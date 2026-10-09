namespace SGPdf.App.Features.Reader;

internal sealed class ReaderBookmarkTraversalGuard
{
    internal const int MaxNodes = 10_000;
    internal const int MaxDepth = 128;

    private readonly HashSet<nint> _visited = new();
    private int _acceptedNodes;

    internal bool TryEnter(nint nativeHandle, int depth)
    {
        if (nativeHandle == 0 || depth < 0 || depth > MaxDepth || _acceptedNodes >= MaxNodes)
            return false;
        if (!_visited.Add(nativeHandle))
            return false;

        _acceptedNodes++;
        return true;
    }
}
