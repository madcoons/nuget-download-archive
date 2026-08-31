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

Downloaded archives and their decompressed content are cached, so repeated builds do not download the same archive again. The cache lives in `nuget-download-archive` inside the temp directory, so it is cleaned up by the system and does not grow forever.

## Interrupted builds

Every cached archive, decompressed archive and generated output directory has a marker file (`.<name>.complete`) next to it, which is written only after everything is in place and holds the number of files it stands for, not counting markers themselves. Content is used only when its marker is there and the file count still matches, so a leftover of an interrupted build, or content that lost files afterwards to a temp cleanup, is downloaded, decompressed or copied again.

The downloaded archive and the copy into the build output are written beside their destination and moved onto it as a last step, so a reader never meets them half written.

## Concurrent builds

Cache entries are guarded by lock files, one for the download and one for the decompressed copy, so builds running at the same time share the work instead of repeating or corrupting it: the first to arrive downloads, the rest wait and then use what it left.
