namespace Hitbot.Types;

public class DeckOfCards
{
    public List<PlayingCard> Cards;

    public DeckOfCards()
    {
        Cards = new List<PlayingCard>();
        Reset();
    }

    public void Reset()
    {
        Cards.Clear();
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 14; j++)
            Cards.Add(new PlayingCard {Suit = (CardSuit) i, Num = (CardNumber) j});
    }
}