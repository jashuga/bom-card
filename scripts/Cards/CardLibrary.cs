using System.Collections.Generic;
using Godot;

/// <summary>
/// Every card in the game, and the weighted draft roll. Adding a card = one entry here.
///
/// Magazine sizes come from the rarity (Common 30 / Rare 12 / Epic 4) via
/// <see cref="RarityRules.MagazineSize"/> — don't hardcode them in the factories.
/// </summary>
public static class CardLibrary
{
	private static readonly List<Card> All = new()
	{
		// ---- Common ammo (mag 30) -------------------------------------------------
		new AmmoCard("Standard Rounds", "30 rounds. Faster, harder-hitting bullets for the Sidearm.",
			Rarity.Common, () => new Magazine("Standard", Rarity.Common, Scenes.StandardBullet)),

		new AmmoCard("Piercing Rounds", "30 rounds. Passes through up to 4 enemies.",
			Rarity.Common, () => new Magazine("Piercing", Rarity.Common, Scenes.PiercingBullet, fireRateMultiplier: 0.8f)),

		// ---- Rare ammo (mag 12) ---------------------------------------------------
		new AmmoCard("Explosive Shells", "12 rounds. Detonates on impact for area damage.",
			Rarity.Rare, () => new Magazine("Explosive", Rarity.Rare, Scenes.ExplosiveBullet)),

		new AmmoCard("Ricochet Rounds", "12 rounds. Bounces off walls up to 4 times.",
			Rarity.Rare, () => new Magazine("Ricochet", Rarity.Rare, Scenes.RicochetBullet, fireRateMultiplier: 1.2f)),

		// ---- Epic ammo (mag 4) ----------------------------------------------------
		new AmmoCard("Homing Missiles", "4 rounds. Seeks the nearest enemy and detonates.",
			Rarity.Epic, () => new Magazine("Homing", Rarity.Epic, Scenes.HomingBullet, fireRateMultiplier: 0.8f)),

		new AmmoCard("Laser Cells", "4 rounds. Instant beam that burns through everything in a line.",
			Rarity.Epic, () => new Magazine("Laser", Rarity.Epic, Scenes.LaserBullet)),

		// ---- Sustain --------------------------------------------------------------
		new HealCard("Field Repair", "Restore 35 health.", Rarity.Common, heal: 35f),
		new ShieldCard("Shield Cell", "Gain 30 shield.", Rarity.Common, shield: 30f),
		new HealCard("Nanite Patch", "Restore 70 health.", Rarity.Rare, heal: 70f),
		new ShieldCard("Barrier Matrix", "Gain 60 shield.", Rarity.Rare, shield: 60f),
		new HealCard("Vitality Core", "+25 max health, fully applied now.", Rarity.Rare, heal: 0f, maxHealthBonus: 25f),
		new HealCard("Combat Stims", "Restore 150 health and +40 max health.", Rarity.Epic, heal: 150f, maxHealthBonus: 40f),
	};

	/// <summary>Draft roll: <paramref name="count"/> distinct cards, rarer ones showing up more as waves climb.</summary>
	public static List<Card> Draw(int count, int wave, RandomNumberGenerator rng)
	{
		var pool = new List<Card>(All);
		var picked = new List<Card>(count);

		while (picked.Count < count && pool.Count > 0)
		{
			float total = 0f;
			foreach (Card card in pool)
				total += Weight(card.Rarity, wave);

			float roll = rng.Randf() * total;
			int chosen = pool.Count - 1;

			for (int i = 0; i < pool.Count; i++)
			{
				roll -= Weight(pool[i].Rarity, wave);
				if (roll <= 0f)
				{
					chosen = i;
					break;
				}
			}

			picked.Add(pool[chosen]);
			pool.RemoveAt(chosen);
		}

		return picked;
	}

	private static float Weight(Rarity rarity, int wave) => rarity switch
	{
		Rarity.Common => 60f,
		Rarity.Rare => Mathf.Min(45f, 22f + wave * 3f),
		Rarity.Epic => Mathf.Min(30f, 4f + wave * 2.5f),
		_ => 0f,
	};
}
