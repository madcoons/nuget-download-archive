using System.Security.Cryptography;
using System.Text;

namespace DownloadArchive;

public class ArchiveCacher(
    string packageRoot
)
{
    private readonly string _cacheDir = Path.Combine(packageRoot, "_archives-cache");

    public string GetCachePath(string url) => GetCacheFilePath(url);

    public async Task<string> CacheAsync(Stream stream, string url)
    {
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }

        string cacheFilePath = GetCacheFilePath(url);
#if NET5_0_OR_GREATER
        await using FileStream file = File.OpenWrite(cacheFilePath);
#else
        using FileStream file = File.OpenWrite(cacheFilePath);
#endif
        await stream.CopyToAsync(file);

        return cacheFilePath;
    }

    private string GetCacheFilePath(string url)
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