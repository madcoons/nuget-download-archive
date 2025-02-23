using System.Runtime.CompilerServices;

namespace DownloadArchive;

public class OutputManager(
    string baseDir,
    bool useLinks
)
{
    public void GenerateOutput(string inputDir, string runtimeId, string name)
    {
        string outputBaseDir = Path.Combine(baseDir, name);

        if (!Directory.Exists(outputBaseDir))
        {
            Directory.CreateDirectory(outputBaseDir);
        }

        string outputDir = Path.Combine(outputBaseDir, runtimeId);

        if (Directory.Exists(outputDir))
        {
            return;
        }

        if (useLinks)
        {
            _DirectoryCreateSymbolicLinkImpl(outputDir, inputDir);
        }
        else
        {
            CopyDirectory(inputDir, outputDir);
        }
    }

    static void CopyDirectory(string sourceDir, string destinationDir)
    {
        DirectoryInfo dir = new(sourceDir);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        string? dirLinkTarget = _DirectoryLinkTargetImpl(dir);
        if (dirLinkTarget is not null)
        {
            _DirectoryCreateSymbolicLinkImpl(destinationDir, dirLinkTarget);
        }
        else
        {
            Directory.CreateDirectory(destinationDir);

            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                string? fileLinkTarget = _FileLinkTargetImpl(file);

                if (fileLinkTarget is not null)
                {
                    _FileCreateSymbolicLinkImpl(targetFilePath, fileLinkTarget);
                }
                else
                {
                    file.CopyTo(targetFilePath);
                }
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void _DirectoryCreateSymbolicLinkImpl(string path, string pathToTarget) => Directory.CreateSymbolicLink(path, pathToTarget);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void _FileCreateSymbolicLinkImpl(string path, string pathToTarget) => File.CreateSymbolicLink(path, pathToTarget);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? _DirectoryLinkTargetImpl(DirectoryInfo fileInfo) => fileInfo.LinkTarget;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? _FileLinkTargetImpl(FileInfo directoryInfo) => directoryInfo.LinkTarget;
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void _DirectoryCreateSymbolicLinkImpl(string path, string pathToTarget) =>
        Alphaleonis.Win32.Filesystem.Directory.CreateSymbolicLink(path, pathToTarget);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void _FileCreateSymbolicLinkImpl(string path, string pathToTarget) =>
        Alphaleonis.Win32.Filesystem.File.CreateSymbolicLink(path, pathToTarget);

    private static string? _DirectoryLinkTargetImpl(DirectoryInfo directoryInfo)
    {
        if (Alphaleonis.Win32.Filesystem.File.Exists(directoryInfo.FullName) &&
            Alphaleonis.Win32.Filesystem.File.GetAttributes(directoryInfo.FullName).HasFlag(FileAttributes.ReparsePoint))
        {
            return Alphaleonis.Win32.Filesystem.File.GetLinkTargetInfo(directoryInfo.FullName)?.PrintName;
        }

        return null;
    }

    private static string? _FileLinkTargetImpl(FileInfo fileInfo)
    {
        if (Alphaleonis.Win32.Filesystem.File.Exists(fileInfo.FullName) &&
            Alphaleonis.Win32.Filesystem.File.GetAttributes(fileInfo.FullName).HasFlag(FileAttributes.ReparsePoint))
        {
            return Alphaleonis.Win32.Filesystem.File.GetLinkTargetInfo(fileInfo.FullName)?.PrintName;
        }

        return null;
    }
#endif
}