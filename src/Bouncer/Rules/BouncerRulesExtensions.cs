using Microsoft.Extensions.DependencyInjection;

namespace Bouncer.Rules;

public static class BouncerRulesExtensions
{
    public static IServiceCollection AddBouncerRules(this IServiceCollection services)
    {
        services.AddSingleton<IRuleEngine, RegexRuleEngine>();
        return services;
    }
}
