using Godot;
using System;
using System.Collections;
public partial class CharacterBody2d : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	private Queue PrimaryQueue = new Queue();
	private Queue SecondaryQueue = new Queue();
	private Queue UltimateQueue = new Queue();

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;


		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Y = direction.Y * Speed;
			GD.Print("Direction: " + direction);
			GD.Print("Velocity: " + velocity);
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
	    public override void _Input(InputEvent @event)
    {
        //shoot is space bar ot left click
        //upon shooting creates a bullet
        if (@event.IsActionPressed("primary"))
			SpawnObject(GD.Load<PackedScene>($"res://bullet.tscn").Instantiate() as bullet);
        {
           
        }
		if (@event.IsActionPressed("secondary"))
        {
           SpawnObject(SecondaryQueue.Dequeue() as bullet);
        }
		if (@event.IsActionPressed("ultimate"))
        {
        	SpawnObject(UltimateQueue.Dequeue() as bullet);
        }

    }

	private void SpawnObject(bullet b)
	{
		AddSibling(b);
	}

	private void AddAmmo(Type bullet)
	{
		bullet bulletScene = (bullet)GD.Load<PackedScene>($"res://{nameof(bullet)}.tscn").Instantiate();
		bulletScene.BulletType = BulletTypes.SECONDARY;
		if(bulletScene.BulletType == BulletTypes.PRIMARY){
			GD.Print("Primary Bullet");
			PrimaryQueue.Enqueue(bulletScene);
		}
		else if(bulletScene.BulletType == BulletTypes.SECONDARY){
			SecondaryQueue.Enqueue(bulletScene);
			GD.Print("Secondary Bullet");
		}
		else if(bulletScene.BulletType == BulletTypes.ULTIMATE){
			UltimateQueue.Enqueue(bulletScene);
			GD.Print("Ultimate Bullet");
		}
	}
	private void OnNewCardTimerTimeout()
	{
		AddAmmo(typeof(bullet));
	}

	private void OnNewEnemyTimerTimeout()
	{
		var enemy = (CharacterBody2D)GD.Load<PackedScene>("res://enemy.tscn").Instantiate();
		enemy.Position = new Vector2(100, 100);
		AddSibling(enemy);
	}
}
