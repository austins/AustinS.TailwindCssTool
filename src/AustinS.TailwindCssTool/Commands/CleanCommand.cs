using AustinS.TailwindCssTool.Binary;
using ConsoleAppFramework;

namespace AustinS.TailwindCssTool.Commands;

[RegisterCommands]
internal sealed class CleanCommand
{
    private readonly IBinaryManager _binaryManager;

    public CleanCommand(IBinaryManager binaryManager)
    {
        _binaryManager = binaryManager;
    }

    /// <summary>
    /// Clean downloaded Tailwind CSS binaries.
    /// </summary>
    [Command("clean")]
    public void Handle()
    {
        _binaryManager.CleanDownloads();
    }
}
