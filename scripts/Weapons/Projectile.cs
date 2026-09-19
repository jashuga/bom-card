using System.Collections.Generic;
using Godot;

/// <summary>
/// Base class for every projectile. One subclass per ammo type — override the three
/// hooks below rather than touching this file, so four people can add ammo in parallel.
///
///   OnSpawn()            one-time setup after Direction/Shooter/Hostile are set
///   Move(delta)          how it travels (default: straight line)
///   OnHitDamageable(..)  what it does to a target (default: damage once, then despawn)
///   OnHitWorld(..)       what it does to a wall (default: despawn)
///
/// Spawn contract: set Direction, Shooter and Hostile, set Position, THEN AddChild.
/// _Ready reads all three.
/// </summary>
public abstract partial class Projectile : Area2D
{
	[Export] public float Speed = 700f;
	[Export] public float Damage = 8f;
	[Export] public float Lifetime = 2.0f;

	/// <summary>Normalised travel direction. Set by the shooter before AddChild.</summary>
	public Vector2 Direction = Vector2.Up;

	/// <summary>Who fired it — never damaged by its own shot.</summary>
	public Node2D Shooter;

	/// <summary>True when an enemy fired it: flips the collision layers so it hits the player.</summary>
	public bool Hostile;

	/// <summary>Layer this projectile is allowed to damage.</summary>
	protected uint TargetLayer => Hostile ? Layers.Player : Layers.Enemy;

	/// <summary>Bodies already hit. Pierce/ricochet ammo manages this itself.</summary>
	protected readonly HashSet<ulong> AlreadyHit = new();

	private double _age;

	public override void _Ready()
	{
		if (Direction == Vector2.Zero)
			Direction = Vector2.Up;
		Direction = Direction.Normalized();

		CollisionLayer = Hostile ? Layers.EnemyBullet : Layers.PlayerBullet;
		CollisionMask = Layers.World | TargetLayer;
		Rotation = Direction.Angle() + Mathf.Pi / 2f; // art points "up" at zero rotation

		BodyEntered += OnBodyEntered;
		OnSpawn();
	}

	public override void _PhysicsProcess(double delta)
	{
		_age += delta;
		if (_age >= Lifetime)
		{
			OnExpired();
			return;
		}
		Move(delta);
	}

	// ---- overridable behaviour -------------------------------------------------

	protected virtual void OnSpawn() { }

	protected virtual void Move(double delta) => Position += Direction * Speed * (float)delta;

	protected virtual void OnHitDamageable(Node2D body, IDamageable target)
	{
		target.TakeDamage(Damage, Shooter);
		Despawn();
	}

	protected virtual void OnHitWorld(Node2D body) => Despawn();

	protected virtual void OnExpired() => Despawn();

	// ---- plumbing --------------------------------------------------------------

	public virtual void OnBodyEntered(Node2D body)
	{
		if (body == null || body == Shooter)
			return;
		if (!AlreadyHit.Add(body.GetInstanceId()))
			return;

		if (body is IDamageable target)
		{
			if (target.IsAlive)
				OnHitDamageable(body, target);
			return;
		}

		OnHitWorld(body);
	}

	protected void Despawn()
	{
		if (!IsQueuedForDeletion())
		{
			SetPhysicsProcess(false);
			SetDeferred(Area2D.PropertyName.Monitoring, false);
			QueueFree();
		}
	}

	/// <summary>Nearest living thing on our target layer, or null. Used by homing ammo.</summary>
	protected Node2D FindNearestTarget(float maxRange)
	{
		string group = Hostile ? Groups.Player : Groups.Enemies;
		Node2D best = null;
		float bestDistanceSquared = maxRange * maxRange;

		foreach (Node node in GetTree().GetNodesInGroup(group))
		{
			if (node is not Node2D candidate || !IsInstanceValid(candidate))
				continue;
			if (candidate is IDamageable damageable && !damageable.IsAlive)
				continue;

			float distanceSquared = GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
			if (distanceSquared < bestDistanceSquared)
			{
				bestDistanceSquared = distanceSquared;
				best = candidate;
			}
		}

		return best;
	}
}
