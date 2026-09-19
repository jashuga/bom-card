using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyWaveHandler : Control
{
	Queue<List<Node2D>> enemyWave = new Queue<List<Node2D>>();
	bool WaveDone = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		enemyWave.Enqueue(new List<Node2D>{GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D,
											GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D,
											GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D});
		enemyWave.Enqueue(new List<Node2D>{GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D,
											GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D,
											GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D});									
	SpawnEnemyWave(enemyWave);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SpawnEnemyWave(Queue<List<Node2D>> wave)
	{
			foreach (Node2D enemy in wave.Dequeue())
			{
				AddChild(enemy);
			}
			if(wave.Count == 0)
			{
				WaveDone = true;
				GD.Print("Wave Complete");
			} else{
				var timer = GetNode<Timer>("../NextPhaseTimer");
				timer.Start();
				timer.Timeout += () => SpawnEnemyWave(wave);
				
			} 
	}
	public void OnChildExitingTree(Node n)
	{
		GD.Print(GetChildCount());
		if (GetChildCount() == 1 && WaveDone)
		{
			GD.Print("Wave Complete");
		}
	}
}
