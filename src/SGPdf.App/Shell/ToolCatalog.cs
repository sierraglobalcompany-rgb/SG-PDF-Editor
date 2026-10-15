namespace SGPdf.App.Shell;

internal static class ToolCatalog
{
    private const string F14Reason = "Se conectará al nuevo shell en F14.";
    private const string PdfReason = "Disponible con un PDF abierto; acceso desde Home se conectará en F14.";

    internal static IReadOnlyList<ToolDescriptor> Home { get; } =
    [
        new("merge-pdf", "Unir PDF", "PDF", "Combina documentos PDF en una sola salida.", "ShellIconMerge", null, true, PdfReason),
        new("split-pdf", "Dividir PDF", "PDF", "Separa páginas o rangos de un PDF.", "ShellIconSplit", null, true, PdfReason),
        new("organize-pages", "Organizar páginas", "PDF", "Reordena, rota y administra páginas.", "ShellIconOrganize", null, true, PdfReason),
        new("edit-pdf", "Editar PDF", "PDF", "Edita contenido compatible del documento.", "ShellIconEdit", null, true, PdfReason),
        new("ocr", "OCR", "PDF", "Reconoce texto localmente en documentos escaneados.", "ShellIconOcr", null, true, PdfReason),
        new("redact", "Censurar", "PDF", "Aplica censura segura sobre contenido compatible.", "ShellIconRedact", null, true, PdfReason),
        new("protect", "Proteger", "PDF", "Protege una copia del PDF con contraseña.", "ShellIconProtect", null, true, PdfReason),
        new("sign", "Firmar", "PDF", "Añade una firma visual al documento.", "ShellIconSign", null, true, PdfReason),
        new("compare-pdf", "Comparar PDF", "PDF", "Compara el documento activo con otro PDF.", "ShellIconCompare", null, true, PdfReason),
        new("pdf-to-png", "PDF a PNG", "Convertir", "Exporta páginas del PDF como imágenes PNG.", "ShellIconConvert", null, true, PdfReason),
        new("image-to-pdf", "Imagen a PDF", "Convertir", "Convierte imágenes en un documento PDF.", "ShellIconImageToPdf", null, false, "Próximamente"),
        new("qr", "QR", "Herramientas", "Genera un código QR como imagen PNG local.", "ShellIconQr", null, false, F14Reason),
        new("labels-zpl", "Etiquetas ZPL", "Etiquetas", "Abre y previsualiza archivos ZPL localmente.", "ShellIconZpl", "open-zpl", false),
        new("watermark", "Marca de agua", "PDF", "Añade una marca de agua de texto al PDF.", "ShellIconWatermark", null, true, PdfReason),
        new("page-numbering", "Numerar páginas", "PDF", "Añade numeración o Bates al documento.", "ShellIconNumbering", null, true, PdfReason)
    ];

    internal static IReadOnlyList<ToolDescriptor> Document { get; } =
    [
        new("read", "Leer", "Documento", "Navega y revisa el PDF.", "ShellIconRead", "read", true),
        new("edit", "Editar", "Documento", "Edita contenido compatible del PDF.", "ShellIconEdit", "edit", true),
        new("comment", "Comentar", "Documento", "Añade anotaciones y comentarios.", "ShellIconComment", "comment", true),
        new("sign", "Firmar", "Documento", "Coloca firmas visuales en el PDF.", "ShellIconSign", "sign", true),
        new("organize", "Organizar", "Documento", "Reordena y administra páginas.", "ShellIconOrganize", "organize", true),
        new("forms", "Formularios", "Documento", "Edita campos de formulario compatibles.", "ShellIconForms", null, true, F14Reason),
        new("ocr", "OCR", "Documento", "Reconoce texto localmente en páginas escaneadas.", "ShellIconOcr", null, true, F14Reason),
        new("redact", "Censurar", "Documento", "Censura contenido compatible de forma segura.", "ShellIconRedact", null, true, F14Reason),
        new("compare", "Comparar", "Documento", "Compara el PDF activo con otro documento.", "ShellIconCompare", null, true, F14Reason),
        new("convert", "Convertir", "Documento", "Exporta el PDF con las conversiones locales disponibles.", "ShellIconConvert", null, true, F14Reason),
        new("protect", "Proteger", "Documento", "Protege una copia del PDF con contraseña.", "ShellIconProtect", null, true, F14Reason)
    ];
}
