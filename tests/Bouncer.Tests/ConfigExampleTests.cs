using System.Text;
using Bouncer.Commands;
using Bouncer.Logging;
using Bouncer.Options;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Bouncer.Tests;

[TestClass]
public sealed class ConfigExampleTests
{
    [TestMethod]
    public void ExampleConfig_MatchesDefaults()
    {
        var json = LoadExample();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        var options = new BouncerOptions();
        options.LlmFallback.ProviderChain.Clear();
        configuration.Bind(options);

        options.Should().BeEquivalentTo(new BouncerOptions());

        var fileOptions = new FileLoggingOptions();
        configuration.GetSection(FileLoggingOptions.SectionName).Bind(fileOptions);
        fileOptions.Path.Should().Be(BouncerPaths.LogFile());

        configuration["Logging:LogLevel:Default"].Should().Be("Error");
        configuration["Logging:LogLevel:Bouncer.Audit.Deny"].Should().Be("Information");
        configuration["Logging:LogLevel:Bouncer.Audit.Allow"].Should().Be("None");
        configuration["Logging:LogLevel:Bouncer.Pipeline"].Should().Be("Warning");
        configuration["llmFallback:providerChain:0:apiKeyCommand"].Should().BeNull();
    }

    private static string LoadExample()
    {
        var assembly = typeof(InitCommand).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith(".bouncer.json.example", StringComparison.OrdinalIgnoreCase));
        using var resourceStream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded example config not found.");
        using var reader = new StreamReader(resourceStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
