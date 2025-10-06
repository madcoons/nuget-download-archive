using System.CommandLine;

namespace DownloadArchive.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Handle build")
    {
        Option<string> targetDir = new("--target-dir")
        {
            Description = "Target directory for output."
        };
        Add(targetDir);

        Option<string> name = new("--name")
        {
            Description = "Name of the archive."
        };
        Add(name);

        Option<string> rid = new("--rid")
        {
            Description = "Runtime Identifier."
        };
        Add(rid);

        Option<string> url = new("--url")
        {
            Description = "Archive URL."
        };
        Add(url);

        Option<string> packageRoot = new("--package-root")
        {
            Description = "Package root."
        };
        Add(packageRoot);

        Option<bool> useSymLinks = new("--use-sym-links")
        {
            Description = "Use symbolic links instead of copying files."
        };
        Add(useSymLinks);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var targetDirValue = parseResult.GetRequiredValue(targetDir);
            var nameValue = parseResult.GetRequiredValue(name);
            var packageRootValue = parseResult.GetRequiredValue(packageRoot);
            var useSymLinksValue = parseResult.GetRequiredValue(useSymLinks);
            var ridValue = parseResult.GetRequiredValue(rid);
            var urlValue = parseResult.GetRequiredValue(url);

            ArchiveCacher archiveCacher = new(packageRootValue);
            ArchiveDecompressor archiveDecompressor = new(packageRootValue);
            OutputManager outputManager = new(targetDirValue, useSymLinksValue);
            ArchiveDownloader archiveDownloader = new();

            var lockPath = archiveCacher.GetCachePath(urlValue) + "_lock";
            var lockDir = Path.GetDirectoryName(lockPath);
            if (!string.IsNullOrEmpty(lockDir))
            {
                Directory.CreateDirectory(lockDir);
            }

            await using var s2 = new FileStream(
                path: lockPath,
                mode: FileMode.OpenOrCreate,
                access: FileAccess.ReadWrite,
                share: FileShare.None
            );

            var cachePath = archiveCacher.GetCachePath(urlValue);
            if (!File.Exists(cachePath))
            {
                await using var archiveStream = await archiveDownloader.DownloadAsync(urlValue);
                await archiveCacher.CacheAsync(archiveStream, urlValue, cancellationToken);
            }

            var decompressedDir = await archiveDecompressor.DecompressAsync(cachePath, urlValue, cancellationToken);

            outputManager.GenerateOutput(decompressedDir, ridValue, nameValue, cancellationToken);
        });
    }
}