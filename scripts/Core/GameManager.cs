using System.Collections.Generic;
using Godot;

/// <summary>
/// Arena root. Owns the run loop: start wave -> wave cleared -> pause -> draft -> next wave.
/// Everything else talks through signals, so this is the only file that knows the order.
/// </summary>
public partial class GameManager : Node2D
{
	[Export] public NodePath PlayerPath = "Player";
	[Export] public NodePath WaveManagerPath = "WaveManager";
	[Export] public NodePath HudPath = "Hud";
	[Export] public NodePath DraftScreenPath = "DraftScreen";

	[Export] public int CardsOffered = 3;
	[Export] public float DraftDelay = 0.9f;

	public Deck Deck { get; } = new();
	public int Wave { get; private set; }

	private PlayerController _player;
	private WaveManager _waves;
	private Hud _hud;
	private DraftScreen _draft;
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

		_hud.BindPlayer(_player);
		_hud.BindWaves(_waves);

		_waves.WaveCleared += OnWaveCleared;
		_draft.CardChosen += OnCardChosen;
		_player.Died += OnPlayerDied;

		StartNextWave();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_gameOver && @event.IsActionPressed("restart"))
		{
			GetTree().Paused = false;
			GetTree().ReloadCurrentScene();
		}
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
		_waves.ClearField();
		_hud.ShowBanner($"DEAD  —  WAVE {Wave}  —  PRESS R", 600f);
	}
}
