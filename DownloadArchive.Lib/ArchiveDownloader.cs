namespace DownloadArchive.Lib;

public class ArchiveDownloader(Action<int, string> log)
{
    private readonly HttpClient _httpClient = new();

    public async Task<Stream> DownloadAsync(string url)
    {
        log(0, $"Downloading {url}");

        Stream archiveStream = await _httpClient.GetStreamAsync(url);
        return archiveStream;
    }
}