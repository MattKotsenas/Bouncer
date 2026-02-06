using Bouncer.Commands;
using FluentAssertions;

namespace Bouncer.Tests;

[TestClass]
public sealed class InitCommandTests
{
    [TestMethod]
    public async Task InitCommand_WritesConfigFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bouncer-{Guid.NewGuid()}.json");
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await InitCommand.ExecuteAsync(path, output, error);

        exitCode.Should().Be(0);
        error.ToString().Should().BeEmpty();
        File.Exists(path).Should().BeTrue();

        var content = await File.ReadAllTextAsync(path);
        content.Should().Contain("\"defaultAction\"");

        File.Delete(path);
    }
}
