using System.Formats.Tar;
using System.IO.Compression;

namespace DownloadArchive.Lib;

public class ArchiveDecompressor(Action<int, string> log)
{
    public async Task DecompressAsync(string inputPath, string url,
        CancellationToken cancellationToken = default)
    {
        var destinationDir = CachePaths.GetDecompressedDir(inputPath);
        if (CompletionMarker.IsComplete(destinationDir))
        {
            return;
        }

        var originalFileName = Path.GetFileName(new Uri(url, UriKind.Absolute).LocalPath);

        log(0, $"Decompressing {inputPath} to {destinationDir}");

        // The marker goes first: from here until it is written again the directory counts as incomplete,
        // so whatever an interrupted decompression left behind is dropped rather than trusted.
        CompletionMarker.Remove(destinationDir);
        DirHelpers.DeleteDirIfExists(destinationDir);
        Directory.CreateDirectory(destinationDir);

        await DecompressToDirAsync(inputPath, destinationDir, originalFileName, cancellationToken);

        CompletionMarker.Create(destinationDir);
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
