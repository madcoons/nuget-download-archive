namespace DownloadArchive.Lib;

/// <summary>
/// A lock held across processes, so concurrent builds do not work on the same cache entry at once.
/// It is a lock file rather than a named <see cref="Mutex"/> on purpose: a named mutex is not honoured
/// across processes once this library is compiled ahead of time on Unix, and it fails silently there,
/// while FileShare.None becomes a real advisory lock on every platform the package ships for.
/// </summary>
public static class FileLocker
{
    public static async Task<IAsyncDisposable> LockForFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var lockPath = path + "_lock";
        DirHelpers.EnsureDirExistsForFile(lockPath);

        var timeout = TimeSpan.FromMinutes(10);
        var retryDelay = TimeSpan.FromMilliseconds(200);

        var start = DateTime.UtcNow;
        var deadline = start + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                return File.OpenWrite(lockPath);
            }
            catch (IOException)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        throw new TimeoutException($"Timed out acquiring lock on {path}");
    }
}