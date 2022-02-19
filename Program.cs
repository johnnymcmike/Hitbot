using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Hitbot.Commands;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hitbot;

internal class Program
{
    private static ConnectionMultiplexer redis;

    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        //CONNECTIONS
        //read balances file
        if (File.Exists("balances.json"))
        {
            EconModule.Balances = JsonConvert.DeserializeObject<Dictionary<DiscordMember,int>>(await File.ReadAllTextAsync("balances.json"))!;
        }
        else
        {
            File.Create("balances.json");
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
        redis = await ConnectionMultiplexer.ConnectAsync("localhost");
        IDatabase db = redis.GetDatabase();
        
        
        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}