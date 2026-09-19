using Godot;

/// <summary>
/// A loaded clip of special ammo. Created by an <see cref="AmmoCard"/>, consumed by a
/// <see cref="Gun"/>. Plain C# — magazines are pure data, they never enter the scene tree.
///
/// Rarity here must match the gun's rarity: that is the whole gate on the ammo economy.
/// </summary>
public sealed class Magazine
{
	public string AmmoName { get; }
	public Rarity Rarity { get; }
	public PackedScene BulletScene { get; }

	/// <summary>Multiplies the gun's base fire rate while this ammo is loaded.</summary>
	public float FireRateMultiplier { get; }

	/// <summary>Projectiles per trigger pull (shotgun-style ammo). Costs one round regardless.</summary>
	public int ProjectilesPerShot { get; }

	/// <summary>Total cone width in degrees when ProjectilesPerShot > 1.</summary>
	public float SpreadDegrees { get; }

	public int Rounds { get; private set; }
	public int Capacity { get; }
	public bool IsEmpty => Rounds <= 0;

	public Magazine(
		string ammoName,
		Rarity rarity,
		PackedScene bulletScene,
		float fireRateMultiplier = 1f,
		int projectilesPerShot = 1,
		float spreadDegrees = 0f,
		int? rounds = null)
	{
		AmmoName = ammoName;
		Rarity = rarity;
		BulletScene = bulletScene;
		FireRateMultiplier = fireRateMultiplier;
		ProjectilesPerShot = Mathf.Max(1, projectilesPerShot);
		SpreadDegrees = spreadDegrees;
		Capacity = rounds ?? rarity.MagazineSize();
		Rounds = Capacity;
	}

	public bool TryConsume()
	{
		if (IsEmpty)
			return false;
		Rounds--;
		return true;
	}
}
