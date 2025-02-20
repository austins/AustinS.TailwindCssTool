#pragma warning disable CA1822 // Mark members as static
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
using ConsoleAppFramework;

namespace AustinS.TailwindCssTool;

[RegisterCommands]
internal sealed class Commands
{
    private readonly BinaryManager _binaryManager;

    public Commands(BinaryManager binaryManager)
    {
        _binaryManager = binaryManager;
    }

    /// <summary>
    /// Install the Tailwind CSS binary.
    /// </summary>
    /// <param name="version">The version of Tailwind CSS to install. If not specified, the latest is installed.</param>
    [Command("install")]
    public Task InstallAsync([Argument] string? version = null)
    {
        return _binaryManager.DownloadAsync(CancellationToken.None, version);
    }

    /// <summary>
    /// Generate Tailwind CSS output.
    /// </summary>
    /// <param name="input">The input CSS file path.</param>
    /// <param name="output">The output CSS file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="minify">Whether to minify the output CSS.</param>
    [Command("build")]
    public void Build(
        [Argument] string input,
        [Argument] string output,
        CancellationToken cancellationToken,
        bool minify = false)
    {
        _binaryManager.EnsureBinaryExists();

        Console.Write("hello");
    }

    /// <summary>
    /// Watch for changes and generate Tailwind CSS output on any change.
    /// </summary>
    /// <param name="input">The input CSS file path.</param>
    /// <param name="output">The output CSS file path.</param>
    /// <param name="minify">Whether to minify the output CSS.</param>
    [Command("watch")]
    public void Watch([Argument] string input, [Argument] string output, bool minify = false)
    {
        _binaryManager.EnsureBinaryExists();

        Console.Write("hello");
    }
}
