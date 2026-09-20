using System.Collections.Generic;
using Godot;

/// <summary>
/// One place for every sound file we load, mirroring <see cref="Scenes"/>. Loads are cached,
/// so calling these per shot is fine.
/// </summary>
public static class Sounds
{
	private const string Dir = "res://assets/sound effects/";

	private static readonly Dictionary<string, AudioStream> Cache = new();

	public static AudioStream Get(string file)
	{
		if (Cache.TryGetValue(file, out AudioStream cached) && cached != null)
			return cached;

		var stream = GD.Load<AudioStream>(Dir + file);
		if (stream == null)
			GD.PushError($"Sounds.Get: missing audio at {Dir}{file}");
		Cache[file] = stream;
		return stream;
	}

	// Player fire — one per ammo type.
	public static AudioStream StandardShoot => Get("standardShoot.wav");
	public static AudioStream PiercingShoot => Get("piercingShoot.wav");
	public static AudioStream ExplosiveShoot => Get("explosiveShoot.wav");
	public static AudioStream RicochetShoot => Get("ricochetShoot.wav");
	public static AudioStream HomingShoot => Get("homingShoot.wav");
	public static AudioStream LaserShoot => Get("laserShoot.wav");

	// Everything else.
	public static AudioStream EnemyShoot => Get("enemyShoot.wav");
	public static AudioStream EnemyHurt => Get("enemyHurt.wav");
	public static AudioStream Equip => Get("equip.wav");
	public static AudioStream MenuSelect => Get("menuSelect.wav");
	public static AudioStream GameOver => Get("gameOver.wav");
}
