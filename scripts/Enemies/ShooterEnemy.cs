using Godot;

/// <summary>
/// Keeps its distance and plinks at you with basic bullets. Reuses the player's
/// bullet pipeline with Hostile = true rather than owning a separate projectile path.
/// </summary>
public partial class ShooterEnemy : EnemyBase
{
	[Export] public float PreferredRange = 280f;
	[Export] public float RangeTolerance = 60f;
	[Export] public float FireInterval = 1.4f;
	[Export] public float BulletDamage = 8f;

	private double _cooldown;
	private Node _bulletContainer;

	protected override void OnSpawn()
	{
		_cooldown = GD.RandRange(0.3f, FireInterval);
		_bulletContainer = GetTree().Root.FindChild("BulletContainer", recursive: true, owned: false);
	}

	protected override void Act(double delta)
	{
		float distance = DistanceToPlayer;
		Vector2 toPlayer = DirectionToPlayer;

		if (distance > PreferredRange + RangeTolerance)
			Steer(toPlayer, delta);
		else if (distance < PreferredRange - RangeTolerance)
			Steer(-toPlayer, delta);
		else
			Steer(toPlayer.Orthogonal(), delta, 0.5f); // strafe

		_cooldown -= delta;
		if (_cooldown <= 0.0 && toPlayer != Vector2.Zero)
		{
			Fire(toPlayer);
			_cooldown = FireInterval;
		}
	}

	private void Fire(Vector2 direction)
	{
		if (_bulletContainer == null)
			return;

		var bullet = Scenes.BasicRound.Instantiate<BasicRound>();
		bullet.Direction = direction;
		bullet.Shooter = this;
		bullet.Hostile = true;
		bullet.Damage = BulletDamage;
		bullet.Speed = 420f;

		Vector2 muzzle = GlobalPosition + direction * 26f;
		bullet.Position = _bulletContainer is Node2D container ? container.ToLocal(muzzle) : muzzle;

		_bulletContainer.AddChild(bullet);
	}
}
