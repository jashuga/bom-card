using Godot;

/// <summary>
/// Shared radial damage. Used by Explosive ammo and the Homing Missile so the two
/// don't drift apart. Purely a query — it does not spawn or free anything but the VFX.
/// </summary>
public static class Blast
{
	public static void Apply(Node2D from, Vector2 at, float radius, float damage, uint targetMask, Node2D shooter)
	{
		if (from == null || !from.IsInsideTree())
			return;

		var query = new PhysicsShapeQueryParameters2D
		{
			Shape = new CircleShape2D { Radius = radius },
			Transform = new Transform2D(0f, at),
			CollisionMask = targetMask,
			CollideWithBodies = true,
			CollideWithAreas = false,
		};

		foreach (Godot.Collections.Dictionary hit in from.GetWorld2D().DirectSpaceState.IntersectShape(query, 32))
		{
			if (hit["collider"].As<GodotObject>() is IDamageable target && target.IsAlive)
				target.TakeDamage(damage, shooter);
		}

		BlastVfx.Spawn(from.GetParent(), at, radius);
	}
}
