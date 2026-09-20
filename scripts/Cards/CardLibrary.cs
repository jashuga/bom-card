using System.Collections.Generic;
using Godot;

/// <summary>
/// Every card in the game, and the weighted draft roll. Adding a card = one entry here.
///
/// Magazine sizes come from the rarity (Common 50 / Rare 25 / Epic 10) via
/// <see cref="RarityRules.MagazineSize"/> — don't hardcode them in the factories.
/// </summary>
public static class CardLibrary
{
	// ---- draft bias -------------------------------------------------------------
	// Guns are the point of the game; heal and shield cards are the garnish. Rarity alone
	// used to decide the roll, which made every Common slot a three-way race between one
	// ammo card and two sustain cards — so the draft felt like it handed out shields.
	// These multiply the rarity weight. Tune here, not in the rarity table.

	/// <summary>Ammo cards are the baseline. Everything else is scaled against this.</summary>
	private const float AmmoWeight = 1.00f;

	/// <summary>Heals stay a real option, just not the default one.</summary>
	private const float HealWeight = 0.30f;

	/// <summary>Shields are the rarest garnish — roughly one sixth as likely as a gun.</summary>
	private const float ShieldWeight = 0.18f;

	private static readonly List<Card> All = new()
	{
		new AmmoCard("Piercing Rounds", "50 rounds. Passes through up to 4 enemies.",
			Rarity.Common, () => new Magazine("Piercing", Rarity.Common, Scenes.PiercingRound, fireRateMultiplier: 0.8f)),

		// ---- Rare ammo (mag 25) ---------------------------------------------------
		new AmmoCard("Explosive Shells", "25 rounds. Detonates on impact for area damage.",
			Rarity.Rare, () => new Magazine("Explosive", Rarity.Rare, Scenes.ExplosiveRound)),

		new AmmoCard("Ricochet Rounds", "25 rounds. Bounces off walls up to 4 times.",
			Rarity.Rare, () => new Magazine("Ricochet", Rarity.Rare, Scenes.RicochetRound, fireRateMultiplier: 1.2f)),

		// ---- Epic ammo -------------------------------------------------------------
		// Both Epic cards override the rarity's magazine size (10). A round of either is worth
		// far more than a Rare one, so the Epic count is set per card by how strong the round
		// actually is rather than by the rarity table. Everything else still takes its size
		// from RarityRules.MagazineSize.
		new AmmoCard("Homing Missiles", "7 rounds. Seeks the nearest enemy and detonates.",
			Rarity.Epic, () => new Magazine("Homing", Rarity.Epic, Scenes.HomingRound,
				fireRateMultiplier: 0.8f, rounds: 7)),

		// The beam is an instant kill across a 48px lane and does not stop at enemies, so it
		// is the strongest round in the game per shot — the smallest magazine of anything.
		new AmmoCard("Laser Cells", "6 rounds. Instant beam that burns through everything in a line.",
			Rarity.Epic, () => new Magazine("Laser", Rarity.Epic, Scenes.LaserRound, rounds: 6)),

		// ---- Sustain --------------------------------------------------------------
		new HealCard("Field Repair", "Restore 35 health.", Rarity.Common, heal: 35f),
		new ShieldCard("Shield Cell", "Gain 30 shield.", Rarity.Common, shield: 30f),
		new HealCard("Nanite Patch", "Restore 70 health.", Rarity.Rare, heal: 70f),
		new ShieldCard("Barrier Matrix", "Gain 60 shield.", Rarity.Rare, shield: 60f),
		new HealCard("Vitality Core", "+25 max health, fully applied now.", Rarity.Rare, heal: 0f, maxHealthBonus: 25f),
		new HealCard("Combat Stims", "Restore 150 health and +40 max health.", Rarity.Epic, heal: 150f, maxHealthBonus: 40f),
	};

	/// <summary>
	/// Draft roll: <paramref name="count"/> distinct cards, rarer ones showing up more as waves
	/// climb, and guns showing up far more than sustain at every rarity.
	///
	/// The first pick is drawn from the ammo cards only, so a draft can never be three
	/// heals — with a 3-card offer that is the difference between "usually a gun" and
	/// "always at least one gun".
	/// </summary>
	public static List<Card> Draw(int count, int wave, RandomNumberGenerator rng)
	{
		var pool = new List<Card>(All);
		var picked = new List<Card>(count);

		if (count > 0)
			TakeOne(pool, picked, wave, rng, ammoOnly: true);

		while (picked.Count < count && pool.Count > 0)
			TakeOne(pool, picked, wave, rng, ammoOnly: false);

		// Otherwise the guaranteed gun would always sit in slot 1 and the offer would read
		// as rigged, which it is — just not in an order the player should be able to see.
		Shuffle(picked, rng);

		return picked;
	}

	/// <summary>
	/// Weighted draw of a single card out of <paramref name="pool"/> and into
	/// <paramref name="picked"/>. Does nothing if the filter matches no remaining card.
	/// </summary>
	private static void TakeOne(List<Card> pool, List<Card> picked, int wave,
		RandomNumberGenerator rng, bool ammoOnly)
	{
		Dictionary<Rarity, float> shares = KindTotalsByRarity(pool, ammoOnly);

		float total = 0f;
		foreach (Card card in pool)
		{
			if (!ammoOnly || card is AmmoCard)
				total += Weight(card, wave, shares);
		}

		if (total <= 0f)
			return;

		float roll = rng.Randf() * total;
		int chosen = -1;

		for (int i = 0; i < pool.Count; i++)
		{
			if (ammoOnly && pool[i] is not AmmoCard)
				continue;

			chosen = i; // remember the last eligible index, so rounding can never fall off the end
			roll -= Weight(pool[i], wave, shares);

			if (roll <= 0f)
				break;
		}

		if (chosen < 0)
			return;

		picked.Add(pool[chosen]);
		pool.RemoveAt(chosen);
	}

	/// <summary>
	/// Total kind weight per rarity across the cards eligible for this draw — the divisor that
	/// keeps a rarity's share independent of how many cards it contains.
	/// </summary>
	private static Dictionary<Rarity, float> KindTotalsByRarity(List<Card> pool, bool ammoOnly)
	{
		var totals = new Dictionary<Rarity, float>();

		foreach (Card card in pool)
		{
			if (ammoOnly && card is not AmmoCard)
				continue;

			totals[card.Rarity] = totals.GetValueOrDefault(card.Rarity) + KindWeight(card);
		}

		return totals;
	}

	private static void Shuffle(List<Card> cards, RandomNumberGenerator rng)
	{
		for (int i = cards.Count - 1; i > 0; i--)
		{
			int j = rng.RandiRange(0, i);
			(cards[i], cards[j]) = (cards[j], cards[i]);
		}
	}

	/// <summary>
	/// A card's share of the roll: its rarity's table weight, split between that rarity's
	/// cards in proportion to their kind weight.
	///
	/// The division is the whole point. Weighting each card by its rarity directly made a
	/// rarity's total share its table weight TIMES its card count — and the library holds 3
	/// Common, 5 Rare and 3 Epic, so Rare came out more likely than Common at every wave
	/// (45% vs 43% at wave 1) and Epic nearly caught Common by wave 12. Normalising means the
	/// rarity table says what it means, and adding a card to a tier splits that tier's share
	/// instead of silently inflating the whole tier.
	/// </summary>
	private static float Weight(Card card, int wave, Dictionary<Rarity, float> shares)
	{
		float share = shares.GetValueOrDefault(card.Rarity);

		return share > 0f
			? RarityWeight(card.Rarity, wave) * KindWeight(card) / share
			: 0f;
	}

	private static float KindWeight(Card card) => card switch
	{
		AmmoCard => AmmoWeight,
		ShieldCard => ShieldWeight,
		HealCard => HealWeight,
		_ => HealWeight,
	};

	private static float RarityWeight(Rarity rarity, int wave) => rarity switch
	{
		Rarity.Common => 60f,
		Rarity.Rare => Mathf.Min(45f, 22f + wave * 3f),
		Rarity.Epic => Mathf.Min(30f, 4f + wave * 2.5f),
		_ => 0f,
	};
}
