using Microsoft.Extensions.Logging;

namespace AustinS.TailwindCssTool.Binary;

/// <summary>
/// Factory for creating a process of the Tailwind CSS standalone CLI binary.
/// </summary>
internal sealed class BinaryProcessFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public BinaryProcessFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Create a process of the Tailwind CSS standalone CLI binary.
    /// </summary>
    /// <param name="binaryFilePath">Path to the binary file.</param>
    /// <param name="input">The input CSS file path.</param>
    /// <param name="output">The output CSS file path.</param>
    /// <param name="minify">Whether to minify the output CSS.</param>
    /// <param name="watch">Whether to watch for changes and generate Tailwind CSS output on any change.</param>
    /// <returns>New binary process.</returns>
    public BinaryProcess Create(
        string binaryFilePath,
        string input,
        string output,
        bool minify = false,
        bool watch = false)
    {
        return new BinaryProcess(
            binaryFilePath,
            input,
            output,
            _loggerFactory.CreateLogger<BinaryProcess>(),
            minify,
            watch);
    }
}
