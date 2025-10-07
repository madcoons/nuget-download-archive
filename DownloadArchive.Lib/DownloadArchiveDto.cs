namespace DownloadArchive.Lib;

public record DownloadArchiveDto(
    string ItemSpec,
    IDictionary<string, string> RuntimeIdToUrlMap
);