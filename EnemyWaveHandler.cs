using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyWaveHandler : Control
{
	[Signal]
    public delegate void WaveCompleteEventHandler();
	Queue<List<Node2D>> enemyWave = new Queue<List<Node2D>>();
	bool WaveDone = false;
	int waveCount = 0;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		enemyWave.Enqueue(new List<Node2D>{GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D,
											GD.Load<PackedScene>("res://enemy.tscn").Instantiate() as Node2D,
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
		waveCount++;
		var enemyCount = 0;
		var numberOfEnemies = wave.Peek().Count;
		foreach (Node2D enemy in wave.Dequeue())
		{
			enemyCount++;
			var position = new Vector2(760/numberOfEnemies * enemyCount - 20, 100);
			GD.Print($"Spawning enemy {enemyCount} at position {position}");
			enemy.Position = position;
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
		if (GetChildCount() == 1 && WaveDone)
		{
			OnWaveComplete();
		}
	}

	public void OnWaveComplete()
	{
		EmitSignal(SignalName.WaveComplete);
		GD.Print("Wave Complete");
	}
}
