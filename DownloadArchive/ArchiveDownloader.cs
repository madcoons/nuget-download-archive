namespace DownloadArchive;

public class ArchiveDownloader
{
    private readonly HttpClient _httpClient = new();

    public async Task<Stream> DownlaodAsync(string url)
    {
        Stream archiveStream = await _httpClient.GetStreamAsync(url);
        return archiveStream;
    }
}