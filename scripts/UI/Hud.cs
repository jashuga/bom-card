using Godot;

/// <summary>
/// Health/shield, wave state, and the three gun slots with their loaded ammo.
/// Built entirely in code — attach to a bare CanvasLayer, call Bind* once.
/// </summary>
public partial class Hud : CanvasLayer
{
	private ProgressBar _healthBar;
	private ProgressBar _shieldBar;
	private Label _healthLabel;
	private Label _waveLabel;
	private Label _enemyLabel;
	private Label _bannerLabel;

	private readonly PanelContainer[] _slotPanels = new PanelContainer[GunLibrary.SlotCount];
	private readonly Label[] _slotNames = new Label[GunLibrary.SlotCount];
	private readonly Label[] _slotAmmo = new Label[GunLibrary.SlotCount];

	private WeaponController _weapons;

	public override void _Ready()
	{
		Layer = 10;
		BuildVitals();
		BuildWaveReadout();
		BuildSlots();
		BuildBanner();
	}

	// ---- binding ---------------------------------------------------------------

	public void BindPlayer(PlayerController player)
	{
		player.Health.Changed += OnHealthChanged;
		OnHealthChanged(player.Health.Current, player.Health.Max, player.Health.Shield);

		_weapons = player.Weapons;
		_weapons.AmmoChanged += OnAmmoChanged;
		_weapons.WeaponChanged += OnWeaponChanged;

		for (int slot = 0; slot < _slotNames.Length; slot++)
			_slotNames[slot].Text = $"{slot + 1}  {_weapons.Guns[slot].Name}";

		_weapons.EmitAllAmmo();
	}

	public void BindWaves(WaveManager waves)
	{
		waves.WaveStarted += OnWaveStarted;
		waves.WaveCleared += OnWaveCleared;
		waves.EnemyCountChanged += remaining => _enemyLabel.Text = $"ENEMIES  {remaining}";
	}

	public void ShowBanner(string text, float seconds = 1.6f)
	{
		_bannerLabel.Text = text;
		_bannerLabel.Modulate = Colors.White;
		_bannerLabel.Visible = true;

		Tween tween = CreateTween();
		tween.TweenInterval(seconds);
		tween.TweenProperty(_bannerLabel, "modulate:a", 0f, 0.4f);
		tween.TweenCallback(Callable.From(() => _bannerLabel.Visible = false));
	}

	// ---- signal handlers -------------------------------------------------------

	private void OnHealthChanged(float current, float max, float shield)
	{
		_healthBar.MaxValue = max;
		_healthBar.Value = current;
		_shieldBar.MaxValue = Mathf.Max(1f, Mathf.Max(shield, max));
		_shieldBar.Value = shield;
		_shieldBar.Visible = shield > 0f;
		_healthLabel.Text = shield > 0f
			? $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}   +{Mathf.CeilToInt(shield)}"
			: $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
	}

	private void OnAmmoChanged(int slot, int rounds, int capacity, string ammoName)
	{
		_slotAmmo[slot].Text = capacity > 0 ? $"{ammoName}  {rounds}/{capacity}" : ammoName;
		_slotAmmo[slot].AddThemeColorOverride("font_color", capacity > 0 ? _weapons.Guns[slot].Rarity.Tint() : UiTheme.Muted);
	}

	private void OnWeaponChanged(int slot)
	{
		for (int i = 0; i < _slotPanels.Length; i++)
		{
			bool active = i == slot;
			Color border = active ? _weapons.Guns[i].Rarity.Tint() : new Color(1f, 1f, 1f, 0.10f);
			_slotPanels[i].AddThemeStyleboxOverride("panel",
				UiTheme.Box(active ? UiTheme.PanelActive : UiTheme.Panel, border));
		}
	}

	private void OnWaveStarted(int wave, int enemyCount)
	{
		_waveLabel.Text = $"WAVE  {wave}";
		_enemyLabel.Text = $"ENEMIES  {enemyCount}";
		ShowBanner($"WAVE {wave}");
	}

	private void OnWaveCleared(int wave) => ShowBanner("WAVE CLEAR", 1.0f);

	// ---- construction ----------------------------------------------------------

	/// <summary>
	/// Pins a container to one corner and lets it size itself to its contents.
	/// Anchors alone give a zero rect; the grow direction decides which way the
	/// minimum size expands, which is what actually keeps it on screen.
	/// </summary>
	private T Pin<T>(T control, Control.LayoutPreset preset, Vector2 offset,
		Control.GrowDirection growH, Control.GrowDirection growV) where T : Control
	{
		control.SetAnchorsPreset(preset);
		control.GrowHorizontal = growH;
		control.GrowVertical = growV;
		control.Position = offset;
		control.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(control);
		return control;
	}

	private void BuildVitals()
	{
		var column = Pin(new VBoxContainer(), Control.LayoutPreset.TopLeft, new Vector2(20f, 16f),
			Control.GrowDirection.End, Control.GrowDirection.End);
		column.AddThemeConstantOverride("separation", 4);

		_healthLabel = UiTheme.MakeLabel("100 / 100", 16);
		column.AddChild(_healthLabel);

		_healthBar = UiTheme.MakeBar(UiTheme.HealthFill, 260f, 14f);
		column.AddChild(_healthBar);

		_shieldBar = UiTheme.MakeBar(UiTheme.ShieldFill, 260f, 7f);
		_shieldBar.Visible = false;
		column.AddChild(_shieldBar);
	}

	private void BuildWaveReadout()
	{
		var column = Pin(new VBoxContainer(), Control.LayoutPreset.TopRight, new Vector2(-20f, 16f),
			Control.GrowDirection.Begin, Control.GrowDirection.End);
		column.AddThemeConstantOverride("separation", 2);

		_waveLabel = UiTheme.MakeLabel("WAVE  1", 20);
		_waveLabel.HorizontalAlignment = HorizontalAlignment.Right;
		column.AddChild(_waveLabel);

		_enemyLabel = UiTheme.MakeLabel("ENEMIES  0", 14, UiTheme.Muted);
		_enemyLabel.HorizontalAlignment = HorizontalAlignment.Right;
		column.AddChild(_enemyLabel);
	}

	private void BuildSlots()
	{
		var row = Pin(new HBoxContainer(), Control.LayoutPreset.BottomLeft, new Vector2(20f, -20f),
			Control.GrowDirection.End, Control.GrowDirection.Begin);
		row.AddThemeConstantOverride("separation", 10);

		for (int slot = 0; slot < _slotPanels.Length; slot++)
		{
			var panel = new PanelContainer
			{
				CustomMinimumSize = new Vector2(190f, 0f),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			panel.AddThemeStyleboxOverride("panel", UiTheme.Box(UiTheme.Panel, new Color(1f, 1f, 1f, 0.10f)));
			row.AddChild(panel);

			var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
			column.AddThemeConstantOverride("separation", 2);
			panel.AddChild(column);

			_slotNames[slot] = UiTheme.MakeLabel($"{slot + 1}", 14);
			column.AddChild(_slotNames[slot]);

			_slotAmmo[slot] = UiTheme.MakeLabel("Empty", 12, UiTheme.Muted);
			column.AddChild(_slotAmmo[slot]);

			_slotPanels[slot] = panel;
		}
	}

	private void BuildBanner()
	{
		var center = Pin(new CenterContainer(), Control.LayoutPreset.CenterTop, new Vector2(0f, 110f),
			Control.GrowDirection.Both, Control.GrowDirection.End);

		_bannerLabel = UiTheme.MakeLabel(string.Empty, 44);
		_bannerLabel.Visible = false;
		center.AddChild(_bannerLabel);
	}
}
