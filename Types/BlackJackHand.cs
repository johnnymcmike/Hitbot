namespace Hitbot.Types;

public class BlackJackHand
{
    public List<PlayingCard> Cards { get; set; }

    public BlackJackHand()
    {
        Clear();
    }

    public int GetHandValue()
    {
        var val = 0;
        var aces = new List<PlayingCard>();
        foreach (var card in Cards)
        {
            if (card.BlackJackValue == -1)
            {
                aces.Add(card);
                continue;
            }

            val += card.BlackJackValue;
        }

        foreach (var ace in aces)
            if (val + 11 > 21)
                val += 1;
            else
                val += 11;

        return val;
    }

    public void Clear()
    {
        Cards = new List<PlayingCard>();
    }

    public override string ToString()
    {
        var result = "";
        foreach (var card in Cards) result += card + "\n";

        return result;
    }
}