using AustinS.TailwindCssTool.Binary;
using AustinS.TailwindCssTool.Commands;

namespace AustinS.TailwindCssTool.Tests.Unit.Commands;

public sealed class BuildCommandTests
{
    private readonly IBinaryManager _binaryManager;
    private readonly IBinaryProcessFactory _binaryProcessFactory;
    private readonly BuildCommand _sut;

    public BuildCommandTests()
    {
        _binaryManager = Substitute.For<IBinaryManager>();
        _binaryProcessFactory = Substitute.For<IBinaryProcessFactory>();

        _sut = new BuildCommand(_binaryManager, _binaryProcessFactory);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "v4.0.0")]
    [InlineData(true, null)]
    [InlineData(true, "v3.4.17")]
    public async Task HandleAsync_Success(bool minify, string? tailwindVersion)
    {
        // Arrange
        const string input = "input.css";
        const string output = "output.css";
        const string binaryFilePath = "/path/to/binary";

        _binaryManager
            .EnsureDownloadedAsync(tailwindVersion, TestContext.Current.CancellationToken)
            .Returns(binaryFilePath);

        var process = Substitute.For<IBinaryProcess>();
        _binaryProcessFactory.Create(binaryFilePath, input, output, minify).Returns(process);

        // Act
        await _sut.HandleAsync(input, output, TestContext.Current.CancellationToken, minify, tailwindVersion);

        // Assert
        await _binaryManager.Received(1).EnsureDownloadedAsync(tailwindVersion, TestContext.Current.CancellationToken);
        _binaryProcessFactory.Received(1).Create(binaryFilePath, input, output, minify);
        process.Received(1).Start();
        await process.Received(1).WaitForExitAsync(TestContext.Current.CancellationToken);
    }
}
