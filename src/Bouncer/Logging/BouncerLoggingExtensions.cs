using Bouncer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bouncer.Logging;

public static class BouncerLoggingExtensions
{
    public static IServiceCollection AddBouncerLogging(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<ILoggerProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BouncerOptions>>().Value;
            return new FileAuditLoggerProvider(options.Logging);
        });

        return services;
    }
}
