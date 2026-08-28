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
        nint cacheDirPtr,
        nint logPtr
    )
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

            var cacheDir = Marshal.PtrToStringUTF8(cacheDirPtr);

            ExecuteDownloadAsync(
                targetDir: targetDir,
                rid: rid,
                name: name,
                url: url,
                cacheDir: cacheDir,
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

    [UnmanagedCallersOnly(EntryPoint = "get_current_rid", CallConvs = [typeof(CallConvCdecl)])]
    public static int GetProcessRuntimeIdentifier(nint buffer, int maxLength)
    {
        var rid = RuntimeInformationHelpers.GetRunningRID();
        var resBuffer = Encoding.UTF8.GetBytes(rid);
        if (rid.Length > maxLength)
        {
            return -1;
        }

        Marshal.Copy(resBuffer, 0, buffer, resBuffer.Length);

        return rid.Length;
    }

    private static async Task ExecuteDownloadAsync(
        string targetDir,
        string rid,
        string name,
        string url,
        string? cacheDir,
        Action<int, string> log,
        CancellationToken cancellationToken = default
    )
    {
        CachePaths cachePaths = new(cacheDir);
        ArchiveCacher archiveCacher = new(cachePaths, log);
        ArchiveDecompressor archiveDecompressor = new(cachePaths, log);
        OutputManager outputManager = new(targetDir, log);
        ArchiveDownloader archiveDownloader = new(log);

        var cachePath = archiveCacher.GetCachePath(url);

        await using (await FileLocker.LockForFileAsync(cachePath, cancellationToken))
        {
            if (!archiveCacher.IsCached(url))
            {
                await using var archiveStream = await archiveDownloader.DownloadAsync(url);
                await archiveCacher.CacheAsync(archiveStream, url, cancellationToken);
            }

            var decompressedDir = await archiveDecompressor.DecompressAsync(cachePath, url, cancellationToken);
            outputManager.GenerateOutput(decompressedDir, rid, name, cancellationToken);
        }
    }
}