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

public partial class bullet : Area2D
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
		//temporary code to move bullet up
		//for debugging
		Vector2 velocity = new Vector2();
		velocity.Y = -500;
		Position += velocity * (float)delta;
		if(Position.Y < 0)
		{
			QueueFree();
		}
	}

	private void OnBulletEntered(Node e)
	{
		if(e is Enemy)
		{
			GD.Print("hit");
			e.QueueFree(); //DELETES ENEMY
			//QueueFree(); //DELETES BULLET
		}
	}
}
