using Godot;

/// <summary>
/// Epic ammo — hitscan. Resolves the whole shot the instant it spawns: one ray, damaging
/// every target it passes through until it meets a wall. The node then lingers for a few
/// frames purely to draw the beam.
///
/// It is still a Projectile subclass so guns have exactly one way to fire anything.
/// </summary>
public partial class LaserRound : Projectile
{
	public override AudioStream ShotSound => Sounds.LaserShoot;

	[Export] public float Range = 1400f;
	[Export] public float BeamWidth = 7f;
	[Export] public int MaxTargets = 16;

	private Vector2 _beamEndLocal;
	private float _fade = 1f;

	public LaserRound()
	{
		Speed = 0f;
		Damage = 45f;
		Lifetime = 0.16f;
	}

	protected override void OnSpawn()
	{
		SetDeferred(Area2D.PropertyName.Monitoring, false); // the ray is the hit test
		Rotation = 0f;                                      // we draw in world-aligned local space
		ZIndex = 4;

		Vector2 origin = GlobalPosition;
		Vector2 cursor = origin;
		Vector2 end = origin + Direction * Range;

		var exclude = new Godot.Collections.Array<Rid>();
		if (Shooter is CollisionObject2D shooterBody)
			exclude.Add(shooterBody.GetRid());

		PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;

		for (int i = 0; i < MaxTargets; i++)
		{
			var query = PhysicsRayQueryParameters2D.Create(cursor, end, Layers.World | TargetLayer, exclude);
			query.CollideWithAreas = false;
			Godot.Collections.Dictionary hit = space.IntersectRay(query);

			if (hit.Count == 0)
				break;

			var collider = hit["collider"].As<GodotObject>();
			cursor = hit["position"].AsVector2();

			if (collider is IDamageable target)
			{
				if (target.IsAlive)
					target.TakeDamage(Damage, Shooter);

				if (collider is CollisionObject2D body)
					exclude.Add(body.GetRid());

				cursor += Direction * 1f; // nudge past the surface we just resolved
				continue;
			}

			// A wall: the beam stops here.
			end = cursor;
			break;
		}

		_beamEndLocal = ToLocal(end);
		CreateTween().TweenMethod(Callable.From<float>(SetFade), 1f, 0f, Lifetime);
	}

	protected override void Move(double delta) { }

	private void SetFade(float f)
	{
		_fade = f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var core = new Color(1f, 0.95f, 1f, _fade);
		var glow = new Color(0.78f, 0.49f, 1f, _fade * 0.45f);
		DrawLine(Vector2.Zero, _beamEndLocal, glow, BeamWidth * 2.4f, true);
		DrawLine(Vector2.Zero, _beamEndLocal, core, BeamWidth, true);
	}
}
