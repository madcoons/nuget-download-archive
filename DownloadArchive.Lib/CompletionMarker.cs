namespace DownloadArchive.Lib;

/// <summary>
/// A cached archive, a decompressed archive or a generated output is only considered usable
/// once its marker file exists. The marker is written after everything is in place, so leftovers
/// of an interrupted build are recognized as incomplete and get redone instead of being used.
/// </summary>
public static class CompletionMarker
{
    public static bool Exists(string path)
    {
        return File.Exists(GetMarkerPath(path));
    }

    public static void Create(string path)
    {
        var markerPath = GetMarkerPath(path);
        DirHelpers.EnsureDirExistsForFile(markerPath);

        File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
    }

    public static void Remove(string path)
    {
        var markerPath = GetMarkerPath(path);
        if (File.Exists(markerPath))
        {
            File.Delete(markerPath);
        }
    }

    private static string GetMarkerPath(string path)
    {
        var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var dir = Path.GetDirectoryName(trimmedPath) ?? string.Empty;

        return Path.Combine(dir, $".{Path.GetFileName(trimmedPath)}.complete");
    }
}
