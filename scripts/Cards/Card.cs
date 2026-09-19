/// <summary>
/// A draftable card. Plain C# — cards are built in <see cref="CardLibrary"/>, not authored
/// as resources, so adding one is a single entry in that file plus (optionally) a subclass.
/// </summary>
public abstract class Card
{
	public string Title { get; }
	public string Description { get; }
	public Rarity Rarity { get; }

	protected Card(string title, string description, Rarity rarity)
	{
		Title = title;
		Description = description;
		Rarity = rarity;
	}

	public abstract void Apply(PlayerController player);
}
