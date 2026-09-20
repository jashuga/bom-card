using Godot;

/// <summary>
/// The portrait play area. Positions itself so the field lands centred between the two HUD
/// gutters, then builds its own backdrop and walls from <see cref="ArenaLayout"/>.
///
/// Everything that lives in game space — Player, EnemyContainer, BulletContainer — is a
/// child of this node, so gameplay code keeps using plain 0..PlayWidth / 0..PlayHeight
/// coordinates and is completely unaware of the side columns.
///
/// Walls are built in code rather than authored in the .tscn for the same reason the UI is:
/// four people editing one scene file is the expensive merge, and these are derived numbers
/// that would otherwise have to be kept in sync by hand.
/// </summary>
public partial class Playfield : Node2D
{
	/// <summary>Left / right / bottom are solid for everyone.</summary>
	private const string WallsName = "Walls";

	/// <summary>The top is open to enemies (they walk in) but closed to the player.</summary>
	private const string BarrierName = "TopBarrier";

	public override void _Ready()
	{
		Position = ArenaLayout.PlayOrigin;

		BuildOutline();
		BuildWalls();
		BuildTopBarrier();
	}

	/// <summary>
	/// A plain 2px box round the play area so you can see where the portrait screen is.
	/// Placeholder, not art — delete this node or replace it once the real backdrop exists.
	/// </summary>
	private void BuildOutline()
	{
		float w = ArenaLayout.PlayWidth;
		float h = ArenaLayout.PlayHeight;

		AddChild(new Line2D
		{
			Name = "Outline",
			Width = 2f,
			ZIndex = -10,
			Points = new[]
			{
				Vector2.Zero, new Vector2(w, 0f), new Vector2(w, h), new Vector2(0f, h), Vector2.Zero,
			},
		});
	}

	private void BuildWalls()
	{
		var walls = new StaticBody2D
		{
			Name = WallsName,
			CollisionLayer = Layers.World,
			CollisionMask = 0,
		};
		AddChild(walls);

		float t = ArenaLayout.WallThickness;
		float w = ArenaLayout.PlayWidth;
		float h = ArenaLayout.PlayHeight;

		// Side walls run past the top and bottom edges so nothing squeezes through a corner.
		AddBox(walls, "Left", new Vector2(-t * 0.5f, h * 0.5f), new Vector2(t, h + t * 2f));
		AddBox(walls, "Right", new Vector2(w + t * 0.5f, h * 0.5f), new Vector2(t, h + t * 2f));
		AddBox(walls, "Bottom", new Vector2(w * 0.5f, h + t * 0.5f), new Vector2(w + t * 2f, t));
	}

	/// <summary>
	/// Enemies spawn above the screen and march down into it, so the top edge cannot be a
	/// normal wall. It sits on <see cref="Layers.Barrier"/>, which only the player masks.
	/// </summary>
	private void BuildTopBarrier()
	{
		var barrier = new StaticBody2D
		{
			Name = BarrierName,
			CollisionLayer = Layers.Barrier,
			CollisionMask = 0,
		};
		AddChild(barrier);

		float t = ArenaLayout.WallThickness;
		AddBox(barrier, "Top", new Vector2(ArenaLayout.PlayWidth * 0.5f, -t * 0.5f),
			new Vector2(ArenaLayout.PlayWidth + t * 2f, t));
	}

	private static void AddBox(StaticBody2D body, string name, Vector2 center, Vector2 size)
	{
		body.AddChild(new CollisionShape2D
		{
			Name = name,
			Position = center,
			Shape = new RectangleShape2D { Size = size },
		});
	}
}
