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
		new AmmoCard("Piercing Rounds", "50 rounds. Passes through up to 4 enemies.",
			Rarity.Common, () => new Magazine("Piercing", Rarity.Common, Scenes.PiercingRound, fireRateMultiplier: 0.8f)),

		// ---- Rare ammo (mag 12) ---------------------------------------------------
		new AmmoCard("Explosive Shells", "25 rounds. Detonates on impact for area damage.",
			Rarity.Rare, () => new Magazine("Explosive", Rarity.Rare, Scenes.ExplosiveRound)),

		new AmmoCard("Ricochet Rounds", "25 rounds. Bounces off walls up to 4 times.",
			Rarity.Rare, () => new Magazine("Ricochet", Rarity.Rare, Scenes.RicochetRound, fireRateMultiplier: 1.2f)),

		// ---- Epic ammo (mag 4) ----------------------------------------------------
		new AmmoCard("Homing Missiles", "10 rounds. Seeks the nearest enemy and detonates.",
			Rarity.Epic, () => new Magazine("Homing", Rarity.Epic, Scenes.HomingRound, fireRateMultiplier: 0.8f)),

		new AmmoCard("Laser Cells", "50 rounds. Instant beam that burns through everything in a line.",
			Rarity.Common, () => new Magazine("Laser", Rarity.Common, Scenes.LaserRound)),
		new AmmoCard("Piercing Rounds", "50 rounds. Passes through up to 4 enemies.",
			Rarity.Common, () => new Magazine("Piercing", Rarity.Common, Scenes.PiercingRound, fireRateMultiplier: 0.8f)),

		// ---- Rare ammo (mag 25) ---------------------------------------------------
		new AmmoCard("Explosive Shells", "25 rounds. Detonates on impact for area damage.",
			Rarity.Rare, () => new Magazine("Explosive", Rarity.Rare, Scenes.ExplosiveRound)),

		new AmmoCard("Ricochet Rounds", "25 rounds. Bounces off walls up to 4 times.",
			Rarity.Rare, () => new Magazine("Ricochet", Rarity.Rare, Scenes.RicochetRound, fireRateMultiplier: 1.2f)),

		// ---- Epic ammo (mag 4) ----------------------------------------------------
		new AmmoCard("Homing Missiles", "10 rounds. Seeks the nearest enemy and detonates.",
			Rarity.Epic, () => new Magazine("Homing", Rarity.Epic, Scenes.HomingRound, fireRateMultiplier: 0.8f)),

		new AmmoCard("Laser Cells", "50 rounds. Instant beam that burns through everything in a line.",
			Rarity.Common, () => new Magazine("Laser", Rarity.Common, Scenes.LaserRound)),

		// ---- Sustain --------------------------------------------------------------
		new HealCard("Field Repair", "Restore 25 health.", Rarity.Rare, heal: 25f),
		new ShieldCard("Shield Cell", "Gain 20 shield.", Rarity.Rare, shield: 20f),
		new HealCard("Nanite Patch", "Restore 50 health.", Rarity.Epic, heal: 50f),
		new ShieldCard("Barrier Matrix", "Gain 40 shield.", Rarity.Epic, shield: 40f),
		new HealCard("Vitality Core", "+25 max health, fully applied now.", Rarity.Epic, heal: 0f, maxHealthBonus: 25f),
		new HealCard("Combat Stims", "Restore all health and +40 max health.", Rarity.SuperEpic, heal: 1000f, maxHealthBonus: 40f),
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
		Rarity.Common => 100f,
		Rarity.Rare => Mathf.Min(40f, 25f + wave * 2f),
		Rarity.Epic => Mathf.Min(10f, 5f + wave * 1.5f),
		Rarity.SuperEpic => Mathf.Min(5f, 2f + wave * 0.5f),
		_ => 0f,
	};
}
