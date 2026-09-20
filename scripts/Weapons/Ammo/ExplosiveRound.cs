using Godot;

/// <summary>
/// Rare ammo — slow shell, radial damage on any impact (including walls).
/// The blast does NOT hurt the shooter; Blast only queries the target layer.
/// </summary>
public partial class ExplosiveRound : Projectile
{
	public override AudioStream ShotSound => Sounds.ExplosiveShoot;

	[Export] public float BlastRadius = 90f;
	[Export] public float BlastDamage = 22f;

	public ExplosiveRound()
	{
		Speed = 520f;
		Damage = 14f;
		Lifetime = 2.5f;
	}

	protected override void OnHitDamageable(Node2D body, IDamageable target)
	{
		target.TakeDamage(Damage, Shooter);
		Explode();
	}

	protected override void OnHitWorld(Node2D body) => Explode();

	private void Explode()
	{
		Blast.Apply(this, GlobalPosition, BlastRadius, BlastDamage, TargetLayer, Shooter);
		Despawn();
	}
}
