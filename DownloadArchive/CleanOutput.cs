using Microsoft.Build.Framework;

namespace DownloadArchive;

public class CleanOutput : Microsoft.Build.Utilities.Task
{
    private bool _isTestProject;

    [Required] public string BaseDir { get; set; } = null!;

    public ITaskItem[] DownloadArchives { get; set; } = [];

    public string? IsTestProject
    {
        get => _isTestProject.ToString();
        set => _isTestProject = value is not null && bool.Parse(value);
    }

    [Required] public string OutputType { get; set; } = null!;

    public override bool Execute()
    {
        if (!_isTestProject && !OutputType.Equals("exe", StringComparison.OrdinalIgnoreCase))
        {
            Log.LogMessage("[{0}] Skipping cleanup to non Exe and non Test projects",
                nameof(CleanOutput));
            return true;
        }

        foreach (ITaskItem archive in DownloadArchives)
        {
            string outputPath = Path.Combine(BaseDir, archive.ItemSpec);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }

        return !Log.HasLoggedErrors;
    }
}