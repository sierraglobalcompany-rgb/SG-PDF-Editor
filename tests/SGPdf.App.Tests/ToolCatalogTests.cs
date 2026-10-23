using System.Collections;
using System.Reflection;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ToolCatalogTests
{
    private static readonly string[] ApprovedDocumentOrder =
    {
        "read", "edit", "comment", "sign", "organize",
        "forms", "ocr", "redact", "compare", "convert", "protect"
    };

    [Fact]
    public void DocumentCatalog_UsesApprovedOrder_AndAllElevenActionsAreExecutable()
    {
        var items = ReadCatalog("Document");

        Assert.Equal(ApprovedDocumentOrder, items.Select(item => item.Id));
        Assert.Equal(ApprovedDocumentOrder, items.Where(item => item.ActionId is not null).Select(item => item.Id));

        foreach (var item in items)
            Assert.Equal(item.Id, item.ActionId);

        Assert.Equal("Selecciona texto compatible desde Editar.", Assert.Single(items, item => item.Id == "redact").Description);
        Assert.Equal("Exporta PDF a PNG.", Assert.Single(items, item => item.Id == "convert").Description);
    }

    [Fact]
    public void Catalogs_HaveUniqueIdsAndCompleteDisplayMetadata()
    {
        foreach (var catalogName in new[] { "Home", "Document" })
        {
            var items = ReadCatalog(catalogName);
            Assert.NotEmpty(items);
            Assert.Equal(items.Count, items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());

            foreach (var item in items)
            {
                Assert.False(string.IsNullOrWhiteSpace(item.Id));
                Assert.False(string.IsNullOrWhiteSpace(item.Title));
                Assert.False(string.IsNullOrWhiteSpace(item.Category));
                Assert.False(string.IsNullOrWhiteSpace(item.Description));
                Assert.False(string.IsNullOrWhiteSpace(item.IconKey));
            }
        }
    }

    [Fact]
    public void HomeCatalog_IsCapabilityHonestForCurrentPhase()
    {
        var items = ReadCatalog("Home");

        var imageToPdf = Assert.Single(items, item => item.Id == "image-to-pdf");
        Assert.Null(imageToPdf.ActionId);
        Assert.Contains("Próximamente", imageToPdf.DisabledReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var pdfToPng = Assert.Single(items, item => item.Id == "pdf-to-png");
        Assert.Equal("PDF a PNG", pdfToPng.Title);
        Assert.DoesNotContain("JPG", pdfToPng.Title, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            new[]
            {
                "merge-pdf", "split-pdf", "organize-pages", "edit-pdf", "ocr", "redact", "protect", "sign",
                "compare-pdf", "pdf-to-png", "qr", "labels-zpl", "watermark", "page-numbering"
            },
            items.Where(item => item.ActionId is not null).Select(item => item.Id));

        Assert.Equal(
            new[]
            {
                "merge", "split", "organize", "edit", "ocr", "redact", "protect", "sign",
                "compare", "convert", "qr", "open-zpl", "watermark", "page-numbering"
            },
            items.Where(item => item.ActionId is not null).Select(item => item.ActionId));

    }

    private static IReadOnlyList<ToolView> ReadCatalog(string propertyName)
    {
        var assembly = typeof(MainWindow).Assembly;
        var catalogType = assembly.GetType("SGPdf.App.Shell.ToolCatalog", throwOnError: true)!;
        var property = catalogType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        var value = Assert.IsAssignableFrom<IEnumerable>(property.GetValue(null));

        return value.Cast<object>().Select(ReadTool).ToArray();
    }

    private static ToolView ReadTool(object value)
    {
        var type = value.GetType();
        string? GetString(string name) => (string?)type.GetProperty(name)?.GetValue(value);

        return new ToolView(
            GetString("Id") ?? string.Empty,
            GetString("Title") ?? string.Empty,
            GetString("Category") ?? string.Empty,
            GetString("Description") ?? string.Empty,
            GetString("IconKey") ?? string.Empty,
            GetString("ActionId"),
            (bool?)type.GetProperty("RequiresPdf")?.GetValue(value) ?? false,
            GetString("DisabledReason"));
    }

    private sealed record ToolView(
        string Id,
        string Title,
        string Category,
        string Description,
        string IconKey,
        string? ActionId,
        bool RequiresPdf,
        string? DisabledReason);
}
