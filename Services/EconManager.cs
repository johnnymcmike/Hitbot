using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hitbot.Services;

public class EconManager : IBookKeeper
{
    public readonly string Currencyname;
    private readonly IDatabase db;
    public readonly int Startingamount;
    private const string bookKey = "balances";

    public EconManager(ConnectionMultiplexer redisConnection)
    {
        db = redisConnection.GetDatabase();
        if (!db.KeyExists(bookKey))
            db.HashSet(bookKey, "bucket", 0);

        // BalanceBook = DotnetDictFromRedisHash(book1);

        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            Currencyname = config["currencyname"];
            Startingamount = int.Parse(config["startingamount"]);
        }
        else
        {
            throw new Exception("no config.json found");
        }
    }

    public Dictionary<string, int> BookAsDotnetDict()
    {
        return db.HashGetAll(bookKey).ToStringDictionary()
            .ToDictionary(item => item.Key, item => int.Parse(item.Value));
    }

    public bool BookHasKey(string key)
    {
        return db.HashExists(bookKey, key);
    }

    public void BookClear()
    {
        throw new NotImplementedException();
    }

    public void BookSet(string key, int amount)
    {
        db.HashSet(bookKey, key, amount);
    }

    public int BookGet(string key)
    {
        return (int) db.HashGet(bookKey, key);
    }

    public void BookDecr(string key, int by = 1)
    {
        db.HashDecrement(bookKey, key, by);
    }

    public void BookIncr(string key, int by = 1)
    {
        db.HashIncrement(bookKey, key, by);
    }
}