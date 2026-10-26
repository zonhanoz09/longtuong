using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Hub stays on screen; every widget opens as a popup panel on top of it
/// (dim background + gold-bordered frame + X button). Each widget has its own
/// popup size, tuned to its content: small for Recruit, large for Hero/Stage.
/// </summary>
[GlobalClass]
public partial class HubScene : Control
{
    // Popup content sizes (width x height), one per widget.
    public static readonly Vector2 HeroListPopupSize = new(1100, 620);
    public static readonly Vector2 BagPopupSize = new(1020, 640);
    public static readonly Vector2 HeroPopupSize = new(1140, 680);
    public static readonly Vector2 FormationPopupSize = new(900, 540);
    public static readonly Vector2 StagePopupSize = new(1180, 680);
    public static readonly Vector2 RecruitPopupSize = new(700, 360);

    public bool IsPopupOpen { get; private set; }
    private Panel? _popupFrame;
    private Control? _popupContent;
    private ColorRect? _dim;
    private Button? _closeButton;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        UiTheme.AddCurrencyBar(this, gm);
        RefreshStatus();
        var teamNames = string.Join(", ", gm.SelectedTeamIds.Select(id => HeroDatabase.Get(id).Name));
        var cards = new (string Node, string Text, bool Primary)[]
        {
            ("HeroListButton", $"Danh sách tướng sở hữu\n{gm.Save.OwnedHeroIds.Count}/6 tướng — xem cấp, sao, mảnh", false),
            ("BagButton", $"Túi đồ\n{gm.Save.Gold} Vàng — {gm.Save.Gems} KNB", false),
            ("EquipmentButton", "Trang bị\nCường hóa, tẩy luyện 4 món", false),
            ("FormationButton", $"Bày trận\nĐội hình: {teamNames}", true),
            ("StageButton", $"Vào ải\nẢi tiếp theo: {gm.Save.LastStage + 1}/10", true),
            ("EnhanceButton", "Cường hóa\nNâng cấp trang bị tướng", false),
            ("RecruitButton", "Chiêu mộ\n100 Vàng = 5 mảnh, tỉ lệ công khai", false),
            ("LogoutButton", "Đăng xuất\nVề màn hình đăng nhập", false),
        };
        foreach (var (node, text, primary) in cards)
        {
            var btn = GetNode<Button>($"WidgetBox/{node}");
            btn.Text = text;
            UiTheme.StyleButton(btn, primary);
        }
        SetupPopupHost();
    }

    private void RefreshStatus()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var status = GetNode<Label>("StatusLabel");
        UiTheme.StyleBody(status, 17);
        var teamNames = string.Join(", ", gm.SelectedTeamIds.Select(id => HeroDatabase.Get(id).Name));
        var starter = gm.Save.OwnedHeroIds.Count > 0 ? HeroDatabase.Get(gm.Save.OwnedHeroIds[0]).Name : "?";
        status.Text = $"Chào {gm.CurrentUsername} | Tướng đại diện: {starter} | Sở hữu: {gm.Save.OwnedHeroIds.Count}/6 tướng | Thắng: {gm.Save.Wins} | Ải cao nhất: {gm.Save.LastStage} | Đội hình: {teamNames}";
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
        RefreshStatus();
        return true;
    }

    private void OnClosePopupPressed() => ClosePopup();

    private void Go(string path) => GetTree().ChangeSceneToFile(path);
    private void OnHeroListPressed() => OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HeroListPopupSize);
    private void OnBagPressed() => OpenPopup("res://features/bag/scenes/BagScene.tscn", BagPopupSize);
    private void OnEquipmentPressed() => OpenPopup("res://features/hero/scenes/HeroScreen.tscn", HeroPopupSize);
    private void OnFormationPressed() => OpenPopup("res://features/formation/scenes/FormationScene.tscn", FormationPopupSize);
    private void OnStagePressed() => OpenPopup("res://features/stage/scenes/StageSelect.tscn", StagePopupSize);
    private void OnEnhancePressed() => OpenPopup("res://features/hero/scenes/HeroScreen.tscn", HeroPopupSize);
    private void OnRecruitPressed() => OpenPopup("res://features/recruit/scenes/RecruitScene.tscn", RecruitPopupSize);
    private void OnLogoutPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.Logout();
        Go("res://features/auth/scenes/AuthScene.tscn");
    }
}
