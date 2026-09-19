using System;

/// <summary>
/// Loads a magazine of special ammo into the gun of matching rarity. The central card type:
/// it's the only way to get anything other than basic bullets, and it's always temporary.
///
/// The factory runs at draft time, so every pick produces a fresh full magazine.
/// </summary>
public sealed class AmmoCard : Card
{
	private readonly Func<Magazine> _magazineFactory;

	public AmmoCard(string title, string description, Rarity rarity, Func<Magazine> magazineFactory)
		: base(title, description, rarity)
	{
		_magazineFactory = magazineFactory;
	}

	public override void Apply(PlayerController player) => player.Weapons.LoadMagazine(_magazineFactory());
}
