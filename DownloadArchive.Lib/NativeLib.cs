using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DownloadArchive.Lib;

public static class NativeLib
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LogCallback(int level, nint data, int length);

    [UnmanagedCallersOnly(EntryPoint = "execute_download", CallConvs = [typeof(CallConvCdecl)])]
    public static bool ExecuteDownload(
        nint targetDirPtr,
        nint ridPtr,
        nint namePtr,
        nint urlPtr,
        nint logPtr
    )
    {
        using Mutex mutex = new(false, "Global\\DownloadArchiveNuget");
        mutex.WaitOne();
        try
        {
            var executeDownloadFunc = Marshal.GetDelegateForFunctionPointer<LogCallback>(logPtr);
            var log = (int level, string message) =>
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                var messageHandle = GCHandle.Alloc(
                    value: bytes,
                    type: GCHandleType.Pinned
                );
                try
                {
                    executeDownloadFunc(level, messageHandle.AddrOfPinnedObject(), bytes.Length);
                }
                finally
                {
                    messageHandle.Free();
                }
            };

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
                    url: url,
                    log: log
                ).GetAwaiter().GetResult();

                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                log(1, e.ToString());

                return false;
            }
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static async Task ExecuteDownloadAsync(
        string targetDir,
        string rid,
        string name,
        string url,
        Action<int, string> log,
        CancellationToken cancellationToken = default
    )
    {
        ArchiveCacher archiveCacher = new(log);
        ArchiveDecompressor archiveDecompressor = new(log);
        OutputManager outputManager = new(targetDir, log);
        ArchiveDownloader archiveDownloader = new(log);

        // var lockPath = archiveCacher.GetCachePath(url) + "_lock";
        // var lockDir = Path.GetDirectoryName(lockPath);
        // if (!string.IsNullOrEmpty(lockDir))
        // {
        //     Directory.CreateDirectory(lockDir);
        // }

        // await using var fileLock = await LockFileAsync(lockPath, cancellationToken);

        var cachePath = archiveCacher.GetCachePath(url);
        if (!File.Exists(cachePath))
        {
            await using var archiveStream = await archiveDownloader.DownloadAsync(url);
            await archiveCacher.CacheAsync(archiveStream, url, cancellationToken);
        }

        var decompressedDir = await archiveDecompressor.DecompressAsync(cachePath, url, cancellationToken);

        outputManager.GenerateOutput(decompressedDir, rid, name, cancellationToken);
    }

    // private static async Task<IAsyncDisposable> LockFileAsync(string path,
    //     CancellationToken cancellationToken = default)
    // {
    //     var timeout = TimeSpan.FromMinutes(10);
    //     var retryDelay = TimeSpan.FromMilliseconds(200);
    //
    //     var stopwatch = Stopwatch.StartNew();
    //     while (stopwatch.Elapsed < timeout)
    //     {
    //         try
    //         {
    //             return new FileStream(
    //                 path,
    //                 FileMode.OpenOrCreate,
    //                 FileAccess.ReadWrite,
    //                 FileShare.None
    //             );
    //         }
    //         catch (IOException)
    //         {
    //             await Task.Delay(retryDelay, cancellationToken);
    //         }
    //     }
    //
    //     throw new TimeoutException($"Timed out acquiring lock on {path}");
    // }
}