using System.CommandLine;
using DownloadArchive.Commands;

RootCommand rootCommand = new("DownloadArchive cli")
{
    new BuildCommand(),
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();