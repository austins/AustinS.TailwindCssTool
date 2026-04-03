using AustinS.TailwindCssTool.Binary;

namespace AustinS.TailwindCssTool.Tests.Unit.Binary;

public sealed class BinaryProcessTests : IDisposable
{
    private readonly BinaryProcess _sut;

    public BinaryProcessTests()
    {
        _sut = new BinaryProcess(
            "/path/to/binary",
            "input.css",
            "output.css",
            Substitute.For<ILogger<BinaryProcess>>());
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public async Task WaitForExitAsync_NotStarted_Returns()
    {
        // Act & Assert
        await _sut.Awaiting(x => x.WaitForExitAsync(TestContext.Current.CancellationToken)).Should().NotThrowAsync();
    }

    [Fact]
    public void Dispose_NotStarted_Returns()
    {
        // Act & Assert
        _sut.Invoking(x => x.Dispose()).Should().NotThrow();
    }
}
