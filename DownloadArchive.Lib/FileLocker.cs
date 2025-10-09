namespace DownloadArchive.Lib;

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
                return new FileStream(
                    path,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None
                );
            }
            catch (IOException)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        throw new TimeoutException($"Timed out acquiring lock on {path}");
    }
}