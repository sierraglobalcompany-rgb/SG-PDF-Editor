using System.IO;

namespace SGPdf.App.Features.Sign;

internal sealed class SignatureLibraryFileOps
{
    internal Action<string, string> PublishNewFile { get; set; }
        = static (source, destination) => File.Move(source, destination);

    internal Action<string, string> ReplaceFile { get; set; }
        = static (source, destination) => File.Replace(source, destination, null);

    internal Action<string> DeleteFile { get; set; }
        = static path => File.Delete(path);
}

internal sealed record SignatureLibraryDeleteResult(
    bool FileCleanupSucceeded,
    string? Warning = null);