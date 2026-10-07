namespace SGPdf.App.Pdf;

public readonly record struct PdfRenderRequest(
    long Version,
    CancellationToken CancellationToken);

public sealed class PdfRenderScheduler : IDisposable
{
    private readonly object _sync = new();
    private CancellationTokenSource? _currentSource;
    private long _version;
    private bool _disposed;

    public PdfRenderRequest Begin()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _currentSource?.Cancel();
            _currentSource?.Dispose();

            _currentSource = new CancellationTokenSource();
            _version++;

            return new PdfRenderRequest(_version, _currentSource.Token);
        }
    }

    public bool IsCurrent(PdfRenderRequest request)
    {
        lock (_sync)
        {
            return !_disposed &&
                   request.Version == _version &&
                   !request.CancellationToken.IsCancellationRequested;
        }
    }

    public void CancelCurrent()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _currentSource?.Cancel();
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _currentSource?.Cancel();
            _currentSource?.Dispose();
            _currentSource = null;
        }

        GC.SuppressFinalize(this);
    }
}
