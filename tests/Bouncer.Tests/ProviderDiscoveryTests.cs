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

    [TestMethod]
    public void SelectProvider_UsesGhAuthToken_WhenGitHubTokenMissing()
    {
        var originalToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        var tempDir = Path.Combine(Path.GetTempPath(), $"bouncer-gh-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var ghPath = Path.Combine(tempDir, OperatingSystem.IsWindows() ? "gh.cmd" : "gh");
            if (OperatingSystem.IsWindows())
            {
                File.WriteAllText(ghPath, "@echo off\r\necho test-token\r\n");
            }
            else
            {
                File.WriteAllText(ghPath, "#!/bin/sh\necho test-token\n");
                File.SetUnixFileMode(
                    ghPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            Environment.SetEnvironmentVariable("PATH", $"{tempDir}{Path.PathSeparator}{originalPath}");
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", string.Empty);

            var options = new BouncerOptions
            {
                LlmFallback = new LlmFallbackOptions
                {
                    ProviderChain =
                    [
                        new LlmProviderOptions
                        {
                            Type = "github-models",
                            Model = "gpt-4o-mini",
                            TimeoutSeconds = 2
                        }
                    ]
                }
            };

            var selection = ProviderDiscovery.SelectProvider(options);

            selection.Should().NotBeNull();
            selection!.ProviderOptions.Type.Should().Be("github-models");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", originalToken);
            Environment.SetEnvironmentVariable("PATH", originalPath);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}

