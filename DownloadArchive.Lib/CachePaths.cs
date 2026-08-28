using System.Security.Cryptography;
using System.Text;

namespace DownloadArchive.Lib;

/// <summary>
/// Resolves where downloaded archives and their decompressed content are kept.
/// The root defaults to a per user cache directory and can be overridden with the
/// DownloadArchiveCacheDir MSBuild property.
/// </summary>
public class CachePaths(string? rootDir = null)
{
    public string RootDir { get; } = ResolveRootDir(rootDir);

    public string GetArchivePath(string url)
    {
        return Path.GetFullPath(Path.Combine(RootDir, "archives-cache", $"{GetUrlHash(url)}.bin"));
    }

    public string GetDecompressedDir(string archivePath)
    {
        var name = Path.GetFileNameWithoutExtension(archivePath);

        return Path.GetFullPath(Path.Combine(RootDir, "archives", name));
    }

    private static string ResolveRootDir(string? rootDir)
    {
        if (!string.IsNullOrWhiteSpace(rootDir))
        {
            return Path.GetFullPath(rootDir);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseDir = string.IsNullOrEmpty(localAppData) ? Path.GetTempPath() : localAppData;

        return Path.GetFullPath(Path.Combine(baseDir, "nuget-download-archive"));
    }

    private static string GetUrlHash(string url)
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        string base64Hash = Convert.ToBase64String(hashBytes);

        return new string(Array.FindAll(base64Hash.ToCharArray(), char.IsLetterOrDigit));
    }
}
