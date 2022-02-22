using StackExchange.Redis;

namespace Hitbot.Services;

public class DailyFlagManager
{
    private readonly IDatabase db;

    public DailyFlagManager(ConnectionMultiplexer redis)
    {
        db = redis.GetDatabase();
    }

    public void SetDaily(string user, string reason, string data = "lol")
    {
        TimeSpan untilmidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
        db.StringSet($"dailies:{reason}:{user}", data, untilmidnight);
    }

    public string GetDaily(string user, string reason)
    {
        return db.StringGet($"dailies:{reason}:{user}");
    }

    public bool DailyExists(string user, string reason)
    {
        return db.KeyExists($"dailies:{reason}:{user}");
    }
}