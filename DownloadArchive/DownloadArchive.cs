using Microsoft.Build.Framework;

namespace DownloadArchive;

public class DownloadArchive : Microsoft.Build.Utilities.Task
{
    private readonly ArchiveDownloader _archiveDownloader = new();

    private bool _isTestProject;
    private bool _useSymLinks;

    [Required] public string BaseDir { get; set; } = null!;

    public ITaskItem[] DownloadArchives { get; set; } = [];

    public string? IsTestProject
    {
        get => _isTestProject.ToString();
        set => _isTestProject = value is not null && bool.Parse(value);
    }

    [Required] public string OutputType { get; set; } = null!;
    [Required] public string PackageRoot { get; set; } = null!;
    public string? RuntimeIdentifier { get; set; }

    [Required]
    public string UseSymLinks
    {
        get => _useSymLinks.ToString();
        set => _useSymLinks = bool.Parse(value);
    }

    public override bool Execute()
    {
        using Mutex mutex = new(false, "Global\\DownloadArchiveNuget");
        mutex.WaitOne();
        try
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private async Task<bool> ExecuteAsync()
    {
        if (!_isTestProject && !OutputType.Equals("exe", StringComparison.OrdinalIgnoreCase))
        {
            Log.LogMessage("[{0}] Skipping downloading archive to non Exe and non Test projects",
                nameof(DownloadArchive));
            return true;
        }

        ArchiveCacher archiveCacher = new(PackageRoot);
        ArchiveDecompressor archiveDecompressor = new(PackageRoot);
        OutputManager outputManager = new(BaseDir, _useSymLinks);

        DownloadArchiveDto[] archives = GetDownloadLinksByArchitecture();

        foreach (DownloadArchiveDto archive in archives)
        {
            foreach ((string runtimeId, string url) in archive.RuntimeIdToUrlMap)
            {
                if (RuntimeIdentifier is not null && !RuntimeIdentifier.Equals(runtimeId))
                {
                    continue;
                }

                string cachePath = archiveCacher.GetCachePath(url);
                if (!File.Exists(cachePath))
                {
                    await using Stream archiveStream = await _archiveDownloader.DownlaodAsync(url);
                    await archiveCacher.CacheAsync(archiveStream, url);
                }

                string outputDir = await archiveDecompressor.DecompressAsync(cachePath, url);

                outputManager.GenerateOutput(outputDir, runtimeId, archive.ItemSpec);
            }
        }

        return !Log.HasLoggedErrors;
    }

    private DownloadArchiveDto[] GetDownloadLinksByArchitecture()
        => DownloadArchives.Select(downloadArchive =>
            {
                const string ridPrefix = "RID-";

                return new DownloadArchiveDto(
                    ItemSpec: downloadArchive.ItemSpec,
                    RuntimeIdToUrlMap: downloadArchive.MetadataNames
                        .Cast<object>()
                        .Select(x => x.ToString())
                        .Where(x => x is not null)
                        .Select(x => x ?? throw new("Null should be excluded."))
                        .Where(x => x.StartsWith(ridPrefix))
                        .ToDictionary(x => x[ridPrefix.Length..], downloadArchive.GetMetadata)
                );
            })
            .ToArray();
}