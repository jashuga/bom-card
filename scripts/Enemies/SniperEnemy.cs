using Godot;

/// <summary>
/// The one-eyed watcher. It solves a real intercept — where you *will* be when the bullet
/// arrives, not where you are — and leads its shots there.
///
/// That would be an unfair aimbot on its own, so it is deliberately held to roughly the
/// controls the player has:
///   * it must swing its aim round at <see cref="AimTurnSpeed"/>, barely above the player's
///     own RotationSpeed, so it can never snap onto a target;
///   * it reads your velocity through a <see cref="ReactionTime"/> lag, so a sharp change of
///     direction beats the prediction the same way it beats a human;
///   * it only shoots once it's actually lined up, with a few degrees of <see cref="AimSpread"/>.
/// The counterplay is therefore the same as against a good player: don't move predictably,
/// and change direction once it commits.
/// </summary>
public partial class SniperEnemy : EnemyBase
{
	[Export] public float PreferredRange = 360f;
	[Export] public float RangeTolerance = 80f;

	[Export] public float FireInterval = 1.6f;
	[Export] public float BulletDamage = 9f;
	[Export] public float BulletSpeed = 480f;
	[Export] public float MuzzleOffset = 28f;

	/// <summary>Aim turn rate, rad/s. The player turns at 4.0 — keep this close to stay fair.</summary>
	[Export] public float AimTurnSpeed = 4.5f;

	/// <summary>Seconds of lag on reading your velocity. Higher = easier to juke.</summary>
	[Export] public float ReactionTime = 0.18f;

	/// <summary>Won't fire until aim is within this many radians of the solution (~7°).</summary>
	[Export] public float FiringCone = 0.12f;

	/// <summary>Random error applied per shot, radians (~3°). Stops it being pixel-perfect.</summary>
	[Export] public float AimSpread = 0.05f;

	/// <summary>Clamp on how far ahead it will lead, so a fast player can't drag aim off-screen.</summary>
	[Export] public float MaxLeadTime = 1.2f;

	private readonly RandomNumberGenerator _rng = new();
	private Vector2 _aim = Vector2.Up;
	private Vector2 _trackedVelocity;
	private double _cooldown;

	protected override void OnSpawn()
	{
		_rng.Randomize();
		_cooldown = _rng.RandfRange(0.3f, FireInterval);

		if (DirectionToPlayer != Vector2.Zero)
			_aim = DirectionToPlayer;
	}

	protected override void Act(double delta)
	{
		float distance = DistanceToPlayer;
		Vector2 toPlayer = DirectionToPlayer;

		// Hold the line at range — it wants a clean sightline, not a brawl.
		if (distance > PreferredRange + RangeTolerance)
			Steer(toPlayer, delta);
		else if (distance < PreferredRange - RangeTolerance)
			Steer(-toPlayer, delta);
		else
			Steer(toPlayer.Orthogonal(), delta, 0.4f);

		TrackPlayerVelocity(delta);

		Vector2 solution = AimSolution();
		if (solution != Vector2.Zero)
			TurnAimToward(solution, delta);

		// Sprite rotation stays fixed, same as the shooter — only the melee type turns.
		_cooldown -= delta;
		if (_cooldown <= 0.0 && solution != Vector2.Zero && Mathf.Abs(_aim.AngleTo(solution)) <= FiringCone)
		{
			FireBasicRound(_aim.Rotated(_rng.RandfRange(-AimSpread, AimSpread)), BulletDamage, BulletSpeed, MuzzleOffset);
			_cooldown = FireInterval;
		}
	}

	/// <summary>
	/// Follow the player's velocity through a lag filter rather than reading it exactly.
	/// Exponential so it's framerate independent — this is the "reflexes" of the thing.
	/// </summary>
	private void TrackPlayerVelocity(double delta)
	{
		Vector2 actual = Player is CharacterBody2D body ? body.Velocity : Vector2.Zero;
		float blend = ReactionTime > 0f ? 1f - Mathf.Exp(-(float)delta / ReactionTime) : 1f;
		_trackedVelocity = _trackedVelocity.Lerp(actual, blend);
	}

	/// <summary>
	/// Direction to the intercept point, or Zero if there's no player. Solves
	/// |offset + v*t| = BulletSpeed*t for the earliest positive t, then aims at where the
	/// player lands after t seconds. Falls back to aiming straight at them if no solution
	/// exists (they're outrunning the bullet).
	/// </summary>
	private Vector2 AimSolution()
	{
		if (Player == null || !IsInstanceValid(Player))
			return Vector2.Zero;

		Vector2 muzzle = GlobalPosition + _aim * MuzzleOffset;
		Vector2 offset = Player.GlobalPosition - muzzle;

		float a = _trackedVelocity.LengthSquared() - BulletSpeed * BulletSpeed;
		float b = 2f * offset.Dot(_trackedVelocity);
		float c = offset.LengthSquared();

		float t;
		if (Mathf.Abs(a) < 0.001f)
		{
			// Player moving at exactly bullet speed — the quadratic degenerates to linear.
			t = Mathf.Abs(b) < 0.001f ? 0f : -c / b;
		}
		else
		{
			float discriminant = b * b - 4f * a * c;
			if (discriminant < 0f)
				return offset.Normalized(); // uncatchable; just point at them

			float root = Mathf.Sqrt(discriminant);
			float t1 = (-b + root) / (2f * a);
			float t2 = (-b - root) / (2f * a);
			t = SmallestPositive(t1, t2);
		}

		if (t <= 0f)
			return offset.Normalized();

		t = Mathf.Min(t, MaxLeadTime);
		return (Player.GlobalPosition + _trackedVelocity * t - muzzle).Normalized();
	}

	private static float SmallestPositive(float a, float b)
	{
		if (a <= 0f) return b;
		if (b <= 0f) return a;
		return Mathf.Min(a, b);
	}

	/// <summary>Swing the aim toward the solution, capped — it can't snap onto you.</summary>
	private void TurnAimToward(Vector2 solution, double delta)
	{
		float difference = _aim.AngleTo(solution);
		float maxTurn = AimTurnSpeed * (float)delta;
		_aim = _aim.Rotated(Mathf.Clamp(difference, -maxTurn, maxTurn)).Normalized();
	}
}
