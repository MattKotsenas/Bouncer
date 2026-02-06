using Bouncer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bouncer.Logging;

public static class BouncerLoggingExtensions
{
    public static IServiceCollection AddBouncerLogging(this IServiceCollection services)
    {
        services.AddSingleton<IAuditLog>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BouncerOptions>>().Value;
            if (!options.Logging.Enabled)
            {
                return new NullAuditLog();
            }

            return new FileAuditLog(options.Logging.Path);
        });

        return services;
    }
}
