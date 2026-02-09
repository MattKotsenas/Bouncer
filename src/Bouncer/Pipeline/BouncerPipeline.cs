using System.Text.Json;
using Bouncer.Llm;
using Bouncer.Logging;
using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bouncer.Pipeline;

public sealed partial class BouncerPipeline : IBouncerPipeline
{
    private readonly IRuleEngine _ruleEngine;
    private readonly ILlmJudge _llmJudge;
    private readonly IHookAdapterFactory _adapterFactory;
    private readonly ILogger _denyLogger;
    private readonly ILogger _allowLogger;
    private readonly ILogger<BouncerPipeline> _pipelineLogger;
    private readonly BouncerOptions _options;

    public BouncerPipeline(
        IRuleEngine ruleEngine,
        ILlmJudge llmJudge,
        IHookAdapterFactory adapterFactory,
        ILoggerFactory loggerFactory,
        IOptions<BouncerOptions> options)
    {
        _ruleEngine = ruleEngine;
        _llmJudge = llmJudge;
        _adapterFactory = adapterFactory;
        _denyLogger = loggerFactory.CreateLogger(AuditLogCategories.Deny);
        _allowLogger = loggerFactory.CreateLogger(AuditLogCategories.Allow);
        _pipelineLogger = loggerFactory.CreateLogger<BouncerPipeline>();
        _options = options.Value;
    }

    public async Task<EvaluationResult> EvaluateAsync(
        HookInput input,
        CancellationToken cancellationToken = default)
    {
        EvaluationResult result;

        var match = _ruleEngine.Evaluate(input);
        if (match is not null)
        {
            result = new EvaluationResult(match.Decision, EvaluationTier.Rules, match.Reason);
        }
        else if (_options.LlmFallback.Enabled)
        {
            var llmDecision = await _llmJudge.EvaluateAsync(input, cancellationToken);
            result = llmDecision is null
                ? new EvaluationResult(
                    GetDefaultDecision(),
                    EvaluationTier.DefaultAction,
                    $"No rules matched; defaultAction: {_options.DefaultAction}")
                : new EvaluationResult(llmDecision.Decision, EvaluationTier.Llm, llmDecision.Reason);
        }
        else
        {
            result = new EvaluationResult(
                GetDefaultDecision(),
                EvaluationTier.DefaultAction,
                $"No rules matched; defaultAction: {_options.DefaultAction}");
        }

        MaybeLog(input, result);
        return result;
    }

    public async Task<int> RunAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(input, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            Log.InvalidHookInput(_pipelineLogger, ex);
            return await WriteDefaultDecisionAsync(output, "Invalid hook input JSON", _adapterFactory.Default, cancellationToken);
        }

        using (document)
        {
            Log.RawHookInput(_pipelineLogger, document.RootElement.ToString());

            var adapter = _adapterFactory.Create(document.RootElement);
            HookInput? hookInput;
            try
            {
                hookInput = adapter.ReadInput(document.RootElement);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                Log.InvalidHookInput(_pipelineLogger, ex);
                return await WriteDefaultDecisionAsync(output, "Invalid hook input JSON", adapter, cancellationToken);
            }

            if (hookInput is null)
            {
                Log.MissingHookInput(_pipelineLogger);
                return await WriteDefaultDecisionAsync(output, "Missing hook input", adapter, cancellationToken);
            }

            var result = await EvaluateAsync(hookInput, cancellationToken);
            await adapter.WriteOutputAsync(output, result, cancellationToken);
            return adapter.GetExitCode(result);
        }
    }

    private PermissionDecision GetDefaultDecision() =>
        string.Equals(_options.DefaultAction, "deny", StringComparison.OrdinalIgnoreCase)
            ? PermissionDecision.Deny
            : PermissionDecision.Allow;

    private async Task<int> WriteDefaultDecisionAsync(
        Stream output,
        string reason,
        IHookAdapter adapter,
        CancellationToken cancellationToken)
    {
        var decision = GetDefaultDecision();
        var fullReason = $"{reason}; defaultAction: {_options.DefaultAction}";
        var result = new EvaluationResult(decision, EvaluationTier.DefaultAction, fullReason);

        await adapter.WriteOutputAsync(output, result, cancellationToken);
        return adapter.GetExitCode(result);
    }

    private void MaybeLog(HookInput input, EvaluationResult result)
    {
        var logger = result.Decision == PermissionDecision.Deny
            ? _denyLogger
            : _allowLogger;

        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var toolInputJson = JsonSerializer.Serialize(input.ToolInput, BouncerJsonContext.Default.ToolInput);
        Log.AuditDecision(logger, input.ToolName, toolInputJson, input.Cwd, result.Decision, result.Tier, result.Reason);
    }

    private static partial class Log
    {
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Audit {ToolName} {ToolInput} {Cwd} {Decision} {Tier} {Reason}")]
        public static partial void AuditDecision(
            ILogger logger,
            string toolName,
            string toolInput,
            string? cwd,
            PermissionDecision decision,
            EvaluationTier tier,
            string reason);

        [LoggerMessage(
            Level = LogLevel.Debug,
            Message = "Hook input: {RawJson}")]
        public static partial void RawHookInput(ILogger logger, string rawJson);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Failed to parse hook input")]
        public static partial void InvalidHookInput(ILogger logger, Exception? exception);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Hook input was null or empty")]
        public static partial void MissingHookInput(ILogger logger);
    }
}
