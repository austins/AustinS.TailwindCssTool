using Microsoft.Extensions.Logging;

namespace AustinS.TailwindCssTool.Binary;

/// <summary>
/// Factory for creating a process of the Tailwind CSS standalone CLI binary.
/// </summary>
internal sealed class BinaryProcessFactory
{
    private readonly BinaryManager _binaryManager;
    private readonly ILoggerFactory _loggerFactory;

    public BinaryProcessFactory(BinaryManager binaryManager, ILoggerFactory loggerFactory)
    {
        _binaryManager = binaryManager;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Create and start a process of the Tailwind CSS standalone CLI binary.
    /// </summary>
    /// <param name="input">The input CSS file path.</param>
    /// <param name="output">The output CSS file path.</param>
    /// <param name="minify">Whether to minify the output CSS.</param>
    /// <param name="watch">Whether to watch for changes and generate Tailwind CSS output on any change.</param>
    /// <returns>New started binary process.</returns>
    public BinaryProcess Start(string input, string output, bool minify = false, bool watch = false)
    {
        _binaryManager.EnsureBinaryExists();

        return new BinaryProcess(
            _binaryManager.BinaryFilePath,
            input,
            output,
            _loggerFactory.CreateLogger<BinaryProcess>(),
            minify,
            watch);
    }
}
