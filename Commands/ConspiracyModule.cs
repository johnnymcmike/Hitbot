using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Hitbot.Services;
using Newtonsoft.Json;
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

    private const int MaxHitCount = 4;
    private readonly List<Player> conspiracyPlayers;

    public ConspiracyModule(EconManager eco, DailyFlagManager da, ConnectionMultiplexer redis)
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        Econ = eco;
        Dailies = da;
        db = redis.GetDatabase();
        if (db.KeyExists("conspiracy"))
            conspiracyPlayers = JsonConvert.DeserializeObject<List<Player>>(db.StringGet("conspiracy"))!;
        else
            conspiracyPlayers = new List<Player>();
    }

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        return Task.Delay(0);
    }

    private async void SaveHitbook()
    {
        TimeSpan untilmidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
        db.StringSet("conspiracy", JsonConvert.SerializeObject(conspiracyPlayers), untilmidnight);
    }

    [Command("hit")]
    public async Task HitCommand(CommandContext ctx, ulong targetId)
    {
        ulong callerId = ctx.Member.Id;
        bool playerIsInList = conspiracyPlayers.Any(p => p.Id == callerId);

        if (playerIsInList)
        {
            int index = conspiracyPlayers.FindIndex(p => p.Id == callerId);
            conspiracyPlayers[index].OutgoingHit = targetId;
        }
        else
        {
            conspiracyPlayers.Add(new Player {Id = callerId, OutgoingHit = targetId});
        }

        bool targetIsInList = conspiracyPlayers.Any(p => p.Id == targetId);
        if (targetIsInList)
        {
            int index = conspiracyPlayers.FindIndex(p => p.Id == targetId);
            conspiracyPlayers[index].IncomingHits.Add(callerId);
        }
        else
        {
            var newTargetPlayer = new Player {Id = targetId};
            newTargetPlayer.IncomingHits.Add(callerId);
            conspiracyPlayers.Add(newTargetPlayer);
        }

        await ctx.Message.DeleteAsync();
    }

    private class Player
    {
        public ulong Id { get; set; }
        public ulong OutgoingHit { get; set; }
        public List<ulong> IncomingHits { get; set; }
    }
}