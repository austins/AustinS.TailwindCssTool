using AustinS.TailwindCssTool.Binary;
using ConsoleAppFramework;

namespace AustinS.TailwindCssTool.Commands;

[RegisterCommands]
internal sealed class WatchCommand
{
    private readonly BinaryProcessFactory _binaryProcessFactory;

    public WatchCommand(BinaryProcessFactory binaryProcessFactory)
    {
        _binaryProcessFactory = binaryProcessFactory;
    }

    /// <summary>
    /// Watch for changes and generate Tailwind CSS output on any change.
    /// </summary>
    /// <param name="input">-i, The input CSS file path.</param>
    /// <param name="output">-o, The output CSS file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="minify">-m, Whether to minify the output CSS.</param>
    [Command("watch")]
    public async Task HandleAsync(string input, string output, CancellationToken cancellationToken, bool minify = false)
    {
        using var process = _binaryProcessFactory.Create(input, output, minify, true);
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
    }
}
