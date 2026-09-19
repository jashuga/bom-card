using Godot;

/// <summary>
/// Shared look-and-feel for the code-built UI. Both the HUD and the draft screen are
/// constructed in C# rather than .tscn files so four people can restyle without
/// fighting over scene merges.
/// </summary>
public static class UiTheme
{
	public const string FontPath = "res://assets/PixelatedEleganceRegular-ovawB.ttf";

	public static readonly Color Ink = new("e8edf2");
	public static readonly Color Muted = new("8a93a0");
	public static readonly Color Panel = new(0.06f, 0.07f, 0.10f, 0.88f);
	public static readonly Color PanelActive = new(0.13f, 0.16f, 0.22f, 0.95f);
	public static readonly Color HealthFill = new("52d17c");
	public static readonly Color ShieldFill = new("4ea8de");
	public static readonly Color TrackFill = new(1f, 1f, 1f, 0.12f);

	private static Font _font;

	public static Font Font => _font ??= ResourceLoader.Load<Font>(FontPath);

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

	public static StyleBoxFlat Box(Color background, Color? border = null, int borderWidth = 2, int corner = 6)
	{
		var box = new StyleBoxFlat
		{
			BgColor = background,
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

	/// <summary>A stylebox with no content margins — for bars, where padding would skew the fill.</summary>
	private static StyleBoxFlat Flat(Color color)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = 3,
			CornerRadiusTopRight = 3,
			CornerRadiusBottomLeft = 3,
			CornerRadiusBottomRight = 3,
		};
	}
}
