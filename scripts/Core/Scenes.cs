using System.Collections.Generic;
using Godot;

/// <summary>
/// One place for every res:// path we load from code. If you move a scene, fix it here only.
/// Loads are cached, so calling these in a hot loop is fine.
/// </summary>
public static class Scenes
{
	private static readonly Dictionary<string, PackedScene> Cache = new();

	public static PackedScene Get(string path)
	{
		if (Cache.TryGetValue(path, out PackedScene cached) && cached != null)
			return cached;

		var scene = GD.Load<PackedScene>(path);
		if (scene == null)
			GD.PushError($"Scenes.Get: missing scene at {path}");
		Cache[path] = scene;
		return scene;
	}

	// Special ammo — one scene per Projectile subclass. Named *Round to stay clear of the
	// root-level *Bullet classes; two script classes may not differ only by case (CS8785).
	public static PackedScene BasicRound => Get("res://scenes/ammo/BasicRound.tscn");
	public static PackedScene StandardRound => Get("res://scenes/ammo/StandardRound.tscn");
	public static PackedScene PiercingRound => Get("res://scenes/ammo/PiercingRound.tscn");
	public static PackedScene ExplosiveRound => Get("res://scenes/ammo/ExplosiveRound.tscn");
	public static PackedScene RicochetRound => Get("res://scenes/ammo/RicochetRound.tscn");
	public static PackedScene HomingRound => Get("res://scenes/ammo/HomingRound.tscn");
	public static PackedScene LaserRound => Get("res://scenes/ammo/LaserRound.tscn");
	public static PackedScene NukeRound => Get("res://scenes/ammo/NukeRound.tscn");

	// Actors.
	public static PackedScene Player => Get("res://scenes/Player.tscn");
	public static PackedScene MeleeEnemy => Get("res://scenes/enemies/MeleeEnemy.tscn");
	public static PackedScene ShooterEnemy => Get("res://scenes/enemies/ShooterEnemy.tscn");
	public static PackedScene TankEnemy => Get("res://scenes/enemies/TankEnemy.tscn");
	public static PackedScene SniperEnemy => Get("res://scenes/enemies/SniperEnemy.tscn");
	
}
