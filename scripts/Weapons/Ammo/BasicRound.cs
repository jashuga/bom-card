/// <summary>
/// The Common gun's infinite fallback, and what enemy shooters fire. Deliberately weak:
/// it is the floor the whole ammo economy is measured against.
/// </summary>
public partial class BasicRound : Projectile
{
	public BasicRound()
	{
		Speed = 620f;
		Damage = 10f;
		Lifetime = 1.6f;
	}
}
