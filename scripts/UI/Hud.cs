using Godot;

/// <summary>
/// Readouts in the two gutters either side of the portrait play area:
///
///   LEFT  — wave number, enemies remaining, the three gun slots
///   RIGHT — health, shield and speed bars, plus the dash charge
///
/// Deliberately UNSTYLED: plain Labels and ProgressBars with Godot's default theme, so the
/// look is a blank slate. Nothing here sets a font, colour or stylebox — positioning and
/// wiring only. Column geometry comes from <see cref="ArenaLayout"/>.
/// </summary>
public partial class Hud : CanvasLayer
{
	/// <summary>Inset from the screen edge and from the play area.</summary>
	private const float Margin = 24f;

	private static float ColumnWidth => ArenaLayout.SideColumnWidth - Margin * 2f;

	private Label _waveLabel;
	private Label _enemyLabel;

	private ProgressBar _healthBar;
	private ProgressBar _shieldBar;
	private Label _healthLabel;
	private Label _shieldLabel;
	private Label _dashLabel;

	private Label _bannerLabel;

	private readonly Label[] _slotLabels = new Label[GunLibrary.SlotCount];
	private readonly string[] _slotText = new string[GunLibrary.SlotCount];

	private WeaponController _weapons;
	private PlayerController _player;

	public override void _Ready()
	{
		Layer = 10;
		BuildLeftColumn();
		BuildRightColumn();
		BuildBanner();
	}

	/// <summary>
	/// The speed bar tracks live velocity, so it has to be polled — there is no "velocity
	/// changed" signal, and adding one would fire every physics frame anyway.
	/// </summary>
	public override void _Process(double delta)
	{
		if (_player == null || !IsInstanceValid(_player))
			return;
	}

	// ---- binding ---------------------------------------------------------------

	public void BindPlayer(PlayerController player)
	{
		_player = player;

		player.Health.Changed += OnHealthChanged;
		OnHealthChanged(player.Health.Current, player.Health.Max, player.Health.Shield);

		player.DashChanged += OnDashChanged;
		OnDashChanged(player.DashesLeft, player.DashesPerWave);

		_shieldBar.MaxValue = Mathf.Max(1f, player.Health.MaxShield);

		_weapons = player.Weapons;
		_weapons.AmmoChanged += OnAmmoChanged;
		_weapons.WeaponChanged += OnWeaponChanged;
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
		_healthLabel.Text = $"HEALTH  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

		_shieldBar.Value = shield;
		_shieldLabel.Text = $"SHIELD  {Mathf.CeilToInt(shield)}";
	}

	private void OnDashChanged(int left, int max) =>
		_dashLabel.Text = left > 0 ? $"DASH  READY  ({left}/{max})" : "DASH  SPENT";

	private void OnAmmoChanged(int slot, int rounds, int capacity, string ammoName)
	{
		string gun = _weapons.Guns[slot].Name;
		string ammo = capacity > 0 ? $"{ammoName}  {rounds}/{capacity}" : ammoName;

		_slotText[slot] = $"{slot + 1}  {gun}  —  {ammo}";
		RefreshSlots();
	}

	private void OnWeaponChanged(int slot) => RefreshSlots();

	/// <summary>
	/// Redraws all three gun lines. The ammo text and the selection marker are composed
	/// together rather than written separately, because an ammo update rewrites the whole
	/// label — writing the marker in its own pass meant firing erased it.
	///
	/// The marker is a leading ">" rather than a colour so it survives a restyle.
	/// </summary>
	private void RefreshSlots()
	{
		for (int i = 0; i < _slotLabels.Length; i++)
			_slotLabels[i].Text = (i == _weapons.ActiveSlot ? "> " : " ") + (_slotText[i] ?? $"{i + 1}");
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
	/// Places a gutter column at an absolute screen position and fixes its width.
	///
	/// Deliberately NOT anchors-plus-grow-direction: anchoring to TopRight and growing Begin
	/// leaves the box pinned at the right edge and growing off-screen, which clips the whole
	/// column. The layout is a known fixed size, so the position is simply computed.
	/// </summary>
	private VBoxContainer PinColumn(float x)
	{
		var column = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(ColumnWidth, 0f),
			Size = new Vector2(ColumnWidth, 0f),
			Position = new Vector2(x, Margin),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		AddChild(column);
		return column;
	}

	private void BuildLeftColumn()
	{
		VBoxContainer column = PinColumn(Margin);

		_waveLabel = new Label { Text = "WAVE  1" };
		column.AddChild(_waveLabel);

		_enemyLabel = new Label { Text = "ENEMIES  0" };
		column.AddChild(_enemyLabel);

		column.AddChild(new Label { Text = string.Empty });

		for (int slot = 0; slot < _slotLabels.Length; slot++)
		{
			_slotLabels[slot] = new Label { Text = $"{slot + 1}" };
			column.AddChild(_slotLabels[slot]);
		}
	}

	private void BuildRightColumn()
	{
		VBoxContainer column = PinColumn(ArenaLayout.RightColumnX + Margin);

		_healthLabel = new Label { Text = "HEALTH" };
		column.AddChild(_healthLabel);
		_healthBar = AddBar(column);

		_shieldLabel = new Label { Text = "SHIELD" };
		column.AddChild(_shieldLabel);
		_shieldBar = AddBar(column);


		_dashLabel = new Label { Text = "DASH" };
		column.AddChild(_dashLabel);
	}

	private static ProgressBar AddBar(VBoxContainer column)
	{
		var bar = new ProgressBar
		{
			ShowPercentage = false,
			MinValue = 0,
			MaxValue = 100,
			Value = 0,
			CustomMinimumSize = new Vector2(0f, 16f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		column.AddChild(bar);
		return bar;
	}

	/// <summary>Wave banners sit over the play area, which is centred between the gutters.</summary>
	private void BuildBanner()
	{
		var center = new CenterContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(ArenaLayout.PlayWidth, 0f),
			Position = ArenaLayout.PlayOrigin + new Vector2(0f, ArenaLayout.PlayHeight * 0.16f),
		};
		AddChild(center);

		_bannerLabel = new Label { Text = string.Empty, Visible = false };
		center.AddChild(_bannerLabel);
	}
}
