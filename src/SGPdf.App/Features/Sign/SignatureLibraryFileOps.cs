using System.IO;

namespace SGPdf.App.Features.Sign;

internal sealed class SignatureLibraryFileOps
{
    internal Action<string, string> PublishNewFile { get; init; }
        = static (source, destination) => File.Move(source, destination);

    internal Action<string, string> ReplaceFile { get; init; }
        = static (source, destination) => File.Replace(source, destination, null);

    internal Action<string> DeleteFile { get; init; }
        = static path => File.Delete(path);
}

internal sealed record SignatureLibraryDeleteResult(
    bool FileCleanupSucceeded,
    string? Warning = null);