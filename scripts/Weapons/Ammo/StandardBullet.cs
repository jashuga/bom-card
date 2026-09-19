/// <summary>
/// Common ammo — the honest upgrade. Same shape as basic, twice the punch.
/// </summary>
public partial class StandardBullet : Projectile
{
	public StandardBullet()
	{
		Speed = 880f;
		Damage = 12f;
		Lifetime = 1.8f;
	}
}
