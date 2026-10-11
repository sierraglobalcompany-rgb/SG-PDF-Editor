using System.Diagnostics;
using System.Reflection;
using SGPdf.App.Features.Ocr;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TesseractProcessRecognizerTests
{
    [Fact]
    public void Task3_process_adapter_contract_is_present_and_isolated_under_ocr()
    {
        var assembly = typeof(TesseractRuntime).Assembly;
        var recognizerType = assembly.GetType("SGPdf.App.Features.Ocr.TesseractProcessRecognizer");
        var resultType = assembly.GetType("SGPdf.App.Features.Ocr.TesseractProcessResult");

        Assert.NotNull(recognizerType);
        Assert.NotNull(resultType);

        Assert.NotNull(recognizerType!.GetMethod(
            "RecognizeAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.NotNull(recognizerType.GetMethod(
            "BuildStartInfo",
            BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(recognizerType.GetMethod(
            "RunProcessAsync",
            BindingFlags.Static | BindingFlags.NonPublic));

        Assert.NotNull(resultType!.GetProperty("TsvPath"));
        Assert.NotNull(resultType.GetProperty("StandardOutput"));
        Assert.NotNull(resultType.GetProperty("StandardError"));
    }

    [Fact]
    public void Process_adapter_does_not_reuse_the_labels_namespace_runner()
    {
        var assembly = typeof(TesseractRuntime).Assembly;
        var recognizerType = assembly.GetType("SGPdf.App.Features.Ocr.TesseractProcessRecognizer");
        Assert.NotNull(recognizerType);

        var fields = recognizerType!.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        Assert.DoesNotContain(fields, field =>
            string.Equals(field.FieldType.FullName, "SGPdf.App.Features.Labels.ChildProcessRunner", StringComparison.Ordinal));
    }
}
