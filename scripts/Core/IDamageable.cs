using Godot;

/// <summary>
/// Anything a bullet can hurt. Implemented by PlayerController and EnemyBase.
/// Bullets only ever talk to targets through this — never through a concrete type.
/// </summary>
public interface IDamageable
{
	bool IsAlive { get; }

	/// <param name="source">Who fired the shot. May be null.</param>
	void TakeDamage(float amount, Node2D source);
}
