using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hitbot;

internal class Program
{
    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        //CONNECTIONS
        //read in json files

        //initialize connection to discord
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = await File.ReadAllTextAsync("token.txt"),
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });
        discord.UseInteractivity(new InteractivityConfiguration
        {
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(30)
        });

        ServiceProvider? services = new ServiceCollection()
            .AddSingleton<EconManager>()
            .BuildServiceProvider();

        CommandsNextExtension? commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new[] {"~"},
            Services = services
        });
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        //initialize connection to redis
        // redis = await ConnectionMultiplexer.ConnectAsync("localhost");
        // IDatabase db = redis.GetDatabase();


        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}