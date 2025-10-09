namespace DownloadArchive.Lib;

public static class DirHelpers
{
    public static void EnsureDirExistsForFile(string path)
    {
        var lockDir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(lockDir))
        {
            Directory.CreateDirectory(lockDir);
        }
    }
}