using System.Collections;
using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowBatchTests
{
    [Fact]
    public void WatermarkRequest_AllPages_ResolvesAgainstEachDocumentPageCount()
    {
        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;
        Type requestType = appAssembly.GetType(
            "SGPdf.App.Features.Utilities.WatermarkUtilityRequest",
            throwOnError: true)!;

        ConstructorInfo? constructor = requestType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(string), typeof(double), typeof(double), typeof(double),
                typeof(double), typeof(double), typeof(int), typeof(int),
                typeof(bool), typeof(bool), typeof(string), typeof(byte),
                typeof(byte), typeof(byte), typeof(bool)
            ],
            modifiers: null);
        Assert.NotNull(constructor);

        object request = constructor!.Invoke(
        [
            "LOTE", 24d, 0.5d, 0d, 0d, 0d,
            1, 1, false, false, "Center",
            (byte)128, (byte)128, (byte)128, true
        ]);

        MethodInfo tryBuild = requestType.GetMethod(
            "TryBuild",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        object?[] arguments = [3, 0, null, null, null, null];
        bool built = (bool)tryBuild.Invoke(request, arguments)!;

        Assert.True(built, arguments[5] as string);
        Assert.NotNull(arguments[3]);
        MethodInfo resolve = arguments[3]!.GetType().GetMethod(
            "Resolve",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        int[] pages = ((IEnumerable)resolve.Invoke(arguments[3], [3])!).Cast<int>().ToArray();
        Assert.Equal([0, 1, 2], pages);
    }

    [Fact]
    public void BuildBatchSummary_ReportsSuccessesAndFailures()
    {
        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;
        Type resultType = appAssembly.GetType(
            "SGPdf.App.Features.Batch.PdfBatchItemResult",
            throwOnError: true)!;
        object success = Activator.CreateInstance(
            resultType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: ["a.pdf", "a-marca-agua.pdf", true, null],
            culture: null)!;
        object failure = Activator.CreateInstance(
            resultType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: ["b.pdf", "b-marca-agua.pdf", false, "PDF protegido"],
            culture: null)!;

        Type windowType = appAssembly.GetType("SGPdf.App.MainWindow", throwOnError: true)!;
        MethodInfo? summaryMethod = windowType.GetMethod(
            "BuildBatchSummary",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(summaryMethod);

        Array results = Array.CreateInstance(resultType, 2);
        results.SetValue(success, 0);
        results.SetValue(failure, 1);
        object readOnly = typeof(Array)
            .GetMethod(nameof(Array.AsReadOnly))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [results])!;

        string summary = (string)summaryMethod!.Invoke(null, [readOnly])!;
        Assert.Contains("1 correcto", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 con error", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("b.pdf", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PDF protegido", summary, StringComparison.OrdinalIgnoreCase);
    }
}
