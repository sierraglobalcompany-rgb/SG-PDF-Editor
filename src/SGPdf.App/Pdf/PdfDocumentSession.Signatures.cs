using System.Runtime.InteropServices;
using System.Text;

namespace SGPdf.App.Pdf;

public sealed partial class PdfDocumentSession
{
    internal IReadOnlyList<PdfCryptographicSignatureInfo> GetCryptographicSignatures(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        PdfiumRuntime.NativeGate.Wait(cancellationToken);
        try
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            int count = PdfiumNative.FPDF_GetSignatureCount(_document);
            if (count < 0)
                throw new InvalidOperationException("PDFium no pudo consultar las firmas criptográficas del documento.");
            if (count == 0)
                return Array.Empty<PdfCryptographicSignatureInfo>();

            var signatures = new List<PdfCryptographicSignatureInfo>(count);
            for (int index = 0; index < count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IntPtr signature = PdfiumNative.FPDF_GetSignatureObject(_document, index);
                if (signature == IntPtr.Zero)
                    throw new InvalidOperationException($"PDFium no pudo leer la firma criptográfica {index + 1}.");

                signatures.Add(new PdfCryptographicSignatureInfo(
                    ReadSignatureContents(signature),
                    ReadSignatureByteRange(signature),
                    ReadSignatureSubFilter(signature)));
            }

            return signatures;
        }
        finally
        {
            PdfiumRuntime.NativeGate.Release();
        }
    }

    private static byte[] ReadSignatureContents(IntPtr signature)
    {
        uint byteCount = PdfiumNative.FPDFSignatureObj_GetContents(signature, IntPtr.Zero, 0);
        if (byteCount == 0 || byteCount > int.MaxValue)
            throw new InvalidOperationException("PDFium devolvió un Contents de firma inválido.");

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)byteCount));
        try
        {
            uint written = PdfiumNative.FPDFSignatureObj_GetContents(signature, buffer, byteCount);
            if (written != byteCount)
                throw new InvalidOperationException("PDFium no pudo copiar el Contents completo de la firma.");

            var contents = new byte[checked((int)written)];
            Marshal.Copy(buffer, contents, 0, contents.Length);
            return contents;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int[] ReadSignatureByteRange(IntPtr signature)
    {
        uint itemCount = PdfiumNative.FPDFSignatureObj_GetByteRange(signature, IntPtr.Zero, 0);
        if (itemCount == 0 || itemCount > int.MaxValue / sizeof(int))
            throw new InvalidOperationException("PDFium devolvió un ByteRange de firma inválido.");

        int byteCount = checked((int)itemCount * sizeof(int));
        IntPtr buffer = Marshal.AllocHGlobal(byteCount);
        try
        {
            uint written = PdfiumNative.FPDFSignatureObj_GetByteRange(signature, buffer, itemCount);
            if (written != itemCount)
                throw new InvalidOperationException("PDFium no pudo copiar el ByteRange completo de la firma.");

            var byteRange = new int[checked((int)written)];
            Marshal.Copy(buffer, byteRange, 0, byteRange.Length);
            return byteRange;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string ReadSignatureSubFilter(IntPtr signature)
    {
        uint byteCount = PdfiumNative.FPDFSignatureObj_GetSubFilter(signature, IntPtr.Zero, 0);
        if (byteCount <= 1 || byteCount > int.MaxValue)
            throw new InvalidOperationException("PDFium devolvió un SubFilter de firma inválido.");

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)byteCount));
        try
        {
            uint written = PdfiumNative.FPDFSignatureObj_GetSubFilter(signature, buffer, byteCount);
            if (written != byteCount)
                throw new InvalidOperationException("PDFium no pudo copiar el SubFilter completo de la firma.");

            var bytes = new byte[checked((int)written)];
            Marshal.Copy(buffer, bytes, 0, bytes.Length);
            if (bytes[^1] != 0 || bytes[..^1].Any(value => value > 0x7F))
                throw new InvalidOperationException("PDFium devolvió un SubFilter que no es ASCII terminado en NUL.");

            string subFilter = Encoding.ASCII.GetString(bytes, 0, bytes.Length - 1);
            if (string.IsNullOrWhiteSpace(subFilter))
                throw new InvalidOperationException("PDFium devolvió un SubFilter vacío.");

            return subFilter;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
