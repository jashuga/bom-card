/// <summary>Restores health, and optionally raises the ceiling.</summary>
public sealed class HealCard : Card
{
	private readonly float _heal;
	private readonly float _maxHealthBonus;

	public HealCard(string title, string description, Rarity rarity, float heal, float maxHealthBonus = 0f)
		: base(title, description, rarity)
	{
		_heal = heal;
		_maxHealthBonus = maxHealthBonus;
	}

	public override void Apply(PlayerController player)
	{
		if (_maxHealthBonus > 0f)
			player.Health.AddMaxHealth(_maxHealthBonus);
		if (_heal > 0f)
			player.Health.Heal(_heal);
	}
}
