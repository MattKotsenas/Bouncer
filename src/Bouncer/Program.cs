using Bouncer.Commands;
using Bouncer.Llm;
using Bouncer.Logging;
using Bouncer.Options;
using Bouncer.Pipeline;
using Bouncer.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

if (args.Length > 0 && args[0].Equals("init", StringComparison.OrdinalIgnoreCase))
{
    var path = args.Length > 1 ? args[1] : null;
    return await InitCommand.ExecuteAsync(path, Console.Out, Console.Error);
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile(".bouncer.json", optional: true)
    .AddEnvironmentVariables("BOUNCER_")
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddBouncerOptions(configuration);
services.AddBouncerRules();
services.AddBouncerLlm();
services.AddBouncerLogging(configuration);
services.AddBouncerPipeline();

using var provider = services.BuildServiceProvider();
var pipeline = provider.GetRequiredService<IBouncerPipeline>();

if (args.Length == 0)
{
    return await HookCommand.ExecuteAsync(pipeline);
}

var command = args[0].ToLowerInvariant();
return command switch
{
    "check" => CheckCommand.Execute(
        provider.GetRequiredService<IOptions<BouncerOptions>>().Value,
        Console.Out),
    "test" => await TestCommand.ExecuteAsync(
        args.Skip(1).ToArray(),
        pipeline,
        provider.GetRequiredService<IRuleEngine>(),
        provider.GetRequiredService<IOptions<BouncerOptions>>().Value,
        Console.Out,
        Console.Error),
    _ => UnknownCommand(command)
};

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine("Available commands: init, check, test");
    return 1;
}
