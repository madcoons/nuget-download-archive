namespace DownloadArchive.Lib;

public class ArchiveCacher(Action<int, string> log)
{
    public string GetCachePath(string url)
    {
        return CachePaths.GetArchivePath(url);
    }

    public bool IsCached(string url)
    {
        return CompletionMarker.IsComplete(GetCachePath(url));
    }

    public async Task CacheAsync(Stream stream, string url, CancellationToken cancellationToken = default)
    {
        var cacheFilePath = GetCachePath(url);
        DirHelpers.EnsureDirExistsForFile(cacheFilePath);

        CompletionMarker.Remove(cacheFilePath);

        log(0, $"Writing cache for {url} to {cacheFilePath}");

        var tempFilePath = DirHelpers.GetTempSiblingPath(cacheFilePath);
        try
        {
            await using (var file = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(file, cancellationToken);
            }

            File.Move(tempFilePath, cacheFilePath, true);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        CompletionMarker.Create(cacheFilePath);
    }
}
