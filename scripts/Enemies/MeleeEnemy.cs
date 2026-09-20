using Godot;

/// <summary>
/// Runs straight at you and hurts on contact. Pressure, not threat — its job is to
/// deny the player the space they need to line up Piercing shots.
///
/// It starts a run noticeably slower than its top speed and reaches full pace by
/// <see cref="FullSpeedWave"/>, so the first ones you meet can be walked away from.
/// It cruises at <see cref="EnemyBase.MoveSpeed"/> at range and winds up to
/// <see cref="ChargeSpeedScale"/> as it closes, so the danger is letting one get near rather
/// than one existing. It twitches aside from incoming fire, but only enough to read as alive —
/// the chase dominates, so it should still be straightforward to shoot. Set
/// <see cref="DodgeWeight"/> to 0 to switch that off entirely.
/// </summary>
public partial class MeleeEnemy : EnemyBase
{
	[Export] public float ContactDamage = 5f;

	/// <summary>
	/// Must clear the two colliders' combined radii (18 + 17), or the enemy can never reach
	/// attack range while it's actually resting against the player — physics stops it at 35px.
	/// </summary>
	[Export] public float ContactRange = 42f;
	[Export] public float AttackCooldown = 0.6f;

	/// <summary>
	/// Speed multiplier on the wave they first appear, ramping to 1.0 by
	/// <see cref="FullSpeedWave"/>. Early chasers are meant to be outrun; late ones aren't.
	/// </summary>
	[Export] public float StartingSpeedScale = 0.5f;

	/// <summary>Wave at which chasers hit their full MoveSpeed.</summary>
	[Export] public int FullSpeedWave = 6;

	/// <summary>Inside this it starts winding up; at zero distance it's at full charge speed.</summary>
	[Export] public float ChargeRange = 250f;

	/// <summary>Top speed as a multiple of MoveSpeed, reached only right on top of the player.</summary>
	[Export] public float ChargeSpeedScale = 1.25f;

	/// <summary>Player bullets nearer than this get sidestepped.</summary>
	[Export] public float DodgeRadius = 110f;

	/// <summary>
	/// How hard a flinch pulls against the chase. Kept low on purpose: this is a tell, not
	/// evasion, and the enemy should still be easy to hit. 0 disables the check entirely.
	/// </summary>
	[Export] public float DodgeWeight = 0.45f;

	/// <summary>Dot cutoff for "that shot is aimed at me" — 0.5 is a 60° cone.</summary>
	[Export] public float DodgeCone = 0.5f;

	/// <summary>Ring the non-leading chasers hold. Tight enough to crowd you, still outside
	/// <see cref="ContactRange"/> so they can't all land hits.</summary>
	[Export] public float StandoffRange = 95f;
	[Export] public float StandoffTolerance = 22f;

	/// <summary>Speed of a chaser that's waiting its turn. Just under 1 so it can hold the ring
	/// around a moving player instead of trailing behind it.</summary>
	[Export] public float WaitingSpeedScale = 0.9f;

	/// <summary>How much closer a challenger must be before it takes the lead. Hysteresis — without
	/// it the role flickers between two enemies at similar range and neither commits.</summary>
	[Export] public float LeadHandoffMargin = 25f;

	/// <summary>
	/// The single chaser currently allowed to attack. Static on purpose: it's one role shared
	/// across the whole field, not per-instance state. Cleared when its holder dies or is freed.
	/// </summary>
	private static MeleeEnemy _leader;

	private bool IsLeader => _leader == this;

	private readonly RandomNumberGenerator _rng = new();
	private float _orbitSign = 1f;
	private double _cooldown;

	protected override void OnSpawn()
	{
		_rng.Randomize();
		_orbitSign = _rng.Randf() < 0.5f ? -1f : 1f; // half circle each way, so the ring doesn't rotate as one
	}

	public override void ApplyWaveScaling(int wave)
	{
		float progress = FullSpeedWave <= 1
			? 1f
			: Mathf.Clamp((wave - 1f) / (FullSpeedWave - 1f), 0f, 1f);

		MoveSpeed *= Mathf.Clamp(Mathf.Lerp(StartingSpeedScale, 1f, progress), 1f, 1f);
	}

	protected override void Act(double delta)
	{
		_cooldown -= delta;
		UpdateLeadership();

		float distance = DistanceToPlayer;
		Vector2 chase = DirectionToPlayer;
		bool leading = IsLeader;

		Vector2 heading;
		float speedScale;

		if (leading)
		{
			heading = chase;

			// Speed ramps from 1x at ChargeRange up to ChargeSpeedScale at contact.
			speedScale = distance < ChargeRange
				? Mathf.Lerp(1f, ChargeSpeedScale, 1f - distance / ChargeRange)
				: 1f;
		}
		else
		{
			// Waiting its turn: settle onto the ring and circle, never crowd in.
			if (distance < StandoffRange - StandoffTolerance)
				heading = -chase;
			else if (distance > StandoffRange + StandoffTolerance)
				heading = chase;
			else
				heading = chase.Orthogonal() * _orbitSign;

			speedScale = WaitingSpeedScale;
		}

		Vector2 dodge = DodgeVector();
		if (dodge != Vector2.Zero && heading != Vector2.Zero)
			heading = (heading + dodge * DodgeWeight).Normalized();

		Steer(heading, delta, speedScale);
		FacePlayer(delta); // melee is the only type that turns to look at you

		if (!leading || _cooldown > 0.0 || Player is not IDamageable target || !target.IsAlive)
			return;

		// Range check OR a real body-on-body contact, so being pinned against the player
		// keeps the hits coming instead of needing a fresh run-up each time.
		if (distance <= ContactRange || IsTouchingPlayer())
		{
			target.TakeDamage(ContactDamage, this);
			_cooldown = AttackCooldown;
			_leader = null; // hand the lead on so the pack rotates instead of one enemy grinding you down
		}
	}

	/// <summary>
	/// Claim or pass on the single attacking role. Anyone takes a vacant lead; a held one only
	/// changes hands to someone <see cref="LeadHandoffMargin"/> closer, so it doesn't oscillate.
	/// </summary>
	private void UpdateLeadership()
	{
		if (_leader != null && (!IsInstanceValid(_leader) || !_leader.IsAlive))
			_leader = null;

		if (_leader == null)
		{
			_leader = this;
			return;
		}

		if (!IsLeader && DistanceToPlayer < _leader.DistanceToPlayer - LeadHandoffMargin)
			_leader = this;
	}

	/// <summary>
	/// Flinch direction away from incoming player fire, or Zero when nothing is threatening.
	/// Only counts bullets actually travelling at us — it ignores shots that already went past,
	/// so the enemy doesn't flinch at the whole screen. Nearer threats pull harder.
	/// </summary>
	private Vector2 DodgeVector()
	{
		Node container = BulletContainer;
		if (container == null || DodgeWeight <= 0f)
			return Vector2.Zero; // skip the per-frame bullet scan when it can't matter

		Vector2 dodge = Vector2.Zero;

		foreach (Node child in container.GetChildren())
		{
			if (child is not Projectile bullet || bullet.Hostile || !IsInstanceValid(bullet))
				continue;

			Vector2 toMe = GlobalPosition - bullet.GlobalPosition;
			float distance = toMe.Length();
			if (distance <= 0.001f || distance > DodgeRadius)
				continue;

			if (bullet.Direction.Dot(toMe / distance) < DodgeCone)
				continue; // heading elsewhere, or already past us

			// Step square off the bullet's line, on whichever side we're already nearer.
			Vector2 side = bullet.Direction.Orthogonal();
			if (side.Dot(toMe) < 0f)
				side = -side;

			dodge += side * (1f - distance / DodgeRadius);
		}

		return dodge == Vector2.Zero ? Vector2.Zero : dodge.Normalized();
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
