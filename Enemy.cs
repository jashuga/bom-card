using Godot;
using System;

public partial class Enemy : StaticBody2D
{
	[Export]
	public int speed = 100;
	public override void _PhysicsProcess(double delta)
	{
		var PlayerPos = GetNode<CharacterBody2D>("../Player").Position;
		var VectorToPlayer = (PlayerPos - Position).Normalized();
		var velocity = VectorToPlayer * speed;
		Position += velocity * (float)delta;
	}
}
