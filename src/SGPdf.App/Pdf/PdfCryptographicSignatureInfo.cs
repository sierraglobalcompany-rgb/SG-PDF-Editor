namespace SGPdf.App.Pdf;

internal sealed record PdfCryptographicSignatureInfo(
    byte[] Contents,
    int[] ByteRange,
    string SubFilter);
