using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;

namespace Hitbot.Commands;

public class EconModule : BaseCommandModule
{
    public EconManager econ { get; set; }

    [Command("register")]
    [Description("Add your name to the books and get a starting balance")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        if (econ.BookHasKey(Program.GetBalancebookString(caller)))
        {
            await ctx.Channel.SendMessageAsync("You are already registered in this server.");
        }
        else
        {
            econ.BookSet(Program.GetBalancebookString(caller), econ.Startingamount);
            await ctx.Channel.SendMessageAsync("Registered :)");
        }
    }

    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx)
    {
        if (econ.BookHasKey(Program.GetBalancebookString(ctx.Member)))
            await ctx.Channel.SendMessageAsync(
                $"Your balance is {econ.BookGet(Program.GetBalancebookString(ctx.Member))} {econ.Currencyname}.");
    }

    //overload for getting someone elses balance
    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx,
        [Description("The person whose balance you want to see.")]
        DiscordMember target)
    {
        if (econ.BookHasKey(Program.GetBalancebookString(target)))
            await ctx.Channel.SendMessageAsync(
                $"{target.Nickname}'s balance is {econ.BookGet(Program.GetBalancebookString(target))} {econ.Currencyname}.");
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

        if (!econ.BookHasKey(callerString))
        {
            await ctx.Channel.SendMessageAsync("You aren't registered. Please do so with ~register.");
            return;
        }

        if (recipient.IsBot)
        {
            if (econ.BookGet(callerString) < amount)
            {
                await ctx.Channel.SendMessageAsync("Insufficient funds.");
                return;
            }

            //TODO: here
            // LottoModule.LottoBook["pot"] += amount;
            econ.BookDecr(callerString, amount);
            await ctx.Channel.SendMessageAsync(
                $"You tried to pay a bot, so I put your {amount} kromer into the lottery pot. Lol.");
            return;
        }

        if (!econ.BookHasKey(recipientString))
        {
            await ctx.Channel.SendMessageAsync("Registering recipient...");
            econ.BookSet(recipientString, econ.Startingamount);
        }

        if (econ.BookGet(callerString) < amount)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        econ.BookDecr(callerString, amount);
        econ.BookIncr(recipientString, amount);
        await ctx.Channel.SendMessageAsync(
            $"Paid {amount} {econ.Currencyname} to {recipient.Nickname} (you now have {econ.BookGet(callerString)}, " +
            $"they have {econ.BookGet(recipientString)})");
    }

    [Command("print")]
    [Description("Owner only. Prints new currency and gives to specified user.")]
    [RequireGuild]
    [RequireOwner]
    public async Task PrintCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        econ.BookIncr(Program.GetBalancebookString(recipient), amount);
        await ctx.Channel.SendMessageAsync($"{amount} new currency given to {recipient.Nickname}");
    }

    [Command("leaderboard")]
    [Description("Display a list of registered users sorted by descending balance.")]
    public async Task LeaderboardCommand(CommandContext ctx)
    {
        var dict = econ.BookAsDotnetDict();
        dict.Remove("bucket");
        var sorted = from entry in dict orderby entry.Value descending select entry;
        string result = "";
        int place = 1;
        foreach (var entry in sorted)
        {
            result += $"{place}. {entry.Key.Split("/")[1]} with {entry.Value} {econ.Currencyname}\n";
            place++;
        }

        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        var pages = interactivity.GeneratePagesInEmbed(result);

        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }
}