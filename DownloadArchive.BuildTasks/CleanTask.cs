using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace DownloadArchive.BuildTasks;

public class CleanTask : Task
{
    [Required] public string TargetDir { get; set; } = null!;
    public bool IsTestProject { get; set; }
    public string? OutputType { get; set; }
    [Required] public ITaskItem[] InputItems { get; set; } = null!;

    public override bool Execute()
    {
        if (!IsTestProject && !string.Equals(OutputType, "exe", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var item in InputItems)
        {
            var outputPath = Path.Combine(TargetDir, item.ItemSpec);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }

        return true;
    }
}