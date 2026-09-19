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
	[Export] public float ContactDamage = 9f;

	/// <summary>
	/// Must clear the two colliders' combined radii (18 + 17), or the enemy can never reach
	/// attack range while it's actually resting against the player — physics stops it at 35px.
	/// </summary>
	[Export] public float ContactRange = 42f;
	[Export] public float AttackCooldown = 0.8f;

	/// <summary>
	/// Speed multiplier on the wave they first appear, ramping to 1.0 by
	/// <see cref="FullSpeedWave"/>. Early chasers are meant to be outrun; late ones aren't.
	/// </summary>
	[Export] public float StartingSpeedScale = 0.6f;

	/// <summary>Wave at which chasers hit their full MoveSpeed.</summary>
	[Export] public int FullSpeedWave = 6;

	/// <summary>Inside this it starts winding up; at zero distance it's at full charge speed.</summary>
	[Export] public float ChargeRange = 260f;

	/// <summary>Top speed as a multiple of MoveSpeed, reached only right on top of the player.</summary>
	[Export] public float ChargeSpeedScale = 1.8f;

	/// <summary>Player bullets nearer than this get sidestepped.</summary>
	[Export] public float DodgeRadius = 110f;

	/// <summary>
	/// How hard a flinch pulls against the chase. Kept low on purpose: this is a tell, not
	/// evasion, and the enemy should still be easy to hit. 0 disables the check entirely.
	/// </summary>
	[Export] public float DodgeWeight = 0.45f;

	/// <summary>Dot cutoff for "that shot is aimed at me" — 0.5 is a 60° cone.</summary>
	[Export] public float DodgeCone = 0.5f;

	private double _cooldown;

	public override void ApplyWaveScaling(int wave)
	{
		float progress = FullSpeedWave <= 1
			? 1f
			: Mathf.Clamp((wave - 1f) / (FullSpeedWave - 1f), 0f, 1f);

		MoveSpeed *= Mathf.Lerp(StartingSpeedScale, 1f, progress);
	}

	protected override void Act(double delta)
	{
		_cooldown -= delta;

		float distance = DistanceToPlayer;
		Vector2 chase = DirectionToPlayer;

		// Speed ramps from 1x at ChargeRange up to ChargeSpeedScale at contact.
		float speedScale = distance < ChargeRange
			? Mathf.Lerp(1f, ChargeSpeedScale, 1f - distance / ChargeRange)
			: 1f;

		Vector2 dodge = DodgeVector();
		Vector2 heading = dodge == Vector2.Zero ? chase : (chase + dodge * DodgeWeight).Normalized();

		Steer(heading, delta, speedScale);
		FacePlayer(delta); // melee is the only type that turns to look at you

		if (_cooldown > 0.0 || Player is not IDamageable target || !target.IsAlive)
			return;

		// Range check OR a real body-on-body contact, so being pinned against the player
		// keeps the hits coming instead of needing a fresh run-up each time.
		if (distance <= ContactRange || IsTouchingPlayer())
		{
			target.TakeDamage(ContactDamage, this);
			_cooldown = AttackCooldown;
		}
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
