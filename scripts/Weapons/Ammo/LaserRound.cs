using Godot;

/// <summary>
/// Epic ammo — hitscan death ray. Resolves the whole shot the instant it spawns: everything
/// caught in the beam dies outright, and the beam stops at the first wall. The node then
/// lingers for a few frames purely to draw itself.
///
/// It is still a Projectile subclass so guns have exactly one way to fire anything.
/// </summary>
public partial class LaserRound : Projectile
{
	public override AudioStream ShotSound => Sounds.LaserShoot;

	/// <summary>
	/// Damage that takes anything to zero through any amount of shield, while staying an
	/// ordinary finite float — infinity or NaN here would poison the health arithmetic and
	/// every readout downstream. The beam is lethal simply because this is larger than any
	/// health pool the wave scaling can produce.
	/// </summary>
	public const float Vaporise = 1_000_000f;

	[Export] public float Range = 1400f;

	/// <summary>Both the drawn width AND the hit width — see <see cref="BurnEverything"/>.</summary>
	[Export] public float BeamWidth = 48f;

	/// <summary>Cap on results from the beam query. A safety valve, not a design limit.</summary>
	[Export] public int MaxTargets = 32;

	private Vector2 _beamEndLocal;
	private float _fade = 1f;

	public LaserRound()
	{
		Speed = 0f;
		Damage = Vaporise;
		Lifetime = 0.22f;
	}

	protected override void OnSpawn()
	{
		SetDeferred(Area2D.PropertyName.Monitoring, false); // the beam query is the hit test
		Rotation = 0f;                                      // we draw in world-aligned local space
		ZIndex = 4;

		Vector2 origin = GlobalPosition;
		Vector2 end = origin + Direction * Range;

		PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;

		// Step 1: where does the beam stop? Walls only. A zero-width ray is correct here,
		// because a wall spans the whole beam anyway.
		// Barrier as well as World: the top edge is open to ENEMIES walking in, but the beam
		// should still terminate at the edge of the cabinet rather than firing off-screen.
		var wallQuery = PhysicsRayQueryParameters2D.Create(origin, end, Layers.World | Layers.Barrier);
		wallQuery.CollideWithAreas = false;

		Godot.Collections.Dictionary wall = space.IntersectRay(wallQuery);
		if (wall.Count > 0)
			end = wall["position"].AsVector2();

		// Step 2: kill everything between here and there.
		BurnEverything(space, origin, end);

		_beamEndLocal = ToLocal(end);
		CreateTween().TweenMethod(Callable.From<float>(SetFade), 1f, 0f, Lifetime);
	}

	/// <summary>
	/// Destroys every valid target inside the beam.
	///
	/// A swept RECTANGLE, not a ray. The old version walked a zero-width ray and re-cast it
	/// past each body it hit, which meant the beam was drawn BeamWidth across but could only
	/// be hit with a mathematically perfect line — the picture lied about the hitbox, and
	/// shots that visibly passed through an enemy did nothing. The rectangle is the drawn
	/// beam, so what you see is what you hit. It also removes the re-cast loop entirely:
	/// one query returns everything at once.
	/// </summary>
	private void BurnEverything(PhysicsDirectSpaceState2D space, Vector2 from, Vector2 to)
	{
		float length = from.DistanceTo(to);
		if (length <= 0.01f)
			return;

		var exclude = new Godot.Collections.Array<Rid>();
		if (Shooter is CollisionObject2D shooterBody)
			exclude.Add(shooterBody.GetRid());

		var query = new PhysicsShapeQueryParameters2D
		{
			// Long axis is local X, so the transform rotation is simply the beam angle.
			Shape = new RectangleShape2D { Size = new Vector2(length, BeamWidth) },
			Transform = new Transform2D(Direction.Angle(), (from + to) * 0.5f),
			CollisionMask = TargetLayer,
			CollideWithBodies = true,
			CollideWithAreas = false,
			Exclude = exclude,
		};

		foreach (Godot.Collections.Dictionary hit in space.IntersectShape(query, MaxTargets))
		{
			if (hit["collider"].As<GodotObject>() is IDamageable target && target.IsAlive)
				target.TakeDamage(Damage, Shooter);
		}
	}

	protected override void Move(double delta) { }

	private void SetFade(float f)
	{
		_fade = f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		// Drawn width MUST equal BeamWidth, because that is exactly the rectangle that kills.
		// The old version drew a 2.4x halo around the core, which made the beam look far wider
		// than it hit — the same lie that made this weapon feel broken. The halo is now inside
		// the hit width, with a white-hot core inside that.
		DrawLine(Vector2.Zero, _beamEndLocal, new Color(0.78f, 0.49f, 1f, _fade * 0.45f), BeamWidth, true);
		DrawLine(Vector2.Zero, _beamEndLocal, new Color(0.92f, 0.80f, 1f, _fade * 0.85f), BeamWidth * 0.55f, true);
		DrawLine(Vector2.Zero, _beamEndLocal, new Color(1f, 0.98f, 1f, _fade), BeamWidth * 0.22f, true);
	}
}
