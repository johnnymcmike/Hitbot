using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;
using StackExchange.Redis;

namespace Hitbot.Commands;

[Group("conspiracy")]
[Description("Commands for social deduction game.")]
[RequireGuild]
public class ConspiracyModule : BaseCommandModule
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
    public async Task HitCommand(CommandContext ctx, ulong tag)
    {
        DiscordMember? caller = ctx.Member;
        var mems = ctx.Guild.Members;
        if (!mems.ContainsKey(tag))
        {
            await ctx.RespondAsync("Not a valid ID in this server. Try again.");
            return;
        }

        DiscordMember? target = mems[tag];
    }
}