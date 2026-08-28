using System.Security.Cryptography;
using System.Text;

namespace DownloadArchive.Lib;

/// <summary>
/// Resolves where downloaded archives and their decompressed content are kept. This is the temp
/// directory, so the cache is cleaned up by the system instead of growing forever.
/// </summary>
public static class CachePaths
{
    public static string GetArchivePath(string url)
    {
        return Path.GetFullPath(Path.Combine(GetRootDir(), "archives-cache", $"{GetUrlHash(url)}.bin"));
    }

    public static string GetDecompressedDir(string archivePath)
    {
        var name = Path.GetFileNameWithoutExtension(archivePath);

        return Path.GetFullPath(Path.Combine(GetRootDir(), "archives", name));
    }

    private static string GetRootDir()
    {
        return Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nuget-download-archive"));
    }

    private static string GetUrlHash(string url)
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        string base64Hash = Convert.ToBase64String(hashBytes);

        return new string(Array.FindAll(base64Hash.ToCharArray(), char.IsLetterOrDigit));
    }
}
