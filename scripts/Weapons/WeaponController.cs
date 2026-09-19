using Godot;

/// <summary>
/// Owns the three guns, the selected slot, and turning a <see cref="Shot"/> into bullet
/// nodes. Lives as a child of the player (or of an enemy, for hostile fire).
///
/// Bullets are parented to a shared container rather than the shooter, so they keep
/// flying when the shooter dies. That container is expected to sit at the arena origin.
/// </summary>
public partial class WeaponController : Node2D
{
	[Signal] public delegate void WeaponChangedEventHandler(int slot);
	[Signal] public delegate void AmmoChangedEventHandler(int slot, int rounds, int capacity, string ammoName);
	[Signal] public delegate void FiredEventHandler(int slot);

	/// <summary>Where bullets are parented. Falls back to our own parent's parent if unset.</summary>
	[Export] public NodePath BulletContainerPath;

	[Export] public float MuzzleOffset = 22f;

	/// <summary>Set true on enemies so their shots hit the player instead of other enemies.</summary>
	[Export] public bool Hostile;

	/// <summary>See <see cref="GunLibrary.CreateLoadout"/>.</summary>
	[Export] public bool EveryGunHasBasicAmmo;

	public Gun[] Guns { get; private set; }
	public int ActiveSlot { get; private set; }
	public Gun Active => Guns[ActiveSlot];

	/// <summary>Set every frame by whoever owns us. Normalised.</summary>
	public Vector2 AimDirection = Vector2.Up;

	/// <summary>The node credited with the damage, and never hit by its own bullets.</summary>
	public Node2D Shooter;

	private Node _bulletContainer;

	public override void _Ready()
	{
		Guns = GunLibrary.CreateLoadout(EveryGunHasBasicAmmo);
		foreach (Gun gun in Guns)
			gun.MagazineEmptied += OnMagazineEmptied;

		Shooter ??= GetParent<Node2D>();
		_bulletContainer = ResolveBulletContainer();
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (Gun gun in Guns)
			gun.Tick(delta);
	}

	// ---- public API -------------------------------------------------------------

	public void SelectSlot(int slot)
	{
		if (slot < 0 || slot >= Guns.Length || slot == ActiveSlot)
			return;

		ActiveSlot = slot;
		EmitSignal(SignalName.WeaponChanged, slot);
		EmitAmmo(slot);
	}

	/// <summary>Pull the trigger. Safe to call every frame — the gun's cooldown gates it.</summary>
	public void TryFire()
	{
		Gun gun = Active;

		if (!gun.IsUsable)
		{
			FallBackToCommon();
			return;
		}

		if (!gun.TryFire(out Shot shot))
			return;

		Vector2 aim = AimDirection == Vector2.Zero ? Vector2.Up : AimDirection.Normalized();

		for (int i = 0; i < shot.Projectiles; i++)
		{
			float offset = 0f;
			if (shot.Projectiles > 1 && shot.SpreadDegrees > 0f)
			{
				float t = i / (float)(shot.Projectiles - 1);
				offset = Mathf.DegToRad(Mathf.Lerp(-shot.SpreadDegrees * 0.5f, shot.SpreadDegrees * 0.5f, t));
			}

			Spawn(shot.BulletScene, aim.Rotated(offset), shot.DamageMultiplier);
		}

		EmitSignal(SignalName.Fired, ActiveSlot);
		EmitAmmo(ActiveSlot);
	}

	/// <summary>
	/// Routes a magazine to the gun of matching rarity — the core rule of the ammo system.
	/// Returns false if no gun matches (shouldn't happen; all three rarities exist).
	/// </summary>
	public bool LoadMagazine(Magazine magazine)
	{
		if (magazine == null)
			return false;

		for (int slot = 0; slot < Guns.Length; slot++)
		{
			if (!Guns[slot].TryLoad(magazine))
				continue;

			EmitAmmo(slot);
			return true;
		}

		GD.PushWarning($"No {magazine.Rarity} gun to load '{magazine.AmmoName}' into.");
		return false;
	}

	public void EmitAllAmmo()
	{
		for (int slot = 0; slot < Guns.Length; slot++)
			EmitAmmo(slot);
		EmitSignal(SignalName.WeaponChanged, ActiveSlot);
	}

	// ---- internals ---------------------------------------------------------------

	private void Spawn(PackedScene scene, Vector2 direction, float damageMultiplier)
	{
		if (scene == null || _bulletContainer == null)
			return;

		var bullet = scene.Instantiate<Projectile>();
		bullet.Direction = direction;
		bullet.Shooter = Shooter;
		bullet.Hostile = Hostile;
		bullet.Damage *= damageMultiplier;

		Vector2 muzzle = GlobalPosition + direction * MuzzleOffset;
		bullet.Position = _bulletContainer is Node2D container2D ? container2D.ToLocal(muzzle) : muzzle;

		_bulletContainer.AddChild(bullet);
	}

	private void OnMagazineEmptied(Gun gun)
	{
		if (!gun.IsUsable && gun == Active)
			FallBackToCommon();
	}

	/// <summary>A dry Rare/Epic gun drops you back to slot 1, which always works.</summary>
	private void FallBackToCommon()
	{
		for (int slot = 0; slot < Guns.Length; slot++)
		{
			if (Guns[slot].IsUsable)
			{
				SelectSlot(slot);
				return;
			}
		}
	}

	private void EmitAmmo(int slot)
	{
		Gun gun = Guns[slot];
		EmitSignal(SignalName.AmmoChanged, slot, gun.RoundsLeft, gun.Magazine?.Capacity ?? 0, gun.LoadedAmmoName);
	}

	private Node ResolveBulletContainer()
	{
		if (BulletContainerPath != null && !BulletContainerPath.IsEmpty)
		{
			Node explicitContainer = GetNodeOrNull(BulletContainerPath);
			if (explicitContainer != null)
				return explicitContainer;
		}

		// Convention: the arena owns a node called "BulletContainer".
		Node found = GetTree().Root.FindChild("BulletContainer", recursive: true, owned: false);
		if (found != null)
			return found;

		GD.PushWarning("WeaponController: no BulletContainer found; bullets will parent to the arena root.");
		return Shooter?.GetParent() ?? GetParent();
	}
}
