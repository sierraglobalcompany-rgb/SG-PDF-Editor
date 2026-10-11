using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class TesseractRuntimeTests
{
    private static readonly object EnvironmentGate = new();

    [Fact]
    public void Missing_private_runtime_fails_instead_of_using_global_install()
    {
        var appBase = CreateTempDirectory();
        var fakeGlobal = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(fakeGlobal, "tesseract.exe"), "not-a-real-exe");

            lock (EnvironmentGate)
            {
                var originalPath = Environment.GetEnvironmentVariable("PATH");
                try
                {
                    Environment.SetEnvironmentVariable(
                        "PATH",
                        string.IsNullOrEmpty(originalPath) ? fakeGlobal : fakeGlobal + Path.PathSeparator + originalPath);

                    var exception = Assert.ThrowsAny<Exception>(() => ResolveExecutablePath(appBase));
                    Assert.True(
                        Unwrap(exception) is FileNotFoundException,
                        $"Expected FileNotFoundException but got {Unwrap(exception).GetType().FullName}: {Unwrap(exception).Message}");
                }
                finally
                {
                    Environment.SetEnvironmentVariable("PATH", originalPath);
                }
            }
        }
        finally
        {
            Directory.Delete(appBase, recursive: true);
            Directory.Delete(fakeGlobal, recursive: true);
        }
    }

    [Fact]
    public void Private_runtime_resolves_only_app_owned_executable_and_tessdata()
    {
        var appBase = CreateTempDirectory();
        try
        {
            var runtimeDirectory = Path.Combine(appBase, "tesseract");
            var tessdataDirectory = Path.Combine(runtimeDirectory, "tessdata");
            Directory.CreateDirectory(tessdataDirectory);
            File.WriteAllText(Path.Combine(runtimeDirectory, "tesseract.exe"), "fixture");
            File.WriteAllText(Path.Combine(tessdataDirectory, "spa.traineddata"), "spa");
            File.WriteAllText(Path.Combine(tessdataDirectory, "eng.traineddata"), "eng");

            Assert.Equal(
                Path.GetFullPath(Path.Combine(runtimeDirectory, "tesseract.exe")),
                ResolveExecutablePath(appBase));
            Assert.Equal(
                Path.GetFullPath(tessdataDirectory),
                ResolveTessdataDirectory(appBase));
        }
        finally
        {
            Directory.Delete(appBase, recursive: true);
        }
    }

    [Theory]
    [InlineData("spa.traineddata")]
    [InlineData("eng.traineddata")]
    public void Tessdata_requires_both_F10_languages(string missingFile)
    {
        var appBase = CreateTempDirectory();
        try
        {
            var runtimeDirectory = Path.Combine(appBase, "tesseract");
            var tessdataDirectory = Path.Combine(runtimeDirectory, "tessdata");
            Directory.CreateDirectory(tessdataDirectory);
            File.WriteAllText(Path.Combine(runtimeDirectory, "tesseract.exe"), "fixture");

            foreach (var name in new[] { "spa.traineddata", "eng.traineddata" })
            {
                if (!string.Equals(name, missingFile, StringComparison.Ordinal))
                    File.WriteAllText(Path.Combine(tessdataDirectory, name), name);
            }

            var exception = Assert.ThrowsAny<Exception>(() => ResolveTessdataDirectory(appBase));
            Assert.True(
                Unwrap(exception) is FileNotFoundException,
                $"Expected FileNotFoundException but got {Unwrap(exception).GetType().FullName}: {Unwrap(exception).Message}");
        }
        finally
        {
            Directory.Delete(appBase, recursive: true);
        }
    }

    [Fact]
    public void F10_V1_does_not_require_osd_traineddata()
    {
        var appBase = CreateTempDirectory();
        try
        {
            var runtimeDirectory = Path.Combine(appBase, "tesseract");
            var tessdataDirectory = Path.Combine(runtimeDirectory, "tessdata");
            Directory.CreateDirectory(tessdataDirectory);
            File.WriteAllText(Path.Combine(runtimeDirectory, "tesseract.exe"), "fixture");
            File.WriteAllText(Path.Combine(tessdataDirectory, "spa.traineddata"), "spa");
            File.WriteAllText(Path.Combine(tessdataDirectory, "eng.traineddata"), "eng");

            Assert.Equal(Path.GetFullPath(tessdataDirectory), ResolveTessdataDirectory(appBase));
            Assert.False(File.Exists(Path.Combine(tessdataDirectory, "osd.traineddata")));
        }
        finally
        {
            Directory.Delete(appBase, recursive: true);
        }
    }

    private static string ResolveExecutablePath(string appBase) =>
        InvokeRuntimeMethod("ResolveExecutablePath", appBase);

    private static string ResolveTessdataDirectory(string appBase) =>
        InvokeRuntimeMethod("ResolveTessdataDirectory", appBase);

    private static string InvokeRuntimeMethod(string methodName, string appBase)
    {
        var runtimeType = typeof(PdfiumRuntime).Assembly.GetType("SGPdf.App.Features.Ocr.TesseractRuntime");
        Assert.NotNull(runtimeType);

        var method = runtimeType!.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            var result = method!.Invoke(null, new object?[] { appBase });
            return Assert.IsType<string>(result);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException!;
        }
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: not null } target
            ? target.InnerException
            : exception;

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sgpdf-f10-runtime-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
