using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hitbot.Services;

public class GamesManager : IBookKeeper
{
    private readonly IDatabase db;
    public readonly int LottoDrawprice;
    public readonly int LottoTicketprice;
    private const string BookKey = "lotto";

    public GamesManager(ConnectionMultiplexer redisConnection)
    {
        db = redisConnection.GetDatabase();
        if (!db.KeyExists(BookKey))
            db.HashSet(BookKey, "pot", 0);

        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            LottoTicketprice = int.Parse(config["lottoticketprice"]);
            LottoDrawprice = int.Parse(config["lottodrawprice"]);
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

    public override void BookSet(string key, int amount)
    {
        db.HashSet(BookKey, key, amount);
    }


    public override int BookGet(string key)
    {
        return (int) db.HashGet(BookKey, key);
    }

    public override void BookDecr(string key, int by = 1)
    {
        db.HashDecrement(BookKey, key, by);
    }

    public override bool BookHasKey(string key)
    {
        return db.HashExists(BookKey, key);
    }

    public override void BookIncr(string key, int by = 1)
    {
        db.HashIncrement(BookKey, key, by);
    }

    public override void BookClear()
    {
        db.KeyDelete(BookKey);
        db.HashSet(BookKey, "pot", 0);
    }
}