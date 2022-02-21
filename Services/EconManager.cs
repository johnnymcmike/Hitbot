using DSharpPlus.Entities;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hitbot.Services;

public class EconManager
{
    // public Dictionary<string, int> BalanceBook;
    public string Currencyname;
    public int startingamount;
    private readonly IDatabase db;

    public EconManager(ConnectionMultiplexer redisConnection)
    {
        db = redisConnection.GetDatabase();
        if (!db.KeyExists("balances"))
            db.HashSet("balances", "bucket", 0);

        // BalanceBook = DotnetDictFromRedisHash("balances");

        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            Currencyname = config["currencyname"];
            startingamount = int.Parse(config["startingamount"]);
        }
        else
        {
            throw new Exception("no config.json found");
        }
    }

    public void WriteBalances()
    {
        // JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
        // {
        //     TypeNameHandling = TypeNameHandling.All
        // };
        // File.WriteAllText("balances.json", JsonConvert.SerializeObject(BalanceBook));
    }

    public string GetBalancebookString(DiscordMember member)
    {
        return member.Id + "/" + member.Username + "#" + member.Discriminator;
    }

    public Dictionary<string, int> BalanceBookAsDotnetDict()
    {
        return db.HashGetAll("balancebook").ToStringDictionary()
            .ToDictionary(item => item.Key, item => int.Parse(item.Value));
    }

    public bool BalanceBookHasKey(string key)
    {
        return db.HashExists("balances", key);
    }

    public void BalanceBookSet(string key, int amount)
    {
        db.HashSet("balances", key, amount);
    }

    public int BalanceBookGet(string key)
    {
        return (int) db.HashGet("balances", key);
    }

    public void BalanceBookDecr(string key, int by = 1)
    {
        db.HashDecrement("balances", key, by);
    }

    public void BalanceBookIncr(string key, int by = 1)
    {
        db.HashIncrement("balances", key, by);
    }
}