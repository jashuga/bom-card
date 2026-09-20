/// <summary>
/// The three guns, fixed for the whole run. Balance lives here — if you are tuning
/// fire rates, this is the only file you need to touch.
///
///   slot 0 (key 1)  Sidearm      Common  6.0 shots/s   infinite basic ammo
///   slot 1 (key 2)  Repeater     Rare    3.2 shots/s   dry without a magazine
///   slot 2 (key 3)  Hand Cannon  Epic    1.2 shots/s   dry without a magazine
/// </summary>
public static class GunLibrary
{
	public const int SlotCount = 3;

	/// <param name="everyGunHasBasicAmmo">
	/// The brief is ambiguous here: "guns 2 and 3 are limited" vs "the gun reverts to its
	/// infinite basic ammo". Default false = Rare/Epic go dry and the controller auto-swaps
	/// you back to slot 1. Flip to true to give all three a basic fallback instead.
	/// </param>
	public static Gun[] CreateLoadout(bool everyGunHasBasicAmmo = false) => new[]
	{
		new Gun("Sidearm", Rarity.Common, 3.5f, Scenes.BasicRound, true),
		new Gun("Repeater", Rarity.Rare, 2.8f, Scenes.BasicRound, everyGunHasBasicAmmo),
		new Gun("Hand Cannon", Rarity.Epic, 1.2f, Scenes.BasicRound, everyGunHasBasicAmmo),
	};
}
