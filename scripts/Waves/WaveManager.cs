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

<<<<<<< HEAD
	/// <summary>Arena rect, top-left at origin. Enemies spawn just inside this.</summary>
	[Export] public Vector2 ArenaSize = new(720, 360f);
=======
	/// <summary>Arena rect, top-left at origin.</summary>
	[Export] public Vector2 ArenaSize = new(1080f, 1080f);

	/// <summary>Keeps spawns off the left and right walls.</summary>
>>>>>>> 63ad041f9ed0c412a0a8bcc4a17b0b57d43b51a7
	[Export] public float SpawnInset = 56f;

	/// <summary>How far above the top edge enemies appear, so they walk into frame rather than
	/// popping into it. Must clear the top barrier band.</summary>
	[Export] public float SpawnOutsideMargin = 70f;

	/// <summary>Seconds between individual spawns, so a wave trickles in instead of popping.</summary>
	[Export] public float SpawnInterval = 0.3f;

	[Export] public int BaseEnemyCount = 4;
	[Export] public int EnemiesPerWave = 2;
	[Export] public int MaxEnemyCount = 26;

	// Introduction schedule: wave 1 shooters only, 2-3 adds chasers, 4-5 adds tanks,
	// 6-7 adds snipers. One new thing at a time, each given a couple of waves to land.

	/// <summary>Opening waves are nothing but shooters, so the basic threat is legible first.</summary>
	[Export] public int ShooterOnlyWaves = 1;

	/// <summary>Tanks debut here as a single one, then come as a pair on every wave after.</summary>
	[Export] public int FirstTankWave = 4;
	[Export] public int TanksPerWave = 2;

	/// <summary>How far in from the side walls the paired tanks enter, as a fraction of width.</summary>
	[Export] public float TankSpawnEdgeFraction = 0.18f;

	/// <summary>Snipers debut here at one, then one more every <see cref="WavesPerExtraSniper"/>.</summary>
	[Export] public int FirstSniperWave = 6;
	[Export] public int WavesPerExtraSniper = 2;
	[Export] public int MaxSniperCount = 6;

	/// <summary>Chaser share of a wave once the specials are accounted for. Shooters fill the rest.</summary>
	[Export] public float MeleeRatio = 0.35f;

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
	private int _tanksSpawned;

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
		_tanksSpawned = 0;

		int total = Mathf.Min(MaxEnemyCount, BaseEnemyCount + wave * EnemiesPerWave);

		var roster = new List<PackedScene>(total);

		if (wave <= ShooterOnlyWaves)
		{
			AddCopies(roster, Scenes.ShooterEnemy, total);
			QueueRoster(roster, wave, total);
			return;
		}

		// One on the debut wave to introduce it, a pair on opposite sides from then on.
		int tanks = wave < FirstTankWave ? 0 : wave == FirstTankWave ? 1 : TanksPerWave;
		tanks = Mathf.Min(tanks, total);

		// Debut is a single sniper, then a gradual build — same shape as the tank ramp.
		int snipers = wave >= FirstSniperWave
			? Mathf.Min(MaxSniperCount, 1 + (wave - FirstSniperWave) / Mathf.Max(1, WavesPerExtraSniper))
			: 0;
		snipers = Mathf.Min(snipers, total - tanks);

		// Specials displace chaff rather than adding to it; chasers take a fixed cut of the
		// rest and shooters fill out the wave, which keeps them the staple enemy.
		int remainder = total - tanks - snipers;
		int melee = Mathf.RoundToInt(remainder * Mathf.Clamp(MeleeRatio, 0f, 1f));

		AddCopies(roster, Scenes.TankEnemy, tanks);
		AddCopies(roster, Scenes.SniperEnemy, snipers);
		AddCopies(roster, Scenes.MeleeEnemy, melee);
		AddCopies(roster, Scenes.ShooterEnemy, total - roster.Count);

		QueueRoster(roster, wave, total);
	}

	/// <summary>Shuffle a built roster into the spawn queue and announce the wave.</summary>
	private void QueueRoster(List<PackedScene> roster, int wave, int total)
	{
		// Fisher-Yates so the ranged types aren't all front-loaded.
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
		enemy.ArenaBounds = ArenaSize;
		enemy.Position = enemy is TankEnemy ? NextTankSpawnPosition() : TopSpawnPosition();
		enemy.Died += OnEnemyDied;

		_enemyContainer.AddChild(enemy);

		// Health lives on a child node, so scale it after the enemy is in the tree.
		float scale = 1f + CurrentWave * HealthScalePerWave;
		enemy.Health.AddMaxHealth(enemy.Health.Max * (scale - 1f));

		// Types that ramp over a run (chaser speed, say) tune themselves here.
		enemy.ApplyWaveScaling(CurrentWave);

		_alive++;
		EmitSignal(SignalName.EnemyCountChanged, Remaining);
	}

	private static void AddCopies(List<PackedScene> roster, PackedScene scene, int count)
	{
		for (int i = 0; i < count; i++)
			roster.Add(scene);
	}

	/// <summary>Anywhere along the top, above the frame, so enemies march down into view.</summary>
	private Vector2 TopSpawnPosition() =>
		new(_rng.RandfRange(SpawnInset, ArenaSize.X - SpawnInset), -SpawnOutsideMargin);

	/// <summary>
	/// Tanks alternate sides, so the pair in a wave always arrives on opposite flanks rather
	/// than both wandering in from the same place.
	/// </summary>
	private Vector2 NextTankSpawnPosition()
	{
		bool left = _tanksSpawned++ % 2 == 0;
		float fraction = Mathf.Clamp(TankSpawnEdgeFraction, 0.05f, 0.45f);
		float x = ArenaSize.X * (left ? fraction : 1f - fraction);

		return new Vector2(x, -SpawnOutsideMargin);
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
