namespace DownloadArchive.Lib;

public class ArchiveDownloader
{
    private readonly HttpClient _httpClient = new();

    public async Task<Stream> DownloadAsync(string url)
    {
        Stream archiveStream = await _httpClient.GetStreamAsync(url);
        return archiveStream;
    }
}