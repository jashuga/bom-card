using Godot;

/// <summary>
/// Epic ammo — a missile that steers onto the nearest target and detonates.
/// Only 4 rounds, so each one should feel like a guaranteed kill.
/// </summary>
public partial class HomingRound : Projectile
{
	public override AudioStream ShotSound => Sounds.HomingShoot;

	[Export] public float TurnRate = 6.0f;   // radians/sec
	[Export] public float SeekRadius = 700f;
	[Export] public float BlastRadius = 60f;
	[Export] public float BlastDamage = 15f;
	[Export] public float RetargetInterval = 0.25f;

	private Node2D _target;
	private double _sinceRetarget;

	public HomingRound()
	{
		Speed = 540f;
		Damage = 30f;
		Lifetime = 5.0f;
	}

	protected override void Move(double delta)
	{
		_sinceRetarget += delta;
		bool targetLost = _target == null
			|| !IsInstanceValid(_target)
			|| (_target is IDamageable damageable && !damageable.IsAlive);

		if (targetLost || _sinceRetarget >= RetargetInterval)
		{
			_target = FindNearestTarget(SeekRadius);
			_sinceRetarget = 0;
		}

		if (_target != null && IsInstanceValid(_target))
		{
			Vector2 desired = (_target.GlobalPosition - GlobalPosition).Normalized();
			float maxTurn = TurnRate * (float)delta;
			Direction = Direction.Rotated(Mathf.Clamp(Direction.AngleTo(desired), -maxTurn, maxTurn)).Normalized();
			Rotation = Direction.Angle() + Mathf.Pi / 2f;
		}

		Position += Direction * Speed * (float)delta;
	}

	protected override void OnHitDamageable(Node2D body, IDamageable target)
	{
		target.TakeDamage(Damage, Shooter);
		Detonate();
	}

	protected override void OnHitWorld(Node2D body) => Detonate();

	private void Detonate()
	{
		Blast.Apply(this, GlobalPosition, BlastRadius, BlastDamage, TargetLayer, Shooter);
		Despawn();
	}
}
