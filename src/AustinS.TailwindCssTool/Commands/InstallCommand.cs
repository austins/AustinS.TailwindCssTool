using ConsoleAppFramework;

namespace AustinS.TailwindCssTool.Commands;

[RegisterCommands]
internal sealed class InstallCommand
{
    private readonly Binary.BinaryManager _binaryManager;

    public InstallCommand(Binary.BinaryManager binaryManager)
    {
        _binaryManager = binaryManager;
    }

    /// <summary>
    /// Install the Tailwind CSS binary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="tailwindVersion">-t, The version of Tailwind CSS to install (e.g. v4.0.0, v3.4.17). If not specified, the latest is installed.</param>
    /// <param name="overwrite">-o, Whether to overwrite an existing Tailwind CSS binary.</param>
    [Command("install")]
    public Task HandleAsync(CancellationToken cancellationToken, string? tailwindVersion = null, bool overwrite = false)
    {
        return _binaryManager.DownloadAsync(tailwindVersion, overwrite, cancellationToken);
    }
}
