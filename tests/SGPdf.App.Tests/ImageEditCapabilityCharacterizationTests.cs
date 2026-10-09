using System.Runtime.InteropServices;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditCapabilityCharacterizationTests
{
    [Fact]
    public void ConditionalExports_CharacterizationReport()
    {
        var library = PdfiumImageEditApiAvailabilityTests.LoadPinnedPdfium();
        try
        {
            var report = PdfiumImageEditApiAvailabilityTests.ConditionalExports
                .Select(name => $"{name}={(NativeLibrary.TryGetExport(library, name, out _) ? "available" : "missing")}")
                .ToArray();

            Assert.Fail("F6 conditional export characterization: " + string.Join("; ", report));
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }
}
