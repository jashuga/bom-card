using Godot;

/// <summary>
/// Runs straight at you and hurts on contact. Pressure, not threat — its job is to
/// deny the player the space they need to line up Piercing shots.
/// </summary>
public partial class MeleeEnemy : EnemyBase
{
	[Export] public float ContactDamage = 12f;
	[Export] public float ContactRange = 34f;
	[Export] public float AttackCooldown = 0.8f;

	private double _cooldown;

	protected override void Act(double delta)
	{
		_cooldown -= delta;

		Steer(DirectionToPlayer, delta);

		if (_cooldown <= 0.0 && DistanceToPlayer <= ContactRange && Player is IDamageable target && target.IsAlive)
		{
			target.TakeDamage(ContactDamage, this);
			_cooldown = AttackCooldown;
		}
	}
}
