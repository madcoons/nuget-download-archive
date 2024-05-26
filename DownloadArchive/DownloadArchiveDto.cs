namespace DownloadArchive;

public record DownloadArchiveDto(
    string ItemSpec,
    IDictionary<string, string> RuntimeIdToUrlMap
);