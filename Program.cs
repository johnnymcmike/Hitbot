using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Hitbot.Commands;
using Newtonsoft.Json;

namespace Hitbot;

internal class Program
{
    public static Dictionary<string, string> config;

    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        //CONNECTIONS
        //read in json files
        if (File.Exists("balances.json"))
        {
            EconModule.BalanceBook =
                JsonConvert.DeserializeObject<Dictionary<string, int>>(await File.ReadAllTextAsync("balances.json"))!;
        }

        if (File.Exists("config.json"))
        {
            config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                await File.ReadAllTextAsync("config.json"))!;
            EconModule.Currencyname = config["currencyname"];
        }

        //initialize connection to discord
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = await File.ReadAllTextAsync("token.txt"),
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = new[] {"~"}
        });
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        //initialize connection to redis
        // redis = await ConnectionMultiplexer.ConnectAsync("localhost");
        // IDatabase db = redis.GetDatabase();


        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}