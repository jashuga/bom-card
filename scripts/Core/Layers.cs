/// <summary>
/// Physics layer bitmasks. Keep these in sync with Project Settings > Layer Names > 2D Physics.
/// Layer 1 World / 2 Player / 3 Enemy / 4 PlayerBullet / 5 EnemyBullet.
/// </summary>
public static class Layers
{
	public const uint World = 1 << 0;
	public const uint Player = 1 << 1;
	public const uint Enemy = 1 << 2;
	public const uint PlayerBullet = 1 << 3;
	public const uint EnemyBullet = 1 << 4;
}

/// <summary>Node group names. Strings in one place so nobody typos "enemy" vs "enemies".</summary>
public static class Groups
{
	public const string Player = "player";
	public const string Enemies = "enemies";
}
