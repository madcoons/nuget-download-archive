using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DownloadArchive.Lib;

public static class NativeLib
{
    [UnmanagedCallersOnly(EntryPoint = "execute_download", CallConvs = [typeof(CallConvCdecl)])]
    public static bool ExecuteDownload(
        nint targetDirPtr,
        nint ridPtr,
        nint namePtr,
        nint urlPtr
    )
    {
        try
        {
            var targetDir = Marshal.PtrToStringUTF8(targetDirPtr);
            ArgumentNullException.ThrowIfNull(targetDir);

            var rid = Marshal.PtrToStringUTF8(ridPtr);
            ArgumentNullException.ThrowIfNull(rid);

            var name = Marshal.PtrToStringUTF8(namePtr);
            ArgumentNullException.ThrowIfNull(name);

            var url = Marshal.PtrToStringUTF8(urlPtr);
            ArgumentNullException.ThrowIfNull(url);

            ExecuteDownloadAsync(
                targetDir: targetDir,
                rid: rid,
                name: name,
                url: url
            ).GetAwaiter().GetResult();

            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }
    }

    private static async Task ExecuteDownloadAsync(
        string targetDir,
        string rid,
        string name,
        string url,
        CancellationToken cancellationToken = default
    )
    {
        ArchiveCacher archiveCacher = new();
        ArchiveDecompressor archiveDecompressor = new();
        OutputManager outputManager = new(targetDir);
        ArchiveDownloader archiveDownloader = new();

        var lockPath = archiveCacher.GetCachePath(url) + "_lock";
        var lockDir = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrEmpty(lockDir))
        {
            Directory.CreateDirectory(lockDir);
        }

        await using var fileLock = await LockFileAsync(lockPath, cancellationToken);

        var cachePath = archiveCacher.GetCachePath(url);
        if (!File.Exists(cachePath))
        {
            await using var archiveStream = await archiveDownloader.DownloadAsync(url);
            await archiveCacher.CacheAsync(archiveStream, url, cancellationToken);
        }

        var decompressedDir = await archiveDecompressor.DecompressAsync(cachePath, url, cancellationToken);

        outputManager.GenerateOutput(decompressedDir, rid, name, cancellationToken);
    }

    private static async Task<IAsyncDisposable> LockFileAsync(string path,
        CancellationToken cancellationToken = default)
    {
        var timeout = TimeSpan.FromMinutes(10);
        var retryDelay = TimeSpan.FromMilliseconds(200);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                return new FileStream(
                    path,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None
                );
            }
            catch (IOException)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        throw new TimeoutException($"Timed out acquiring lock on {path}");
    }
}