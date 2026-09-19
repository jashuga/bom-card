using System.Collections.Generic;
using Godot;

/// <summary>
/// Spawns waves around the arena edge and reports when one is cleared.
/// Knows nothing about cards or UI — <see cref="GameManager"/> reacts to WaveCleared.
/// </summary>
public partial class WaveManager : Node
{
	[Signal] public delegate void WaveStartedEventHandler(int wave, int enemyCount);
	[Signal] public delegate void WaveClearedEventHandler(int wave);
	[Signal] public delegate void EnemyCountChangedEventHandler(int remaining);

	[Export] public NodePath EnemyContainerPath;

	/// <summary>Arena rect, top-left at origin. Enemies spawn just inside this.</summary>
	[Export] public Vector2 ArenaSize = new(1280f, 720f);
	[Export] public float SpawnInset = 56f;

	/// <summary>Seconds between individual spawns, so a wave trickles in instead of popping.</summary>
	[Export] public float SpawnInterval = 0.3f;

	[Export] public int BaseEnemyCount = 4;
	[Export] public int EnemiesPerWave = 2;
	[Export] public int MaxEnemyCount = 26;

	/// <summary>Tanks start showing up here, then one more every <see cref="WavesPerExtraTank"/>.</summary>
	[Export] public int FirstTankWave = 3;
	[Export] public int WavesPerExtraTank = 3;
	[Export] public int MaxTankCount = 3;

	/// <summary>Enemy max health is multiplied by 1 + wave * this.</summary>
	[Export] public float HealthScalePerWave = 0.12f;

	public int CurrentWave { get; private set; }
	public int Remaining => _alive + _pending.Count;
	public bool WaveInProgress { get; private set; }

	private readonly Queue<PackedScene> _pending = new();
	private readonly RandomNumberGenerator _rng = new();
	private Node _enemyContainer;
	private int _alive;
	private double _sinceSpawn;

	public override void _Ready()
	{
		_rng.Randomize();
		_enemyContainer = EnemyContainerPath != null && !EnemyContainerPath.IsEmpty
			? GetNodeOrNull(EnemyContainerPath)
			: GetTree().Root.FindChild("EnemyContainer", recursive: true, owned: false);

		if (_enemyContainer == null)
			GD.PushError("WaveManager: no EnemyContainer — enemies have nowhere to spawn.");

		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		if (_pending.Count == 0)
			return;

		_sinceSpawn += delta;
		if (_sinceSpawn < SpawnInterval)
			return;

		_sinceSpawn = 0;
		SpawnOne(_pending.Dequeue());
	}

	public void StartWave(int wave)
	{
		CurrentWave = wave;
		WaveInProgress = true;
		_alive = 0;
		_sinceSpawn = SpawnInterval; // first enemy lands immediately
		_pending.Clear();

		int total = Mathf.Min(MaxEnemyCount, BaseEnemyCount + wave * EnemiesPerWave);

		// Tanks are a slow trickle — they're a wall to work around, not the bulk of a wave.
		int tanks = wave >= FirstTankWave
			? Mathf.Min(MaxTankCount, 1 + (wave - FirstTankWave) / Mathf.Max(1, WavesPerExtraTank))
			: 0;
		tanks = Mathf.Min(tanks, total);

		// Shooters take their cut of what's left, so tanks displace chaff rather than adding to it.
		float shooterRatio = Mathf.Clamp(0.15f + wave * 0.06f, 0f, 0.5f);
		int shooters = Mathf.RoundToInt((total - tanks) * shooterRatio);

		var roster = new List<PackedScene>(total);
		for (int i = 0; i < total; i++)
		{
			roster.Add(i < tanks ? Scenes.TankEnemy
				: i < tanks + shooters ? Scenes.ShooterEnemy
				: Scenes.MeleeEnemy);
		}

		// Fisher-Yates so shooters aren't all front-loaded.
		for (int i = roster.Count - 1; i > 0; i--)
		{
			int j = _rng.RandiRange(0, i);
			(roster[i], roster[j]) = (roster[j], roster[i]);
		}

		foreach (PackedScene scene in roster)
			_pending.Enqueue(scene);

		SetProcess(true);
		EmitSignal(SignalName.WaveStarted, wave, total);
		EmitSignal(SignalName.EnemyCountChanged, Remaining);
	}

	/// <summary>Kills everything on the field without crediting a wave clear. Used on game over / restart.</summary>
	public void ClearField()
	{
		_pending.Clear();
		SetProcess(false);
		WaveInProgress = false;

		foreach (Node enemy in GetTree().GetNodesInGroup(Groups.Enemies))
			enemy.QueueFree();

		_alive = 0;
	}

	private void SpawnOne(PackedScene scene)
	{
		if (scene == null || _enemyContainer == null)
			return;

		var enemy = scene.Instantiate<EnemyBase>();
		enemy.Position = RandomEdgePosition();
		enemy.Died += OnEnemyDied;

		_enemyContainer.AddChild(enemy);

		// Health lives on a child node, so scale it after the enemy is in the tree.
		float scale = 1f + CurrentWave * HealthScalePerWave;
		enemy.Health.AddMaxHealth(enemy.Health.Max * (scale - 1f));

		_alive++;
		EmitSignal(SignalName.EnemyCountChanged, Remaining);
	}

	private Vector2 RandomEdgePosition()
	{
		float x = _rng.RandfRange(SpawnInset, ArenaSize.X - SpawnInset);
		float y = _rng.RandfRange(SpawnInset, ArenaSize.Y - SpawnInset);

		return _rng.RandiRange(0, 3) switch
		{
			0 => new Vector2(x, SpawnInset),                 // top
			1 => new Vector2(x, ArenaSize.Y - SpawnInset),   // bottom
			2 => new Vector2(SpawnInset, y),                 // left
			_ => new Vector2(ArenaSize.X - SpawnInset, y),   // right
		};
	}

	private void OnEnemyDied(EnemyBase enemy)
	{
		_alive = Mathf.Max(0, _alive - 1);
		EmitSignal(SignalName.EnemyCountChanged, Remaining);

		if (!WaveInProgress || _alive > 0 || _pending.Count > 0)
			return;

		WaveInProgress = false;
		SetProcess(false);
		EmitSignal(SignalName.WaveCleared, CurrentWave);
	}
}
