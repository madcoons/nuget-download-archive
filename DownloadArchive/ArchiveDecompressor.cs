using System.IO.Compression;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Formats.Tar;
#else
using SharpCompress.Archives;
using TarArchive = SharpCompress.Archives.Tar.TarArchive;
using ZipArchive = SharpCompress.Archives.Zip.ZipArchive;
using SharpCompress.Common;
#endif

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
#if NET5_0_OR_GREATER
        await using FileStream file = File.OpenRead(inputPath);
#else
        using FileStream file = File.OpenRead(inputPath);
#endif

        if (originalFileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
#if NET5_0_OR_GREATER
            await using GZipStream decompressor = new GZipStream(file, CompressionMode.Decompress);
#else
            using GZipStream decompressor = new GZipStream(file, CompressionMode.Decompress);
#endif
            await _ExtractTarToDirectoryAsyncImpl(decompressor, dir, true);
        }
        else if (originalFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            _ExtractZipToDirectoryImpl(file, dir, true);
        }
        else
        {
            throw new($"File '{originalFileName}' not supported.");
        }
    }

#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void _ExtractZipToDirectoryImpl(Stream source, string destinationDirectoryName, bool overwriteFiles) =>
        ZipFile.ExtractToDirectory(source, destinationDirectoryName, overwriteFiles);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task _ExtractTarToDirectoryAsyncImpl(Stream source, string destinationDirectoryName, bool overwriteFiles) =>
        TarFile.ExtractToDirectoryAsync(source, destinationDirectoryName, overwriteFiles);
#else
    private static void _ExtractZipToDirectoryImpl(Stream source, string destinationDirectoryName, bool overwriteFiles) {
        using var archive = ZipArchive.Open(source);
        archive.WriteToDirectory(destinationDirectoryName, new ExtractionOptions
        {
            ExtractFullPath = true,
            Overwrite = overwriteFiles
        });
    }

    private static async Task _ExtractTarToDirectoryAsyncImpl(Stream source, string destinationDirectoryName, bool overwriteFiles) =>
        await Task.Run(() =>
        {
            using var archive = TarArchive.Open(source);
            archive.WriteToDirectory(destinationDirectoryName, new ExtractionOptions
            {
                ExtractFullPath = true,
                Overwrite = overwriteFiles
            });
        }).ConfigureAwait(false);
#endif
}