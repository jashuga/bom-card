using Godot;

/// <summary>
/// Big, slow and hard to shift. It refuses to let you get close — back it into a corner and
/// it still shuffles away — and answers with six shots fired around itself: the four diagonals
/// plus straight left and right. The volley is fixed in world space rather than aimed, so the
/// only gaps are directly above and below it.
/// </summary>
public partial class TankEnemy : EnemyBase
{
	/// <summary>Closer than this and it retreats; past the tolerance band it lumbers back in.</summary>
	[Export] public float KeepAwayRange = 320f;
	[Export] public float RangeTolerance = 70f;

	/// <summary>Seconds between volleys. Slow enough to read and walk out of.</summary>
	[Export] public float VolleyInterval = 2.2f;
	[Export] public float BulletDamage = 7f;
	[Export] public float BulletSpeed = 300f;

	/// <summary>Has to clear the tank's own (large) collider or the shots look like they hatch inside it.</summary>
	[Export] public float MuzzleOffset = 42f;

	/// <summary>
	/// Four corners plus straight left and right — six shots, normalised on use. Fixed in world
	/// space; the tank never aims. The gaps are now only straight up and straight down.
	/// </summary>
	private static readonly Vector2[] ShotDirections =
	{
		new(1f, 1f), new(-1f, 1f), new(-1f, -1f), new(1f, -1f),
		new(1f, 0f), new(-1f, 0f),
	};

	private double _cooldown;

	protected override void OnSpawn()
	{
		// Stagger the first volley so two tanks in one wave don't fire as a single wall.
		_cooldown = GD.RandRange(0.4f, VolleyInterval);
	}

	protected override void Act(double delta)
	{
		float distance = DistanceToPlayer;

		if (distance < KeepAwayRange)
			Steer(-DirectionToPlayer, delta);
		else if (distance > KeepAwayRange + RangeTolerance)
			Steer(DirectionToPlayer, delta, 0.6f);
		else
			Steer(Vector2.Zero, delta); // in the sweet spot — plant and let the volleys work

		_cooldown -= delta;
		if (_cooldown <= 0.0)
		{
			FireVolley();
			_cooldown = VolleyInterval;
		}
	}

	private void FireVolley()
	{
		foreach (Vector2 direction in ShotDirections)
			FireBasicRound(direction, BulletDamage, BulletSpeed, MuzzleOffset);
	}
}
