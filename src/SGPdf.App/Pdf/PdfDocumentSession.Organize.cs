using System.Runtime.InteropServices;

namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    private static readonly string[] OrganizeMetadataTags =
    {
        "Title",
        "Author",
        "Subject",
        "Keywords",
        "Creator",
        "Producer",
        "CreationDate",
        "ModDate"
    };

    public int GetPageRotation(int pageIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageIndex(pageIndex, cancellationToken);

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var page = PdfiumNative.FPDF_LoadPage(_document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"PDFium no pudo cargar la página {pageIndex + 1} para consultar su rotación.");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rotation = PdfiumNative.FPDFPage_GetRotation(page);
                if (rotation is < 0 or > 3)
                    throw new InvalidOperationException($"PDFium devolvió una rotación inválida para la página {pageIndex + 1}: {rotation}.");
                return rotation;
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

    internal int GetOrganizeFormType(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return PdfiumNative.FPDF_GetFormType(_document);
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal bool HasOrganizeNamedDestinations(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return PdfiumNative.FPDF_CountNamedDests(_document) > 0u;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal bool IsOrganizeTagged(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return PdfiumNative.FPDFCatalog_IsTagged(_document) != 0;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal bool HasOrganizePageLabels(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var pageCount = PdfiumNative.FPDF_GetPageCount(_document);
            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (PdfiumNative.FPDF_GetPageLabel(_document, pageIndex, IntPtr.Zero, 0) > 2u)
                    return true;
            }

            return false;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal bool HasOrganizeAttachments(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return PdfiumNative.FPDFDoc_GetAttachmentCount(_document) > 0;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal string GetOrganizeMetadataText(
        string tag,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("La etiqueta de metadatos es obligatoria.", nameof(tag));

        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var requiredBytes = PdfiumNative.FPDF_GetMetaText(_document, tag, IntPtr.Zero, 0);
            if (requiredBytes <= 2u)
                return string.Empty;

            var buffer = Marshal.AllocHGlobal(checked((int)requiredBytes));
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var writtenBytes = PdfiumNative.FPDF_GetMetaText(_document, tag, buffer, requiredBytes);
                if (writtenBytes <= 2u)
                    return string.Empty;

                return Marshal.PtrToStringUni(buffer) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    internal bool HasOrganizeMetadata(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            foreach (var tag in OrganizeMetadataTags)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (PdfiumNative.FPDF_GetMetaText(_document, tag, IntPtr.Zero, 0) > 2u)
                    return true;
            }

            return false;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }
}
