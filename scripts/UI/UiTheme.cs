using Godot;

/// <summary>
/// Shared look-and-feel for the code-built UI. Both the HUD and the draft screen are
/// constructed in C# rather than .tscn files so four people can restyle without
/// fighting over scene merges.
/// </summary>
public static class UiTheme
{
	/// <summary>Press Start 2P — the 8-bit arcade face (Galaga / Pac-Man era). OFL licensed;
	/// the licence ships next to it in assets/fonts.</summary>
	public const string FontPath = "res://assets/fonts/PressStart2P-vaV7.ttf";

	public static readonly Color Ink = new("e8edf2");
	public static readonly Color Muted = new("8a93a0");
	public static readonly Color Panel = new(0.06f, 0.07f, 0.10f, 0.88f);
	public static readonly Color PanelActive = new(0.13f, 0.16f, 0.22f, 0.95f);
	public static readonly Color HealthFill = new("52d17c");
	public static readonly Color ShieldFill = new("4ea8de");
	public static readonly Color TrackFill = new(1f, 1f, 1f, 0.12f);

	private static Font _font;
	private static bool _fontResolved;

	/// <summary>
	/// The pixel font, or null if it isn't in the project — every caller already null-checks
	/// and falls back to Godot's default font.
	///
	/// Must go through ResourceLoader.Exists first: ResourceLoader.Load THROWS on a missing
	/// resource rather than returning null, which took the whole DraftScreen down when this
	/// font was deleted upstream. The resolved result is cached either way, so a missing font
	/// costs one lookup rather than one per label.
	/// </summary>
	public static Font Font
	{
		get
		{
			if (_fontResolved)
				return _font;

			_fontResolved = true;
			_font = ResourceLoader.Exists(FontPath) ? ResourceLoader.Load<Font>(FontPath) : null;

			if (_font == null)
				GD.Print($"UiTheme: {FontPath} not found — using the default font.");
			else
				MakeCrisp(_font);

			return _font;
		}
	}

	/// <summary>
	/// Turn off smoothing. A pixel face is drawn on an 8x8 grid, and antialiasing or hinting
	/// smears the edges into grey mush — exactly what makes a retro font stop looking retro.
	/// Done here rather than in the .import so it holds however the file was imported.
	/// </summary>
	private static void MakeCrisp(Font font)
	{
		if (font is not FontFile file)
			return;

		file.Antialiasing = TextServer.FontAntialiasing.None;
		file.Hinting = TextServer.Hinting.None;
		file.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
		file.MultichannelSignedDistanceField = false;
	}

	public static Label MakeLabel(string text, int size, Color? color = null)
	{
		var label = new Label { Text = text };
		Style(label, size, color);
		return label;
	}

	public static void Style(Label label, int size, Color? color = null)
	{
		if (Font != null)
			label.AddThemeFontOverride("font", Font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color ?? Ink);
	}

	public static void Style(Button button, int size, Color? color = null)
	{
		if (Font != null)
			button.AddThemeFontOverride("font", Font);
		button.AddThemeFontSizeOverride("font_size", size);
		button.AddThemeColorOverride("font_color", color ?? Ink);
	}

	/// <summary>
	/// A panel box. Square corners, a single-pixel border and NO antialiasing — a rounded or
	/// smoothed edge is the one thing that reads as modern next to an 8x8 pixel font.
	/// AntiAliasing is the important one: StyleBoxFlat smooths its edges by default, which
	/// leaves a soft half-lit fringe on what should be a hard one-pixel line.
	/// </summary>
	public static StyleBoxFlat Box(Color background, Color? border = null, int borderWidth = 1, int corner = 0)
	{
		var box = new StyleBoxFlat
		{
			BgColor = background,
			AntiAliasing = false,
			CornerRadiusTopLeft = corner,
			CornerRadiusTopRight = corner,
			CornerRadiusBottomLeft = corner,
			CornerRadiusBottomRight = corner,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 8,
			ContentMarginBottom = 8,
		};

		if (border.HasValue)
		{
			box.BorderColor = border.Value;
			box.BorderWidthLeft = borderWidth;
			box.BorderWidthRight = borderWidth;
			box.BorderWidthTop = borderWidth;
			box.BorderWidthBottom = borderWidth;
		}

		return box;
	}

	public static ProgressBar MakeBar(Color fill, float width, float height)
	{
		var bar = new ProgressBar
		{
			ShowPercentage = false,
			MinValue = 0,
			MaxValue = 100,
			Value = 100,
			CustomMinimumSize = new Vector2(width, height),
		};

		bar.AddThemeStyleboxOverride("background", Flat(TrackFill));
		bar.AddThemeStyleboxOverride("fill", Flat(fill));
		return bar;
	}

	/// <summary>
	/// A stylebox with no content margins — for bars, where padding would skew the fill.
	/// Square and unsmoothed for the same reason as <see cref="Box"/>.
	/// </summary>
	private static StyleBoxFlat Flat(Color color)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			AntiAliasing = false,
		};
	}
}
