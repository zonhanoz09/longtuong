using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.34: Lò rèn (forge) — buy blueprints + ore with Vàng,
/// forge set equipment into the bag. Opened from the Trang bị popup.
/// </summary>
[GlobalClass]
public partial class ForgeScene : Control
{
    public static readonly Vector2 PopupSize = new(760, 600);

    private string _selectedSet = "chu_tuoc";
    private string _selectedSlot = "weapon";
    private Label? _oreLabel;
    private Label? _infoLabel;
    private Label? _statusLabel;
    private VBoxContainer? _box;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        var title = GetNode<Label>("TitleLabel");
        title.Text = "Lò Rèn";
        UiTheme.StyleTitle(title);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        var scroll = GetNode<ScrollContainer>("Scroll");
        _box = GetNode<VBoxContainer>("Scroll/Box");
        Refresh();
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        if (_box == null) return;
        foreach (var child in _box.GetChildren()) { _box.RemoveChild(child); child.QueueFree(); }

        _box.AddChild(SectionLabel("Bản vẽ:"));
        foreach (var setId in ForgeService.BlueprintPrices.Keys)
        {
            var set = EquipmentSets.Get(setId);
            bool learned = ForgeService.HasBlueprint(save, setId);
            var row = new HBoxContainer { Name = $"BlueprintRow_{setId}" };
            row.AddThemeConstantOverride("separation", 8);
            var info = new Label
            {
                Text = $"{set.Name} (Lv{set.Tier} · {EquipmentSets.QualityName(set.Quality)})",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            UiTheme.StyleBody(info, UiTheme.FontSmall);
            info.AddThemeColorOverride("font_color", EquipmentSets.QualityColor(set.Quality));
            row.AddChild(info);
            var buyBtn = new Button
            {
                Name = $"BlueprintBuy_{setId}",
                Text = learned ? "Đã học ✓" : $"Mua {ForgeService.BlueprintPrices[setId]} Vàng",
                CustomMinimumSize = new Vector2(190, 44),
                Disabled = learned || save.Gold < ForgeService.BlueprintPrices[setId],
            };
            buyBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
            UiTheme.StyleButton(buyBtn, !learned);
            var sid = setId;
            buyBtn.Pressed += () =>
            {
                var gm2 = GetNode<GameManager>("/root/GameManager");
                SetStatus(ForgeService.TryBuyBlueprint(gm2.Save, sid)
                    ? $"Đã học bản vẽ {EquipmentSets.Get(sid).Name}!"
                    : "Không đủ Vàng mua bản vẽ!");
                gm2.SaveGame();
                Refresh();
            };
            row.AddChild(buyBtn);
            _box.AddChild(row);
        }

        _box.AddChild(new HSeparator());
        var oreRow = new HBoxContainer();
        oreRow.AddThemeConstantOverride("separation", 8);
        _oreLabel = new Label
        {
            Name = "OreCountLabel",
            Text = $"{ForgeService.OreName}: {ForgeService.OreCount(save)}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        UiTheme.StyleBody(_oreLabel, UiTheme.FontButton);
        oreRow.AddChild(_oreLabel);
        var oreBtn = new Button
        {
            Name = "OreBuyButton",
            Text = $"Mua +10 ({10 * ForgeService.OrePriceGold} Vàng)",
            CustomMinimumSize = new Vector2(220, 44),
            Disabled = save.Gold < 10 * ForgeService.OrePriceGold,
        };
        oreBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
        UiTheme.StyleButton(oreBtn, false);
        oreBtn.Pressed += () =>
        {
            var gm2 = GetNode<GameManager>("/root/GameManager");
            SetStatus(ForgeService.TryBuyOre(gm2.Save, 10) ? "Đã mua 10 Khoáng thạch!" : "Không đủ Vàng!");
            gm2.SaveGame();
            Refresh();
        };
        oreRow.AddChild(oreBtn);
        _box.AddChild(oreRow);

        _box.AddChild(new HSeparator());
        _box.AddChild(SectionLabel("Rèn trang bị:"));
        _box.AddChild(SmallLabel("Chọn bộ:"));
        var setRow = new HBoxContainer { Name = "ForgeSetRow" };
        setRow.AddThemeConstantOverride("separation", 8);
        foreach (var setId in ForgeService.BlueprintPrices.Keys)
        {
            bool learned = ForgeService.HasBlueprint(save, setId);
            var setBtn = new Button
            {
                Name = $"ForgeSet_{setId}",
                Text = EquipmentSets.Get(setId).Name,
                ToggleMode = true,
                ButtonPressed = setId == _selectedSet,
                Disabled = !learned,
                CustomMinimumSize = new Vector2(0, 44),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            UiTheme.StyleButton(setBtn, setId == _selectedSet);
            setBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
            var sid = setId;
            setBtn.Pressed += () => { _selectedSet = sid; Refresh(); };
            setRow.AddChild(setBtn);
        }
        _box.AddChild(setRow);

        _box.AddChild(SmallLabel("Chọn ô:"));
        var slotGrid = new GridContainer { Name = "ForgeSlotGrid", Columns = 3 };
        slotGrid.AddThemeConstantOverride("h_separation", 8);
        slotGrid.AddThemeConstantOverride("v_separation", 8);
        foreach (var slot in ProgressionService.Slots)
        {
            var slotBtn = new Button
            {
                Name = $"ForgeSlot_{slot.Id}",
                Text = slot.Name,
                ToggleMode = true,
                ButtonPressed = slot.Id == _selectedSlot,
                CustomMinimumSize = new Vector2(0, 44),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            UiTheme.StyleButton(slotBtn, slot.Id == _selectedSlot);
            slotBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
            var sl = slot.Id;
            slotBtn.Pressed += () => { _selectedSlot = sl; Refresh(); };
            slotGrid.AddChild(slotBtn);
        }
        _box.AddChild(slotGrid);

        var selSet = EquipmentSets.Get(_selectedSet);
        int mainLevel = ProgressionService.ProgressOf(save, "custom_hero").Level;
        _infoLabel = new Label
        {
            Name = "ForgeInfo",
            Text = $"Rèn: {selSet.Name} — {ProgressionService.Slots.Find(s => s.Id == _selectedSlot)?.Name}\n" +
                $"Cần: {ForgeService.OrePerForge(_selectedSet)} {ForgeService.OreName} · Yêu cầu cấp tướng chính {selSet.Tier} (hiện {mainLevel})",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        UiTheme.StyleBody(_infoLabel, UiTheme.FontSmall);
        _box.AddChild(_infoLabel);
        var forgeBtn = new Button
        {
            Name = "ForgeButton",
            Text = "Rèn",
            CustomMinimumSize = new Vector2(0, UiTheme.ButtonHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        UiTheme.StyleButton(forgeBtn, true);
        forgeBtn.Pressed += OnForgePressed;
        _box.AddChild(forgeBtn);
        _statusLabel = new Label { Name = "ForgeStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart, Text = _lastStatus };
        UiTheme.StyleBody(_statusLabel, UiTheme.FontSmall);
        _statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
        _box.AddChild(_statusLabel);
    }

    private void OnForgePressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var slotName = ProgressionService.Slots.Find(s => s.Id == _selectedSlot)?.Name ?? _selectedSlot;
        string msg = ForgeService.TryForge(gm.Save, _selectedSet, _selectedSlot, _rng) switch
        {
            ForgeService.ForgeResult.Success => $"Rèn thành công: {slotName} {EquipmentSets.Get(_selectedSet).Name}! (vào Túi)",
            ForgeService.ForgeResult.NoBlueprint => "Chưa học bản vẽ của bộ này!",
            ForgeService.ForgeResult.LowLevel => $"Cấp tướng chính chưa đủ (cần Lv{EquipmentSets.Get(_selectedSet).Tier})!",
            ForgeService.ForgeResult.NoOre => $"Không đủ {ForgeService.OreName}!",
            _ => "Không rèn được!",
        };
        SetStatus(msg);
        gm.SaveGame();
        Refresh();
    }

    private static readonly System.Random _rng = new();
    private string _lastStatus = "";

    private void SetStatus(string msg)
    {
        _lastStatus = msg;
        if (_statusLabel != null) _statusLabel.Text = msg;
    }

    private static Label SectionLabel(string text)
    {
        var l = new Label { Text = text };
        UiTheme.StyleBody(l, UiTheme.FontButton);
        l.AddThemeColorOverride("font_color", UiTheme.Gold);
        return l;
    }

    private static Label SmallLabel(string text)
    {
        var l = new Label { Text = text };
        UiTheme.StyleBody(l, UiTheme.FontSmall);
        return l;
    }
}
