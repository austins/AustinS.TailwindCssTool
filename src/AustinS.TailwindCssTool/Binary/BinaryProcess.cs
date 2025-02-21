using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AustinS.TailwindCssTool.Binary;

/// <summary>
/// A process of the Tailwind CSS standalone CLI binary.
/// </summary>
internal sealed partial class BinaryProcess : IDisposable
{
    private readonly Log _log;
    private Process? _process;

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

        // Start the process.
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    /// <summary>
    /// Wait for the process to exit gracefully or a cancellation is requested, then dispose of the process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (_process?.HasExited == false)
        {
            await _process.WaitForExitAsync(cancellationToken);
            Dispose();
        }
    }

    /// <summary>
    /// Dispose of the process instance if it exists.
    /// </summary>
    public void Dispose()
    {
        if (_process is not null)
        {
            _process.Kill();
            _process.Dispose();
            _process = null;
        }
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
