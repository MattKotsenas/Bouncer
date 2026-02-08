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
    private readonly ILogger _denyLogger;
    private readonly ILogger _allowLogger;
    private readonly BouncerOptions _options;

    public BouncerPipeline(
        IRuleEngine ruleEngine,
        ILlmJudge llmJudge,
        ILoggerFactory loggerFactory,
        IOptions<BouncerOptions> options)
    {
        _ruleEngine = ruleEngine;
        _llmJudge = llmJudge;
        _denyLogger = loggerFactory.CreateLogger(AuditLogCategories.Deny);
        _allowLogger = loggerFactory.CreateLogger(AuditLogCategories.Allow);
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
        HookInput? hookInput;
        try
        {
            hookInput = await JsonSerializer.DeserializeAsync(
                input,
                BouncerJsonContext.Default.HookInput,
                cancellationToken);
        }
        catch (JsonException)
        {
            return await WriteDefaultDecisionAsync(output, "Invalid hook input JSON", cancellationToken);
        }

        if (hookInput is null)
        {
            return await WriteDefaultDecisionAsync(output, "Missing hook input", cancellationToken);
        }

        var result = await EvaluateAsync(hookInput, cancellationToken);
        var hookOutput = result.Decision == PermissionDecision.Deny
            ? HookOutput.Deny(result.Reason)
            : HookOutput.Allow(result.Reason);

        await JsonSerializer.SerializeAsync(
            output,
            hookOutput,
            BouncerJsonContext.Default.HookOutput,
            cancellationToken);
        await output.FlushAsync(cancellationToken);

        return result.Decision == PermissionDecision.Deny ? 2 : 0;
    }

    private PermissionDecision GetDefaultDecision() =>
        string.Equals(_options.DefaultAction, "deny", StringComparison.OrdinalIgnoreCase)
            ? PermissionDecision.Deny
            : PermissionDecision.Allow;

    private async Task<int> WriteDefaultDecisionAsync(
        Stream output,
        string reason,
        CancellationToken cancellationToken)
    {
        var decision = GetDefaultDecision();
        var hookOutput = decision == PermissionDecision.Deny
            ? HookOutput.Deny($"{reason}; defaultAction: {_options.DefaultAction}")
            : HookOutput.Allow($"{reason}; defaultAction: {_options.DefaultAction}");

        await JsonSerializer.SerializeAsync(
            output,
            hookOutput,
            BouncerJsonContext.Default.HookOutput,
            cancellationToken);
        await output.FlushAsync(cancellationToken);

        return decision == PermissionDecision.Deny ? 2 : 0;
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
        Log.AuditDecision(logger, input.ToolName, toolInputJson, result.Decision, result.Tier, result.Reason);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            EventName = "Audit",
            Level = LogLevel.Information,
            Message = "Audit {ToolName} {ToolInput} {Decision} {Tier} {Reason}")]
        public static partial void AuditDecision(
            ILogger logger,
            string toolName,
            string toolInput,
            PermissionDecision decision,
            EvaluationTier tier,
            string reason);
    }
}
