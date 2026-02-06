using Bouncer.Commands;
using Bouncer.Options;
using FluentAssertions;

namespace Bouncer.Tests;

[TestClass]
public sealed class CheckCommandTests
{
    [TestMethod]
    public void CheckCommand_PrintsSummary()
    {
        var options = new BouncerOptions();
        var output = new StringWriter();

        var exitCode = CheckCommand.Execute(options, output);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("DefaultAction: allow");
        output.ToString().Should().Contain("Provider chain:");
    }
}
