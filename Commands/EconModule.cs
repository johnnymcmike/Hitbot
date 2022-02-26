using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;

namespace Hitbot.Commands;

public class EconModule : BaseCommandModule
{
    private EconManager Econ { get; }
    private DailyFlagManager Daily { get; }

    public EconModule(EconManager eco, DailyFlagManager dail)
    {
        Econ = eco;
        Daily = dail;
    }

    [Command("register")]
    [Description("Add your name to the books and get a starting balance")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        if (Econ.BookHasKey(Program.GetBalancebookString(caller)))
        {
            await ctx.Channel.SendMessageAsync("You are already registered in this server.");
        }
        else
        {
            Econ.BookSet(Program.GetBalancebookString(caller), Econ.Startingamount);
            await ctx.Channel.SendMessageAsync("Registered :)");
        }
    }

    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx)
    {
        if (Econ.BookHasKey(Program.GetBalancebookString(ctx.Member)))
            await ctx.Channel.SendMessageAsync(
                $"Your balance is {Econ.BookGet(Program.GetBalancebookString(ctx.Member))} {Econ.Currencyname}.");
    }

    //overload for getting someone elses balance
    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx,
        [Description("The person whose balance you want to see.")]
        DiscordMember target)
    {
        if (Econ.BookHasKey(Program.GetBalancebookString(target)))
            await ctx.Channel.SendMessageAsync(
                $"{target.Nickname}'s balance is {Econ.BookGet(Program.GetBalancebookString(target))} {Econ.Currencyname}.");
    }

    [Command("pay")]
    [Description("Pay currency to another user.")]
    public async Task PayCommand(CommandContext ctx,
        [Description("The @ of the person you intend to pay (you do have to @ them)")]
        DiscordMember recipient,
        [Description("The amount to be paid. No decimals.")]
        int amount)
    {
        amount = Math.Abs(amount);
        DiscordMember? caller = ctx.Member;
        if (caller.Equals(recipient))
        {
            await ctx.Channel.SendMessageAsync("You can't pay yourself!");
            return;
        }

        string callerString = Program.GetBalancebookString(caller);
        string recipientString = Program.GetBalancebookString(recipient);

        if (!Econ.BookHasKey(callerString))
        {
            await ctx.Channel.SendMessageAsync("You aren't registered. Please do so with ~register.");
            return;
        }

        if (recipient.IsBot)
        {
            if (Econ.BookGet(callerString) < amount)
            {
                await ctx.Channel.SendMessageAsync("Insufficient funds.");
                return;
            }

            //TODO: here
            // LottoModule.LottoBook["pot"] += amount;
            Econ.BookDecr(callerString, amount);
            await ctx.Channel.SendMessageAsync(
                $"You tried to pay a bot, so I put your {amount} kromer into the lottery pot. Lol.");
            return;
        }

        if (!Econ.BookHasKey(recipientString))
        {
            await ctx.Channel.SendMessageAsync("Registering recipient...");
            Econ.BookSet(recipientString, Econ.Startingamount);
        }

        if (Econ.BookGet(callerString) < amount)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        Econ.BookDecr(callerString, amount);
        Econ.BookIncr(recipientString, amount);
        await ctx.Channel.SendMessageAsync(
            $"Paid {amount} {Econ.Currencyname} to {recipient.Nickname} (you now have {Econ.BookGet(callerString)}, " +
            $"they have {Econ.BookGet(recipientString)})");
    }

    [Command("print")]
    [Description("Owner only. Prints new currency and gives to specified user.")]
    [RequireGuild]
    [RequireOwner]
    public async Task PrintCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        Econ.BookIncr(Program.GetBalancebookString(recipient), amount);
        await ctx.Channel.SendMessageAsync($"{amount} new currency given to {recipient.Nickname}");
    }

    [Command("leaderboard")]
    [Description("Display a list of registered users sorted by descending balance.")]
    public async Task LeaderboardCommand(CommandContext ctx)
    {
        var dict = Econ.BookAsDotnetDict();
        dict.Remove("bucket");
        var sorted = from entry in dict orderby entry.Value descending select entry;
        string result = "";
        int place = 1;
        foreach (var entry in sorted)
        {
            result += $"{place}. {entry.Key.Split("/")[1]} with {entry.Value} {Econ.Currencyname}\n";
            place++;
        }

        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        var pages = interactivity.GeneratePagesInEmbed(result);

        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }

    [Command("claimdaily")]
    [Description("Claim your daily amount of currency allowed.")]
    public async Task ClaimDailyCommand(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        string callerString = Program.GetBalancebookString(caller);
        if (!Daily.DailyExists(callerString, "dailykromer"))
        {
            Econ.BookIncr(callerString, 10); //TODO: dont hardcode this
            Daily.SetDaily(callerString, "dailykromer");
            await ctx.RespondAsync($"Enjoy your 10 {Econ.Currencyname}!");
        }
        else
        {
            await ctx.RespondAsync("You have already claimed this today.");
        }
    }

    [Command("turgle")]
    [Description("Bad idea.")]
    public async Task TurgleCommand(CommandContext ctx, int amount = 20)
    {
        string callerstring = Program.GetBalancebookString(ctx.Member);
        Econ.BookDecr(callerstring, amount);
        await ctx.RespondAsync(
            $"You turgled away {amount} perfectly good {Econ.Currencyname}, leaving you with {Econ.BookGet(callerstring)}.");
    }
}