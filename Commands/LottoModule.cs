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
        Lotto.IncrPot(Lotto.LottoTicketprice);
        await ctx.RespondAsync("You are entered : )");
    }

    [Command("draw")]
    [Description(
        "For a fee, draw the lottery. This either pays out the whole pot to one lucky winner, or nobody gets it and the pot is preserved.")]
    public async Task DrawLottoCommand(CommandContext ctx)
    {
        if (Econ.BookGet(Program.GetBalancebookString(ctx.Member)) < Lotto.LottoTicketprice)
        {
            await ctx.RespondAsync("Insufficient funds.");
            return;
        }

        if (Dailies.DailyExists("any", "lottodraw"))
        {
            await ctx.RespondAsync("Lotto was already drawn today. Come back tomorrow!");
            return;
        }

        var lottoList = Lotto.LottoUsersAsList();
        if (lottoList.Count <= 1)
        {
            await ctx.RespondAsync("Not enough people to draw.");
            return;
        }

        Econ.BookDecr(Program.GetBalancebookString(ctx.Member), Lotto.LottoDrawprice);
        Lotto.IncrPot(Lotto.LottoDrawprice);

        string winner = lottoList[Rand.Next(0, lottoList.Count)];
        int nobody = Rand.Next(0, 8);
        if (nobody == 0)
        {
            int previousPot = Lotto.Pot;
            await ctx.RespondAsync("Nobody won. The pot has been preserved. Better luck next time!");
            Lotto.ClearLotto();
            Lotto.IncrPot(previousPot);
            Dailies.SetDaily("any", "lottodraw");
            return;
        }


        Econ.BookIncr(winner, Lotto.Pot);
        await ctx.RespondAsync($"{winner.Split("/")[1]} won, gaining {Lotto.Pot} {Econ.Currencyname}. Yippee!");
        Dailies.SetDaily("any", "lottodraw");
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