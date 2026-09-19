using System.Collections.Generic;

/// <summary>
/// Everything the player has drafted this run, newest last. Cards currently apply the
/// moment they're picked; the deck is the record of that, and the hook for any later
/// "draw from your deck" mechanic.
/// </summary>
public sealed class Deck
{
	private readonly List<Card> _cards = new();

	public IReadOnlyList<Card> Cards => _cards;
	public int Count => _cards.Count;

	public void Add(Card card)
	{
		if (card != null)
			_cards.Add(card);
	}

	public int CountOf(Rarity rarity)
	{
		int n = 0;
		foreach (Card card in _cards)
		{
			if (card.Rarity == rarity)
				n++;
		}
		return n;
	}

	public void Clear() => _cards.Clear();
}
