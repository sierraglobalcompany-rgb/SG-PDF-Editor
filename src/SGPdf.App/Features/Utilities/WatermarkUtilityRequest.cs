using SGPdf.App.Features.Edit.Text;

namespace SGPdf.App.Features.Utilities;

internal sealed class WatermarkUtilityRequest
{
    internal WatermarkUtilityRequest(
        string text,
        double fontSize,
        double opacity,
        double rotationDegrees,
        double offsetXPoints,
        double offsetYPoints,
        int startPageNumber,
        int endPageNumber,
        bool useCurrentPage,
        bool background,
        string anchorName,
        byte red,
        byte green,
        byte blue)
    {
        Text = text;
        FontSize = fontSize;
        Opacity = opacity;
        RotationDegrees = rotationDegrees;
        OffsetXPoints = offsetXPoints;
        OffsetYPoints = offsetYPoints;
        StartPageNumber = startPageNumber;
        EndPageNumber = endPageNumber;
        UseCurrentPage = useCurrentPage;
        Background = background;
        AnchorName = anchorName;
        Red = red;
        Green = green;
        Blue = blue;
    }

    internal string Text { get; }
    internal double FontSize { get; }
    internal double Opacity { get; }
    internal double RotationDegrees { get; }
    internal double OffsetXPoints { get; }
    internal double OffsetYPoints { get; }
    internal int StartPageNumber { get; }
    internal int EndPageNumber { get; }
    internal bool UseCurrentPage { get; }
    internal bool Background { get; }
    internal string AnchorName { get; }
    internal byte Red { get; }
    internal byte Green { get; }
    internal byte Blue { get; }

    internal bool TryBuild(
        int pageCount,
        int currentPageIndex,
        out TextOverlaySettings? settings,
        out UtilityPageRange? range,
        out WatermarkAnchor anchor,
        out string? error)
    {
        settings = null;
        range = null;
        anchor = default;
        error = null;

        if (string.IsNullOrWhiteSpace(Text) ||
            !double.IsFinite(FontSize) || FontSize <= 0d ||
            !double.IsFinite(Opacity) || Opacity < 0d || Opacity > 1d ||
            !double.IsFinite(RotationDegrees) ||
            !double.IsFinite(OffsetXPoints) ||
            !double.IsFinite(OffsetYPoints) ||
            pageCount <= 0 ||
            currentPageIndex < 0 || currentPageIndex >= pageCount ||
            string.IsNullOrWhiteSpace(AnchorName) ||
            !Enum.TryParse(AnchorName, ignoreCase: false, out anchor) ||
            !Enum.IsDefined(anchor))
        {
            error = "Revisa los valores: el texto, tamaño, opacidad, posición y rango deben ser válidos.";
            return false;
        }

        if (UseCurrentPage)
        {
            range = UtilityPageRange.Current(currentPageIndex);
        }
        else
        {
            if (StartPageNumber < 1 || EndPageNumber < StartPageNumber || EndPageNumber > pageCount)
            {
                error = "Revisa los valores: el rango de páginas debe ser válido y estar dentro del documento.";
                return false;
            }

            range = UtilityPageRange.Explicit(StartPageNumber - 1, EndPageNumber - 1);
        }

        settings = new TextOverlaySettings(
            Text.Trim(),
            FontSize,
            Opacity,
            Background ? TextOverlayLayer.Background : TextOverlayLayer.Foreground,
            new PdfTextFillColor(Red, Green, Blue, 255u));
        return true;
    }
}
