using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
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
            new(":botemoji:", 5),
            new(":botemoji:", 5),
            new(":botemoji:", 5)
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
            slotresultstr +=
                DiscordEmoji.FromName(ctx.Client, choice).ToString(); //TODO: this is whats rate limiting you
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
        bet = Math.Abs(bet);
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
    [Description(
        "A text-based Blackjack game, optionally including bets and multiplayer. Pays 2 to 1. Dealer draws to 16.")]
    public async Task BlackJackCommand(CommandContext ctx, string mode = "free")
    {
        var interactivity = ctx.Client.GetInteractivity();
        var myemoji = DiscordEmoji.FromName(ctx.Client, ":black_joker:");
        DiscordMessage entrymsg = await
            ctx.Channel.SendMessageAsync($"A {mode} game of blackjack is starting! React " +
                                         $"{myemoji} within 20 seconds to enter.");
        await entrymsg.CreateReactionAsync(myemoji);

        var reactions = await entrymsg.CollectReactionsAsync(TimeSpan.FromSeconds(20));
        var players = new HashSet<DiscordMember>();
        foreach (var reactionObject in reactions)
        {
            var reactedUser = reactionObject.Users.First();
            if (!reactedUser.Equals(ctx.Client.CurrentUser))
                players.Add((DiscordMember) reactedUser);
        }

        if (players.Count == 0)
        {
            await ctx.RespondAsync("Timed out.");
            return;
        }

        var bets = new Dictionary<DiscordUser, int>();
        int pot = 0;
        if (mode != "free")
        {
            await ctx.Channel.SendMessageAsync("Getting everyone's bets...");
            foreach (var currentPlayer in players)
            {
                int thisBet = 0;
                await ctx.Channel.SendMessageAsync($"{currentPlayer.Mention}, what's your bet?");
                var action = await interactivity.WaitForMessageAsync(x =>
                    x.Author.Equals(currentPlayer) && int.TryParse(x.Content, out thisBet));
                if (action.TimedOut) await ctx.Channel.SendMessageAsync("Timed out, defaulting to 0.");
                thisBet = Math.Abs(thisBet);
                if (thisBet > Econ.BookGet(Program.GetBalancebookString(currentPlayer)))
                {
                    thisBet = Econ.BookGet(Program.GetBalancebookString(currentPlayer));
                    await ctx.Channel.SendMessageAsync(
                        $"You tried to bet more than you have, so you'll be betting {thisBet} {Econ.Currencyname}, which is all you have. :)");
                }

                bets.Add(currentPlayer, thisBet);
                Econ.BookDecr(Program.GetBalancebookString(currentPlayer), thisBet);
            }

            pot += bets.Sum(x => x.Value);
        }

        var deck = new DeckOfCards();
        deck.Shuffle();

        //Dictionary where the keys are the DiscordUsers we just got, and the values start as an empty card list
        var playerHands = players.ToDictionary(x => x, _ => new BlackJackHand());
        //Draw for the dealer
        var dealerHand = new BlackJackHand();
        dealerHand.Cards = new List<PlayingCard>
        {
            deck.DrawCard(),
            deck.DrawCard()
        };
        //Deal first two cards and tell people their hands
        foreach (var (key, value) in playerHands)
        {
            value.Cards.Add(deck.DrawCard());
            value.Cards.Add(deck.DrawCard());
            await key.SendMessageAsync($"Your hand is: {value}\n Your hand's value is {value.GetHandValue()}");
        }

        //Announce everyone's first card
        var firstCardAnnounce = "Here is everyone's first card:\n";
        firstCardAnnounce += $"Dealer: {dealerHand.Cards[0]}\n";
        foreach (var (key, value) in playerHands) firstCardAnnounce += $"{key.DisplayName}: {value.Cards[0]}\n";
        await ctx.Channel.SendMessageAsync(firstCardAnnounce);
        //Check for blackjacks
        // var blackJackedUsers = new List<DiscordMember>();
        // foreach (var (key, value) in playerHands) //this mess checks if you have a face and an ace with NO tens
        //     if (value.Cards.Exists(x => x.BlackJackValue == 1) && !value.Cards.Exists(x => x.Num == CardNumber.Ten) &&
        //         value.Cards.Exists(x => x.BlackJackValue == 10))
        //     {
        //         blackJackedUsers.Add(key);
        //         await ctx.Channel.SendMessageAsync($"{key.DisplayName} got a blackjack!");
        //     }
        //
        // if (blackJackedUsers.Count >= 1)
        // {
        //     //TODO: what to do if multiple get blackjack, and implement checking for dealer blackjack
        //     await ctx.Channel.SendMessageAsync(
        //         "Exiting prematurely because 1 or more users got a blackjack. Remind me to finish this later");
        //     return;
        // }

        await ctx.Channel.SendMessageAsync("-----------------------------------------");

        //Begin turns
        string[] possibleActions = {"hit", "stand"};

        await ctx.Channel.SendMessageAsync("When it's your turn, say \"hit\" to hit, and \"stand\" to stand.");
        foreach (var currentPlayer in players)
        {
            var turnOver = false;
            await ctx.Channel.SendMessageAsync($"{currentPlayer.DisplayName}'s turn!");

            while (!turnOver)
            {
                var action = await interactivity.WaitForMessageAsync(x =>
                    x.Author.Equals(currentPlayer) && possibleActions.Contains(x.Content.ToLower()));
                if (action.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync(
                        "Someone's turn timed out, so we're defaulting to stand.");
                    turnOver = true;
                    continue;
                }

                string lala = action.Result.Content.ToLower();
                if (lala == "hit")
                {
                    await ctx.Channel.SendMessageAsync("Hitting...");
                    var drawncard = deck.DrawCard();
                    playerHands[currentPlayer].Cards.Add(drawncard);
                    await currentPlayer.SendMessageAsync(
                        $"You got the {drawncard}. Your new hand value is {playerHands[currentPlayer].GetHandValue()}.");

                    if (playerHands[currentPlayer].GetHandValue() > 21)
                    {
                        await ctx.Channel.SendMessageAsync("You busted! Next turn...");
                        break;
                    }
                }
                else if (lala == "stand")
                {
                    turnOver = true;
                }
            }
        }

        await Task.Delay(2000);
        //Play dealer's turn
        await ctx.Channel.SendMessageAsync("My turn.");
        var dealerBust = false;
        while (dealerHand.GetHandValue() < 16)
        {
            await ctx.Channel.SendMessageAsync("I'm hitting...");
            await Task.Delay(1000);
            var drawnCard = deck.DrawCard();
            dealerHand.Cards.Add(drawnCard);
            if (dealerHand.GetHandValue() > 21)
            {
                await ctx.Channel.SendMessageAsync("I busted :(");
                dealerBust = true;
                break;
            }
        }

        if (!dealerBust)
            await ctx.Channel.SendMessageAsync("I stand.");
        await Task.Delay(2000);
        //Print out everyone's hands
        string everyhand = "```Here's everyone's final hand.\n";
        foreach (var (key, value) in playerHands)
        {
            everyhand += $"-----------{key.DisplayName} had:\n";
            everyhand += value;
            everyhand += $"...with a value of {value.GetHandValue()}\n";
        }

        everyhand += "-----------Dealer had:\n" + dealerHand;
        everyhand += $"...with a value of {dealerHand.GetHandValue()}```";
        await ctx.Channel.SendMessageAsync(everyhand);
        await Task.Delay(2000);
        //Determine winner
        playerHands.Add(ctx.Guild.CurrentMember, dealerHand);
        DiscordMember? currentWinner = null;
        var duplicateScores = new Dictionary<DiscordMember, BlackJackHand>();
        foreach (var (dictkey, dictvalue) in playerHands)
        {
            bool justSet = false;
            if (dictvalue.GetHandValue() > 21)
                continue;

            if (currentWinner is null)
            {
                currentWinner = dictkey;
                justSet = true;
            }

            if (playerHands[currentWinner].GetHandValue() < dictvalue.GetHandValue())
            {
                currentWinner = dictkey;
                justSet = true;
            }
            else if (playerHands[currentWinner].GetHandValue() == dictvalue.GetHandValue() && !justSet)
            {
                if (!duplicateScores.ContainsKey(currentWinner))
                    duplicateScores.Add(currentWinner, playerHands[currentWinner]);
                if (!duplicateScores.ContainsKey(dictkey))
                    duplicateScores.Add(dictkey, dictvalue);
            }
        }

        if (currentWinner is null)
        {
            await ctx.Channel.SendMessageAsync("nobody won lol");
            return;
        }

        if (duplicateScores.Count > 1 && duplicateScores.ContainsKey(currentWinner))
        {
            var realWinner = currentWinner;
            foreach (var (key, value) in duplicateScores)
                if (value.GetHandWeight() > duplicateScores[realWinner].GetHandWeight())
                {
                    realWinner = key;
                }
                else if (value.GetHandWeight() == duplicateScores[realWinner].GetHandWeight() &&
                         !duplicateScores[realWinner].Equals(value))
                {
                    await ctx.Channel.SendMessageAsync("There was a *true* tie, so all tied players win.");
                    foreach (var (winner, _) in duplicateScores)
                        if (!winner.Equals(ctx.Guild.CurrentMember))
                            Econ.BookIncr(Program.GetBalancebookString(winner),
                                (int) (2 * pot * ((float) bets[winner] / pot)));
                    return;
                }

            currentWinner = realWinner;
        }

        if (currentWinner.Equals(ctx.Guild.CurrentMember))
        {
            await ctx.Channel.SendMessageAsync("the house won ;)");
            return;
        }

        if (mode == "free" || pot == 0)
        {
            await ctx.Channel.SendMessageAsync(
                $"{currentWinner.Mention} won! :)");
        }
        else
        {
            int payout =
                (int) (2 * pot * ((float) bets[currentWinner] / pot));
            Econ.BookIncr(Program.GetBalancebookString(currentWinner), payout);
            await ctx.Channel.SendMessageAsync(
                $"{currentWinner.Mention} won, net-gaining {payout - bets[currentWinner]} {Econ.Currencyname}!");
        }
    }
}