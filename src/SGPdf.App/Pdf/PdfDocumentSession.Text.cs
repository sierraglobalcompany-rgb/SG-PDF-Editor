namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    public IReadOnlyList<PdfTextMatch> FindTextOnPage(
        int pageIndex,
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);
        if (query.Length == 0)
            return Array.Empty<PdfTextMatch>();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para buscar texto.");

            try
            {
                var textPage = PdfiumNative.FPDFText_LoadPage(page);
                if (textPage == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la capa de texto de la página {pageIndex + 1}.");

                try
                {
                    const uint flags = 0u;
                    var search = PdfiumNative.FPDFText_FindStart(textPage, query, flags, 0);
                    if (search == IntPtr.Zero)
                        return Array.Empty<PdfTextMatch>();

                    try
                    {
                        var matches = new List<PdfTextMatch>();
                        while (PdfiumNative.FPDFText_FindNext(search) != 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var startIndex = PdfiumNative.FPDFText_GetSchResultIndex(search);
                            var characterCount = PdfiumNative.FPDFText_GetSchCount(search);
                            if (startIndex < 0 || characterCount <= 0)
                                continue;

                            var rects = GetTextRangeRectsCore(textPage, startIndex, characterCount);
                            matches.Add(new PdfTextMatch(pageIndex, startIndex, characterCount, rects));
                        }

                        return matches;
                    }
                    finally
                    {
                        PdfiumNative.FPDFText_FindClose(search);
                    }
                }
                finally
                {
                    PdfiumNative.FPDFText_ClosePage(textPage);
                }
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    public int GetCharacterIndexAtPoint(
        int pageIndex,
        double pdfX,
        double pdfY,
        double xTolerance,
        double yTolerance,
        CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(pdfX) || !double.IsFinite(pdfY))
            throw new ArgumentOutOfRangeException(nameof(pdfX));
        if (!double.IsFinite(xTolerance) || xTolerance < 0d ||
            !double.IsFinite(yTolerance) || yTolerance < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(xTolerance));
        }

        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para consultar texto.");

            try
            {
                var textPage = PdfiumNative.FPDFText_LoadPage(page);
                if (textPage == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la capa de texto de la página {pageIndex + 1}.");

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return PdfiumNative.FPDFText_GetCharIndexAtPos(
                        textPage, pdfX, pdfY, xTolerance, yTolerance);
                }
                finally
                {
                    PdfiumNative.FPDFText_ClosePage(textPage);
                }
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    public string GetTextRange(
        int pageIndex,
        int startIndex,
        int characterCount,
        CancellationToken cancellationToken = default)
    {
        ValidateTextRange(startIndex, characterCount);
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);
        if (characterCount == 0)
            return string.Empty;

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para extraer texto.");

            try
            {
                var textPage = PdfiumNative.FPDFText_LoadPage(page);
                if (textPage == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la capa de texto de la página {pageIndex + 1}.");

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var buffer = new ushort[checked(characterCount + 1)];
                    var written = PdfiumNative.FPDFText_GetText(textPage, startIndex, characterCount, buffer);
                    if (written <= 0)
                        return string.Empty;

                    var length = Math.Min(written, buffer.Length);
                    while (length > 0 && buffer[length - 1] == 0)
                        length--;

                    var chars = new char[length];
                    for (var index = 0; index < length; index++)
                        chars[index] = (char)buffer[index];
                    cancellationToken.ThrowIfCancellationRequested();
                    return new string(chars);
                }
                finally
                {
                    PdfiumNative.FPDFText_ClosePage(textPage);
                }
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    public IReadOnlyList<PdfTextRect> GetTextRangeRects(
        int pageIndex,
        int startIndex,
        int characterCount,
        CancellationToken cancellationToken = default)
    {
        ValidateTextRange(startIndex, characterCount);
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);
        if (characterCount == 0)
            return Array.Empty<PdfTextRect>();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"No se pudo cargar la página {pageIndex + 1} para consultar rectángulos de texto.");

            try
            {
                var textPage = PdfiumNative.FPDFText_LoadPage(page);
                if (textPage == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo cargar la capa de texto de la página {pageIndex + 1}.");

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return GetTextRangeRectsCore(textPage, startIndex, characterCount);
                }
                finally
                {
                    PdfiumNative.FPDFText_ClosePage(textPage);
                }
            }
            finally
            {
                PdfiumNative.FPDF_ClosePage(page);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static IReadOnlyList<PdfTextRect> GetTextRangeRectsCore(
        IntPtr textPage,
        int startIndex,
        int characterCount)
    {
        var count = PdfiumNative.FPDFText_CountRects(textPage, startIndex, characterCount);
        if (count <= 0)
            return Array.Empty<PdfTextRect>();

        var rects = new List<PdfTextRect>(count);
        for (var index = 0; index < count; index++)
        {
            if (PdfiumNative.FPDFText_GetRect(
                    textPage, index,
                    out var left, out var top, out var right, out var bottom) == 0)
            {
                continue;
            }

            if (!double.IsFinite(left) || !double.IsFinite(bottom) ||
                !double.IsFinite(right) || !double.IsFinite(top) ||
                right <= left || top <= bottom)
            {
                continue;
            }

            rects.Add(new PdfTextRect(left, bottom, right, top));
        }

        return rects;
    }

    private static void ValidateTextRange(int startIndex, int characterCount)
    {
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (characterCount < 0)
            throw new ArgumentOutOfRangeException(nameof(characterCount));
    }
}
