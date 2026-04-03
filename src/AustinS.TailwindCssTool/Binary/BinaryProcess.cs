using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AustinS.TailwindCssTool.Binary;

internal sealed partial class BinaryProcess : IBinaryProcess
{
    private readonly Log _log;
    private readonly Process _process;
    private bool _isStarted;

    public BinaryProcess(
        string binaryFilePath,
        string input,
        string output,
        ILogger<BinaryProcess> logger,
        bool minify = false,
        bool watch = false)
    {
        _log = new Log(logger);

        // Set up the arguments.
        var arguments = $"-i {input} -o {output}";
        if (minify)
        {
            arguments += " --minify";
        }

        if (watch)
        {
            arguments += " --watch";
        }

        // Initialize the process.
        _process = new Process
        {
            StartInfo = new ProcessStartInfo(binaryFilePath, arguments)
            {
                WorkingDirectory = Environment.CurrentDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        _process.OutputDataReceived += LogOutput;
        _process.ErrorDataReceived += LogOutput;
    }

    public void Start()
    {
        if (!_isStarted)
        {
            _process.Start();
            _isStarted = true;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
    }

    public async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (_isStarted)
        {
            await _process.WaitForExitAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        if (_isStarted && !_process.HasExited)
        {
            _process.Kill();
            _isStarted = false;
        }

        _process.Dispose();
    }

    private void LogOutput(object _, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            _log.Output(e.Data);
        }
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Tailwind CSS: {output}")]
        public partial void Output(string output);
    }
}

/// <summary>
/// A process of the Tailwind CSS standalone CLI binary.
/// </summary>
internal interface IBinaryProcess : IDisposable
{
    /// <summary>
    /// Start the process.
    /// </summary>
    public void Start();

    /// <summary>
    /// Wait for the process to exit gracefully or a cancellation is requested, then dispose of the process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task WaitForExitAsync(CancellationToken cancellationToken);
}
