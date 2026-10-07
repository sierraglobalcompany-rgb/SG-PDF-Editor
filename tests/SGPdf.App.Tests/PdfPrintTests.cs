using System.Globalization;
using System.Reflection;
using System.Text;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfPrintTests
{
    private static Assembly AppAssembly => typeof(PdfDocumentSession).Assembly;

    private static Type PrintRangeType
    {
        get
        {
            var type = AppAssembly.GetType("SGPdf.App.Printing.PdfPrintRange");
            Assert.NotNull(type);
            return type!;
        }
    }

    private static Type PaginatorType
    {
        get
        {
            var type = AppAssembly.GetType("SGPdf.App.Printing.PdfDocumentPaginator");
            Assert.NotNull(type);
            return type!;
        }
    }

    [Fact]
    public void AllRange_CoversEveryPage()
    {
        var range = InvokeStatic(PrintRangeType, "All", 5);

        Assert.Equal(0, GetInt(range, "FirstPageIndex"));
        Assert.Equal(4, GetInt(range, "LastPageIndex"));
        Assert.Equal(5, GetInt(range, "PageCount"));
    }

    [Fact]
    public void CurrentRange_CoversOnlyCurrentPage()
    {
        var range = InvokeStatic(PrintRangeType, "Current", 2, 5);

        Assert.Equal(2, GetInt(range, "FirstPageIndex"));
        Assert.Equal(2, GetInt(range, "LastPageIndex"));
        Assert.Equal(1, GetInt(range, "PageCount"));
    }

    [Fact]
    public void UserPages_UsesOneBasedInclusivePageNumbers()
    {
        var range = InvokeStatic(PrintRangeType, "UserPages", 2, 4, 5);

        Assert.Equal(1, GetInt(range, "FirstPageIndex"));
        Assert.Equal(3, GetInt(range, "LastPageIndex"));
        Assert.Equal(3, GetInt(range, "PageCount"));
    }

    [Fact]
    public void UserPages_RejectsOutOfBoundsRange()
    {
        var method = PrintRangeType.GetMethod("UserPages", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var ex = Assert.Throws<TargetInvocationException>(() => method!.Invoke(null, new object[] { 0, 3, 5 }));
        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    [Fact]
    public void Paginator_ExposesRequestedRangeAndPrintablePageSize()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-print-{Guid.NewGuid():N}.pdf");

        try
        {
            File.WriteAllBytes(path, CreateTwoPagePdf());
            using var session = PdfDocumentSession.Open(path);
            var range = InvokeStatic(PrintRangeType, "All", session.PageCount);
            var paginator = Activator.CreateInstance(PaginatorType, session, range, 768d, 1008d);
            Assert.NotNull(paginator);

            Assert.Equal(2, GetInt(paginator!, "PageCount"));

            var pageSize = PaginatorType.GetProperty("PageSize")!.GetValue(paginator);
            Assert.NotNull(pageSize);
            Assert.Equal(768d, GetDouble(pageSize!, "Width"));
            Assert.Equal(1008d, GetDouble(pageSize!, "Height"));

            var getPage = PaginatorType.GetMethod("GetPage");
            Assert.NotNull(getPage);

            var firstPage = getPage!.Invoke(paginator, new object[] { 0 });
            var secondPage = getPage.Invoke(paginator, new object[] { 1 });
            Assert.NotNull(firstPage);
            Assert.NotNull(secondPage);
            Assert.Equal("DocumentPage", firstPage!.GetType().Name);
            Assert.Equal("DocumentPage", secondPage!.GetType().Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static object InvokeStatic(Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        var value = method!.Invoke(null, args);
        Assert.NotNull(value);
        return value!;
    }

    private static int GetInt(object target, string propertyName)
        => (int)target.GetType().GetProperty(propertyName)!.GetValue(target)!;

    private static double GetDouble(object target, string propertyName)
        => (double)target.GetType().GetProperty(propertyName)!.GetValue(target)!;

    private static byte[] CreateTwoPagePdf()
    {
        const string pageOneContent = "0 0 0 rg\n72 720 144 36 re f\n";
        const string pageTwoContent = "0 0 0 rg\n72 500 216 54 re f\n";
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n",
            $"4 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(pageOneContent)} >>\nstream\n{pageOneContent}endstream\nendobj\n",
            "5 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 792 612] /Contents 6 0 R >>\nendobj\n",
            $"6 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(pageTwoContent)} >>\nstream\n{pageTwoContent}endstream\nendobj\n"
        };

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long>();
        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, obj);
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Length + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets)
            WriteAscii(stream, $"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");

        WriteAscii(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
