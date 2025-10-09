using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace DownloadArchive.BuildTasks;

public class DownloadTask : Task
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LogCallback(int level, nint data, int length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool ExecuteDownload(
        nint targetDirPtr,
        nint ridPtr,
        nint namePtr,
        nint urlPtr,
        nint logPtr
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetCurrentRuntimeIdentifier(nint refBuffer, int maxLength);

    [Required] public string DownloadArchiveLib { get; set; } = null!;
    [Required] public string TargetDir { get; set; } = null!;
    [Required] public bool FallbackToProcessRuntimeInformation { get; set; }
    public bool IsTestProject { get; set; }
    public string? RuntimeIdentifier { get; set; }
    public string? OutputType { get; set; }
    [Required] public ITaskItem[] InputItems { get; set; } = null!;

    private static readonly ConcurrentDictionary<string, nint> LibHandles = new();

    public override bool Execute()
    {
        if (!IsTestProject && !string.Equals(OutputType, "exe", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var libHandle = LibHandles.GetOrAdd(DownloadArchiveLib, static path =>
        {
            var libHandle = NativeLibrary.Load(path);
            if (libHandle == IntPtr.Zero)
            {
                throw new Exception($"Failed to load library \"{path}\".");
            }

            return libHandle;
        });

        var success = true;

        var currentRuntimeIdentifier = GeCurrentRuntimeIdentifier(libHandle);

        var executeDownloadFuncHandle = NativeLibrary.GetExport(libHandle, "execute_download");
        if (executeDownloadFuncHandle == IntPtr.Zero)
        {
            throw new Exception("Failed to load function \"execute_download\".");
        }

        var executeDownloadFunc = Marshal.GetDelegateForFunctionPointer<ExecuteDownload>(executeDownloadFuncHandle);

        LogCallback log = (level, data, length) =>
        {
            var buffer = new byte[length];
            Marshal.Copy(data, buffer, 0, length);
            var message = Encoding.UTF8.GetString(buffer);

            if (level == 0)
            {
                Log.LogMessage(message);
            }
            else if (level == 1)
            {
                Log.LogError(message);
            }
        };

        nint logCallbackPtr = Marshal.GetFunctionPointerForDelegate(log);

        foreach (var item in InputItems)
        {
            foreach (var metadataName in item.MetadataNames.OfType<string>())
            {
                if (metadataName.StartsWith("RID-", StringComparison.OrdinalIgnoreCase))
                {
                    var val = item.GetMetadata(metadataName);
                    if (string.IsNullOrEmpty(val))
                    {
                        continue;
                    }

                    var runtimeId = metadataName.Substring(4);
                    var targetId = RuntimeIdentifier ??
                                   (FallbackToProcessRuntimeInformation ? currentRuntimeIdentifier : null);

                    if (targetId != null &&
                        !targetId.Equals(runtimeId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var targetDirHandle = GCHandle.Alloc(
                        value: Encoding.UTF8.GetBytes(TargetDir + "\0"),
                        type: GCHandleType.Pinned
                    );
                    try
                    {
                        var ridHandle = GCHandle.Alloc(
                            value: Encoding.UTF8.GetBytes(runtimeId + "\0"),
                            type: GCHandleType.Pinned
                        );
                        try
                        {
                            var nameHandle = GCHandle.Alloc(
                                value: Encoding.UTF8.GetBytes(item.ItemSpec + "\0"),
                                type: GCHandleType.Pinned
                            );
                            try
                            {
                                var urlHandle = GCHandle.Alloc(
                                    value: Encoding.UTF8.GetBytes(val + "\0"),
                                    type: GCHandleType.Pinned
                                );
                                try
                                {
                                    success &= executeDownloadFunc(
                                        targetDirPtr: targetDirHandle.AddrOfPinnedObject(),
                                        ridPtr: ridHandle.AddrOfPinnedObject(),
                                        namePtr: nameHandle.AddrOfPinnedObject(),
                                        urlPtr: urlHandle.AddrOfPinnedObject(),
                                        logPtr: logCallbackPtr
                                    );
                                }
                                finally
                                {
                                    urlHandle.Free();
                                }
                            }
                            finally
                            {
                                nameHandle.Free();
                            }
                        }
                        finally
                        {
                            ridHandle.Free();
                        }
                    }
                    finally
                    {
                        targetDirHandle.Free();
                    }
                }
            }
        }

        GC.KeepAlive(log);

        return success;
    }

    private static string GeCurrentRuntimeIdentifier(nint libHandle)
    {
        var getCurrentRIDFuncHandle = NativeLibrary.GetExport(libHandle, "get_current_rid");
        if (getCurrentRIDFuncHandle == IntPtr.Zero)
        {
            throw new Exception("Failed to load function \"get_current_rid\".");
        }

        var getCurrentRIDFunc =
            Marshal.GetDelegateForFunctionPointer<GetCurrentRuntimeIdentifier>(getCurrentRIDFuncHandle);

        var resBuffer = new byte[100];
        var resBufferHandle = GCHandle.Alloc(
            value: resBuffer,
            type: GCHandleType.Pinned
        );
        try
        {
            var length = getCurrentRIDFunc(resBufferHandle.AddrOfPinnedObject(), resBuffer.Length);
            if (length < 0)
            {
                throw new Exception("Failed to get current RID.");
            }

            var subBuffer = new byte[length];
            Array.Copy(resBuffer, subBuffer, length);
            var currenRID = Encoding.UTF8.GetString(subBuffer);
            return currenRID;
        }
        finally
        {
            resBufferHandle.Free();
        }
    }
}