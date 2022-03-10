using StackExchange.Redis;

namespace Hitbot.Services;

/// <summary>
///     A service that manages daily redis flags, namespaced internally by user and reason. If action is not user-specific,
///     use "any" by convention as value for user.
///     Daily flags can also optionally store string data.
/// </summary>
public class DailyFlagManager
{
    private readonly IDatabase db;

    public DailyFlagManager(ConnectionMultiplexer redis)
    {
        db = redis.GetDatabase();
    }

    /// <summary>
    ///     Sets a string, defaulting to meaningless nonsense, that will be deleted next midnight GMT.
    /// </summary>
    /// <param name="user">The user for whom the data is being stored. Use "any" if the reason is not user-specific</param>
    /// <param name="reason">The reason or command for which you are storing a daily flag.</param>
    /// <param name="data">Optionally store meaningful data at this key.</param>
    public void SetDaily(string user, string reason, string data = "lol")
    {
        TimeSpan untilmidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
        db.StringSet($"dailies:{reason}:{user}", data, untilmidnight);
    }

    /// <summary>
    ///     Returns the daily flag for the given user and reason.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public string GetDaily(string user, string reason)
    {
        return db.StringGet($"dailies:{reason}:{user}");
    }

    /// <summary>
    ///     Returns whether or not a daily flag exists for the given user and reason.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public bool DailyExists(string user, string reason)
    {
        return db.KeyExists($"dailies:{reason}:{user}");
    }
}