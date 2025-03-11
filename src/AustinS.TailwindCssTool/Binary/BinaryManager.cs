using AustinS.TailwindCssTool.Exceptions;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace AustinS.TailwindCssTool.Binary;

/// <summary>
/// Manages the binary for the Tailwind CSS standalone CLI.
/// </summary>
internal sealed partial class BinaryManager
{
    public const string HttpClientName = nameof(BinaryManager);
    private const string GitHubOwnerName = "tailwindlabs";
    private const string GitHubRepoName = "tailwindcss";
    private readonly string _binaryFileName;

    /// <summary>
    /// The path to the binaries directory that resides next to the app.
    /// </summary>
    private static readonly string BinariesDirectory = Path.Combine(
        Path.GetDirectoryName(AppContext.BaseDirectory)!,
        "binaries");

    /// <summary>
    /// The base URL for the GitHub API. Must end with a trailing slash.
    /// </summary>
    private static readonly Uri GitHubApiBaseUrl =
        new($"https://api.github.com/repos/{GitHubOwnerName}/{GitHubRepoName}/");

    /// <summary>
    /// The base URL for the repo on GitHub. Must end with a trailing slash.
    /// </summary>
    private static readonly Uri GitHubBaseUrl = new($"https://github.com/{GitHubOwnerName}/{GitHubRepoName}/");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Log _log;

    public BinaryManager(IHttpClientFactory httpClientFactory, ILogger<BinaryManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _log = new Log(logger);
        _binaryFileName = DetermineBinaryFileName();
    }

    /// <summary>
    /// Ensures that the requested Tailwind CSS standalone CLI binary is downloaded.
    /// </summary>
    /// <param name="versionArg">Optional version to download. Latest if not specified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the binary.</returns>
    public async Task<string> EnsureDownloadedAsync(string? versionArg, CancellationToken cancellationToken)
    {
        var parsedVersion = ParseVersion(versionArg);

        // Create binaries directory if it does not exist.
        Directory.CreateDirectory(BinariesDirectory);

        // If a specific version is requested, check if it is already downloaded.
        if (parsedVersion is not null)
        {
            var existingBinaryPath = Path.Combine(BinariesDirectory, $"{parsedVersion}_{_binaryFileName}");
            if (File.Exists(existingBinaryPath))
            {
                _log.Exists(parsedVersion);
                return existingBinaryPath;
            }
        }

        string binaryPath;
        try
        {
            var releaseInfo = await GetGitHubReleaseInfoAsync(parsedVersion, cancellationToken);
            binaryPath = Path.Combine(BinariesDirectory, $"{releaseInfo.Version}_{_binaryFileName}");

            if (parsedVersion is null)
            {
                _log.LatestVersion(releaseInfo.Version);

                // Now that we know the latest version number, we can check if it exists.
                // If it exists, don't download it.
                if (File.Exists(binaryPath))
                {
                    _log.Exists(releaseInfo.Version);
                    return binaryPath;
                }
            }

            await DownloadAsync(
                releaseInfo.Version,
                releaseInfo.Assets.First(x => x.FileName == _binaryFileName).DownloadUrl,
                binaryPath,
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is not HttpStatusCode.NotFound)
        {
            // If getting release info from GitHub failed for any reason other than a 404,
            // attempt to use the latest installed binary if any exist for the current command.
            var latestInstalledVersion = Directory
                .GetFiles(BinariesDirectory, $"v*_{_binaryFileName}", SearchOption.TopDirectoryOnly)
                .Select(
                    x =>
                    {
                        var fileName = Path.GetFileName(x);
                        return SemanticVersion.Parse(fileName[1..fileName.IndexOf('_', StringComparison.Ordinal)]);
                    })
                .OrderDescending(VersionComparer.VersionRelease)
                .FirstOrDefault();

            // If no version is installed, rethrow.
            if (latestInstalledVersion is null)
            {
                throw;
            }

            // Use the latest version installed.
            var latestInstalledVersionString = $"v{latestInstalledVersion}";
            _log.UsingLatestInstalledVersion(parsedVersion ?? "latest", latestInstalledVersionString);
            binaryPath = Path.Combine(BinariesDirectory, $"{latestInstalledVersionString}_{_binaryFileName}");
        }

        return binaryPath;
    }

    /// <summary>
    /// Parse a version argument value from a command.
    /// Ensures that it is a semantic version and prepends a 'v' to match the tag name format of Tailwind CSS' releases.
    /// </summary>
    /// <param name="versionArg">The version argument value to parse.</param>
    /// <returns>Parsed and formatted version.</returns>
    /// <exception cref="ArgumentException">Invalid semantic version.</exception>
    private static string? ParseVersion(string? versionArg)
    {
        return string.IsNullOrWhiteSpace(versionArg)
            ? null
#pragma warning disable CA1308
            : $"v{SemanticVersion.Parse(versionArg.TrimStart('v').ToLowerInvariant())}";
#pragma warning restore CA1308
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
    /// Download a specific version of the Tailwind CSS standalone CLI binary.
    /// </summary>
    /// <param name="version">Version of the binary to download.</param>
    /// <param name="downloadUrl">Download URL of the binary.</param>
    /// <param name="binaryPath">Path to save the binary to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task DownloadAsync(
        string version,
        Uri downloadUrl,
        string binaryPath,
        CancellationToken cancellationToken)
    {
        _log.Downloading(version, downloadUrl);

        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var downloadResponse = await httpClient.GetAsync(downloadUrl, cancellationToken);
        downloadResponse.EnsureSuccessStatusCode();

        var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        await File.WriteAllBytesAsync(binaryPath, fileBytes, cancellationToken);
        _log.Success(version);

        // If the OS is Linux or macOS, we need to make the binary executable in order to run it.
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            using var chmodProcess = Process.Start("chmod", $"+x {binaryPath}");
            await chmodProcess.WaitForExitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Get the release info for a version of Tailwind CSS from GitHub.
    /// </summary>
    /// <param name="parsedVersion">The parsed version. If null, gets the info for the latest version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Release info for a version of Tailwind CSS from GitHub.</returns>
    private async Task<GitHubReleaseInfo> GetGitHubReleaseInfoAsync(
        string? parsedVersion,
        CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        GitHubReleaseInfo releaseInfo;
        try
        {
            // Try to get info about the release from the GitHub API.
            // The API is more reliable than checking URLs as the direct URLs can change at any time.
            using var releaseResponse = await httpClient.GetAsync(
                new Uri(GitHubApiBaseUrl, $"releases/{(parsedVersion is null ? "latest" : $"tags/{parsedVersion}")}"),
                cancellationToken);

            releaseResponse.EnsureSuccessStatusCode();

            releaseInfo = (await releaseResponse.Content.ReadFromJsonAsync<GitHubReleaseInfo>(cancellationToken))!;
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden
                                                  or HttpStatusCode.TooManyRequests)
        {
            // The GitHub API rate limit might have been reached if a 403 or 429 was returned. See: https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api?apiVersion=2022-11-28#rate-limit-errors
            // The rate limit is 60 requests per hour for unauthenticated requests. See: https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28#primary-rate-limit-for-unauthenticated-users
            // Try to check if the version exists using direct URLs and build a download URL.
            _log.ReachedGitHubRateLimit();

            var checkedVersionExists = false;
            if (parsedVersion is null)
            {
                // Try to get latest version number if version is specified.
                using var latestReleaseResponse = await httpClient.GetAsync(
                    new Uri(GitHubBaseUrl, "releases/latest"),
                    cancellationToken);

                var segments = latestReleaseResponse.RequestMessage?.RequestUri?.Segments;
                if (segments is null || segments[^2] != "tag/")
                {
                    // Resulting URL is not in the expected format.
                    throw;
                }

                parsedVersion = ParseVersion(segments[^1]);
                checkedVersionExists = true;
            }

            if (!checkedVersionExists)
            {
                // Ensure that the version exists.
                using var releaseResponse = await httpClient.GetAsync(
                    new Uri(GitHubBaseUrl, $"releases/tag/{parsedVersion}"),
                    cancellationToken);

                releaseResponse.EnsureSuccessStatusCode();
            }

            releaseInfo = new GitHubReleaseInfo(
                parsedVersion!,
                [
                    new GitHubReleaseAsset(
                        _binaryFileName,
                        new Uri(GitHubBaseUrl, $"releases/download/{parsedVersion}/{_binaryFileName}"))
                ]);
        }

        return releaseInfo;
    }

    private sealed record GitHubReleaseAsset(
        [property: JsonPropertyName("name")]
        string FileName,
        [property: JsonPropertyName("browser_download_url")]
        Uri DownloadUrl);

    private sealed record GitHubReleaseInfo(
        [property: JsonPropertyName("tag_name")]
        string Version,
        [property: JsonPropertyName("assets")]
        IReadOnlyList<GitHubReleaseAsset> Assets);

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Reached GitHub API rate limit. Starting attempt to get release info from direct URLs.")]
        public partial void ReachedGitHubRateLimit();

        [LoggerMessage(Level = LogLevel.Information, Message = "The latest Tailwind CSS is {version}.")]
        public partial void LatestVersion(string version);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Tailwind CSS {version} already exists. Skipping download.")]
        public partial void Exists(string version);

        [LoggerMessage(Level = LogLevel.Information, Message = "Downloading Tailwind CSS {version} from: {url}")]
        public partial void Downloading(string version, Uri url);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully downloaded Tailwind CSS {version}.")]
        public partial void Success(string version);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Failed to fetch Tailwind CSS ({attemptedVersion}). Using latest installed ({version}).")]
        public partial void UsingLatestInstalledVersion(string attemptedVersion, string version);
    }
}
