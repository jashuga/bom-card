using Godot;

/// <summary>
/// Throwaway expanding ring. No scene file — built in code so nobody has to own a .tscn for it.
/// </summary>
public partial class BlastVfx : Node2D
{
	private float _radius = 8f;
	private float _alpha = 0.9f;
	private float _targetRadius = 64f;

	/// <summary>
	/// Safe to call from inside a physics callback: the add is deferred, because Godot
	/// refuses AddChild while the parent is flushing collision signals.
	/// </summary>
	public static void Spawn(Node parent, Vector2 globalPosition, float radius)
	{
		if (parent == null)
			return;

		var vfx = new BlastVfx { _targetRadius = radius, Position = globalPosition };
		parent.CallDeferred(Node.MethodName.AddChild, vfx);
	}

	public override void _Ready()
	{
		ZIndex = 5;
		var tween = CreateTween().SetParallel();
		tween.TweenMethod(Callable.From<float>(SetRadius), 8f, _targetRadius, 0.22f)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenMethod(Callable.From<float>(SetAlpha), 0.9f, 0f, 0.28f);
		tween.Chain().TweenCallback(Callable.From(QueueFree));
	}

	private void SetRadius(float r) { _radius = r; QueueRedraw(); }
	private void SetAlpha(float a) { _alpha = a; QueueRedraw(); }

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, _radius, new Color(1f, 0.62f, 0.25f, _alpha * 0.35f));
		DrawArc(Vector2.Zero, _radius, 0f, Mathf.Tau, 32, new Color(1f, 0.85f, 0.45f, _alpha), 3f, true);
	}
}
