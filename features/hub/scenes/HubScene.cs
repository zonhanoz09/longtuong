using Godot;
using System;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Hub in 3 zones, measured from Huynh's second sketch (1280x720):
/// top 0-306 = avatar (left, 0-273) + Stage / Logout (right),
/// middle 306-612 = courtyard where the default hero walks,
/// bottom 612-720 = 5 function buttons, starting at x=360 as sketched.
/// Every widget still opens as a popup panel on top of the Hub
/// (dim background + gold-bordered frame + X).
/// Popup sizes are unchanged from Phase 2.3.
/// Note: the old separate Trang bi button is gone on purpose — it opened
/// the same HeroScreen as Cuong hoa, so equipment is reached via Cuong hoa.
/// </summary>
[GlobalClass]
public partial class HubScene : Control
{
	// Popup content sizes (width x height), one per widget.
	// Phase 2.18: 1000x600 (was 1000x560 in 2.17 — the dismiss button, the
	// stats grid and the "Kinh nghiệm" title need 40px more, still smaller
	// than the original 1180x660; frame is 1036x636, centred at (122, 42)).
	public static readonly Vector2 HeroListPopupSize = new(1000, 600);
	public static readonly Vector2 BagPopupSize = new(760, 600);
	public static readonly Vector2 HeroPopupSize = new(1140, 680);
	public static readonly Vector2 FormationPopupSize = new(900, 540);
	public static readonly Vector2 StagePopupSize = new(1180, 680);
	// Phase 2.23: the tavern popup (grades + 3 generals + RPS + soul list).
	// 1000x620 (not 1000x600: the 2.3 test requires every widget popup
	// size to be different, and 1000x600 is the hero-list popup).
	public static readonly Vector2 RecruitPopupSize = new(1000, 620);
	// Phase 2.29: Tướng Tinh moved out of the Trang bị popup (Huynh 2b).
	public static readonly Vector2 StarChartPopupSize = new(520, 400);
	// Phase 2.31a: Cây Rung Tiền (minimal Bạc income for the silver economy).
	public static readonly Vector2 MoneyTreePopupSize = new(440, 360);
	// Phase 2.42: Hoạt Động (campaign-gated PvE modes). The bottom bar is
	// full (positions fixed by 2.14), so the button sits in the top bar,
	// left of Vào ải.
	public static readonly Vector2 ActivityPopupSize = new(960, 560);
	// Phase 2.46: Phó Bản screen (opened from the Hoạt Động card).
	public static readonly Vector2 DungeonPopupSize = new(920, 600);
	// Phase 2.47: Ải Vượt Tướng (+ Cửa hàng Chiến tích tab, 2.48).
	public static readonly Vector2 DuelPopupSize = new(720, 520);
	// Phase 2.49: Thí Luyện Tháp.
	public static readonly Vector2 TrialPopupSize = new(700, 500);
	// Phase 2.51: Chiến Dịch Tổ Đội.
	public static readonly Vector2 PartyPopupSize = new(860, 540);

	// Zone bounds on the 1280x720 viewport, from the second sketch:
	// top 42.5%, middle 42.5%, bottom 14.9%; avatar column 21.3%.
	public static readonly Rect2 TopBarRect = new(0, 0, 1280, 110);
	public static readonly Rect2 CourtyardRect = new(0, 110, 1280, 502);
	public static readonly Rect2 FunctionBarRect = new(0, 612, 1280, 108);

	// Walk area, in Courtyard-local coordinates, for the character's
	// top-left corner (character body is 64x96 + name label to +124;
	// courtyard is 306 tall, so max top-left y is 174).
	public static readonly Rect2 WalkArea = new(80, 40, 1056, 330);
	public static readonly Vector2 CharacterStart = new(608, 180);
	public const float CharacterSpeed = 140f;

	public bool IsPopupOpen { get; private set; }
	public Vector2 CharacterPosition => _character?.Position ?? CharacterStart;
	public bool IsCharacterMoving => _hasTarget;

	private Panel? _popupFrame;
	private Control? _popupContent;
	private ColorRect? _dim;
	private Button? _closeButton;
	private Control? _character;
	private ColorRect? _characterBody;
	private Label? _characterName;
	private Vector2 _target;
	private bool _hasTarget;
	private float _idleTimer = 3f;
	private float _bobTime;
	private readonly Random _random = new();

	public override void _Ready()
	{
		UiTheme.ApplyBackground(this);
		var gm = GetNode<GameManager>("/root/GameManager");
		ProgressionService.EnsureDefaults(gm.Save);
		var avatarPanel = GetNode<Panel>("TopBar/AvatarPanel");
		var panelStyle = new StyleBoxFlat
		{
			BgColor = UiTheme.Panel,
			BorderColor = UiTheme.Gold,
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
		};
		avatarPanel.AddThemeStyleboxOverride("panel", panelStyle);
		BuildIconRows();
		_character = GetNode<Control>("Courtyard/HubCharacter");
		_characterBody = GetNode<ColorRect>("Courtyard/HubCharacter/CharacterBody");
		_characterName = GetNode<Label>("Courtyard/HubCharacter/CharacterNameLabel");
		_character.Position = CharacterStart;
		RefreshAvatar();
		SetupPopupHost();
	}

	public override void _Process(double delta) => StepCharacter((float)delta);

	/// <summary>ESC closes the open Hub popup (any widget, not just Vo tuong).
	/// Unhandled input only, so widgets that consume a key keep it; a held
	/// key's echo events are ignored; with no popup open, ESC does nothing
	/// (it never quits the game from the Hub).</summary>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsPopupOpen) return;
		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
		{
			ClosePopup();
			GetViewport()?.SetInputAsHandled();
		}
	}

	/// <summary>Advance the courtyard character by delta seconds. Public so
	/// headless tests can drive it without waiting for rendered frames.</summary>
	public void StepCharacter(float delta)
	{
		if (_character == null) return;
		if (_hasTarget)
		{
			var toTarget = _target - _character.Position;
			var dist = toTarget.Length();
			var step = CharacterSpeed * delta;
			if (dist <= Math.Max(step, 2f))
			{
				_character.Position = _target;
				_hasTarget = false;
				_idleTimer = 2f + (float)_random.NextDouble() * 3f; // idle 2-5s, then wander
				if (_characterBody != null) _characterBody.Position = Vector2.Zero;
			}
			else
			{
				_character.Position += toTarget / dist * step;
				// Walk bob (idle placeholder has no walk animation yet):
				// bounce only the body sprite, never the tracked position.
				_bobTime += delta * 10f;
				if (_characterBody != null)
					_characterBody.Position = new Vector2(0, (float)Math.Sin(_bobTime) * 3f);
			}
			return;
		}
		_idleTimer -= delta;
		if (_idleTimer <= 0f)
			MoveCharacterTo(new Vector2(
				WalkArea.Position.X + (float)_random.NextDouble() * WalkArea.Size.X,
				WalkArea.Position.Y + (float)_random.NextDouble() * WalkArea.Size.Y));
	}

	/// <summary>Set the character's destination (top-left corner, clamped
	/// to the walk area). Click handling converts a click point to this
	/// form by subtracting half the body size first.</summary>
	public void MoveCharacterTo(Vector2 position)
	{
		_target = new Vector2(
			Math.Clamp(position.X, WalkArea.Position.X, WalkArea.Position.X + WalkArea.Size.X),
			Math.Clamp(position.Y, WalkArea.Position.Y, WalkArea.Position.Y + WalkArea.Size.Y));
		_hasTarget = true;
	}

	private void OnCourtyardGuiInput(InputEvent @event)
	{
		if (IsPopupOpen) return;
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			MoveCharacterTo(mb.Position - new Vector2(32, 48));
	}

	private void RefreshAvatar()
	{
		var gm = GetNode<GameManager>("/root/GameManager");
		var heroId = gm.SelectedHeroId;
		if (string.IsNullOrEmpty(heroId) || !gm.Save.OwnedHeroIds.Contains(heroId))
			heroId = gm.Save.OwnedHeroIds.Count > 0 ? gm.Save.OwnedHeroIds[0] : "";
		GetNode<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarNameLabel").Text =
			string.IsNullOrEmpty(gm.CurrentUsername) ? "Chưa đăng nhập" : gm.CurrentUsername;
		// Phase 2.27: procedural portrait — the AvatarPortrait ColorRect
		// gets a tier-tinted background plus the hero's given-name char.
		var portrait = GetNode<ColorRect>("TopBar/AvatarPanel/AvatarHBox/AvatarPortrait");
		var charLabel = portrait.GetChildren().OfType<Label>().FirstOrDefault();
		if (charLabel == null)
		{
			charLabel = new Label
			{
				Name = "AvatarPortraitChar",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			};
			charLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			portrait.AddChild(charLabel);
		}
		if (!string.IsNullOrEmpty(heroId))
		{
			var hero = HeroDatabase.Get(heroId);
			var prog = ProgressionService.ProgressOf(gm.Save, heroId);
			GetNode<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarHeroLabel").Text =
				$"Tướng: {hero.Name} · Lv {prog.Level}";
			if (_characterName != null) _characterName.Text = hero.Name;
			var tier = TavernData.TierOf(heroId);
			var color = tier == null ? UiTheme.Gold : UiTheme.TierColor(tier.Value);
			portrait.Color = UiTheme.Panel;
			charLabel.Text = AssetIcons.PortraitChar(heroId);
			charLabel.AddThemeColorOverride("font_color", color);
			charLabel.AddThemeFontSizeOverride("font_size", AssetIcons.PortraitChar(heroId).Length <= 2 ? 28 : 20);
			GetNode<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarHeroLabel").AddThemeColorOverride("font_color", color);
		}
		else
		{
			GetNode<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarHeroLabel").Text = "Chưa có tướng";
			if (_characterName != null) _characterName.Text = "—";
			charLabel.Text = "";
		}
		GetNode<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarCurrencyLabel").Text =
			$"Vàng: {gm.Save.Gold:N0}  |  Bạc: {gm.Save.Silver:N0}";
	}

	// Phase 2.57: icon rows (top/bottom) like the reference — 64x64 rounded
	// square buttons with symbol + 12px label below.
	private void BuildIconRows()
	{
		// Đăng xuất: small button top-right of the slim bar.
		var logout = new Button { Name = "LogoutButton", Text = "Đăng xuất" };
		logout.Position = new Vector2(1170, 37);
		logout.Size = new Vector2(94, 36);
		UiTheme.StyleButton(logout, false);
		logout.CustomMinimumSize = new Vector2(0, 0);
		logout.Size = new Vector2(94, 36);
		logout.AddThemeFontSizeOverride("font_size", 13);
		logout.Pressed += OnLogoutPressed;
		GetNode<Control>("TopBar").AddChild(logout);

		// Top row (right-aligned, below TopBar): Hoạt Động, Vào ải.
		var topIcons = new (string Name, string Label, string Symbol, string Method)[]
		{
			("ActivityButton", "Hoạt Động", "HĐ", "OnActivityPressed"),
			("StageButton", "Vào ải", "Ải", "OnStagePressed"),
		};
		for (int i = 0; i < topIcons.Length; i++)
		{
			var (name, label, symbol, method) = topIcons[i];
			var icon = MakeIcon(name, symbol, label);
			icon.Position = new Vector2(1104 + i * 80, 120);
			AddChild(icon);
			var btn = icon.GetNode<Button>(name);
			if (method == "OnActivityPressed") btn.Pressed += OnActivityPressed;
			else btn.Pressed += OnStagePressed;
		}

		// Bottom row (centered): 7 functions.
		var bottomIcons = new (string Name, string Label, string Symbol, string Method)[]
		{
			("StarChartButton", "Tướng Tinh", "★", "OnStarChartPressed"),
			("MoneyTreeButton", "Cây Tiền", "$", "OnMoneyTreePressed"),
			("HeroListButton", "Võ tướng", "Võ", "OnHeroListPressed"),
			("BagButton", "Túi đồ", "Túi", "OnBagPressed"),
			("FormationButton", "Bày trận", "Trận", "OnFormationPressed"),
			("EnhanceButton", "Trang bị", "Bị", "OnEnhancePressed"),
			("RecruitButton", "Chiêu mộ", "Mộ", "OnRecruitPressed"),
		};
		var bar = GetNode<Control>("FunctionBar");
		float startX = (1280 - (bottomIcons.Length * 80 - 16)) / 2;
		for (int i = 0; i < bottomIcons.Length; i++)
		{
			var (name, label, symbol, method) = bottomIcons[i];
			var icon = MakeIcon(name, symbol, label);
			icon.Position = new Vector2(startX + i * 80, 8);
			bar.AddChild(icon);
			var btn = icon.GetNode<Button>(name);
			switch (method)
			{
				case "OnStarChartPressed": btn.Pressed += OnStarChartPressed; break;
				case "OnMoneyTreePressed": btn.Pressed += OnMoneyTreePressed; break;
				case "OnHeroListPressed": btn.Pressed += OnHeroListPressed; break;
				case "OnBagPressed": btn.Pressed += OnBagPressed; break;
				case "OnFormationPressed": btn.Pressed += OnFormationPressed; break;
				case "OnEnhancePressed": btn.Pressed += OnEnhancePressed; break;
				case "OnRecruitPressed": btn.Pressed += OnRecruitPressed; break;
			}
		}
	}

	// 64x64 rounded-square icon button + 12px label below.
	private static VBoxContainer MakeIcon(string name, string symbol, string label)
	{
		var box = new VBoxContainer { Name = name + "Icon" };
		box.AddThemeConstantOverride("separation", 2);
		var btn = new Button { Name = name, CustomMinimumSize = new Vector2(64, 64), Text = symbol };
		btn.AddThemeFontSizeOverride("font_size", 22);
		var normal = new StyleBoxFlat
		{
			BgColor = UiTheme.Panel,
			BorderColor = UiTheme.Gold,
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
		};
		var hover = (StyleBoxFlat)normal.Duplicate();
		hover.BgColor = UiTheme.Background;
		btn.AddThemeStyleboxOverride("normal", normal);
		btn.AddThemeStyleboxOverride("hover", hover);
		btn.AddThemeStyleboxOverride("pressed", normal);
		btn.AddThemeStyleboxOverride("disabled", normal);
		btn.AddThemeColorOverride("font_color", UiTheme.Gold);
		box.AddChild(btn);
		var lab = new Label { Name = "Label", Text = label, HorizontalAlignment = HorizontalAlignment.Center };
		lab.AddThemeFontSizeOverride("font_size", 12);
		lab.AddThemeColorOverride("font_color", UiTheme.Text);
		lab.CustomMinimumSize = new Vector2(64, 0);
		box.AddChild(lab);
		return box;
	}

	private void SetupPopupHost()
	{
		_dim = GetNode<ColorRect>("Dim");
		_popupFrame = GetNode<Panel>("PopupFrame");
		_popupContent = GetNode<Control>("PopupFrame/PopupContent");
		_closeButton = GetNode<Button>("PopupFrame/CloseButton");
		var frameStyle = new StyleBoxFlat
		{
			BgColor = UiTheme.Panel,
			BorderColor = UiTheme.Gold,
			BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3,
			CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
		};
		_popupFrame.AddThemeStyleboxOverride("panel", frameStyle);
		_popupContent.ClipContents = true;
		UiTheme.StyleButton(_closeButton, true);
		_closeButton.Text = "X";
		_closeButton.CustomMinimumSize = new Vector2(40, 40);
		_closeButton.Pressed += OnClosePopupPressed;
		_dim.Visible = false;
		_popupFrame.Visible = false;
	}

	public void OpenPopup(string scenePath, Vector2 contentSize)
	{
		if (_popupFrame == null || _popupContent == null || _dim == null || _closeButton == null) return;
		ClosePopupSilently();
		var scene = ResourceLoader.Load<PackedScene>(scenePath);
		if (scene == null) return;
		var widget = scene.Instantiate<Control>();
		const float margin = 18;
		var frameSize = contentSize + new Vector2(margin * 2, margin * 2);
		_popupFrame.SetPosition(new Vector2((1280 - frameSize.X) / 2, (720 - frameSize.Y) / 2));
		_popupFrame.SetSize(frameSize);
		_popupContent.SetPosition(new Vector2(margin, margin));
		_popupContent.SetSize(contentSize);
		_closeButton.SetPosition(new Vector2(frameSize.X - 46, 6));
		_closeButton.SetSize(new Vector2(40, 40));
		_popupContent.AddChild(widget);
		// Widget root fills the content area (anchors full rect in its .tscn).
		widget.SetPosition(Vector2.Zero);
		widget.SetSize(contentSize);
		_dim.Visible = true;
		_popupFrame.Visible = true;
		_popupFrame.MoveToFront();
		IsPopupOpen = true;
	}

	private void ClosePopupSilently()
	{
		if (_popupContent == null) return;
		// Detach immediately (so the next popup sees an empty host) but free
		// deferred: freeing here would lock when ClosePopup runs from inside
		// the widget's own button handler.
		foreach (var child in _popupContent.GetChildren())
		{
			_popupContent.RemoveChild(child);
			child.QueueFree();
		}
		IsPopupOpen = false;
	}

	public bool ClosePopup()
	{
		if (!IsPopupOpen) return false;
		ClosePopupSilently();
		if (_dim != null) _dim.Visible = false;
		if (_popupFrame != null) _popupFrame.Visible = false;
		RefreshAvatar();
		return true;
	}

	private void OnClosePopupPressed() => ClosePopup();

	private void Go(string path) => GetTree().ChangeSceneToFile(path);
	private void OnHeroListPressed() => OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HeroListPopupSize);
	private void OnBagPressed() => OpenPopup("res://features/bag/scenes/BagScene.tscn", BagPopupSize);
	private void OnFormationPressed() => OpenPopup("res://features/formation/scenes/FormationScene.tscn", FormationPopupSize);
	private void OnStagePressed() => OpenPopup("res://features/stage/scenes/StageSelect.tscn", StagePopupSize);
	private void OnEnhancePressed() => OpenPopup("res://features/hero/scenes/HeroScreen.tscn", HeroPopupSize);
	private void OnRecruitPressed() => OpenPopup("res://features/recruit/scenes/RecruitScene.tscn", RecruitPopupSize);
	private void OnStarChartPressed() => OpenPopup("res://features/hero/scenes/StarChartScene.tscn", StarChartPopupSize);
	private void OnMoneyTreePressed() => OpenPopup("res://features/moneytree/scenes/MoneyTreeScene.tscn", MoneyTreePopupSize);
	private void OnActivityPressed() => OpenPopup("res://features/activity/scenes/ActivityScene.tscn", ActivityPopupSize);
	private void OnLogoutPressed()
	{
		var gm = GetNode<GameManager>("/root/GameManager");
		gm.Logout();
		Go("res://features/auth/scenes/AuthScene.tscn");
	}
}
