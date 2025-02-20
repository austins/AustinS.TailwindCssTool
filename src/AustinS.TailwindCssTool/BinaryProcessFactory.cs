using Microsoft.Extensions.Logging;

namespace AustinS.TailwindCssTool;

internal sealed class BinaryProcessFactory
{
    private readonly string _binaryFilePath;
    private readonly ILoggerFactory _loggerFactory;

    public BinaryProcessFactory(BinaryManager binaryManager, ILoggerFactory loggerFactory)
    {
        _binaryFilePath = binaryManager.BinaryFilePath;
        _loggerFactory = loggerFactory;
    }

    public BinaryProcess Start(string input, string output, bool minify = false, bool watch = false)
    {
        return new BinaryProcess(
            _binaryFilePath,
            input,
            output,
            _loggerFactory.CreateLogger<BinaryProcess>(),
            minify,
            watch);
    }
}
