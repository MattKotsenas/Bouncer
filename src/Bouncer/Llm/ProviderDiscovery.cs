using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Bouncer.Options;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using OllamaSharp;
using System.ClientModel;

namespace Bouncer.Llm;

public sealed record LlmProviderSelection(IChatClient ChatClient, LlmProviderOptions ProviderOptions);

public static class ProviderDiscovery
{
    private const string AnthropicEndpoint = "https://api.anthropic.com";
    private const string GitHubModelsEndpoint = "https://models.inference.ai.azure.com";

    public static LlmProviderSelection? SelectProvider(BouncerOptions options)
    {
        foreach (var provider in options.LlmFallback.ProviderChain)
        {
            var selection = provider.Type.ToLowerInvariant() switch
            {
                "anthropic" => TryCreateAnthropic(provider),
                "github-models" => TryCreateOpenAi(provider, "GITHUB_TOKEN", GitHubModelsEndpoint),
                "openai" => TryCreateOpenAi(provider, "OPENAI_API_KEY", provider.Endpoint),
                "ollama" => TryCreateOllama(provider),
                _ => null
            };

            if (selection is not null)
            {
                return selection;
            }
        }

        return null;
    }

    private static LlmProviderSelection? TryCreateAnthropic(LlmProviderOptions provider)
    {
        var apiKey = ResolveApiKey("ANTHROPIC_API_KEY", provider.ApiKeyCommand);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(provider.Model))
        {
            return null;
        }

        var baseAddress = provider.Endpoint ?? AnthropicEndpoint;
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseAddress, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds)
        };

        httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = new AnthropicChatClient(httpClient, provider.Model, disposeClient: true);
        return new LlmProviderSelection(client, provider);
    }

    private static LlmProviderSelection? TryCreateOpenAi(
        LlmProviderOptions provider,
        string apiKeyEnvVar,
        string? endpointOverride)
    {
        var apiKey = ResolveApiKey(apiKeyEnvVar, provider.ApiKeyCommand);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(provider.Model))
        {
            return null;
        }

        var endpoint = endpointOverride ?? provider.Endpoint;
        var chatClient = string.IsNullOrWhiteSpace(endpoint)
            ? new ChatClient(provider.Model, apiKey)
            : new ChatClient(
                provider.Model,
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint, UriKind.Absolute) });

        return new LlmProviderSelection(chatClient.AsIChatClient(), provider);
    }

    private static LlmProviderSelection? TryCreateOllama(LlmProviderOptions provider)
    {
        var endpoint = provider.Endpoint ?? "http://localhost:11434";
        if (string.IsNullOrWhiteSpace(provider.Model))
        {
            return null;
        }

        if (!IsOllamaAvailable(endpoint, provider.TimeoutSeconds))
        {
            return null;
        }

        var chatClient = new OllamaApiClient(endpoint, provider.Model);
        return new LlmProviderSelection(chatClient, provider);
    }

    private static bool IsOllamaAvailable(string endpoint, int timeoutSeconds)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
        try
        {
            var tagsUri = new Uri(new Uri(endpoint, UriKind.Absolute), "/api/tags");
            var response = client.GetAsync(tagsUri).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private static string? ResolveApiKey(string envVar, string? apiKeyCommand)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        if (string.IsNullOrWhiteSpace(apiKeyCommand))
        {
            return null;
        }

        return ExecuteCommand(apiKeyCommand);
    }

    private static string ExecuteCommand(string command)
    {
        var isWindows = OperatingSystem.IsWindows();
        var fileName = isWindows ? "cmd.exe" : "/bin/sh";
        var args = isWindows ? $"/c {command}" : $"-c \"{command}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("apiKeyCommand failed to start.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"apiKeyCommand failed: {error}");
        }

        return output.Trim();
    }
}
