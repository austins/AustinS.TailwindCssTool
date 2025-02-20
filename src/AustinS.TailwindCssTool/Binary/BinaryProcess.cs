using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AustinS.TailwindCssTool.Binary;

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

        var arguments = $"-i {input} -o {output}";
        if (minify)
        {
            arguments += " --minify";
        }

        if (watch)
        {
            arguments += " --watch";
        }

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

        _process.OutputDataReceived += (_, e) => LogOutput(e.Data);
        _process.ErrorDataReceived += (_, e) => LogOutput(e.Data);

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (_process?.HasExited == false)
        {
            await _process.WaitForExitAsync(cancellationToken);
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            _process.Kill();
            _process.Dispose();
            _process = null;
        }
    }

    private void LogOutput(string? output)
    {
        if (!string.IsNullOrWhiteSpace(output))
        {
            _log.Output(output);
        }
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Tailwind CSS: {output}")]
        public partial void Output(string output);
    }
}
