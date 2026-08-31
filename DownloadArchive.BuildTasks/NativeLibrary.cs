using System.Runtime.InteropServices;

namespace DownloadArchive.BuildTasks;

/// <summary>
/// Loads the native DownloadArchive.Lib for the running OS. A P/Invoke is resolved the first time it
/// is called, so the imports of the other platforms are only declarations here and one build of this
/// assembly serves every OS.
/// </summary>
public static class NativeLibrary
{
    [DllImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryWindows(string fileName);

    [DllImport("kernel32", EntryPoint = "GetProcAddress", SetLastError = true)]
    private static extern IntPtr GetProcAddressWindows(IntPtr handle, string symbol);

    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    private static extern IntPtr DlOpenLinux(string fileName, int flags);

    [DllImport("libdl.so.2", EntryPoint = "dlsym")]
    private static extern IntPtr DlSymLinux(IntPtr handle, string symbol);

    [DllImport("dl", EntryPoint = "dlopen")]
    private static extern IntPtr DlOpenOsx(string fileName, int flags);

    [DllImport("dl", EntryPoint = "dlsym")]
    private static extern IntPtr DlSymOsx(IntPtr handle, string symbol);

    private const int RTLD_NOW = 2;

    public static IntPtr Load(string libraryPath)
    {
        IntPtr handle;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            handle = LoadLibraryWindows(libraryPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            handle = DlOpenOsx(libraryPath, RTLD_NOW);
        }
        else
        {
            handle = DlOpenLinux(libraryPath, RTLD_NOW);
        }

        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Unable to load native library: {libraryPath}");
        }

        return handle;
    }

    public static IntPtr GetExport(IntPtr libraryHandle, string name)
    {
        IntPtr ptr;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ptr = GetProcAddressWindows(libraryHandle, name);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ptr = DlSymOsx(libraryHandle, name);
        }
        else
        {
            ptr = DlSymLinux(libraryHandle, name);
        }

        if (ptr == IntPtr.Zero)
        {
            throw new MissingMethodException($"Export '{name}' not found.");
        }

        return ptr;
    }
}
