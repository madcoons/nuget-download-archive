using System.Runtime.InteropServices;

namespace DownloadArchive.BuildTasks;

public static class NativeLibrary
{
#if WINDOWS
    private const string KERNEL32 = "kernel32";
    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport(KERNEL32, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport(KERNEL32, SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);
#else
#if LINUX
    private const string LIBDL = "libdl.so.2";
#else
    private const string LIBDL = "dl";
#endif

    [DllImport(LIBDL)]
    private static extern IntPtr dlopen(string fileName, int flags);

    [DllImport(LIBDL)]
    private static extern IntPtr dlsym(IntPtr handle, string symbol);

    [DllImport(LIBDL)]
    private static extern int dlclose(IntPtr handle);

    private const int RTLD_NOW = 2;
#endif

    public static IntPtr Load(string libraryPath)
    {
#if WINDOWS
        var handle = LoadLibrary(libraryPath);
#else
        var handle = dlopen(libraryPath, RTLD_NOW);
#endif
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Unable to load native library: {libraryPath}");
        }

        return handle;
    }

    public static IntPtr GetExport(IntPtr libraryHandle, string name)
    {
#if WINDOWS
        var ptr = GetProcAddress(libraryHandle, name);
#else
        var ptr = dlsym(libraryHandle, name);
#endif
        if (ptr == IntPtr.Zero)
        {
            throw new MissingMethodException($"Export '{name}' not found.");
        }

        return ptr;
    }

    public static void Free(IntPtr libraryHandle)
    {
#if WINDOWS
        FreeLibrary(libraryHandle);
#else
        dlclose(libraryHandle);
#endif
    }
}