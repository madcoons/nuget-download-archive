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
