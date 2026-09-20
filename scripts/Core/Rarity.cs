using Godot;

/// <summary>
/// Gun / ammo tier. A magazine can only ever be loaded into a gun of the SAME rarity.
/// This is the rule the whole ammo economy hangs off — see <see cref="WeaponController.LoadMagazine"/>.
/// </summary>
public enum Rarity
{
	Common,
	Rare,
	Epic,
}

public static class RarityRules
{
	/// <summary>Rounds granted by an ammo card of this rarity.</summary>
	public static int MagazineSize(this Rarity rarity) => rarity switch
	{
		Rarity.Common => 50,
		Rarity.Rare => 25,
		Rarity.Epic => 10,
		_ => 0,
	};

	public static Color Tint(this Rarity rarity) => rarity switch
	{
		Rarity.Common => new Color("b8c0c8"),
		Rarity.Rare => new Color("4ea8de"),
		Rarity.Epic => new Color("c77dff"),
		_ => Colors.White,
	};

	public static string DisplayName(this Rarity rarity) => rarity switch
	{
		Rarity.Common => "Common",
		Rarity.Rare => "Rare",
		Rarity.Epic => "Epic",
		_ => "?",
	};
}
