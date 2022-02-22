using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hitbot.Services;

public class GamblingManager // : IBookKeeper
{
    private readonly IDatabase db;
    public readonly int lottoDrawprice;
    public readonly int lottoTicketprice;
    private const string bookKey = "lotto";

    public GamblingManager(ConnectionMultiplexer redisConnection)
    {
        db = redisConnection.GetDatabase();
        if (!db.KeyExists(bookKey))
            db.HashSet(bookKey, "pot", 0);

        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            lottoTicketprice = int.Parse(config["lottoticketprice"]);
            lottoDrawprice = int.Parse(config["lottodrawprice"]);
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

    public bool BookHasKey(string key)
    {
        return db.HashExists(bookKey, key);
    }

    public void BookIncr(string key, int by = 1)
    {
        db.HashIncrement(bookKey, key, by);
    }

    public void BookClear()
    {
        db.KeyDelete(bookKey);
        db.HashSet(bookKey, "pot", 0);
    }
}