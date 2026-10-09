namespace SGPdf.App.Features.Reader;

internal readonly record struct ReaderPageGeometry(
    int PageIndex,
    double PdfWidthPoints,
    double PdfHeightPoints,
    double DisplayWidth,
    double DisplayHeight,
    double Top,
    double Bottom,
    double ResolvedDpi);

internal readonly record struct ReaderRenderWindow(
    int FirstVisiblePageIndex,
    int LastVisiblePageIndex,
    int FirstRetainedPageIndex,
    int LastRetainedPageIndex,
    IReadOnlyList<int> RenderPriority);
