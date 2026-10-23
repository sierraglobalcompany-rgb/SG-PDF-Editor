namespace SGPdf.App.Shell;

internal static class ToolCatalog
{
    internal static IReadOnlyList<ToolDescriptor> Home { get; } =
    [
        new("merge-pdf", "Unir PDF", "PDF", "Combina documentos PDF en una sola salida.", "ShellIconMerge", "merge", true),
        new("split-pdf", "Dividir PDF", "PDF", "Separa páginas o rangos de un PDF.", "ShellIconSplit", "split", true),
        new("organize-pages", "Organizar páginas", "PDF", "Reordena, rota y administra páginas.", "ShellIconOrganize", "organize", true),
        new("edit-pdf", "Editar PDF", "PDF", "Edita contenido compatible del documento.", "ShellIconEdit", "edit", true),
        new("ocr", "OCR", "PDF", "Reconoce texto localmente en documentos escaneados.", "ShellIconOcr", "ocr", true),
        new("redact", "Censurar", "PDF", "Aplica censura segura sobre contenido compatible.", "ShellIconRedact", "redact", true),
        new("protect", "Proteger", "PDF", "Protege una copia del PDF con contraseña.", "ShellIconProtect", "protect", true),
        new("sign", "Firmar", "PDF", "Añade una firma visual al documento.", "ShellIconSign", "sign", true),
        new("compare-pdf", "Comparar PDF", "PDF", "Compara el documento activo con otro PDF.", "ShellIconCompare", "compare", true),
        new("pdf-to-png", "PDF a PNG", "Convertir", "Exporta páginas del PDF como imágenes PNG.", "ShellIconConvert", "convert", true),
        new("image-to-pdf", "Imagen a PDF", "Convertir", "Convierte imágenes en un documento PDF.", "ShellIconImageToPdf", null, false, "Próximamente"),
        new("qr", "QR", "Herramientas", "Genera un código QR como imagen PNG local.", "ShellIconQr", "qr", false),
        new("labels-zpl", "Etiquetas ZPL", "Etiquetas", "Abre y previsualiza archivos ZPL localmente.", "ShellIconZpl", "open-zpl", false),
        new("watermark", "Marca de agua", "PDF", "Añade una marca de agua de texto al PDF.", "ShellIconWatermark", "watermark", true),
        new("page-numbering", "Numerar páginas", "PDF", "Añade numeración o Bates al documento.", "ShellIconNumbering", "page-numbering", true)
    ];

    internal static IReadOnlyList<ToolDescriptor> Document { get; } =
    [
        new("read", "Leer", "Documento", "Navega y revisa el PDF.", "ShellIconRead", "read", true),
        new("edit", "Editar", "Documento", "Edita contenido compatible del PDF.", "ShellIconEdit", "edit", true),
        new("comment", "Comentar", "Documento", "Añade anotaciones y comentarios.", "ShellIconComment", "comment", true),
        new("sign", "Firmar", "Documento", "Coloca firmas visuales en el PDF.", "ShellIconSign", "sign", true),
        new("organize", "Organizar", "Documento", "Reordena y administra páginas.", "ShellIconOrganize", "organize", true),
        new("forms", "Formularios", "Documento", "Edita campos de formulario compatibles.", "ShellIconForms", "forms", true),
        new("ocr", "OCR", "Documento", "Reconoce texto localmente en páginas escaneadas.", "ShellIconOcr", "ocr", true),
        new("redact", "Censurar", "Documento", "Selecciona texto compatible desde Editar.", "ShellIconRedact", "redact", true),
        new("compare", "Comparar", "Documento", "Compara el PDF activo con otro documento.", "ShellIconCompare", "compare", true),
        new("convert", "Convertir", "Documento", "Exporta PDF a PNG.", "ShellIconConvert", "convert", true),
        new("protect", "Proteger", "Documento", "Protege una copia del PDF con contraseña.", "ShellIconProtect", "protect", true)
    ];
}
