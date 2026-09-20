using System;
using Godot;

/// <summary>What a single trigger pull produced. Returned by <see cref="Gun.TryFire"/>.</summary>
public readonly struct Shot
{
	public readonly PackedScene BulletScene;
	public readonly int Projectiles;
	public readonly float SpreadDegrees;

	/// <summary>Scales the bullet scene's own damage. Lets one gun hit harder with the same ammo.</summary>
	public readonly float DamageMultiplier;

	public Shot(PackedScene bulletScene, int projectiles, float spreadDegrees, float damageMultiplier = 1f)
	{
		BulletScene = bulletScene;
		Projectiles = projectiles;
		SpreadDegrees = spreadDegrees;
		DamageMultiplier = damageMultiplier;
	}
}

/// <summary>
/// One of the three fixed guns. Plain C# — it owns cooldown and ammo state but knows
/// nothing about the scene tree; <see cref="WeaponController"/> turns a Shot into nodes.
///
/// The three guns never change during a run. Only their loaded magazine does.
/// </summary>
public sealed class Gun
{
	public string Name { get; }
	public Rarity Rarity { get; }

	/// <summary>Shots per second with basic ammo.</summary>
	public float BaseFireRate { get; }

	/// <summary>Multiplies whatever damage the loaded bullet scene carries.</summary>
	public float DamageMultiplier { get; }

	/// <summary>Fallback projectile. Only fired when <see cref="HasInfiniteBasicAmmo"/>.</summary>
	public PackedScene BasicBulletScene { get; }

	/// <summary>
	/// True for the Common gun: it always works. False for Rare/Epic, which go dry
	/// once their magazine empties. Flip this to give every gun a basic fallback.
	/// </summary>
	public bool HasInfiniteBasicAmmo { get; }

	public Magazine Magazine { get; private set; }

	/// <summary>Fires when a magazine runs dry, so the HUD can flash and the controller can auto-swap.</summary>
	public event Action<Gun> MagazineEmptied;

	private double _cooldown;

	public Gun(string name, Rarity rarity, float baseFireRate, PackedScene basicBulletScene,
		bool hasInfiniteBasicAmmo, float damageMultiplier = 1f)
	{
		Name = name;
		Rarity = rarity;
		BaseFireRate = baseFireRate;
		BasicBulletScene = basicBulletScene;
		HasInfiniteBasicAmmo = hasInfiniteBasicAmmo;
		DamageMultiplier = damageMultiplier;
	}

	public bool HasSpecialAmmo => Magazine is { IsEmpty: false };
	public int RoundsLeft => Magazine?.Rounds ?? 0;
	public string LoadedAmmoName => Magazine?.AmmoName ?? (HasInfiniteBasicAmmo ? "Basic" : "Empty");

	/// <summary>False when the gun can't fire at all right now and never will without a reload.</summary>
	public bool IsUsable => HasInfiniteBasicAmmo || HasSpecialAmmo;

	public void Tick(double delta) => _cooldown = Math.Max(0.0, _cooldown - delta);

	/// <summary>Loads special ammo. Rejects any magazine whose rarity doesn't match.</summary>
	public bool TryLoad(Magazine magazine)
	{
		if (magazine == null || magazine.Rarity != Rarity)
			return false;

		Magazine = magazine;
		return true;
	}

	public bool TryFire(out Shot shot)
	{
		shot = default;

		if (_cooldown > 0.0)
			return false;

		float rate = BaseFireRate;

		if (HasSpecialAmmo)
		{
			rate *= Magazine.FireRateMultiplier;
			shot = new Shot(Magazine.BulletScene, Magazine.ProjectilesPerShot, Magazine.SpreadDegrees, DamageMultiplier);
			Magazine.TryConsume();

			if (Magazine.IsEmpty)
			{
				Magazine = null; // revert to basic ammo
				MagazineEmptied?.Invoke(this);
			}
		}
		else if (HasInfiniteBasicAmmo)
		{
			shot = new Shot(BasicBulletScene, 1, 0f, DamageMultiplier);
		}
		else
		{
			return false;
		}

		_cooldown = 1.0 / Math.Max(0.01f, rate);
		return shot.BulletScene != null;
	}
}
