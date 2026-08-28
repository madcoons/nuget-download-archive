using System.Formats.Tar;
using System.IO.Compression;

namespace DownloadArchive.Lib;

public class ArchiveDecompressor(CachePaths cachePaths, Action<int, string> log)
{
    public async Task<string> DecompressAsync(string inputPath, string url,
        CancellationToken cancellationToken = default)
    {
        var destinationDir = cachePaths.GetDecompressedDir(inputPath);
        if (CompletionMarker.IsComplete(destinationDir))
        {
            return destinationDir;
        }

        var originalFileName = Path.GetFileName(new Uri(url, UriKind.Absolute).LocalPath);

        log(0, $"Decompressing {inputPath} to {destinationDir}");

        var tempDir = DirHelpers.GetTempSiblingPath(destinationDir);
        try
        {
            Directory.CreateDirectory(tempDir);

            await DecompressToDirAsync(inputPath, tempDir, originalFileName, cancellationToken);

            DirHelpers.ReplaceDir(tempDir, destinationDir);
        }
        finally
        {
            DirHelpers.DeleteDirIfExists(tempDir);
        }

        CompletionMarker.Create(destinationDir);

        return destinationDir;
    }

    private async Task DecompressToDirAsync(string inputPath, string dir, string originalFileName,
        CancellationToken cancellationToken = default)
    {
        await using var file = File.OpenRead(inputPath);

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
            throw new Exception($"File '{originalFileName}' not supported.");
        }
    }
}
