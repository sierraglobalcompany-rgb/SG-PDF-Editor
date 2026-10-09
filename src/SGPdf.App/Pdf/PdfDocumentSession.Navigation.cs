using System.Runtime.InteropServices;
using System.Text;
using SGPdf.App.Features.Reader;

namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    private const uint MaxNavigationStringBytes = 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public IReadOnlyList<PdfBookmarkNode> GetBookmarks(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        lock (PdfiumRuntime.NativeGate)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            var first = PdfiumNative.FPDFBookmark_GetFirstChild(_document, IntPtr.Zero);
            if (first == IntPtr.Zero)
                return Array.Empty<PdfBookmarkNode>();

            var guard = new ReaderBookmarkTraversalGuard();
            return ReadBookmarkSiblings(first, 0, guard, cancellationToken);
        }
    }

    public IReadOnlyList<PdfPageLink> GetPageLinks(int pageIndex, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidatePageIndex(pageIndex);
        cancellationToken.ThrowIfCancellationRequested();

        lock (PdfiumRuntime.NativeGate)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"PDFium no pudo cargar la página {pageIndex + 1} para leer enlaces.");

            try
            {
                var links = new List<PdfPageLink>();
                var startPosition = 0;
                while (PdfiumNative.FPDFLink_Enumerate(page, ref startPosition, out var nativeLink) != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (nativeLink == IntPtr.Zero || PdfiumNative.FPDFLink_GetAnnotRect(nativeLink, out var nativeRect) == 0)
                        continue;

                    var rect = NormalizeRect(nativeRect);
                    if (rect is null)
                        continue;

                    links.Add(ReadPageLink(pageIndex, rect, nativeLink));
                }

                return links;
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
    }

    private IReadOnlyList<PdfBookmarkNode> ReadBookmarkSiblings(
        IntPtr first,
        int depth,
        ReaderBookmarkTraversalGuard guard,
        CancellationToken cancellationToken)
    {
        var nodes = new List<PdfBookmarkNode>();
        var current = first;
        while (current != IntPtr.Zero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!guard.TryEnter(current, depth))
                break;

            var title = ReadBookmarkTitle(current);
            var destinationPageIndex = ReadBookmarkDestinationPageIndex(current);
            var childHandle = PdfiumNative.FPDFBookmark_GetFirstChild(_document, current);
            var children = childHandle == IntPtr.Zero
                ? Array.Empty<PdfBookmarkNode>()
                : ReadBookmarkSiblings(childHandle, depth + 1, guard, cancellationToken);

            nodes.Add(new PdfBookmarkNode(title, destinationPageIndex, children));
            current = PdfiumNative.FPDFBookmark_GetNextSibling(_document, current);
        }

        return nodes;
    }

    private string ReadBookmarkTitle(IntPtr bookmark)
    {
        var byteCount = PdfiumNative.FPDFBookmark_GetTitle(bookmark, IntPtr.Zero, 0);
        if (byteCount == 0 || byteCount > MaxNavigationStringBytes)
            return string.Empty;

        var buffer = Marshal.AllocHGlobal(checked((int)byteCount));
        try
        {
            var written = PdfiumNative.FPDFBookmark_GetTitle(bookmark, buffer, byteCount);
            if (written == 0 || written > byteCount)
                return string.Empty;

            var bytes = new byte[written];
            Marshal.Copy(buffer, bytes, 0, bytes.Length);
            var text = Encoding.Unicode.GetString(bytes);
            var terminator = text.IndexOf('\0');
            return terminator >= 0 ? text[..terminator] : text;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private int? ReadBookmarkDestinationPageIndex(IntPtr bookmark)
    {
        var action = PdfiumNative.FPDFBookmark_GetAction(bookmark);
        IntPtr destination;
        if (action == IntPtr.Zero)
        {
            destination = PdfiumNative.FPDFBookmark_GetDest(_document, bookmark);
        }
        else
        {
            if (PdfiumNative.FPDFAction_GetType(action) != PdfiumNative.PDFACTION_GOTO)
                return null;
            destination = PdfiumNative.FPDFAction_GetDest(_document, action);
        }

        return ReadDestinationPageIndex(destination);
    }

    private PdfPageLink ReadPageLink(int pageIndex, PdfTextRect rect, IntPtr nativeLink)
    {
        var directDestination = PdfiumNative.FPDFLink_GetDest(_document, nativeLink);
        var destinationPageIndex = ReadDestinationPageIndex(directDestination);
        if (destinationPageIndex is not null)
            return new PdfPageLink(pageIndex, rect, PdfLinkActionKind.InternalGoto, destinationPageIndex, null);

        var action = PdfiumNative.FPDFLink_GetAction(nativeLink);
        if (action == IntPtr.Zero)
            return new PdfPageLink(pageIndex, rect, PdfLinkActionKind.None, null, null);

        return PdfiumNative.FPDFAction_GetType(action) switch
        {
            PdfiumNative.PDFACTION_GOTO => new PdfPageLink(
                pageIndex,
                rect,
                PdfLinkActionKind.InternalGoto,
                ReadDestinationPageIndex(PdfiumNative.FPDFAction_GetDest(_document, action)),
                null),
            PdfiumNative.PDFACTION_URI => ReadUriPageLink(pageIndex, rect, action),
            _ => new PdfPageLink(pageIndex, rect, PdfLinkActionKind.Unsupported, null, null)
        };
    }

    private PdfPageLink ReadUriPageLink(int pageIndex, PdfTextRect rect, IntPtr action)
    {
        var uri = ReadActionUri(action);
        return uri is null
            ? new PdfPageLink(pageIndex, rect, PdfLinkActionKind.Unsupported, null, null)
            : new PdfPageLink(pageIndex, rect, PdfLinkActionKind.Uri, null, uri);
    }

    private string? ReadActionUri(IntPtr action)
    {
        var byteCount = PdfiumNative.FPDFAction_GetURIPath(_document, action, IntPtr.Zero, 0);
        if (byteCount == 0 || byteCount > MaxNavigationStringBytes)
            return null;

        var buffer = Marshal.AllocHGlobal(checked((int)byteCount));
        try
        {
            var written = PdfiumNative.FPDFAction_GetURIPath(_document, action, buffer, byteCount);
            if (written == 0 || written > byteCount)
                return null;

            var bytes = new byte[written];
            Marshal.Copy(buffer, bytes, 0, bytes.Length);
            var length = bytes.Length;
            while (length > 0 && bytes[length - 1] == 0)
                length--;
            if (length == 0)
                return null;

            try
            {
                return StrictUtf8.GetString(bytes, 0, length);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private int? ReadDestinationPageIndex(IntPtr destination)
    {
        if (destination == IntPtr.Zero)
            return null;

        var pageIndex = PdfiumNative.FPDFDest_GetDestPageIndex(_document, destination);
        return pageIndex >= 0 && pageIndex < PageCount ? pageIndex : null;
    }

    private static PdfTextRect? NormalizeRect(PdfiumNative.RectF rect)
    {
        var left = Math.Min(rect.Left, rect.Right);
        var right = Math.Max(rect.Left, rect.Right);
        var bottom = Math.Min(rect.Bottom, rect.Top);
        var top = Math.Max(rect.Bottom, rect.Top);
        if (!double.IsFinite(left) || !double.IsFinite(right) || !double.IsFinite(bottom) || !double.IsFinite(top) ||
            right <= left || top <= bottom)
        {
            return null;
        }

        return new PdfTextRect(left, bottom, right, top);
    }
}
