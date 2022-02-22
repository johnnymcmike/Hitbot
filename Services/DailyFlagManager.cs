using StackExchange.Redis;

namespace Hitbot.Services;

public class DailyFlagManager
{
    private readonly IDatabase db;

    public DailyFlagManager(ConnectionMultiplexer redis)
    {
        db = redis.GetDatabase();
    }

    public void TriggerDaily(string user, string reason)
    {
        TimeSpan untilmidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
        db.HashSet($"dailies{user}", "Wa", 1);
        db.StringSet($"dailies:{reason}:{user}", 1, untilmidnight);
    }

    public bool DailyExists(string user, string reason)
    {
        return db.KeyExists($"dailies:{reason}:{user}");
    }
}