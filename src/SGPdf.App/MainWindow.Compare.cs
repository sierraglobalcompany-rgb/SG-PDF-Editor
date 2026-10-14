using SGPdf.App.Features.Compare;

namespace SGPdf.App;

public partial class MainWindow
{
    private static ComparePresentation BuildComparePresentation(
        string leftDisplayName,
        string rightDisplayName,
        PdfCompareResult result)
    {
        bool samePageCount = result.LeftPageCount == result.RightPageCount;
        bool hasDifferences = !samePageCount || result.TextChanged || result.VisualChanged;

        return new ComparePresentation(
            leftDisplayName,
            rightDisplayName,
            result.LeftPageCount,
            result.RightPageCount,
            result.TextChanged ? "Cambios detectados" : "Sin cambios",
            "Cambio visual independiente del texto",
            result.VisualChanged ? "Cambios detectados" : "Sin cambios",
            samePageCount ? "Igual cantidad de páginas" : "Cantidad de páginas diferente",
            hasDifferences ? "Se encontraron diferencias" : "Sin diferencias detectadas");
    }
}

internal sealed record ComparePresentation(
    string LeftDisplayName,
    string RightDisplayName,
    int LeftPageCount,
    int RightPageCount,
    string TextStatus,
    string VisualLabel,
    string VisualStatus,
    string StructureStatus,
    string Conclusion);
