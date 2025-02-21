using AustinS.TailwindCssTool.Binary;
using ConsoleAppFramework;

namespace AustinS.TailwindCssTool.Commands;

[RegisterCommands]
internal sealed class BuildCommand
{
    private readonly BinaryProcessFactory _binaryProcessFactory;

    public BuildCommand(BinaryProcessFactory binaryProcessFactory)
    {
        _binaryProcessFactory = binaryProcessFactory;
    }

    /// <summary>
    /// Generate Tailwind CSS output.
    /// </summary>
    /// <param name="input">-i, The input CSS file path.</param>
    /// <param name="output">-o, The output CSS file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="minify">-m, Whether to minify the output CSS.</param>
    [Command("build")]
    public async Task HandleAsync(string input, string output, CancellationToken cancellationToken, bool minify = false)
    {
        using var process = _binaryProcessFactory.Start(input, output, minify);
        await process.WaitForExitAsync(cancellationToken);
    }
}
