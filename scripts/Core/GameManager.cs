using System.Collections.Generic;
using Godot;

/// <summary>
/// Arena root. Owns the run loop: tutorial -> start wave -> wave cleared -> pause -> draft ->
/// next wave. Everything else talks through signals, so this is the only file that knows the
/// order.
///
/// It is also the one place that pushes <see cref="ArenaLayout"/> into gameplay: the player
/// spawn and the wave-spawn bounds both come from there, so the portrait play area has a
/// single definition rather than a copy per system.
/// </summary>
public partial class GameManager : Node2D
{
	[Export] public NodePath PlayfieldPath = "Playfield";
	[Export] public NodePath PlayerPath = "Playfield/Player";
	[Export] public NodePath WaveManagerPath = "WaveManager";
	[Export] public NodePath HudPath = "Hud";
	[Export] public NodePath DraftScreenPath = "DraftScreen";
	[Export] public NodePath TutorialScreenPath = "TutorialScreen";

	[Export] public int CardsOffered = 3;
	[Export] public float DraftDelay = 0.9f;

	/// <summary>Show the how-to-play card before wave 1. Off makes the run start immediately.</summary>
	[Export] public bool ShowTutorialOnStart = true;

	public Deck Deck { get; } = new();
	public int Wave { get; private set; }

	private PlayerController _player;
	private WaveManager _waves;
	private Hud _hud;
	private DraftScreen _draft;
	private TutorialScreen _tutorial;
	private readonly RandomNumberGenerator _rng = new();
	private List<Card> _pendingOffer = new();
	private bool _gameOver;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always; // keeps restart working while paused
		_rng.Randomize();

		_player = GetNode<PlayerController>(PlayerPath);
		_waves = GetNode<WaveManager>(WaveManagerPath);
		_hud = GetNode<Hud>(HudPath);
		_draft = GetNode<DraftScreen>(DraftScreenPath);
		_tutorial = GetNodeOrNull<TutorialScreen>(TutorialScreenPath);

		// The play area is a portrait strip inside a wider window; both of these are local to
		// the Playfield node, which is what puts the field between the two HUD gutters.
		_player.Position = ArenaLayout.PlayerStart;
		_waves.ArenaSize = ArenaLayout.PlaySize;

		_hud.BindPlayer(_player);
		_hud.BindWaves(_waves);

		_waves.WaveCleared += OnWaveCleared;
		_draft.CardChosen += OnCardChosen;
		_player.Died += OnPlayerDied;

		if (_tutorial != null && ShowTutorialOnStart)
		{
			_tutorial.Dismissed += OnTutorialDismissed;
			GetTree().Paused = true;
			_tutorial.Open(firstTime: true);
			return;
		}

		StartNextWave();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_gameOver && @event.IsActionPressed("restart"))
		{
			GetTree().Paused = false;
			GetTree().ReloadCurrentScene();
			return;
		}

		// Help is available mid-run, but not on top of a draft: dismissing it unpauses, which
		// would drop the player back into the game with an unpicked card still pending.
		if (@event.IsActionPressed("tutorial") && _tutorial != null && !_tutorial.IsOpen && !_draft.IsOpen)
		{
			GetTree().Paused = true;
			_tutorial.Open(firstTime: false);
		}
	}

	private void OnTutorialDismissed()
	{
		GetTree().Paused = false;

		// Wave 1 waits for the first dismissal. Later reopens are just a reference card.
		if (Wave == 0)
			StartNextWave();
	}

	private void StartNextWave()
	{
		Wave++;
		_player.RefillDashes();
		_waves.StartWave(Wave);
	}

	private void OnWaveCleared(int wave)
	{
		if (_gameOver)
			return;

		// Let the last kill land before the screen takes over.
		SceneTreeTimer timer = GetTree().CreateTimer(DraftDelay);
		timer.Timeout += OpenDraft;
	}

	private void OpenDraft()
	{
		if (_gameOver)
			return;

		_pendingOffer = CardLibrary.Draw(CardsOffered, Wave, _rng);
		GetTree().Paused = true;
		_draft.Present(Wave, _pendingOffer);
	}

	private void OnCardChosen(int index)
	{
		if (index < 0 || index >= _pendingOffer.Count)
			return;

		Card card = _pendingOffer[index];
		card.Apply(_player);
		Deck.Add(card);

		GetTree().Paused = false;
		_hud.ShowBanner(card.Title, 1.0f);
		StartNextWave();
	}

	private void OnPlayerDied()
	{
		_gameOver = true;
		Sfx.Play(Sounds.GameOver, pitchSpread: 0f);
		_waves.ClearField();
		_hud.ShowBanner($"DEAD  —  WAVE {Wave}  —  PRESS R", 600f);
	}
}
