using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Hitbot.Services;
using StackExchange.Redis;

namespace Hitbot.Commands;

[Group("conspiracy")]
[Description("Commands for social deduction game.")]
public class ConspiracyModule
{
    private EconManager Econ { get; }
    private DailyFlagManager Dailies { get; }
    private IDatabase db { get; }

    public ConspiracyModule(EconManager eco, DailyFlagManager da, ConnectionMultiplexer redis)
    {
        Econ = eco;
        Dailies = da;
        db = redis.GetDatabase();
    }

    [Command("hit")]
    public async Task HitCommand(CommandContext ctx, string tag)
    {
        await ctx.Message.DeleteAsync();
    }
}