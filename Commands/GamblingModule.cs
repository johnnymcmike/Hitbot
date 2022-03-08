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
        var interactivity = ctx.Client.GetInteractivity();
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

        var deck = new DeckOfCards();
        deck.Shuffle();

        //Dictionary where the keys are the DiscordUsers we just got, and the values start as an empty card list
        var playerHands = users.ToDictionary(x => x, _ => new BlackJackHand());
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
        var blackJackedUsers = new List<DiscordMember>();
        foreach (var (key, value) in playerHands) //this mess checks if you have a face and an ace with NO tens
            if (value.Cards.Exists(x => x.BlackJackValue == -1) && !value.Cards.Exists(x => x.Num == CardNumber.Ten) &&
                value.Cards.Exists(x => x.BlackJackValue == 10))
            {
                blackJackedUsers.Add(key);
                await ctx.Channel.SendMessageAsync($"{key.DisplayName} got a blackjack!");
            }

        if (blackJackedUsers.Count >= 1)
        {
            //TODO: what to do if multiple get blackjack, and implement checking for dealer blackjack
            await ctx.Channel.SendMessageAsync(
                "Exiting prematurely because 1 or more users got a blackjack. Remind me to finish this later");
            return;
        }

        await ctx.Channel.SendMessageAsync("-----------------------------------------");

        //Begin turns
        await ctx.Channel.SendMessageAsync("When it's your turn, say \"hit\" to hit, and \"stand\" to stand.");
        foreach (var currentPlayer in users)
        {
            var turnOver = false;
            await ctx.Channel.SendMessageAsync($"{currentPlayer.DisplayName}'s turn!");

            while (!turnOver)
            {
                //TODO: change this predicate such that it only accepts hit and stand or else it times out
                var action = await interactivity.WaitForMessageAsync(x => x.Author.Equals(currentPlayer));
                if (action.TimedOut)
                {
                    //TODO: behavior for if turn times out
                    await ctx.Channel.SendMessageAsync(
                        "Someone's turn timed out, so I'm exiting the whole game. Remind me to fix this.");
                    return;
                }

                var lala = action.Result.Content.ToLower();
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

        //Play dealer's turn
        await ctx.Channel.SendMessageAsync("My turn.");
        var dealerBust = false;
        while (dealerHand.GetHandValue() < 16)
        {
            await Task.Delay(500);
            await ctx.Channel.SendMessageAsync("I'm hitting...");
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
        //Print out everyone's hands
        var everyhand = "```Here's everyone's final hand.\n";
        foreach (var (key, value) in playerHands)
        {
            everyhand += $"-----------{key.DisplayName} had:\n";
            everyhand += value;
            everyhand += $"...with a value of {value.GetHandValue()}\n";
        }

        everyhand += "-----------Dealer had:\n" + dealerHand;
        everyhand += $"...with a value of {dealerHand.GetHandValue()}```";
        await ctx.Channel.SendMessageAsync(everyhand);
        Console.WriteLine("i made it to line 306!");

        //Determine winner
        playerHands.Add(ctx.Guild.CurrentMember, dealerHand);
        var currentWinner = ctx.Guild.CurrentMember;
        var duplicateScores = new Dictionary<DiscordMember, BlackJackHand>();
        foreach (var (dictkey, dictvalue) in playerHands)
            if (playerHands[currentWinner].GetHandValue() < dictvalue.GetHandValue())
            {
                currentWinner = dictkey;
            }
            else if (playerHands[currentWinner].GetHandValue() == dictvalue.GetHandValue() &&
                     dictvalue.GetHandValue() != 0)
            {
                duplicateScores.Add(currentWinner, playerHands[currentWinner]);
                duplicateScores.Add(dictkey, dictvalue);
            }

        await ctx.Channel.SendMessageAsync(
            $"I'm pretty sure {currentWinner.DisplayName} won but hey this isnt finished lol");
    }
}