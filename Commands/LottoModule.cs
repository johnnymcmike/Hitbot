using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;

namespace Hitbot.Commands;

[Group("lotto")]
[Description("Lottery commands.")]
[RequireGuild]
public class LottoModule : BaseCommandModule
{
    private LottoManager Lotto { get; }

    private EconManager Econ { get; }

    private Random Rand { get; }
    private DailyFlagManager Dailies { get; }

    public LottoModule(LottoManager lotto, EconManager eco, Random rng, DailyFlagManager dail)
    {
        Lotto = lotto;
        Econ = eco;
        Rand = rng;
        Dailies = dail;
    }


    [Command("buyticket")]
    [Description("Buy a lottery ticket for a preset fee.")]
    public async Task EnterLottoCommand(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        string callerstring = Program.GetBalancebookString(caller);
        if (Econ.BookGet(callerstring) < Lotto.LottoTicketprice)
        {
            await ctx.RespondAsync("Insufficient funds.");
            return;
        }

        Econ.BookDecr(callerstring, Lotto.LottoTicketprice);
        Lotto.EnterLotto(callerstring);
        await ctx.RespondAsync("You are entered : )");
    }

    [Command("draw")]
    [Description(
        "For a fee, draw the lottery. This either pays out the whole pot to one lucky winner, or nobody gets it and the pot is preserved.")]
    public async Task DrawLottoCommand(CommandContext ctx)
    {
        var lottoList = Lotto.LottoUsersAsList();
        string winner;
        try
        {
            winner = lottoList[Rand.Next(0, lottoList.Count + 1)];
        }
        catch (IndexOutOfRangeException)
        {
            int previousPot = Lotto.Pot;
            await ctx.RespondAsync("Nobody won. The pot has been preserved. Better luck next time!");
            Lotto.ClearLotto();
            Lotto.IncrPot(previousPot);
            return;
        }

        Econ.BookIncr(winner, Lotto.Pot);
        await ctx.RespondAsync($"{winner.Split("/")[1]} won, gaining {Lotto.Pot} {Econ.Currencyname}. Yippee!");
        Lotto.ClearLotto();
    }

    [Command("view")]
    [Description("Display all entered users and see the pot.")]
    public async Task ViewLottoCommand(CommandContext ctx)
    {
        var lottoList = Lotto.LottoUsersAsList();
        string result = $"The pot is {Lotto.Pot}. {lottoList.Count} users are entered. They are:\n";
        foreach (string entry in lottoList)
        {
            result += $"{entry.Split("/")[1]}\n";
        }

        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        var pages = interactivity.GeneratePagesInEmbed(result);

        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }
}