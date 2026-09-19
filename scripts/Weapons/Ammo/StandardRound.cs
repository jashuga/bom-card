/// <summary>
/// Common ammo — the honest upgrade. Same shape as basic, twice the punch.
/// </summary>
public partial class StandardRound : Projectile
{
	public StandardRound()
	{
		Speed = 880f;
		Damage = 12f;
		Lifetime = 1.8f;
	}
}
