global using enums;
using Godot;
using System;

namespace enums{
	public enum BulletTypes
		{
			PRIMARY,
			SECONDARY,
			ULTIMATE
		}

}

public partial class bullet : CharacterBody2D
{
	
	//default bullet type is primary
	public BulletTypes BulletType = BulletTypes.PRIMARY;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//sets bullet position to player position
		Position = GetNode<CharacterBody2D>("../Player").Position;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2 velocity = Velocity;
		velocity.Y *= 100;
		Velocity = velocity;
	}
}
