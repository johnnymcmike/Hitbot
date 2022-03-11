using DSharpPlus.Entities;
using StackExchange.Redis;

namespace Hitbot.Services;

public class ContraptionManager
{
    private IDatabase db { get; }
    private Random rng { get; }
    private const int cap = 1000;
    private const int MethodRange = 4;

    public ContraptionManager(ConnectionMultiplexer redis)
    {
        db = redis.GetDatabase();
        rng = new Random();
    }

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

    public int Feed(int amount, DiscordMember user)
    {
        if (IncrContraption(amount))
            return 0;
        return 0;
    }
}