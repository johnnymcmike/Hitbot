using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
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

    public static void SetData<T>(string key, T data)
    {
        IDatabase db = redis.GetDatabase();
        db.StringSet(key, JsonConvert.SerializeObject(data));
    }

    public static T GetData<T>(string key)
    {
        try
        {
            IDatabase db = redis.GetDatabase();
            RedisValue jsonresult = db.StringGet(key);
            if (jsonresult.IsNull)
                return default!;
            return JsonConvert.DeserializeObject<T>(jsonresult)!;
        }
        catch
        {
            return default!;
        }
    }
}