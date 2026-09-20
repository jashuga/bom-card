using Godot;

/// <summary>
/// One-shot sound playback through a small pool of voices, so overlapping shots don't cut
/// each other off. Drop one node called "Sfx" in the arena and call the statics from anywhere.
///
/// Silently does nothing when no instance exists, so nothing has to null-check and tests or
/// stripped-down scenes still run.
/// </summary>
public partial class Sfx : Node
{
	private static Sfx _instance;

	/// <summary>How many sounds can overlap. Beyond this the oldest voice is reused.</summary>
	[Export] public int Voices = 16;

	/// <summary>Global trim for all effects, in decibels.</summary>
	[Export] public float VolumeDb = -6f;

	private AudioStreamPlayer[] _players;
	private int _next;

	public override void _Ready()
	{
		_instance = this;

		// Always, so menu and game-over sounds still play with the tree paused.
		ProcessMode = ProcessModeEnum.Always;

		_players = new AudioStreamPlayer[Mathf.Max(1, Voices)];
		for (int i = 0; i < _players.Length; i++)
		{
			var player = new AudioStreamPlayer { ProcessMode = ProcessModeEnum.Always };
			AddChild(player);
			_players[i] = player;
		}
	}

	public override void _ExitTree()
	{
		if (_instance == this)
			_instance = null;
	}

	/// <summary>
	/// Play a one-shot. <paramref name="pitchSpread"/> randomises pitch a little so repeated
	/// shots don't sound like a machine stamping.
	/// </summary>
	public static void Play(AudioStream stream, float volumeDb = 0f, float pitchSpread = 0.06f)
	{
		if (stream == null || _instance == null || !IsInstanceValid(_instance))
			return;

		_instance.PlayOn(stream, volumeDb, pitchSpread);
	}

	private void PlayOn(AudioStream stream, float volumeDb, float pitchSpread)
	{
		AudioStreamPlayer player = _players[_next];
		_next = (_next + 1) % _players.Length;

		player.Stream = stream;
		player.VolumeDb = VolumeDb + volumeDb;
		player.PitchScale = pitchSpread > 0f
			? 1f + (float)GD.RandRange(-pitchSpread, pitchSpread)
			: 1f;
		player.Play();
	}
}
