﻿namespace Hitbot.Types;

public class DeckOfCards
{
    public List<PlayingCard> Cards;
    private readonly Random rng;

    public DeckOfCards()
    {
        Cards = new List<PlayingCard>();
        Reset();
        rng = new Random();
    }

    private void Reset()
    {
        Cards.Clear();
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 14; j++)
            Cards.Add(new PlayingCard {Suit = (CardSuit) i, Num = (CardNumber) j});
    }

    public void Shuffle()
    {
        var shuffledcards = Cards.OrderBy(a => rng.Next()).ToList();
        Cards = shuffledcards;
    }
}