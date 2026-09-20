using System.Collections.Generic;
using Godot;

/// <summary>
/// Between-wave draft: three cards, pick one. Click or press 1/2/3.
/// Runs while the tree is paused, so ProcessMode is Always.
/// </summary>
public partial class DraftScreen : CanvasLayer
{
	[Signal] public delegate void CardChosenEventHandler(int index);

	private const int Slots = 3;

	private Control _root;
	private Label _headline;
	private readonly Button[] _buttons = new Button[Slots];
	private readonly Label[] _rarityLabels = new Label[Slots];
	private readonly Label[] _titleLabels = new Label[Slots];
	private readonly Label[] _bodyLabels = new Label[Slots];

	private List<Card> _offer = new();

	public bool IsOpen => _root != null && _root.Visible;

	public override void _Ready()
	{
		Layer = 20;
		ProcessMode = ProcessModeEnum.Always;
		Build();
		_root.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsOpen)
			return;

		for (int i = 0; i < Slots; i++)
		{
			if (@event.IsActionPressed($"weapon_{i + 1}"))
			{
				GetViewport().SetInputAsHandled();
				Choose(i);
				return;
			}
		}
	}

	public void Present(int wave, List<Card> cards)
	{
		_offer = cards;
		_headline.Text = $"WAVE {wave} CLEAR  —  DRAFT A CARD";

		for (int i = 0; i < Slots; i++)
		{
			bool has = i < cards.Count;
			_buttons[i].Visible = has;
			if (!has)
				continue;

			Card card = cards[i];
			Color tint = card.Rarity.Tint();

			_rarityLabels[i].Text = card.Rarity.DisplayName().ToUpperInvariant();
			_rarityLabels[i].AddThemeColorOverride("font_color", tint);
			_titleLabels[i].Text = card.Title;
			_bodyLabels[i].Text = card.Description;

			// Full-strength tint even when idle: at one pixel wide, the old half-alpha border
			// washed out to grey and the rarity stopped reading as blue or purple at all.
			_buttons[i].AddThemeStyleboxOverride("normal", UiTheme.Box(UiTheme.Panel, tint));
			_buttons[i].AddThemeStyleboxOverride("hover", UiTheme.Box(UiTheme.PanelActive, tint));
			_buttons[i].AddThemeStyleboxOverride("pressed", UiTheme.Box(UiTheme.PanelActive, tint));
			_buttons[i].AddThemeStyleboxOverride("focus", UiTheme.Box(UiTheme.PanelActive, tint));
		}

		_root.Visible = true;
		_buttons[0].GrabFocus();
	}

	public void Close() => _root.Visible = false;

	private void Choose(int index)
	{
		if (index < 0 || index >= _offer.Count)
			return;

		Sfx.Play(Sounds.MenuSelect);
		Close();
		EmitSignal(SignalName.CardChosen, index);
	}

	private void Build()
	{
		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.MouseFilter = Control.MouseFilterEnum.Stop;
		AddChild(_root);

		var dim = new ColorRect { Color = new Color(0.02f, 0.03f, 0.05f, 0.82f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		dim.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(dim);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		center.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(center);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 28);
		column.Alignment = BoxContainer.AlignmentMode.Center;
		center.AddChild(column);

		_headline = UiTheme.MakeLabel("DRAFT A CARD", 28);
		_headline.HorizontalAlignment = HorizontalAlignment.Center;
		column.AddChild(_headline);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 20);
		column.AddChild(row);

		for (int i = 0; i < Slots; i++)
			row.AddChild(BuildCardButton(i));

		var hint = UiTheme.MakeLabel("click a card, or press 1 / 2 / 3", 13, UiTheme.Muted);
		hint.HorizontalAlignment = HorizontalAlignment.Center;
		column.AddChild(hint);
	}

	private Button BuildCardButton(int index)
	{
		var button = new Button
		{
			CustomMinimumSize = new Vector2(250f, 300f),
			Text = string.Empty,
			FocusMode = Control.FocusModeEnum.All,
		};
		button.Pressed += () => Choose(index);
		_buttons[index] = button;

		// Buttons don't lay out children, so the body is a full-rect container on top.
		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		column.OffsetLeft = 18;
		column.OffsetTop = 18;
		column.OffsetRight = -18;
		column.OffsetBottom = -18;
		column.AddThemeConstantOverride("separation", 10);
		button.AddChild(column);

		var key = UiTheme.MakeLabel($"[{index + 1}]", 13, UiTheme.Muted);
		column.AddChild(key);

		_rarityLabels[index] = UiTheme.MakeLabel("COMMON", 13);
		column.AddChild(_rarityLabels[index]);

		_titleLabels[index] = UiTheme.MakeLabel("Card", 22);
		_titleLabels[index].AutowrapMode = TextServer.AutowrapMode.WordSmart;
		column.AddChild(_titleLabels[index]);

		_bodyLabels[index] = UiTheme.MakeLabel(string.Empty, 14, UiTheme.Muted);
		_bodyLabels[index].AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_bodyLabels[index].SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		column.AddChild(_bodyLabels[index]);

		return button;
	}
}
