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
        db.StringSet($"dailies:{reason}:{user}", 1, TimeSpan.FromDays(1));
    }

    public bool DailyExists(string user, string reason)
    {
        return db.KeyExists($"dailies:{reason}:{user}");
    }
}