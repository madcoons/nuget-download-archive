using System.Formats.Tar;
using System.IO.Compression;

namespace DownloadArchive.Lib;

public class ArchiveDecompressor
{
    public string GetOutputDir(string inputPath)
    {
        var archivesDir = Path.GetFullPath(Path.Combine(
            Path.GetTempPath(),
            "nuget-download-archive",
            "archives"
        ));

        string fileName = Path.GetFileNameWithoutExtension(inputPath);
        string destinationDir = Path.GetFullPath(Path.Combine(archivesDir, fileName));

        return destinationDir;
    }

    public async Task<string> DecompressAsync(string inputPath, string url,
        CancellationToken cancellationToken = default)
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

        await DecompressToDirAsync(inputPath, destinationDir, originalFileName, cancellationToken);

        return destinationDir;
    }

    private async Task DecompressToDirAsync(string inputPath, string dir, string originalFileName,
        CancellationToken cancellationToken = default)
    {
        await using FileStream file = File.OpenRead(inputPath);

        if (originalFileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            await using GZipStream decompressor = new GZipStream(file, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(decompressor, dir, true, cancellationToken);
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