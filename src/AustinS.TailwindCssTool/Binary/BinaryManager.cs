using AustinS.TailwindCssTool.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace AustinS.TailwindCssTool.Binary;

/// <summary>
/// Manages the binary for the Tailwind CSS standalone CLI.
/// </summary>
internal sealed partial class BinaryManager
{
    /// <summary>
    /// The base URL for the download on GitHub. Must end with a trailing slash.
    /// </summary>
    private static readonly Uri GitHubBaseUrl = new("https://github.com/tailwindlabs/tailwindcss/releases/");

    private readonly string _binaryFileName;
    private readonly Log _log;

    public BinaryManager(ILogger<BinaryManager> logger)
    {
        _binaryFileName = DetermineBinaryFileName();
        BinaryFilePath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)!, "binaries", _binaryFileName);
        _log = new Log(logger);
    }

    /// <summary>
    /// The path that the binary will be/is saved to.
    /// </summary>
    public string BinaryFilePath { get; }

    /// <summary>
    /// Check if the binary exists on the file system and throws if it doesn't.
    /// </summary>
    /// <exception cref="BinaryNotFoundException">Tailwind CSS binary not found.</exception>
    public void EnsureBinaryExists()
    {
        if (!File.Exists(BinaryFilePath))
        {
            throw new BinaryNotFoundException("Tailwind CSS binary not found. Use the install command first.");
        }
    }

    /// <summary>
    /// Download the Tailwind CSS standalone CLI binary.
    /// </summary>
    /// <param name="version">Optional version to download. Latest if not specified.</param>
    /// <param name="overwrite">Whether to overwrite an existing download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DownloadAsync(string? version, bool overwrite, CancellationToken cancellationToken)
    {
        ThrowIfInvalidVersion(version);

        // Check if binary exists already.
        if (File.Exists(BinaryFilePath))
        {
            _log.Exists();

            // Do not download if overwriting is not allowed.
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

        // If the OS is Linux or MacOS, we need to make the binary executable in order to run it.
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            using var chmodProcess = Process.Start("chmod", $"+x {BinaryFilePath}");
            await chmodProcess.WaitForExitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Determine the binary file to download based on the operating system.
    /// </summary>
    /// <returns>Binary file name for the operating system.</returns>
    /// <exception cref="UnsupportedOperatingSystemException">The operating system running this app is not supported.</exception>
    private static string DetermineBinaryFileName()
    {
        var osArch = RuntimeInformation.OSArchitecture;

#pragma warning disable CA1308
        var osArchIdentifier = osArch.ToString().ToLowerInvariant();
#pragma warning restore CA1308

        if (osArch is not (Architecture.X64 or Architecture.Arm64))
        {
            throw new UnsupportedOperatingSystemException(
                $"Unsupported operating system architecture. Only {Architecture.X64} and {Architecture.Arm64} are supported.");
        }

        if (OperatingSystem.IsWindows() && osArch is Architecture.X64)
        {
            return $"tailwindcss-windows-{osArchIdentifier}.exe";
        }

        if (OperatingSystem.IsLinux())
        {
            var linuxBinaryFileName = $"tailwindcss-linux-{osArchIdentifier}";

            // If the OS uses the musl library instead of glibc, download the binary that supports musl.
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

        throw new UnsupportedOperatingSystemException(
            "Unsupported operating system. Only Windows, Linux, and MacOS are supported.");
    }

    /// <summary>
    /// Validate that the binary version requested is in a valid format or throw.
    /// </summary>
    /// <param name="version">The version requested. May be null for latest.</param>
    /// <exception cref="ArgumentException">Invalid version string.</exception>
    private static void ThrowIfInvalidVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return;
        }

#pragma warning disable CA1308
        if (!version.Trim().ToLowerInvariant().StartsWith('v'))
#pragma warning restore CA1308
        {
            throw new ArgumentException("Invalid version. It must be in a format such as v4.0.0.", nameof(version));
        }
    }

    /// <summary>
    /// Get the full download URL for the Tailwind CSS standalone CLI binary.
    /// </summary>
    /// <param name="version">The version to download. May be null for latest.</param>
    /// <returns>Full download URL for the Tailwind CSS standalone CLI binary.</returns>
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
