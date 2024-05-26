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
            Directory.CreateSymbolicLink(outputDir, inputDir);
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

        Directory.CreateDirectory(destinationDir);

        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }
}