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

    /// <summary>
    /// Path of a temporary sibling of <paramref name="path"/>, used to build content up before it
    /// is moved into its final place.
    /// </summary>
    public static string GetTempSiblingPath(string path)
    {
        return $"{path}.{Guid.NewGuid():N}.tmp";
    }

    public static void DeleteDirIfExists(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>
    /// Moves fully prepared content over <paramref name="destinationDir"/>, replacing anything left
    /// there by an interrupted run. The move is the last step, so the destination is never half written.
    /// </summary>
    public static void ReplaceDir(string tempDir, string destinationDir)
    {
        CompletionMarker.Remove(destinationDir);
        DeleteDirIfExists(destinationDir);
        EnsureDirExistsForFile(destinationDir);

        Directory.Move(tempDir, destinationDir);
    }
}
