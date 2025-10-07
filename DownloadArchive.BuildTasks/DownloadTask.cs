using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace DownloadArchive.BuildTasks;

public class DownloadTask : Task
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool ExecuteDownload(
        nint targetDirPtr,
        nint ridPtr,
        nint namePtr,
        nint urlPtr
    );

    [Required] public string DownloadArchiveLib { get; set; } = null!;
    [Required] public string TargetDir { get; set; } = null!;
    public bool IsTestProject { get; set; }
    public string? RuntimeIdentifier { get; set; }
    public string? OutputType { get; set; }
    [Required] public ITaskItem[] InputItems { get; set; } = null!;

    public override bool Execute()
    {
        if (!IsTestProject && !(OutputType is null || OutputType.Equals("exe", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var libHandle = NativeLibrary.Load(DownloadArchiveLib);
        if (libHandle == IntPtr.Zero)
        {
            throw new Exception($"Failed to load library \"{DownloadArchiveLib}\".");
        }

        var success = true;
        try
        {
            var executeDownloadFuncHandle = NativeLibrary.GetExport(libHandle, "execute_download");
            if (executeDownloadFuncHandle == IntPtr.Zero)
            {
                throw new Exception("Failed to load function \"execute_download\".");
            }

            var executeDownloadFunc = Marshal.GetDelegateForFunctionPointer<ExecuteDownload>(executeDownloadFuncHandle);
            if (executeDownloadFunc is null)
            {
                throw new Exception("Failed to get delegate.");
            }

            foreach (var item in InputItems)
            {
                foreach (var metadataName in item.MetadataNames.OfType<string>())
                {
                    if (metadataName.StartsWith("RID-", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = item.GetMetadata(metadataName);
                        if (!string.IsNullOrEmpty(val))
                        {
                            var runtimeId = metadataName.Substring(4);
                            if (RuntimeIdentifier != null &&
                                !RuntimeIdentifier.Equals(runtimeId, StringComparison.OrdinalIgnoreCase))
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
                                                urlPtr: urlHandle.AddrOfPinnedObject()
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
            }
        }
        finally
        {
            NativeLibrary.Free(libHandle);
        }

        return success;
    }
}