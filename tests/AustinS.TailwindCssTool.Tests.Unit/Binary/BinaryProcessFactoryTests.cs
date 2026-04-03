using AustinS.TailwindCssTool.Binary;

namespace AustinS.TailwindCssTool.Tests.Unit.Binary;

public sealed class BinaryProcessFactoryTests
{
    private readonly BinaryProcessFactory _sut;

    public BinaryProcessFactoryTests()
    {
        _sut = new BinaryProcessFactory(Substitute.For<ILoggerFactory>());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Create_Success(bool minify, bool watch)
    {
        // Arrange
        const string binaryFilePath = "/path/to/binary";
        const string input = "input.css";
        const string output = "output.css";

        // Act
        using var process = _sut.Create(binaryFilePath, input, output, minify, watch);

        // Assert
        process.Should().BeOfType<BinaryProcess>();
    }
}
