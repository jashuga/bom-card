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

	// Bullets — one scene per ammo subclass.
	public static PackedScene BasicBullet => Get("res://scenes/bullets/BasicBullet.tscn");
	public static PackedScene StandardBullet => Get("res://scenes/bullets/StandardBullet.tscn");
	public static PackedScene PiercingBullet => Get("res://scenes/bullets/PiercingBullet.tscn");
	public static PackedScene ExplosiveBullet => Get("res://scenes/bullets/ExplosiveBullet.tscn");
	public static PackedScene RicochetBullet => Get("res://scenes/bullets/RicochetBullet.tscn");
	public static PackedScene HomingBullet => Get("res://scenes/bullets/HomingBullet.tscn");
	public static PackedScene LaserBullet => Get("res://scenes/bullets/LaserBullet.tscn");

	// Actors.
	public static PackedScene Player => Get("res://scenes/Player.tscn");
	public static PackedScene MeleeEnemy => Get("res://scenes/enemies/MeleeEnemy.tscn");
	public static PackedScene ShooterEnemy => Get("res://scenes/enemies/ShooterEnemy.tscn");
}
