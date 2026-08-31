namespace DownloadArchive.Lib;

public class OutputManager(
    string baseDir,
    Action<int, string> log
)
{
    public void GenerateOutput(string inputDir, string runtimeId, string name,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var outputBaseDir = Path.Combine(baseDir, name);

        Directory.CreateDirectory(outputBaseDir);

        string outputDir = Path.Combine(outputBaseDir, runtimeId);

        if (CompletionMarker.IsComplete(outputDir))
        {
            return;
        }

        log(0, $"Coping {inputDir} to {outputDir}");

        var tempDir = DirHelpers.GetTempSiblingPath(outputDir);
        try
        {
            CopyDirectory(inputDir, tempDir, cancellationToken);

            DirHelpers.ReplaceDir(tempDir, outputDir);
        }
        finally
        {
            DirHelpers.DeleteDirIfExists(tempDir);
        }

        CompletionMarker.Create(outputDir);
    }

    static void CopyDirectory(string sourceDir, string destinationDir, CancellationToken cancellationToken = default)
    {
        DirectoryInfo dir = new(sourceDir);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        if (dir.LinkTarget is not null)
        {
            Directory.CreateSymbolicLink(destinationDir, dir.LinkTarget);
        }
        else
        {
            Directory.CreateDirectory(destinationDir);

            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (FileInfo file in dir.GetFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                string targetFilePath = Path.Combine(destinationDir, file.Name);
                if (file.LinkTarget is not null)
                {
                    File.CreateSymbolicLink(targetFilePath, file.LinkTarget);
                }
                else
                {
                    file.CopyTo(targetFilePath);
                }
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, cancellationToken);
            }
        }
    }
}
