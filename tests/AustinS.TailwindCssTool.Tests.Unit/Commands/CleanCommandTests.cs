using AustinS.TailwindCssTool.Binary;
using AustinS.TailwindCssTool.Commands;

namespace AustinS.TailwindCssTool.Tests.Unit.Commands;

public sealed class CleanCommandTests
{
    private readonly IBinaryManager _binaryManager;
    private readonly CleanCommand _sut;

    public CleanCommandTests()
    {
        _binaryManager = Substitute.For<IBinaryManager>();

        _sut = new CleanCommand(_binaryManager);
    }

    [Fact]
    public void Handle_Success()
    {
        // Act
        _sut.Handle();

        // Assert
        _binaryManager.Received(1).CleanDownloads();
    }
}
