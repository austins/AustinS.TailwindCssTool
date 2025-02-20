#pragma warning disable CA1822 // Mark members as static
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
using ConsoleAppFramework;

namespace AustinS.TailwindCssTool;

[RegisterCommands]
internal sealed class Commands
{
    private readonly BinaryManager _binaryManager;
    private readonly BinaryProcessFactory _binaryProcessFactory;

    public Commands(BinaryManager binaryManager, BinaryProcessFactory binaryProcessFactory)
    {
        _binaryManager = binaryManager;
        _binaryProcessFactory = binaryProcessFactory;
    }

    /// <summary>
    /// Install the Tailwind CSS binary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="tailwindVersion">-t, The version of Tailwind CSS to install (e.g. v4.0.0, v3.4.17). If not specified, the latest is installed.</param>
    /// <param name="overwrite">-o, Whether to overwrite an existing Tailwind CSS binary.</param>
    [Command("install")]
    public Task InstallAsync(
        CancellationToken cancellationToken,
        string? tailwindVersion = null,
        bool overwrite = false)
    {
        return _binaryManager.DownloadAsync(tailwindVersion, overwrite, cancellationToken);
    }

    /// <summary>
    /// Generate Tailwind CSS output.
    /// </summary>
    /// <param name="input">-i, The input CSS file path.</param>
    /// <param name="output">-o, The output CSS file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="minify">-m, Whether to minify the output CSS.</param>
    [Command("build")]
    public async Task BuildAsync(string input, string output, CancellationToken cancellationToken, bool minify = false)
    {
        _binaryManager.EnsureBinaryExists();

        using var process = _binaryProcessFactory.Start(input, output, minify);
        await process.WaitForExitAsync(cancellationToken);
    }

    /// <summary>
    /// Watch for changes and generate Tailwind CSS output on any change.
    /// </summary>
    /// <param name="input">-i, The input CSS file path.</param>
    /// <param name="output">-o, The output CSS file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="minify">-m, Whether to minify the output CSS.</param>
    [Command("watch")]
    public async Task WatchAsync(string input, string output, CancellationToken cancellationToken, bool minify = false)
    {
        _binaryManager.EnsureBinaryExists();

        using var process = _binaryProcessFactory.Start(input, output, minify, true);
        await process.WaitForExitAsync(cancellationToken);
    }
}
