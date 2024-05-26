using System.Formats.Tar;
using System.IO.Compression;

namespace DownloadArchive;

public class ArchiveDecompressor(
    string packageRoot
)
{
    private readonly string _archivesDir = Path.Combine(packageRoot, "_archives");

    public string GetOutputDir(string inputPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(inputPath);
        string destinationDir = Path.Combine(_archivesDir, fileName);

        return destinationDir;
    }

    public async Task<string> DecompressAsync(string inputPath, string url)
    {
        string destinationDir = GetOutputDir(inputPath);
        if (Directory.Exists(destinationDir))
        {
            return destinationDir;
        }

        string originalFileName = Path.GetFileName(new Uri(url, UriKind.Absolute).LocalPath);

        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        await DecompressToDirAsync(inputPath, destinationDir, originalFileName);

        return destinationDir;
    }

    private async Task DecompressToDirAsync(string inputPath, string dir, string originalFileName)
    {
        await using FileStream file = File.OpenRead(inputPath);

        if (originalFileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            await using GZipStream decompressor = new GZipStream(file, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(decompressor, dir, true);
        }
        else if (originalFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(file, dir);
        }
        else
        {
            throw new($"File '{originalFileName}' not supported.");
        }
    }
}