using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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

        ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync("localhost");
        ServiceProvider? services = new ServiceCollection()
            .AddSingleton(new EconManager(redis))
            .AddSingleton<Random>()
            .BuildServiceProvider();

        CommandsNextExtension? commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new[] {"~"},
            Services = services
        });
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}