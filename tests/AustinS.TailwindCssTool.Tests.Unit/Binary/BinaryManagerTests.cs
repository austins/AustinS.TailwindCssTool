using AustinS.TailwindCssTool.Binary;
using RichardSzalay.MockHttp;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Runtime.InteropServices;

namespace AustinS.TailwindCssTool.Tests.Unit.Binary;

public sealed class BinaryManagerTests : IDisposable
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockHttpMessageHandler _http;
    private readonly BinaryManager _sut;

    public BinaryManagerTests()
    {
        _fileSystem = new MockFileSystem();

        _http = new MockHttpMessageHandler();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(BinaryManager.HttpClientName).Returns(_ => new HttpClient(_http, false));

        _sut = new BinaryManager(_fileSystem, httpClientFactory, Substitute.For<ILogger<BinaryManager>>());
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    [Fact]
    public async Task EnsureDownloadedAsync_VersionSpecified_BinaryAlreadyExists_ReturnsExistingPath()
    {
        // Arrange
        const string versionArg = "v4.0.0";
        var expectedPath = Path.Combine(_sut.BinariesDirectory, $"{versionArg}_{_sut.BinaryFileName}");
        _fileSystem.AddEmptyFile(expectedPath);

        // Act
        var result = await _sut.EnsureDownloadedAsync(versionArg, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedPath);
    }

    [Fact]
    public async Task EnsureDownloadedAsync_VersionSpecified_BinaryDoesNotExist_DownloadsAndReturnsPath()
    {
        // Arrange
        const string versionArg = "v4.0.0";
        const string downloadUrl = "http://localhost/download";
        var expectedPath = Path.Combine(_sut.BinariesDirectory, $"{versionArg}_{_sut.BinaryFileName}");

        _http
            .When($"{_sut.GitHubApiBaseUrl}releases/tags/{versionArg}")
            .Respond(
                "application/json",
                $$"""
                  {
                      "tag_name": "{{versionArg}}",
                      "assets": [{ "name": "{{_sut.BinaryFileName}}", "browser_download_url": "{{downloadUrl}}" }]
                  }
                  """);

        _http.When(downloadUrl).Respond("application/octet-stream", "fake-binary-content");

        // Act
        var result = await _sut.EnsureDownloadedAsync(versionArg, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedPath);
        _fileSystem.File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureDownloadedAsync_NullVersion_LatestBinaryAlreadyDownloaded_ReturnsExistingPath()
    {
        // Arrange
        const string latestVersion = "v4.0.0";
        var expectedPath = Path.Combine(_sut.BinariesDirectory, $"{latestVersion}_{_sut.BinaryFileName}");

        _http
            .When($"{_sut.GitHubApiBaseUrl}releases/latest")
            .Respond(
                "application/json",
                $$"""
                  {
                      "tag_name": "{{latestVersion}}",
                      "assets": [{ "name": "{{_sut.BinaryFileName}}", "browser_download_url": "http://localhost/download" }]
                  }
                  """);

        _fileSystem.AddEmptyFile(expectedPath);

        // Act
        var result = await _sut.EnsureDownloadedAsync(null, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedPath);
    }

    [Fact]
    public async Task EnsureDownloadedAsync_NullVersion_DownloadsLatestAndReturnsPath()
    {
        // Arrange
        const string latestVersion = "v4.0.0";
        const string downloadUrl = "http://localhost/download";
        var expectedPath = Path.Combine(_sut.BinariesDirectory, $"{latestVersion}_{_sut.BinaryFileName}");

        _http
            .When($"{_sut.GitHubApiBaseUrl}releases/latest")
            .Respond(
                "application/json",
                $$"""
                  {
                      "tag_name": "{{latestVersion}}",
                      "assets": [{ "name": "{{_sut.BinaryFileName}}", "browser_download_url": "{{downloadUrl}}" }]
                  }
                  """);

        _http.When(downloadUrl).Respond("application/octet-stream", "fake-binary-content");

        // Act
        var result = await _sut.EnsureDownloadedAsync(null, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedPath);
        _fileSystem.File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureDownloadedAsync_InvalidVersionFormat_Throws()
    {
        // Act & Assert
        await _sut
            .Awaiting(x => x.EnsureDownloadedAsync("not-a-valid-semver", TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task EnsureDownloadedAsync_GitHubApiReturnsNotFound_ThrowsHttpRequestException()
    {
        // Arrange
        _http.When($"{_sut.GitHubApiBaseUrl}releases/latest").Respond(HttpStatusCode.NotFound);

        // Act & Assert
        await _sut
            .Awaiting(x => x.EnsureDownloadedAsync(null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(null, "latest")]
    [InlineData("v5.0.0", "tags/v5.0.0")]
    public async Task EnsureDownloadedAsync_GitHubApiNonFatalError_InstalledBinaryExists_UsesLatestInstalled(
        string? versionArg,
        string releaseEndpoint)
    {
        // Arrange - v4.0.0 and v3.4.17 are installed; requested version does not exist.
        var latestInstalledPath = Path.Combine(_sut.BinariesDirectory, $"v4.0.0_{_sut.BinaryFileName}");
        _fileSystem.AddEmptyFile(latestInstalledPath);
        _fileSystem.AddEmptyFile(Path.Combine(_sut.BinariesDirectory, $"v3.4.17_{_sut.BinaryFileName}"));

        _http.When($"{_sut.GitHubApiBaseUrl}releases/{releaseEndpoint}").Respond(HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await _sut.EnsureDownloadedAsync(versionArg, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(latestInstalledPath);
    }

    [Fact]
    public async Task EnsureDownloadedAsync_GitHubApiNonFatalError_NoInstalledBinary_Rethrows()
    {
        // Arrange
        _http.When($"{_sut.GitHubApiBaseUrl}releases/latest").Respond(HttpStatusCode.ServiceUnavailable);

        // Act & Assert
        await _sut
            .Awaiting(x => x.EnsureDownloadedAsync(null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task EnsureDownloadedAsync_RateLimited_VersionSpecified_FallsBackToDirectUrl(
        HttpStatusCode rateLimitStatusCode)
    {
        // Arrange
        const string versionArg = "v4.0.0";
        var downloadUrl = new Uri(_sut.GitHubBaseUrl, $"releases/download/{versionArg}/{_sut.BinaryFileName}");
        var expectedPath = Path.Combine(_sut.BinariesDirectory, $"{versionArg}_{_sut.BinaryFileName}");

        // GitHub API is rate-limited.
        _http.When($"{_sut.GitHubApiBaseUrl}releases/tags/{versionArg}").Respond(rateLimitStatusCode);

        // Direct version check and download fall back to github.com URLs.
        _http.When(new Uri(_sut.GitHubBaseUrl, $"releases/tag/{versionArg}").ToString()).Respond(HttpStatusCode.OK);
        _http.When(downloadUrl.ToString()).Respond("application/octet-stream", "fake-binary-content");

        // Act
        var result = await _sut.EnsureDownloadedAsync(versionArg, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedPath);
        _fileSystem.File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureDownloadedAsync_RateLimited_VersionSpecified_DirectUrlNotFound_Throws()
    {
        // Arrange
        const string versionArg = "v4.0.0";

        _http.When($"{_sut.GitHubApiBaseUrl}releases/tags/{versionArg}").Respond(HttpStatusCode.Forbidden);

        // The direct version-existence check returns 404 -> version simply does not exist.
        _http
            .When(new Uri(_sut.GitHubBaseUrl, $"releases/tag/{versionArg}").ToString())
            .Respond(HttpStatusCode.NotFound);

        // Act & Assert
        await _sut
            .Awaiting(x => x.EnsureDownloadedAsync(versionArg, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public void CleanDownloads_DeletionFails_ContinuesAndDoesNotThrow()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var binaryPath = Path.Combine(_sut.BinariesDirectory, $"v4.0.0_{_sut.BinaryFileName}");

        fileSystem.Directory.Exists(_sut.BinariesDirectory).Returns(true);

        fileSystem
            .Directory.EnumerateFiles(_sut.BinariesDirectory, "v*_tailwindcss*", SearchOption.TopDirectoryOnly)
            .Returns([binaryPath]);

        fileSystem.File.When(f => f.Delete(binaryPath)).Do(_ => throw new IOException("Access denied."));

        var sut = new BinaryManager(
            fileSystem,
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<ILogger<BinaryManager>>());

        // Act & Assert
        sut.Invoking(x => x.CleanDownloads()).Should().NotThrow();
    }

    public static bool IsNotLinuxX64 =>
        !OperatingSystem.IsLinux() || RuntimeInformation.OSArchitecture is not Architecture.X64;

    [Fact(Skip = "Can only be run on Linux X64", SkipWhen = nameof(IsNotLinuxX64))]
    public void Constructor_BinaryFileName_LinuxX64_WithMuslLibrary_HasMuslSuffix()
    {
        // Arrange
        _fileSystem.AddEmptyFile("/lib/ld-musl-x86_64.so.1");

        // Act
        var sut = new BinaryManager(
            _fileSystem,
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<ILogger<BinaryManager>>());

        // Assert
        sut.BinaryFileName.Should().Be("tailwindcss-linux-x64-musl");
    }
}
