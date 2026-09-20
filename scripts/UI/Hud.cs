using Godot;

/// <summary>
/// Readouts in the two gutters either side of the portrait play area:
///
///   LEFT  — wave number, enemies remaining, the three gun slots
///   RIGHT — health and shield bars, plus the dash charge
///
/// Near-UNSTYLED: plain Labels and ProgressBars on the project theme, so the look stays a
/// blank slate. The only thing set here is text SIZE, which is layout rather than style — the
/// project font is a monospace arcade face roughly one em per character, so a line that fit
/// the gutter in a proportional font no longer does. Column geometry comes from
/// <see cref="ArenaLayout"/>.
/// </summary>
public partial class Hud : CanvasLayer
{
	/// <summary>Inset from the screen edge and from the play area.</summary>
	private const float Margin = 24f;

	/// <summary>
	/// Gutter text size. At ~1em per glyph this fits about 24 characters in a column, which is
	/// what every readout below is written to stay inside.
	/// </summary>
	private const int TextSize = 12;

	private const int BannerSize = 20;

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
	private readonly Label[] _slotAmmoLabels = new Label[GunLibrary.SlotCount];
	private readonly string[] _slotGun = new string[GunLibrary.SlotCount];
	private readonly string[] _slotAmmo = new string[GunLibrary.SlotCount];

	private WeaponController _weapons;

	public override void _Ready()
	{
		Layer = 10;
		BuildLeftColumn();
		BuildRightColumn();
		BuildBanner();
	}

	// ---- binding ---------------------------------------------------------------

	public void BindPlayer(PlayerController player)
	{
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
		_slotGun[slot] = _weapons.Guns[slot].Name;
		_slotAmmo[slot] = capacity > 0 ? $"{ammoName} {rounds}/{capacity}" : ammoName;
		RefreshSlots();
	}

	private void OnWeaponChanged(int slot) => RefreshSlots();

	/// <summary>
	/// Redraws all three gun lines. The ammo text and the selection marker are composed
	/// together rather than written separately, because an ammo update rewrites the whole
	/// label — writing the marker in its own pass meant firing erased it.
	///
	/// The marker is a leading ">" rather than a colour so it survives a restyle.
	///
	/// Gun and ammo go on separate lines: "3  Hand Cannon  —  Explosive  12/12" is 37 glyphs,
	/// which is half again wider than the gutter in a one-em-per-character font.
	/// </summary>
	private void RefreshSlots()
	{
		for (int i = 0; i < _slotLabels.Length; i++)
		{
			_slotLabels[i].Text = (i == _weapons.ActiveSlot ? "> " : "  ") + $"{i + 1} {_slotGun[i]}";
			_slotAmmoLabels[i].Text = "    " + _slotAmmo[i];
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
	/// Places a gutter column at an absolute screen position and fixes its width.
	///
	/// Deliberately NOT anchors-plus-grow-direction: anchoring to TopRight and growing Begin
	/// leaves the box pinned at the right edge and growing off-screen, which clips the whole
	/// column. The layout is a known fixed size, so the position is simply computed.
	/// </summary>
	/// <summary>
	/// A gutter label. ClipText is the important part: without it a Label's minimum width is
	/// its text width, so one long string stretches the whole column out over the play area
	/// and the game stops looking centred. Clipped, the column can never exceed its gutter.
	/// </summary>
	private static Label MakeColumnLabel(string text)
	{
		var label = new Label
		{
			Text = text,
			ClipText = true,
			CustomMinimumSize = new Vector2(ColumnWidth, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		label.AddThemeFontSizeOverride("font_size", TextSize);
		return label;
	}

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

		_waveLabel = MakeColumnLabel("WAVE  1");
		column.AddChild(_waveLabel);

		_enemyLabel = MakeColumnLabel("ENEMIES  0");
		column.AddChild(_enemyLabel);

		column.AddChild(MakeColumnLabel(string.Empty));

		for (int slot = 0; slot < _slotLabels.Length; slot++)
		{
			_slotLabels[slot] = MakeColumnLabel($"{slot + 1}");
			column.AddChild(_slotLabels[slot]);

			_slotAmmoLabels[slot] = MakeColumnLabel(string.Empty);
			column.AddChild(_slotAmmoLabels[slot]);
		}
	}

	private void BuildRightColumn()
	{
		VBoxContainer column = PinColumn(ArenaLayout.RightColumnX + Margin);

		_healthLabel = MakeColumnLabel("HEALTH");
		column.AddChild(_healthLabel);
		_healthBar = AddBar(column);

		_shieldLabel = MakeColumnLabel("SHIELD");
		column.AddChild(_shieldLabel);
		_shieldBar = AddBar(column);

		_dashLabel = MakeColumnLabel("DASH");
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

	/// <summary>
	/// Wave banners sit over the play area, which is centred by construction.
	///
	/// The label spans the full screen and centres its own text, rather than sitting in a
	/// PlayWidth-wide box pinned at the play area's left edge. That box only centred text
	/// narrower than itself: a longer banner grew the box rightwards from its fixed left edge
	/// and pushed the text off-centre — which is exactly what a wider font caused.
	/// </summary>
	private void BuildBanner()
	{
		_bannerLabel = new Label
		{
			Text = string.Empty,
			Visible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2(0f, ArenaLayout.PlayOrigin.Y + ArenaLayout.PlayHeight * 0.16f),
			Size = new Vector2(ArenaLayout.ScreenWidth, 0f),
		};

		_bannerLabel.AddThemeFontSizeOverride("font_size", BannerSize);
		AddChild(_bannerLabel);
	}
}
