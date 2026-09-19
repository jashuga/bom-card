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

	public Health Health { get; private set; }
	public bool IsAlive => Health != null && Health.IsAlive;

	protected Node2D Player;
	protected Node2D Sprite;

	private double _flashFor;

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

	/// <summary>Set <see cref="CharacterBody2D.Velocity"/> here. MoveAndSlide runs afterwards.</summary>
	protected abstract void Act(double delta);

	protected float DistanceToPlayer =>
		Player != null && IsInstanceValid(Player) ? GlobalPosition.DistanceTo(Player.GlobalPosition) : float.MaxValue;

	protected Vector2 DirectionToPlayer =>
		Player != null && IsInstanceValid(Player)
			? (Player.GlobalPosition - GlobalPosition).Normalized()
			: Vector2.Zero;

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
