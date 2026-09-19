using Godot;

/// <summary>
/// Common ammo — punches through a line of enemies. Rewards lining shots up.
/// </summary>
public partial class PiercingRound : Projectile
{
	[Export] public int MaxPierces = 3;

	private int _pierced;

	public PiercingRound()
	{
		Speed = 960f;
		Damage = 10f;
		Lifetime = 2.0f;
	}

	protected override void OnHitDamageable(Node2D body, IDamageable target)
	{
		target.TakeDamage(Damage, Shooter);

		_pierced++;
		if (_pierced > MaxPierces)
			Despawn();
	}
}
