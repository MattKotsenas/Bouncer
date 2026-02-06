using FluentAssertions;

namespace Bouncer.Tests;

[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void FluentAssertionsIsWired()
    {
        true.Should().BeTrue();
    }
}
