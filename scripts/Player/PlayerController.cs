using Godot;

/// <summary>
/// WASD to move, LEFT/RIGHT arrows to turn, Space to shoot, Shift to dash, 1/2/3 to pick a gun.
///
/// Turret controls: movement is world-relative (WASD always strafes), and facing is a
/// separate axis you steer with the arrows. Shots and the sprite both follow the facing.
/// Aim deliberately never snaps to your movement direction — that felt wonky in playtest.
/// </summary>
public partial class PlayerController : CharacterBody2D, IDamageable
{
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void DashChangedEventHandler(int left, int max);

	[Export] public float Speed = 330f;
	[Export] public float Acceleration = 2600f;
	[Export] public float Friction = 3000f;

	/// <summary>Seconds of immunity after taking a hit, so melee contact can't chain-kill.</summary>
	[Export] public float InvulnerabilityTime = 0.45f;

	/// <summary>Dashes granted at the start of every wave. They do not carry over.</summary>
	[Export] public int DashesPerWave = 1;
	[Export] public float DashSpeed = 1150f;
	[Export] public float DashDuration = 0.16f;

	/// <summary>Dash charges left this wave.</summary>
	public int DashesLeft { get; private set; }
	public bool IsDashing => _dashingFor > 0.0;

	/// <summary>Facing at the start of a run. Up = straight up the screen.</summary>
	[Export] public Vector2 StartingFacing = Vector2.Up;

	/// <summary>Turn rate in radians/sec while an arrow key is held. ~229°/s at 4.0.</summary>
	[Export] public float RotationSpeed = 4.0f;

	/// <summary>Set false to lock facing and make the arrow keys inert.</summary>
	[Export] public bool CanRotate = true;

	public Health Health { get; private set; }
	public WeaponController Weapons { get; private set; }
	public Vector2 AimDirection { get; private set; } = Vector2.Up;

	public bool IsAlive => Health != null && Health.IsAlive;

	private Node2D _sprite;
	private double _invulnerableFor;
	private double _dashingFor;
	private Vector2 _dashDirection;

	public override void _Ready()
	{
		AddToGroup(Groups.Player);

		Health = GetNode<Health>("Health");
		Health.Died += OnDied;

		Weapons = GetNode<WeaponController>("WeaponController");
		Weapons.Shooter = this;

		AimDirection = StartingFacing == Vector2.Zero ? Vector2.Up : StartingFacing.Normalized();
		ApplyFacing();

		_sprite = GetNodeOrNull<Node2D>("Sprite");

		CollisionLayer = Layers.Player;
		CollisionMask = Layers.World | Layers.Barrier | Layers.Enemy;

		RefillDashes();
	}

	/// <summary>Called by the run loop at the start of each wave. Charges don't accumulate.</summary>
	public void RefillDashes()
	{
		DashesLeft = Mathf.Max(0, DashesPerWave);
		EmitSignal(SignalName.DashChanged, DashesLeft, DashesPerWave);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_invulnerableFor > 0.0)
		{
			_invulnerableFor -= delta;
			if (_invulnerableFor <= 0.0 && _sprite != null)
				_sprite.Modulate = Colors.White;
		}

		if (!IsAlive)
		{
			_dashingFor = 0.0;
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Facing is its own axis — steered by the arrows, never by where you're walking.
		if (CanRotate)
		{
			float turn = Input.GetAxis("rotate_left", "rotate_right");
			if (turn != 0f)
			{
				AimDirection = AimDirection.Rotated(turn * RotationSpeed * (float)delta).Normalized();
				ApplyFacing();
			}
		}

		Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (Input.IsActionJustPressed("dash"))
			TryDash(input);

		if (IsDashing)
		{
			// Dash overrides steering entirely — you commit to the direction you left on.
			_dashingFor -= delta;
			Velocity = _dashDirection * DashSpeed;
		}
		else if (input != Vector2.Zero)
		{
			Velocity = Velocity.MoveToward(input * Speed, Acceleration * (float)delta);
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Friction * (float)delta);
		}

		MoveAndSlide();

		Weapons.AimDirection = AimDirection;
		if (Input.IsActionPressed("shoot"))
			Weapons.TryFire();
	}

	/// <summary>
	/// Spend a charge and launch. Dashes where you're steering, or straight ahead if you're
	/// standing still, so it's never a wasted charge.
	/// </summary>
	private void TryDash(Vector2 input)
	{
		if (DashesLeft <= 0 || IsDashing)
			return;

		Vector2 direction = input != Vector2.Zero ? input.Normalized() : AimDirection;
		if (direction == Vector2.Zero)
			return;

		_dashDirection = direction;
		_dashingFor = DashDuration;
		DashesLeft--;
		EmitSignal(SignalName.DashChanged, DashesLeft, DashesPerWave);
	}

	/// <summary>
	/// Turn the body to match <see cref="AimDirection"/>. The +PI/2 is because the art points
	/// up at zero rotation — same convention the projectiles use.
	/// Rotating the root (not just the sprite) keeps the collider circular and means any muzzle
	/// or marker node parented here turns with us for free.
	/// </summary>
	private void ApplyFacing() => Rotation = AimDirection.Angle() + Mathf.Pi / 2f;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("weapon_1")) Weapons.SelectSlot(0);
		else if (@event.IsActionPressed("weapon_2")) Weapons.SelectSlot(1);
		else if (@event.IsActionPressed("weapon_3")) Weapons.SelectSlot(2);
	}

	public void TakeDamage(float amount, Node2D source)
	{
		if (!IsAlive || _invulnerableFor > 0.0)
			return;

		Health.Damage(amount);
		_invulnerableFor = InvulnerabilityTime;

		if (_sprite != null)
			_sprite.Modulate = new Color(1f, 0.45f, 0.45f);
	}

	private void OnDied()
	{
		SetPhysicsProcess(false);
		EmitSignal(SignalName.Died);
	}
}
