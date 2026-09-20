/// <summary>
/// Common ammo — the honest upgrade. Same shape as basic, twice the punch.
/// </summary>
using Godot;
public partial class NukeRound : Projectile
{
	public NukeRound()
	{
		Speed = 880f;
		Damage = 1000f;
		Lifetime = 1.0f;
	}
	protected override void Move(double delta)
	{
		Scale += new Vector2(Speed * (float)delta, Speed * (float)delta);
	}
}
