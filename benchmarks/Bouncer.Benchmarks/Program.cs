using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Bouncer.Llm;
using Bouncer.Logging;
using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

var runLlm = args.Any(arg => arg.Equals("--llm", StringComparison.OrdinalIgnoreCase)
    || arg.Equals("--all", StringComparison.OrdinalIgnoreCase));

BenchmarkRunner.Run<Tier1PipelineBenchmarks>();
BenchmarkRunner.Run<ContainerBuildBenchmarks>();

if (runLlm)
{
    BenchmarkRunner.Run<LlmFallbackBenchmarks>();
}

[MemoryDiagnoser]
public sealed class Tier1PipelineBenchmarks
{
    private IBouncerPipeline _pipeline = null!;
    private HookInput _dangerousInput = null!;
    private HookInput _safeInput = null!;
    private byte[] _dangerousJson = null!;
    private byte[] _safeJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new BouncerOptions();
        options.LlmFallback.Enabled = false;

        var optionsWrapper = Options.Create(options);
        var engine = new RegexRuleEngine(optionsWrapper);
        _pipeline = new BouncerPipeline(
            engine,
            new NullLlmJudge(),
            new NullAuditLog(),
            optionsWrapper);

        _dangerousInput = HookInput.Bash("rm -rf /");
        _safeInput = HookInput.Bash("echo ok");

        _dangerousJson = JsonSerializer.SerializeToUtf8Bytes(
            _dangerousInput,
            BouncerJsonContext.Default.HookInput);
        _safeJson = JsonSerializer.SerializeToUtf8Bytes(
            _safeInput,
            BouncerJsonContext.Default.HookInput);
    }

    [Benchmark]
    public Task<EvaluationResult> EvaluateDangerous() => _pipeline.EvaluateAsync(_dangerousInput);

    [Benchmark]
    public Task<EvaluationResult> EvaluateSafe() => _pipeline.EvaluateAsync(_safeInput);

    [Benchmark]
    public async Task<int> RunDangerous()
    {
        using var inputStream = new MemoryStream(_dangerousJson);
        using var outputStream = new MemoryStream();
        return await _pipeline.RunAsync(inputStream, outputStream);
    }

    [Benchmark]
    public async Task<int> RunSafe()
    {
        using var inputStream = new MemoryStream(_safeJson);
        using var outputStream = new MemoryStream();
        return await _pipeline.RunAsync(inputStream, outputStream);
    }
}

[MemoryDiagnoser]
public sealed class ContainerBuildBenchmarks
{
    [Benchmark]
    public void BuildAndResolvePipeline()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["llmFallback:enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddBouncerOptions(configuration);
        services.AddBouncerRules();
        services.AddBouncerLlm();
        services.AddBouncerLogging();
        services.AddBouncerPipeline();

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IBouncerPipeline>();
    }
}

[MemoryDiagnoser]
public sealed class LlmFallbackBenchmarks
{
    private ILlmJudge _judge = null!;
    private HookInput _input = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new BouncerOptions();
        var selection = ProviderDiscovery.SelectProvider(options);
        if (selection is null)
        {
            throw new InvalidOperationException("No LLM provider available. Set an API key env var to run --llm.");
        }

        _judge = new LlmJudge(selection.ChatClient, options.LlmFallback, selection.ProviderOptions);
        _input = HookInput.Bash("rm -rf /");
    }

    [Benchmark]
    public Task<LlmDecision?> EvaluateAsync() => _judge.EvaluateAsync(_input);
}
