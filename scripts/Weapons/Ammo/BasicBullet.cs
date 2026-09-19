/// <summary>
/// The Common gun's infinite fallback, and what enemy shooters fire. Deliberately weak:
/// it is the floor the whole ammo economy is measured against.
/// </summary>
public partial class BasicBullet : Projectile
{
	public BasicBullet()
	{
		Speed = 620f;
		Damage = 6f;
		Lifetime = 1.6f;
	}
}
