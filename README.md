# DownlaodArchive
This is a Nuget package a which contains custom MSBuild Tasks that allow downloading archives to build and publish directories.

The following example will download geckodriver to output `gecko-driver-0.34.0/(linux-x64|osx-arm64|win-x64)/geckodriver(.exe)?` depending on `RuntimeIdentifier`:
```csproj
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
    </PropertyGroup>

    <ItemGroup>
        <DownloadArchive
                Include="gecko-driver-0.34.0"
                Visible="false"
                RID-linux-x64="https://github.com/mozilla/geckodriver/releases/download/v0.34.0/geckodriver-v0.34.0-linux64.tar.gz"
                RID-osx-arm64="https://github.com/mozilla/geckodriver/releases/download/v0.34.0/geckodriver-v0.34.0-macos-aarch64.tar.gz"
                RID-win-x64="https://github.com/mozilla/geckodriver/releases/download/v0.34.0/geckodriver-v0.34.0-win32.zip"
        />
    </ItemGroup>

    <ItemGroup>
      <PackageReference Include="DownloadArchive" Version="1.0.7" />
    </ItemGroup>
</Project>
```

## Caching

Downloaded archives and their decompressed content are cached, so repeated builds do not download the same archive again. By default the cache lives in `nuget-download-archive` inside the temp directory, so it is cleaned up by the system and does not grow forever.

Set `DownloadArchiveCacheDir` to keep it somewhere else, for example inside the project so it can be restored by a CI cache, keeping in mind that such a location has to be cleaned up by yourself:

```csproj
<PropertyGroup>
    <DownloadArchiveCacheDir>obj/download-archive-cache</DownloadArchiveCacheDir>
</PropertyGroup>
```

Relative paths are resolved against the project directory.

## Interrupted builds

Every cached archive, decompressed archive and generated output directory has a marker file (`.<name>.complete`) next to it, which is written only after everything is in place and holds the number of files it stands for, not counting markers themselves. Content is used only when its marker is there and the file count still matches, so a leftover of an interrupted build, or content that lost files afterwards to a temp cleanup, is downloaded, decompressed or copied again.

Content is also always written to a temporary location first and moved into its final place as the last step, so an interrupted build cannot leave partial content behind for the next one to pick up.
