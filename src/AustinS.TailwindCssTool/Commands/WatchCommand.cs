using AustinS.TailwindCssTool.Binary;
using ConsoleAppFramework;

namespace AustinS.TailwindCssTool.Commands;

[RegisterCommands]
internal sealed class WatchCommand
{
    private readonly BinaryManager _binaryManager;
    private readonly BinaryProcessFactory _binaryProcessFactory;

    public WatchCommand(BinaryManager binaryManager, BinaryProcessFactory binaryProcessFactory)
    {
        _binaryManager = binaryManager;
        _binaryProcessFactory = binaryProcessFactory;
    }

    /// <summary>
    /// Watch for changes and generate Tailwind CSS output on any change.
    /// </summary>
    /// <param name="input">-i, The input CSS file path.</param>
    /// <param name="output">-o, The output CSS file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="minify">-m, Whether to minify the output CSS.</param>
    /// <param name="tailwindVersion">-t, The version of Tailwind CSS to install (e.g. v4.0.0, v3.4.17). If not specified, the latest is installed.</param>
    [Command("watch")]
    public async Task HandleAsync(
        string input,
        string output,
        CancellationToken cancellationToken,
        bool minify = false,
        string? tailwindVersion = null)
    {
        var binaryFilePath = await _binaryManager.EnsureDownloadedAsync(tailwindVersion, cancellationToken);

        using var process = _binaryProcessFactory.Create(binaryFilePath, input, output, minify, true);
        process.Start();

        await process.WaitForExitAsync(cancellationToken);
    }
}
