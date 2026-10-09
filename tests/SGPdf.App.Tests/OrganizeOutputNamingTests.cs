using System.Reflection;
using System.Runtime.ExceptionServices;
using SGPdf.App.Features.Organize;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizeOutputNamingTests
{
    [Fact]
    public void BuildSplitPath_UsesStableBaseOrdinalAndRangeSuffix()
    {
        var root = Path.Combine(Path.GetTempPath(), $"sgpdf-naming-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var basePath = Path.Combine(root, "reporte.pdf");

            var first = Invoke("BuildSplitPath", basePath, 1, 1, 3);
            var second = Invoke("BuildSplitPath", basePath, 2, 4, 4);

            Assert.Equal(Path.Combine(root, "reporte_parte-001_p1-3.pdf"), first);
            Assert.Equal(Path.Combine(root, "reporte_parte-002_p4.pdf"), second);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData("", 1, 1, 2)]
    [InlineData("   ", 1, 1, 2)]
    [InlineData("base.pdf", 0, 1, 2)]
    [InlineData("base.pdf", 1, 0, 2)]
    [InlineData("base.pdf", 1, 3, 2)]
    public void BuildSplitPath_InvalidArguments_AreRejected(string basePath, int ordinal, int first, int last)
    {
        Assert.ThrowsAny<ArgumentException>(() => Invoke("BuildSplitPath", basePath, ordinal, first, last));
    }

    private static string Invoke(string methodName, params object[] args)
    {
        var type = typeof(OrganizePlan).Assembly.GetType(
            "SGPdf.App.Features.Organize.OrganizeOutputNaming",
            throwOnError: true)!;
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        try
        {
            return (string)method.Invoke(null, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
