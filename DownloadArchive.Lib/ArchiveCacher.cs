using System.Security.Cryptography;
using System.Text;

namespace DownloadArchive.Lib;

public class ArchiveCacher(Action<int, string> log)
{
    public async Task CacheAsync(Stream stream, string url, CancellationToken cancellationToken = default)
    {
        var cacheFilePath = GetCachePath(url);

        var dir = Path.GetDirectoryName(cacheFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var file = File.OpenWrite(cacheFilePath);

        log(0, $"Writing cache for {url} to {cacheFilePath}");

        await stream.CopyToAsync(file, cancellationToken);
    }

    public string GetCachePath(string url)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(url);
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        string base64Hash = Convert.ToBase64String(hashBytes);
        string sanitizedBase64Hash = new string(Array.FindAll(base64Hash.ToCharArray(), char.IsLetterOrDigit));

        string cacheFilePath = Path.GetFullPath(Path.Combine(
            Path.GetTempPath(),
            "nuget-download-archive",
            "archives-cache",
            $"{sanitizedBase64Hash}.bin"
        ));

        return cacheFilePath;
    }
}