using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace AustinS.TailwindCssTool.Binary;

internal sealed partial class BinaryManager
{
    private static readonly Uri GitHubBaseUrl = new("https://github.com/tailwindlabs/tailwindcss/releases/");
    private readonly string _binaryFileName;
    private readonly Log _log;

    public BinaryManager(ILogger<BinaryManager> logger)
    {
        _binaryFileName = DetermineBinaryFileName();
        BinaryFilePath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)!, "binaries", _binaryFileName);
        _log = new Log(logger);
    }

    public string BinaryFilePath { get; }

    public void EnsureBinaryExists()
    {
        if (!File.Exists(BinaryFilePath))
        {
            throw new InvalidOperationException("Tailwind CSS binary not found. Use the install command first.");
        }
    }

    public async Task DownloadAsync(string? version, bool overwrite, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(version))
        {
#pragma warning disable CA1308
            version = version.Trim().ToLowerInvariant();
#pragma warning restore CA1308

            if (!version.StartsWith('v'))
            {
                throw new InvalidOperationException("Invalid version. It must be in a format such as v4.0.0.");
            }
        }

        if (File.Exists(BinaryFilePath))
        {
            _log.Exists();

            if (!overwrite)
            {
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(BinaryFilePath)!);

        var url = GetDownloadUrl(version);
        _log.Downloading(url);

        using var httpClient = new HttpClient();
        try
        {
            var fileBytes = await httpClient.GetByteArrayAsync(url, cancellationToken);
            await File.WriteAllBytesAsync(BinaryFilePath, fileBytes, cancellationToken);
            _log.Success();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.NotFound)
            {
                _log.NotFound(url);
            }
            else
            {
                _log.Failure(url);
            }

            throw;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            using var chmodProcess = Process.Start("chmod", $"+x {BinaryFilePath}");
            await chmodProcess.WaitForExitAsync(cancellationToken);
        }
    }

    private static string DetermineBinaryFileName()
    {
        var osArch = RuntimeInformation.OSArchitecture;

#pragma warning disable CA1308
        var osArchIdentifier = osArch.ToString().ToLowerInvariant();
#pragma warning restore CA1308

        if (osArch is not (Architecture.X64 or Architecture.Arm64))
        {
            throw new InvalidOperationException(
                $"Unsupported operating system architecture. Only {Architecture.X64} and {Architecture.Arm64} are supported.");
        }

        if (OperatingSystem.IsWindows() && osArch is Architecture.X64)
        {
            return $"tailwindcss-windows-{osArchIdentifier}.exe";
        }

        if (OperatingSystem.IsLinux())
        {
            var linuxBinaryFileName = $"tailwindcss-linux-{osArchIdentifier}";
            if (File.Exists("/lib/ld-musl-x86_64.so.1") || File.Exists("/lib/libc.musl-x86_64.so.1"))
            {
                linuxBinaryFileName += "-musl";
            }

            return linuxBinaryFileName;
        }

        if (OperatingSystem.IsMacOS())
        {
            return $"tailwindcss-macos-{osArchIdentifier}";
        }

        throw new InvalidOperationException(
            "Unsupported operating system. Only Windows, Linux, and MacOS are supported.");
    }

    private Uri GetDownloadUrl(string? version)
    {
        var gitHubPath = string.IsNullOrWhiteSpace(version) ? "latest/download" : $"download/{version}";
        return new Uri(GitHubBaseUrl, $"{gitHubPath}/{_binaryFileName}");
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "A Tailwind CSS binary already exists.")]
        public partial void Exists();

        [LoggerMessage(Level = LogLevel.Information, Message = "Downloading latest Tailwind CSS binary from: {url}")]
        public partial void Downloading(Uri url);

        [LoggerMessage(Level = LogLevel.Error, Message = "Tailwind CSS binary was not found at: {url}")]
        public partial void NotFound(Uri url);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to download Tailwind CSS binary from: {url}")]
        public partial void Failure(Uri url);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully downloaded the Tailwind CSS binary.")]
        public partial void Success();
    }
}
