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
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddFilter(AuditLogCategories.Deny, LogLevel.Information);
            builder.AddFilter(AuditLogCategories.Allow, LogLevel.None);
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });
        services.AddOptions<FileLoggingOptions>()
            .BindConfiguration(FileLoggingOptions.SectionName);
        services.AddSingleton<ILogFormatter, JsonLogFormatter>();
        services.AddSingleton<ILoggerProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FileLoggingOptions>>().Value;
            var formatter = sp.GetRequiredService<ILogFormatter>();
            return new FileLoggerProvider(options, formatter);
        });

        return services;
    }
}
