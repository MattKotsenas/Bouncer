using Bouncer.Llm;
using Bouncer.Options;
using FluentAssertions;

namespace Bouncer.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ProviderDiscoveryTests
{
    [TestMethod]
    public void SelectProvider_ReturnsOpenAi_WhenApiKeyPresent()
    {
        var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        try
        {
            var options = new BouncerOptions
            {
                LlmFallback = new LlmFallbackOptions
                {
                    ProviderChain =
                    [
                        new LlmProviderOptions
                        {
                            Type = "openai",
                            Model = "gpt-4o-mini",
                            TimeoutSeconds = 2
                        }
                    ]
                }
            };

            var selection = ProviderDiscovery.SelectProvider(options);

            selection.Should().NotBeNull();
            selection!.ProviderOptions.Type.Should().Be("openai");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
        }
    }

    [TestMethod]
    public void SelectProvider_ReturnsNull_WhenNoApiKey()
    {
        var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", string.Empty);

        try
        {
            var options = new BouncerOptions
            {
                LlmFallback = new LlmFallbackOptions
                {
                    ProviderChain =
                    [
                        new LlmProviderOptions
                        {
                            Type = "openai",
                            Model = "gpt-4o-mini",
                            TimeoutSeconds = 2
                        }
                    ]
                }
            };

            var selection = ProviderDiscovery.SelectProvider(options);

            selection.Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
        }
    }
}

