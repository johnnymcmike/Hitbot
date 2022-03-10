using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Hitbot.Services;
using StackExchange.Redis;

namespace Hitbot.Commands;

[Group("contraption")]
[RequireGuild]
public class ContraptionModule
{
    private EconManager Econ { get; }
    private IDatabase db;

    public ContraptionModule(EconManager eco, ConnectionMultiplexer redis)
    {
        Econ = eco;
        db = redis.GetDatabase();
    }

    [Command("feed")]
    [Description("Feed the contraption your riches. Who knows what may come...")]
    public async Task FeedCommand(CommandContext ctx, int amount)
    {
    }
}