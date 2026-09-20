using Godot;

/// <summary>
/// The how-to-play card. Shown once before wave 1, and reopenable any time with H.
///
/// Runs while the tree is paused (ProcessMode Always), same as the draft screen — the run
/// loop leans on that: <see cref="GameManager"/> pauses, shows this, and only starts wave 1
/// once <see cref="Dismissed"/> fires.
///
/// Deliberately UNSTYLED: plain Labels with Godot's default theme. The text is the feature;
/// the presentation is a blank slate.
/// </summary>
public partial class TutorialScreen : CanvasLayer
{
	[Signal] public delegate void DismissedEventHandler();

	private static readonly string[] Lines =
	{
		"HOW TO PLAY",
		"",
		"W A S D        move",
		"LEFT / RIGHT   turn your turret (aim is a separate axis)",
		"SPACE          fire, hold it down",
		"SHIFT          dash, one charge per wave",
		"1 / 2 / 3      switch gun",
		"R              restart after you die",
		"",
		"1  SIDEARM       common, never runs dry",
		"2  REPEATER      rare, dead without a rare magazine",
		"3  HAND CANNON   epic, dead without an epic magazine",
		"",
		"Ammo cards load into the gun of the SAME rarity, and only that gun.",
		"Run a magazine dry and you drop straight back to the sidearm.",
		"",
		"Survive the wave, draft a card, repeat.",
	};

	private Control _root;
	private Label _prompt;

	public bool IsOpen => _root != null && _root.Visible;

	/// <summary>
	/// True once it has been dismissed. The run loop only starts a wave on that first
	/// dismissal; later reopens are just a reference card and must not restart anything.
	/// </summary>
	public bool HasBeenSeen { get; private set; }

	public override void _Ready()
	{
		// Above the draft screen (20) so H is never swallowed by a card pick.
		Layer = 30;
		ProcessMode = ProcessModeEnum.Always;
		Build();
		_root.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsOpen)
			return;

		// Anything that means "go" closes it, so nobody gets stuck on the help screen.
		if (@event.IsActionPressed("all_actions") || @event.IsActionPressed("ui_accept")
			|| @event.IsActionPressed("ui_cancel") || @event.IsActionPressed("tutorial"))
		{
			GetViewport().SetInputAsHandled();
			Dismiss();
		}
	}

	public void Open(bool firstTime)
	{
		_prompt.Text = firstTime
			? "press SPACE to start        (H reopens this screen)"
			: "press SPACE to resume";

		_root.Visible = true;
	}

	private void Dismiss()
	{
		_root.Visible = false;
		HasBeenSeen = true;
		EmitSignal(SignalName.Dismissed);
	}

	private void Build()
	{
		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.MouseFilter = Control.MouseFilterEnum.Stop;
		AddChild(_root);

		// Opaque enough that the paused game behind it does not compete with the text.
		var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.9f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		dim.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(dim);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		center.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(center);

		var column = new VBoxContainer();
		center.AddChild(column);

		foreach (string line in Lines)
			column.AddChild(new Label { Text = line });

		column.AddChild(new Label { Text = string.Empty });

		_prompt = new Label { Text = "press SPACE to start" };
		column.AddChild(_prompt);
	}
}
