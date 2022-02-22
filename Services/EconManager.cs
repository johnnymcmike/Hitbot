using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hitbot.Services;

public class EconManager : IBookKeeper
{
    public readonly string Currencyname;
    private readonly IDatabase db;
    public readonly int Startingamount;
    private const string BookKey = "balances";

    public EconManager(ConnectionMultiplexer redisConnection)
    {
        db = redisConnection.GetDatabase();
        if (!db.KeyExists(BookKey))
            db.HashSet(BookKey, "bucket", 0);

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
        return db.HashGetAll(BookKey).ToStringDictionary()
            .ToDictionary(item => item.Key, item => int.Parse(item.Value));
    }

    public bool BookHasKey(string key)
    {
        return db.HashExists(BookKey, key);
    }

    public void BookClear()
    {
        throw new NotImplementedException();
    }

    public void BookSet(string key, int amount)
    {
        db.HashSet(BookKey, key, amount);
    }

    public int BookGet(string key)
    {
        return (int) db.HashGet(BookKey, key);
    }

    public void BookDecr(string key, int by = 1)
    {
        db.HashDecrement(BookKey, key, by);
    }

    public void BookIncr(string key, int by = 1)
    {
        db.HashIncrement(BookKey, key, by);
    }
}