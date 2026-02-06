using System.Text.Json;
using Bouncer.Models;
using Bouncer.Options;
using Bouncer.Rules;
using Microsoft.Extensions.Options;

namespace Bouncer.Pipeline;

public sealed class BouncerPipeline : IBouncerPipeline
{
    private readonly IRuleEngine _ruleEngine;
    private readonly BouncerOptions _options;

    public BouncerPipeline(IRuleEngine ruleEngine, IOptions<BouncerOptions> options)
    {
        _ruleEngine = ruleEngine;
        _options = options.Value;
    }

    public Task<EvaluationResult> EvaluateAsync(HookInput input, CancellationToken cancellationToken = default)
    {
        var match = _ruleEngine.Evaluate(input);
        if (match is not null)
        {
            return Task.FromResult(new EvaluationResult(match.Decision, EvaluationTier.Rules, match.Reason));
        }

        var decision = GetDefaultDecision();
        var reason = $"No rules matched; defaultAction: {_options.DefaultAction}";

        return Task.FromResult(new EvaluationResult(decision, EvaluationTier.DefaultAction, reason));
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
}
