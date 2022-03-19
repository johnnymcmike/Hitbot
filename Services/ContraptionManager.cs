using StackExchange.Redis;

namespace Hitbot.Services;

public class ContraptionManager
{
    private IDatabase db { get; }
    private Random rng { get; }
    private const int cap = 1000;

    public ContraptionManager(ConnectionMultiplexer redis, Random rnd)
    {
        db = redis.GetDatabase();
        rng = rnd;
    }

    /// <summary>
    ///     Increments the redis key for the contraption's stored value. Resets to 0 if the amount put it over the cap.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>True if it went over and reset, false otherwise.</returns>
    private bool IncrContraption(int amount)
    {
        db.StringIncrement("contraption:value", amount);
        if ((int) db.StringGet("contraption:value") >= cap)
        {
            db.StringSet("contraption:value", 0);
            return true;
        }

        return false;
    }

    public int Feed(int amount)
    {
        if (!IncrContraption(amount))
            return 0;
        return rng.Next(1, 7);
    }

    public string CurrentValueString()
    {
        return $"{db.StringGet("contraption:value")}/{cap}";
    }
}