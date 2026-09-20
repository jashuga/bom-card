using Godot;

/// <summary>
/// Rare ammo — bounces off arena walls. Turns a cramped arena into an advantage.
///
/// Walls are handled by a top-level RayCast2D rather than the Area2D, because an Area2D
/// gives us no surface normal. We therefore drop World out of the collision mask in
/// OnSpawn so the wall never triggers the ordinary "hit something, despawn" path.
/// </summary>
public partial class RicochetRound : Projectile
{
	public override AudioStream ShotSound => Sounds.RicochetShoot;

	[Export] public int MaxBounces = 4;

	private RayCast2D _probe;
	private int _bounces;

	public RicochetRound()
	{
		Speed = 780f;
		Damage = 11f;
		Lifetime = 4.0f;
	}

	protected override void OnSpawn()
	{
		CollisionMask = TargetLayer; // walls are the raycast's job, not the area's

		_probe = new RayCast2D
		{
			Enabled = true,
			TopLevel = true, // so TargetPosition is in world space, unaffected by our rotation
			CollisionMask = Layers.World,
			CollideWithAreas = false,
			CollideWithBodies = true,
		};
		AddChild(_probe);
	}

	protected override void Move(double delta)
	{
		Vector2 step = Direction * Speed * (float)delta;

		_probe.GlobalPosition = GlobalPosition;
		_probe.TargetPosition = step + Direction * 6f; // a little skin so we turn before clipping
		_probe.ForceRaycastUpdate();

		if (_probe.IsColliding())
		{
			if (_bounces >= MaxBounces)
			{
				Despawn();
				return;
			}

			Vector2 normal = _probe.GetCollisionNormal();
			GlobalPosition = _probe.GetCollisionPoint() + normal * 2f;
			Direction = Direction.Bounce(normal).Normalized();
			Rotation = Direction.Angle() + Mathf.Pi / 2f;
			_bounces++;
			AlreadyHit.Clear(); // a bounced shot may hit the same enemy again
			return;
		}

		Position += step;
	}
}
