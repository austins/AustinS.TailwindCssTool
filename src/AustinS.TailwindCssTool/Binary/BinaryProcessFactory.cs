using Microsoft.Extensions.Logging;

namespace AustinS.TailwindCssTool.Binary;

internal sealed class BinaryProcessFactory
{
    private readonly BinaryManager _binaryManager;
    private readonly ILoggerFactory _loggerFactory;

    public BinaryProcessFactory(BinaryManager binaryManager, ILoggerFactory loggerFactory)
    {
        _binaryManager = binaryManager;
        _loggerFactory = loggerFactory;
    }

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
