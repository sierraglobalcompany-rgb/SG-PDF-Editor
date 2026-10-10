using System.Reflection;
using PdfSharp.Pdf;
using SGPdf.App.Features.Edit.Images;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class ImageEditPerformanceOwnershipTests
{
    [Fact]
    public void EnterEditMode_MultiPageDocument_QueriesOnlyActivePageOnce()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sgpdf-f6-active-page-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "two-pages.pdf");
            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.AddPage();
                document.Save(path);
            }

            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, path));
                OrganizeWindowTestHost.SetField(
                    window,
                    "_getCryptographicSignatureCount",
                    (Func<PdfDocumentSession, int>)(_ => 0));

                var queriedPages = new List<int>();
                OrganizeWindowTestHost.SetField(
                    window,
                    "_getImageObjects",
                    (Func<PdfDocumentSession, int, CancellationToken, IReadOnlyList<PdfImageObjectInfo>>)
                    ((_, pageIndex, _) =>
                    {
                        queriedPages.Add(pageIndex);
                        return Array.Empty<PdfImageObjectInfo>();
                    }));

                Assert.True(await OrganizeWindowTestHost.InvokeTask<bool>(window, "TryEnterEditModeAsync"));

                Assert.Equal(new[] { 0 }, queriedPages);
            }
            finally
            {
                ImageEditCommandTestHost.CloseClean(window);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        });
    }

    [Fact]
    public void PersistentImageEditModels_ContainNoNativeHandles()
    {
        var persistentTypes = new[]
        {
            typeof(ImageEditWorkspace),
            typeof(ImageEditState),
            typeof(ImageEditMutation),
            typeof(ImageObjectRef),
            typeof(ImageObjectKey),
            typeof(ImageReplacementAsset),
            typeof(PdfImageObjectInfo),
            typeof(PdfImageObjectMetadata),
            typeof(PdfObjectMatrix),
            typeof(PdfObjectBounds)
        };

        foreach (var type in persistentTypes)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.DoesNotContain(fields, field => IsNativeHandle(field.FieldType));

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.DoesNotContain(properties, property => IsNativeHandle(property.PropertyType));
        }
    }

    private static bool IsNativeHandle(Type type)
        => type == typeof(IntPtr) || type == typeof(UIntPtr);
}
