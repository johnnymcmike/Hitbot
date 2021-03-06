using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Hitbot;

internal class Program
{
    private static void Main()
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
        var rng = new Random();
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "Epic C# Discord Bot (mbjmcm@gmail.com)");
        ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync("localhost");
        ServiceProvider? services = new ServiceCollection()
            .AddSingleton(new EconManager(redis))
            .AddSingleton(new LottoManager(redis))
            .AddSingleton(new DailyFlagManager(redis))
            .AddSingleton(new ContraptionManager(redis, rng))
            .AddSingleton(redis)
            .AddSingleton(rng)
            .AddSingleton(http)
            .BuildServiceProvider();

        CommandsNextExtension? commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new[] {"~"},
            Services = services
        });
        discord.GetCommandsNext().CommandErrored += async (s, e) =>
        {
            Console.WriteLine("Command errored:");
            Console.WriteLine(e.Exception);
        };
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }

    public static string GetBalancebookString(DiscordMember member)
    {
        return member.Id + "/" + member.Username + "#" + member.Discriminator;
    }
}