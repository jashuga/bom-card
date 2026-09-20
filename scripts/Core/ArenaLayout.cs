using Godot;

/// <summary>
/// The one source of truth for screen geometry. The game is a PORTRAIT arcade cabinet
/// screen centred in a wider window, with a HUD column down each side:
///
///     |&lt;-- side --&gt;|&lt;----- play -----&gt;|&lt;-- side --&gt;|
///     |  readouts  |   600 x 904      |   vitals   |
///
/// Play-area coordinates are LOCAL to the Playfield node, so gameplay code keeps working
/// in a plain 0..PlayWidth / 0..PlayHeight space and never has to know about the margins.
/// Change the numbers here and the walls, backdrop, spawns and HUD all follow.
/// </summary>
public static class ArenaLayout
{
	public const float ScreenWidth = 1280f;
	public const float ScreenHeight = 960f;

	/// <summary>Portrait play area width. Tight enough to read as an arcade shooter.</summary>
	public const float PlayWidth = 600f;

	/// <summary>
	/// Gap above and below the play area. Without it the bezel would be drawn hard against the
	/// window edge and the top and bottom rules would be clipped in half.
	/// </summary>
	public const float PlayTopMargin = 28f;

	public const float PlayHeight = ScreenHeight - PlayTopMargin * 2f;

	/// <summary>Width of each HUD gutter. Chosen so the play area lands dead centre.</summary>
	public const float SideColumnWidth = (ScreenWidth - PlayWidth) / 2f;

	/// <summary>Solid wall band outside the play area. Thick enough that nothing tunnels it.</summary>
	public const float WallThickness = 60f;

	/// <summary>Where the Playfield node sits in screen space.</summary>
	public static Vector2 PlayOrigin => new(SideColumnWidth, PlayTopMargin);

	public static Vector2 PlaySize => new(PlayWidth, PlayHeight);

	/// <summary>Left edge of the right-hand HUD gutter, in screen space.</summary>
	public static float RightColumnX => ScreenWidth - SideColumnWidth;

	/// <summary>Player spawn, in play-area local coordinates: centred, low, facing up.</summary>
	public static Vector2 PlayerStart => new(PlayWidth * 0.5f, PlayHeight * 0.82f);

	/// <summary>Screen-space centre of the play area. Equals the screen centre by construction.</summary>
	public static Vector2 PlayCenter => PlayOrigin + PlaySize * 0.5f;
}
