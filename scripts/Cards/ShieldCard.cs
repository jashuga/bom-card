/// <summary>Grants shield, which soaks damage before health does and does not regenerate.</summary>
public sealed class ShieldCard : Card
{
	private readonly float _shield;

	public ShieldCard(string title, string description, Rarity rarity, float shield)
		: base(title, description, rarity)
	{
		_shield = shield;
	}

	public override void Apply(PlayerController player) => player.Health.AddShield(_shield);
}
