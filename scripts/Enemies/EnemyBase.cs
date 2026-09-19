using Godot;

/// <summary>
/// Shared enemy plumbing: health, death, finding the player, a hit flash.
/// Subclass and override <see cref="Act"/> for behaviour — that's the only method a
/// new enemy type needs.
/// </summary>
public abstract partial class EnemyBase : CharacterBody2D, IDamageable
{
	[Signal] public delegate void DiedEventHandler(EnemyBase enemy);

	[Export] public float MoveSpeed = 130f;
	[Export] public float Acceleration = 900f;
	[Export] public int ScoreValue = 10;

	/// <summary>Turn rate in radians/sec for enemies that call <see cref="FacePlayer"/>. 0 snaps instantly.</summary>
	[Export] public float TurnSpeed = 12f;

	public Health Health { get; private set; }
	public bool IsAlive => Health != null && Health.IsAlive;

	protected Node2D Player;
	protected Node2D Sprite;

	private double _flashFor;
	private Node _bulletContainer;

	public override void _Ready()
	{
		AddToGroup(Groups.Enemies);

		Health = GetNode<Health>("Health");
		Health.Died += OnDied;

		Sprite = GetNodeOrNull<Node2D>("Sprite");

		CollisionLayer = Layers.Enemy;
		CollisionMask = Layers.World | Layers.Player | Layers.Enemy;

		OnSpawn();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_flashFor > 0.0)
		{
			_flashFor -= delta;
			if (_flashFor <= 0.0 && Sprite != null)
				Sprite.Modulate = Colors.White;
		}

		if (!IsAlive)
			return;

		if (Player == null || !IsInstanceValid(Player))
			Player = GetTree().GetFirstNodeInGroup(Groups.Player) as Node2D;

		Act(delta);
		MoveAndSlide();
	}

	protected virtual void OnSpawn() { }

	/// <summary>
	/// Turn to look at the player. Opt-in — call it from <see cref="Act"/>; enemies that don't
	/// stay at their spawn rotation. Rotating the root rather than just the sprite keeps the
	/// muzzle offset and any child markers aligned — the collider is a circle, so spinning it
	/// costs nothing. The +PI/2 is because the art points up at zero rotation, same convention
	/// as the player and the projectiles.
	/// </summary>
	protected virtual void FacePlayer(double delta)
	{
		Vector2 facing = DirectionToPlayer;
		if (facing == Vector2.Zero)
			return;

		float target = facing.Angle() + Mathf.Pi / 2f;
		Rotation = TurnSpeed > 0f
			? Mathf.RotateToward(Rotation, target, TurnSpeed * (float)delta)
			: target;
	}

	/// <summary>Set <see cref="CharacterBody2D.Velocity"/> here. MoveAndSlide runs afterwards.</summary>
	protected abstract void Act(double delta);

	protected float DistanceToPlayer =>
		Player != null && IsInstanceValid(Player) ? GlobalPosition.DistanceTo(Player.GlobalPosition) : float.MaxValue;

	protected Vector2 DirectionToPlayer =>
		Player != null && IsInstanceValid(Player)
			? (Player.GlobalPosition - GlobalPosition).Normalized()
			: Vector2.Zero;

	/// <summary>Where spawned bullets are parented. Resolved lazily — the container is a sibling
	/// in the arena, not guaranteed to exist when an enemy runs _Ready.</summary>
	protected Node BulletContainer =>
		_bulletContainer != null && IsInstanceValid(_bulletContainer)
			? _bulletContainer
			: _bulletContainer = GetTree().Root.FindChild("BulletContainer", recursive: true, owned: false);

	/// <summary>
	/// Spawn one hostile BasicRound heading in <paramref name="direction"/>. Reuses the player's
	/// bullet pipeline with Hostile = true rather than giving enemies a separate projectile path.
	/// <paramref name="muzzleOffset"/> should clear your own collider so the shot reads as leaving
	/// the body.
	/// </summary>
	protected void FireBasicRound(Vector2 direction, float damage, float speed, float muzzleOffset = 26f)
	{
		Node container = BulletContainer;
		if (container == null || direction == Vector2.Zero)
			return;

		direction = direction.Normalized();

		var bullet = Scenes.BasicRound.Instantiate<BasicRound>();
		bullet.Direction = direction;
		bullet.Shooter = this;
		bullet.Hostile = true;
		bullet.Damage = damage;
		bullet.Speed = speed;

		Vector2 muzzle = GlobalPosition + direction * muzzleOffset;
		bullet.Position = container is Node2D node ? node.ToLocal(muzzle) : muzzle;

		container.AddChild(bullet);
	}

	protected void Steer(Vector2 desiredDirection, double delta, float speedScale = 1f)
	{
		Velocity = Velocity.MoveToward(desiredDirection * MoveSpeed * speedScale, Acceleration * (float)delta);
	}

	public void TakeDamage(float amount, Node2D source)
	{
		if (!IsAlive)
			return;

		Health.Damage(amount);
		_flashFor = 0.08f;

		if (Sprite != null)
			Sprite.Modulate = new Color(3f, 3f, 3f);
	}

	private void OnDied()
	{
		EmitSignal(SignalName.Died, this);
		QueueFree();
	}
}
