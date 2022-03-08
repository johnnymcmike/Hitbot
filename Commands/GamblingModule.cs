using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;
using Hitbot.Types;

namespace Hitbot.Commands;

public class GamblingModule : BaseCommandModule
{
    private EconManager Econ { get; }
    private LottoManager Lotto { get; }
    private Random Rand { get; }

    public GamblingModule(EconManager eco, Random rng, LottoManager lot)
    {
        Econ = eco;
        Rand = rng;
        Lotto = lot;
    }

    [Command("slots")]
    public async Task SlotMachine(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        string callerString = Program.GetBalancebookString(caller);
        if (!Econ.BookHasKey(callerString) || Econ.BookGet(callerString) < 1)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        Econ.BookDecr(callerString);
        Lotto.IncrPot();

        var emojidefs = new List<KeyValuePair<string, int>>
        {
            new(":1kbtroll:", -420),
            new(":seven:", 300),
            new(":cherries:", 15),
            new(":cherries:", 15),
            new(":fish:", 20),
            new(":fish:", 20),
            new(":bigshot:", 15),
            new(":bigshot:", 15),
            new(":cowredeyes:", 10),
            new(":cowredeyes:", 10),
            new(":cowredeyes:", 10),
            new(":it:", 5),
            new(":it:", 5),
            new(":it:", 5)
        };
        string slotresultstr = " ";
        DiscordMessage slotmsg = await ctx.Channel.SendMessageAsync("Spinning...");
        await Task.Delay(2000);

        string[] possemo = emojidefs.Select(variable => variable.Key).ToArray();

        string[] results = new string[3];
        for (int i = 0; i < 3; i++)
        {
            string choice = possemo[Rand.Next(emojidefs.Count)];
            results[i] = choice;
            await Task.Delay(i * 1000);
            slotresultstr += DiscordEmoji.FromName(ctx.Client, choice).ToString();
            await slotmsg.ModifyAsync(slotresultstr);
        }

        for (int i = 0; i < possemo.Length; i++)
        {
            string emoji = possemo[i];
            if (results.Count(x => x == emoji) == 2)
            {
                int reward = emojidefs[i].Value / 3;
                Econ.BookIncr(callerString, reward);
                await ctx.RespondAsync($"Two {emoji}s! You win {reward} {Econ.Currencyname}! Yippee!");
                return;
            }

            if (results.Count(x => x == emoji) == 3)
            {
                int reward = emojidefs[i].Value;
                Econ.BookIncr(callerString, reward);
                await ctx.RespondAsync(
                    $"THREE {emoji}s! that's a JACKBOT baybee! {reward} {Econ.Currencyname}!!!");
                return;
            }
        }
    }

    [Command("duel")]
    public async Task DuelCommand(CommandContext ctx, DiscordMember target, int bet = 0)
    {
        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        DiscordMember? caller = ctx.Member;
        DiscordEmoji? triumph = DiscordEmoji.FromName(ctx.Client, ":triumph:");
        string callerstring = Program.GetBalancebookString(caller);
        string targetstring = Program.GetBalancebookString(target);
        if (Econ.BookGet(callerstring) < bet || Econ.BookGet(targetstring) < bet)
        {
            await ctx.RespondAsync("Insufficient funds on one or both sides.");
            return;
        }

        DiscordMessage? firstmsg =
            await ctx.Channel.SendMessageAsync($"Time for a duel! {target.Nickname}, react with {triumph} to accept!");
        await firstmsg.CreateReactionAsync(triumph);
        var result = await firstmsg.WaitForReactionAsync(target, triumph);

        if (result.TimedOut)
        {
            await ctx.RespondAsync("Timed out.");
            return;
        }

        int[] rnums = new int[3];
        for (int i = 0; i < 3; i++) rnums[i] = Rand.Next(1, 11);

        await ctx.Channel.SendMessageAsync("First one to say anything after I say \"GO\" wins.");
        await ctx.Channel.SendMessageAsync("Three...");
        await Task.Delay(rnums[0] * 1000);
        if (rnums[1] <= 7)
        {
            await ctx.Channel.SendMessageAsync("Two...");
            await Task.Delay(rnums[1] * 1000);
            if (rnums[2] <= 7)
            {
                await ctx.Channel.SendMessageAsync("One...");
                await Task.Delay(rnums[2] * 1000);
            }
        }

        await ctx.Channel.SendMessageAsync("GO");

        var wa = await interactivity.WaitForMessageAsync(x =>
            x.Channel.Id == ctx.Channel.Id && x.Author.Id == caller.Id || x.Author.Id == target.Id);
        DiscordMessage? winningMessage = wa.Result;

        if (wa.TimedOut || winningMessage is null)
        {
            await ctx.RespondAsync("Nobody won. You slackers.");
            return;
        }

        if (winningMessage.Author.Id == caller.Id)
        {
            Econ.BookIncr(callerstring, bet);
            Econ.BookDecr(targetstring, bet);
        }
        else
        {
            Econ.BookIncr(targetstring, bet);
            Econ.BookDecr(callerstring, bet);
        }

        await ctx.Channel.SendMessageAsync($"{winningMessage.Author.Username} won!");
        await ctx.RespondAsync(
            $"Resulting balances: {Econ.BookGet(callerstring)}, {Econ.BookGet(targetstring)}");
    }

    [Command("blackjack")]
    public async Task BlackJackCommand(CommandContext ctx)
    {
        DiscordEmoji diamondemoji = DiscordEmoji.FromName(ctx.Client, ":diamonds:");
        DiscordMessage entrymsg = await
            ctx.Channel.SendMessageAsync("Blackjack is starting! React " +
                                         $"{diamondemoji} within 20 seconds to enter.");
        await entrymsg.CreateReactionAsync(diamondemoji);

        var reactions = await entrymsg.CollectReactionsAsync(TimeSpan.FromSeconds(20));
        var users = new List<DiscordMember>();
        foreach (Reaction? reactionObject in reactions)
        {
            DiscordUser? reactedUser = reactionObject.Users.First();
            if (!reactedUser.Equals(ctx.Client.CurrentUser))
                users.Add((DiscordMember) reactedUser);
        }

        if (users.Count == 0)
        {
            await ctx.RespondAsync("Timed out.");
            return;
        }

        //DEBUG
        foreach (DiscordMember VARIABLE in users) Console.WriteLine(VARIABLE);

        var deck = new DeckOfCards();
        deck.Shuffle();

        //Dictionary where the keys are the DiscordUsers we just got, and the values start as an empty card list
        var usersAndHands = users.ToDictionary(x => x, _ => new List<PlayingCard>());
        //Draw for the dealer
        var dealerHand = new List<PlayingCard>
        {
            deck.DrawCard(),
            deck.DrawCard()
        };
        //Deal first two cards and tell people their hands
        foreach (var (key, value) in usersAndHands)
        {
            value.Add(deck.DrawCard());
            value.Add(deck.DrawCard());
            await key.SendMessageAsync($"Your hand is: {value[0]}, {value[1]}");
        }

        //Announce everyone's first card
        var firstCardAnnounce = "Here is everyone's first card:\n";
        firstCardAnnounce += $"Dealer: {dealerHand[0]}\n";
        foreach (var (key, value) in usersAndHands) firstCardAnnounce += $"{key.DisplayName}: {value[0]}";
        await ctx.Channel.SendMessageAsync(firstCardAnnounce);
        //Check for blackjacks
        var blackJackedUsers = new List<DiscordMember>();
        foreach (var (key, value) in usersAndHands)
            if (value.Exists(x => x.BlackJackValue == -1) && !value.Exists(x => x.Num == CardNumber.Ten) &&
                value.Exists(x => x.BlackJackValue == 10))
            {
                blackJackedUsers.Add(key);
                await ctx.Channel.SendMessageAsync($"{key.DisplayName} got a blackjack!");
            }

        //Begin turns
        await ctx.Channel.SendMessageAsync("When it's your turn, say \"hit\" to hit, and \"stand\" to stand.");

        foreach (var player in users) await ctx.Channel.SendMessageAsync($"{player.DisplayName}'s turn!");
    }
}