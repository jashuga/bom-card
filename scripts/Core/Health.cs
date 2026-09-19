using Godot;

/// <summary>
/// Hit points + shield, as a reusable child node. Shields soak damage first and are
/// what Shield cards top up. Add one as a child named "Health" on anything damageable.
/// </summary>
public partial class Health : Node
{
	[Signal] public delegate void ChangedEventHandler(float current, float max, float shield);
	[Signal] public delegate void DiedEventHandler();

	[Export] public float Max = 100f;
	[Export] public float StartingShield = 0f;
	[Export] public float MaxShield = 100f;

	public float Current { get; private set; }
	public float Shield { get; private set; }
	public bool IsAlive => Current > 0f;

	public override void _Ready()
	{
		Current = Max;
		Shield = StartingShield;
		Notify();
	}

	public void Damage(float amount)
	{
		if (!IsAlive || amount <= 0f)
			return;

		float absorbed = Mathf.Min(Shield, amount);
		Shield -= absorbed;
		Current = Mathf.Max(0f, Current - (amount - absorbed));

		Notify();
		if (!IsAlive)
			EmitSignal(SignalName.Died);
	}

	public void Heal(float amount)
	{
		if (!IsAlive || amount <= 0f)
			return;
		Current = Mathf.Min(Max, Current + amount);
		Notify();
	}

	public void AddShield(float amount)
	{
		if (amount <= 0f)
			return;
		Shield = Mathf.Min(MaxShield, Shield + amount);
		Notify();
	}

	/// <summary>Raise the ceiling and grant the difference, so +max HP cards feel immediate.</summary>
	public void AddMaxHealth(float amount)
	{
		Max += amount;
		Current += amount;
		Notify();
	}

	private void Notify() => EmitSignal(SignalName.Changed, Current, Max, Shield);
}
