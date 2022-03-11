using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Hitbot.Services;

namespace Hitbot.Commands;

[Group("contraption")]
[Hidden]
[RequireGuild]
public class ContraptionModule : BaseCommandModule
{
    private EconManager Econ { get; }
    private ContraptionManager Contraption { get; }

    public ContraptionModule(EconManager eco, ContraptionManager cont)
    {
        Econ = eco;
        Contraption = cont;
    }

    [Command("feed")]
    [Description("Feed the contraption your riches. Who knows what may come...")]
    public async Task FeedCommand(CommandContext ctx, int amount)
    {
        string callerstring = Program.GetBalancebookString(ctx.Member);
        if (amount > Econ.BookGet(callerstring))
        {
            await ctx.RespondAsync("Insufficient funds.");
            return;
        }

        Econ.BookDecr(callerstring, amount);
    }
}