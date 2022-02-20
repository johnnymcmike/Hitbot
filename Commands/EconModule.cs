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

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        econ.WriteBalances();
        return Task.Delay(0);
    }

    [Command("register")]
    [Description("Add your name to the books and get a starting balance")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        if (econ.BalanceBook.ContainsKey(econ.GetBalancebookString(caller)))
        {
            await ctx.RespondAsync("You are already registered in this server.");
        }
        else
        {
            econ.BalanceBook.Add(econ.GetBalancebookString(caller), econ.startingamount);
            await ctx.RespondAsync("Registered :)");
        }
    }

    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx)
    {
        if (econ.BalanceBook.ContainsKey(econ.GetBalancebookString(ctx.Member)))
            await ctx.RespondAsync(
                $"Your balance is {econ.BalanceBook[econ.GetBalancebookString(ctx.Member)]} {econ.Currencyname}.");
    }

    //overload for getting someone elses balance
    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx,
        [Description("The person whose balance you want to see.")]
        DiscordMember target)
    {
        if (econ.BalanceBook.ContainsKey(econ.GetBalancebookString(target)))
            await ctx.RespondAsync(
                $"{target.Nickname}'s balance is {econ.BalanceBook[econ.GetBalancebookString(target)]} {econ.Currencyname}.");
    }

    [Command("pay")]
    [Description("Pay currency to another user.")]
    public async Task PayCommand(CommandContext ctx,
        [Description("The @ of the person you intend to pay (you do have to @ them)")]
        DiscordMember recipient,
        [Description("The amount to be paid. No decimals.")]
        int amount)
    {
        DiscordMember? caller = ctx.Member;
        if (caller.Equals(recipient))
        {
            await ctx.RespondAsync("You can't pay yourself!");
            return;
        }

        if (!econ.BalanceBook.ContainsKey(econ.GetBalancebookString(caller)))
        {
            await ctx.RespondAsync("You aren't registered. Please do so with ~register.");
            return;
        }

        if (!econ.BalanceBook.ContainsKey(econ.GetBalancebookString(recipient)))
        {
            await ctx.RespondAsync("Registering recipient...");
            econ.BalanceBook.Add(econ.GetBalancebookString(recipient), econ.startingamount);
        }

        if (econ.BalanceBook[econ.GetBalancebookString(caller)] < amount)
        {
            await ctx.RespondAsync("Insufficient funds.");
            return;
        }

        econ.BalanceBook[econ.GetBalancebookString(caller)] -= Math.Abs(amount);
        econ.BalanceBook[econ.GetBalancebookString(recipient)] += Math.Abs(amount);
        await ctx.RespondAsync(
            $"Paid {amount} {econ.Currencyname} to {recipient.Nickname} (you now have {econ.BalanceBook[econ.GetBalancebookString(caller)]}, " +
            $"they have {econ.BalanceBook[econ.GetBalancebookString(recipient)]})");
    }

    [Command("print")]
    [Description("Owner only. Prints new currency and gives to specified user.")]
    [RequireGuild]
    [RequireOwner]
    public async Task PrintCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        econ.BalanceBook[econ.GetBalancebookString(recipient)] += amount;
        await ctx.RespondAsync($"{amount} new currency given to {recipient.Nickname}");
    }

    [Command("leaderboard")]
    [Description("Display a list of registered users sorted by descending balance.")]
    public async Task LeaderboardCommand(CommandContext ctx)
    {
        var sorted = from entry in econ.BalanceBook orderby entry.Value descending select entry;
        string result = "";
        int place = 1;
        foreach (var entry in sorted)
        {
            result += $"{place}. {entry.Key.Split("/")[1]} with {entry.Value}\n";
            place++;
        }

        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        var pages = interactivity.GeneratePagesInEmbed(result);

        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }
}