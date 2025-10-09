using System.Runtime.InteropServices;

namespace DownloadArchive.Lib;

public static class RuntimeInformationHelpers
{
    public static string GetRunningRID()
    {
        string osPart;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            osPart = "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            osPart = "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            osPart = "osx";
        }
        else
        {
            throw new Exception("Unsupported OS.");
        }

        var archPart = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

        var rid = $"{osPart}-{archPart}";
        return rid;
    }
}