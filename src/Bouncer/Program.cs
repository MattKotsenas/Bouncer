using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(".bouncer.json", optional: true)
    .AddEnvironmentVariables("BOUNCER_")
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddBouncerOptions(configuration);
services.AddBouncerRules();
services.AddBouncerPipeline();

using var provider = services.BuildServiceProvider();
var pipeline = provider.GetRequiredService<IBouncerPipeline>();

return await pipeline.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
