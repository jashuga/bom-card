using Godot;

/// <summary>
/// Runs straight at you and hurts on contact. Pressure, not threat — its job is to
/// deny the player the space they need to line up Piercing shots.
/// </summary>
public partial class MeleeEnemy : EnemyBase
{
	[Export] public float ContactDamage = 12f;

	/// <summary>
	/// Must clear the two colliders' combined radii (18 + 17), or the enemy can never reach
	/// attack range while it's actually resting against the player — physics stops it at 35px.
	/// </summary>
	[Export] public float ContactRange = 42f;
	[Export] public float AttackCooldown = 0.8f;

	private double _cooldown;

	protected override void Act(double delta)
	{
		_cooldown -= delta;

		Steer(DirectionToPlayer, delta);
		FacePlayer(delta); // melee is the only type that turns to look at you

		if (_cooldown > 0.0 || Player is not IDamageable target || !target.IsAlive)
			return;

		// Range check OR a real body-on-body contact, so being pinned against the player
		// keeps the hits coming instead of needing a fresh run-up each time.
		if (DistanceToPlayer <= ContactRange || IsTouchingPlayer())
		{
			target.TakeDamage(ContactDamage, this);
			_cooldown = AttackCooldown;
		}
	}

	/// <summary>Did last frame's MoveAndSlide push us into the player? Enemies collide with
	/// each other too, so the collider identity matters.</summary>
	private bool IsTouchingPlayer()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() == Player)
				return true;
		}

		return false;
	}
}
