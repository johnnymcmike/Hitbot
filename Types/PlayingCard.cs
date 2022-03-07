﻿namespace Hitbot.Types;

public class PlayingCard
{
    public CardSuit Suit { get; set; }
    public CardNumber Num { get; set; }

    public override string ToString()
    {
        return $"{Enum.GetName(Num)} of {Enum.GetName(Suit)}";
    }
}

public enum CardSuit
{
    Clubs = 0,
    Diamonds,
    Hearts,
    Spades
}

public enum CardNumber
{
    Ace = 1,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King
}