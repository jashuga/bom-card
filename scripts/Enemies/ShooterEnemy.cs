using Godot;

/// <summary>
/// Keeps its distance and plinks at you with basic bullets. Reuses the player's
/// bullet pipeline with Hostile = true rather than owning a separate projectile path.
/// </summary>
public partial class ShooterEnemy : EnemyBase
{
	[Export] public float PreferredRange = 280f;
	[Export] public float RangeTolerance = 60f;
	/// <summary>Each shot waits a fresh random gap in [Min, Max], so a pack of them never fires in lockstep.</summary>
	[Export] public float FireIntervalMin = 2f;
	[Export] public float FireIntervalMax = 3f;
	[Export] public float BulletDamage = 8f;
	[Export] public float BulletSpeed = 420f;

	private readonly RandomNumberGenerator _rng = new();
	private double _cooldown;

	protected override void OnSpawn()
	{
		_rng.Randomize();
		_cooldown = _rng.RandfRange(0.3f, FireIntervalMax);
	}

	protected override void Act(double delta)
	{
		float distance = DistanceToPlayer;
		Vector2 toPlayer = DirectionToPlayer;

		if (distance > PreferredRange + RangeTolerance)
			Steer(toPlayer, delta);
		else if (distance < PreferredRange - RangeTolerance)
			Steer(-toPlayer, delta);
		else
			Steer(toPlayer.Orthogonal(), delta, 0.5f); // strafe

		_cooldown -= delta;
		if (_cooldown <= 0.0 && toPlayer != Vector2.Zero)
		{
			FireBasicRound(toPlayer, BulletDamage, BulletSpeed);
			_cooldown = _rng.RandfRange(FireIntervalMin, FireIntervalMax);
		}
	}
}
