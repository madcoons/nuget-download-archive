using System.Globalization;

namespace DownloadArchive.Lib;

/// <summary>
/// A cached archive, a decompressed archive or a generated output is only considered usable once its
/// marker file exists and the number of files it recorded is still there. The marker is written after
/// everything is in place, so leftovers of an interrupted build are recognized as incomplete and get
/// redone, and so is content that was partially removed afterwards, for example by a temp cleanup.
/// </summary>
public static class CompletionMarker
{
    private const string MarkerPrefix = ".";
    private const string MarkerSuffix = ".complete";

    public static bool IsComplete(string path)
    {
        var markerPath = GetMarkerPath(path);
        if (!File.Exists(markerPath))
        {
            return false;
        }

        if (!Directory.Exists(path) && !File.Exists(path))
        {
            return false;
        }

        return int.TryParse(File.ReadAllText(markerPath), NumberStyles.None, CultureInfo.InvariantCulture,
                   out var expectedFileCount)
               && expectedFileCount == CountFiles(path);
    }

    public static void Create(string path)
    {
        var markerPath = GetMarkerPath(path);
        DirHelpers.EnsureDirExistsForFile(markerPath);

        File.WriteAllText(markerPath, CountFiles(path).ToString(CultureInfo.InvariantCulture));
    }

    public static void Remove(string path)
    {
        var markerPath = GetMarkerPath(path);
        if (File.Exists(markerPath))
        {
            File.Delete(markerPath);
        }
    }

    /// <summary>
    /// Number of files the marker stands for. Markers are not counted, so a marker never depends on
    /// itself or on the marker of anything else.
    /// </summary>
    private static int CountFiles(string path)
    {
        if (Directory.Exists(path))
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count(filePath =>
                !IsMarkerFileName(Path.GetFileName(filePath)));
        }

        return File.Exists(path) ? 1 : 0;
    }

    private static bool IsMarkerFileName(string fileName)
    {
        return fileName.StartsWith(MarkerPrefix, StringComparison.Ordinal)
               && fileName.EndsWith(MarkerSuffix, StringComparison.Ordinal);
    }

    private static string GetMarkerPath(string path)
    {
        var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var dir = Path.GetDirectoryName(trimmedPath) ?? string.Empty;

        return Path.Combine(dir, $"{MarkerPrefix}{Path.GetFileName(trimmedPath)}{MarkerSuffix}");
    }
}
