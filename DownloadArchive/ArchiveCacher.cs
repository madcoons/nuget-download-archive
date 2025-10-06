using System.Security.Cryptography;
using System.Text;

namespace DownloadArchive;

public class ArchiveCacher(
    string packageRoot
)
{
    private readonly string _cacheDir = Path.Combine(packageRoot, "_archives-cache");

    public async Task CacheAsync(Stream stream, string url, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }

        string cacheFilePath = GetCachePath(url);
        await using FileStream file = File.OpenWrite(cacheFilePath);
        await stream.CopyToAsync(file, cancellationToken);
    }

    public string GetCachePath(string url)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(url);
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        string base64Hash = Convert.ToBase64String(hashBytes);
        string sanitizedBase64Hash = new string(Array.FindAll(base64Hash.ToCharArray(), char.IsLetterOrDigit));

        string cacheFilePath = Path.Combine(_cacheDir, $"{sanitizedBase64Hash}.bin");
        return cacheFilePath;
    }
}