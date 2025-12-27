using AustinS.TailwindCssTool.Binary;
using ConsoleAppFramework;

namespace AustinS.TailwindCssTool.Commands;

[RegisterCommands]
internal sealed class CleanCommand
{
    private readonly BinaryManager _binaryManager;

    public CleanCommand(BinaryManager binaryManager)
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
