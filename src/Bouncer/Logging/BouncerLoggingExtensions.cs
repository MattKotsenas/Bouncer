using Bouncer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bouncer.Logging;

public static class BouncerLoggingExtensions
{
    public static IServiceCollection AddBouncerLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddFilter((category, level) => level >= LogLevel.Error);
            builder.AddFilter(AuditLogCategories.Deny, LogLevel.Information);
            builder.AddFilter(AuditLogCategories.Allow, LogLevel.None);
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });
        services.AddSingleton<ILoggerProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BouncerOptions>>().Value;
            return new FileAuditLoggerProvider(options.Logging);
        });

        return services;
    }
}
