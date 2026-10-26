using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.6 hero/equipment screen, fixed-size layout (popup 1180x660):
/// - Left (260px, scrollable): ALL equipment the user owns — bag items plus
///   items equipped on any hero. An item button shows ONLY slot name + level,
///   never stats. It can be equipped to ANY hero: double-click a bag item to
///   equip it to the currently selected hero, double-click an equipped item
///   to unequip it back to the bag.
/// - Center (500px): selected hero detail — hero selector row, portrait,
///   4 equipped slots (name only, no stats), unequip-all, EXP, skills.
/// - Right (340px): selected hero's detailed stats + a fixed tooltip panel.
/// Hovering any equipment item shows its full detail + stats in the tooltip;
/// single click only selects/highlights, never equips/unequips.
/// </summary>
[GlobalClass]
public partial class HeroListScene : Control
{
    private const string DefaultTooltip = "Hover vào trang bị để xem chi tiết.";
    private string _selectedHeroId = "";
    private string _selectedSlot = "";
    private string _lastSelectedDetail = DefaultTooltip;
    private Label? _tooltipLabel;
    private readonly List<Button> _ownedButtons = new();
    private readonly List<Button> _equippedButtons = new();

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        _tooltipLabel = GetNode<Label>("RightBox/TooltipPanel/TooltipLabel");
        _tooltipLabel.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        _tooltipLabel.CustomMinimumSize = new Vector2(320, 0);
        UiTheme.StyleBody(_tooltipLabel, 14);
        var tooltipPanel = GetNode<PanelContainer>("RightBox/TooltipPanel");
        tooltipPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UiTheme.Panel, BorderColor = UiTheme.Gold,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        });
        _selectedHeroId = gm.Save.OwnedHeroIds.Contains(gm.SelectedHeroId) ? gm.SelectedHeroId
            : gm.Save.OwnedHeroIds.Count > 0 ? gm.Save.OwnedHeroIds[0] : HeroDatabase.Heroes[0].Id;
        BuildAll();
    }

    private static void ClearBox(Node box)
    {
        foreach (var child in box.GetChildren())
        {
            box.RemoveChild(child);
            child.QueueFree();
        }
    }

    // Phase 2.6 layout fix: a Label with autowrap inside a VBoxContainer
    // collapses to width 1 and a huge wrapped height unless it is forced to
    // fill the column width. Always give labels the column width + Fill flag.
    private Label MakeLabel(string text, int size = 15, float width = 480)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.Fill, CustomMinimumSize = new Vector2(width, 0) };
        UiTheme.StyleBody(l, size);
        return l;
    }

    private void SetTooltip(string text)
    {
        if (_tooltipLabel != null) _tooltipLabel.Text = text;
    }

    private void BuildAll()
    {
        if (!IsInsideTree()) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        _ownedButtons.Clear();
        _equippedButtons.Clear();
        _lastSelectedDetail = DefaultTooltip;
        var ownedBox = GetNode<VBoxContainer>("OwnedScroll/OwnedListBox");
        var detail = GetNode<VBoxContainer>("DetailBox");
        var statsBox = GetNode<VBoxContainer>("RightBox/StatsBox");
        ClearBox(ownedBox); ClearBox(detail); ClearBox(statsBox);
        SetTooltip(DefaultTooltip);

        // Left: all owned equipment (bag + equipped on any hero), fixed 240x48 each.
        void AddOwnedButton(string text, string detail, System.Action onDoubleClick)
        {
            var btn = new Button { Text = text, CustomMinimumSize = new Vector2(240, 48) };
            UiTheme.StyleButton(btn);
            btn.CustomMinimumSize = new Vector2(240, 48);
            btn.MouseEntered += () => SetTooltip(detail);
            btn.MouseExited += () => SetTooltip(_lastSelectedDetail);
            btn.GuiInput += (InputEvent e) =>
            {
                if (e is InputEventMouseButton mb && mb.Pressed)
                {
                    if (mb.DoubleClick) onDoubleClick();
                    else
                    {
                        _lastSelectedDetail = detail;
                        foreach (var b in _ownedButtons) b.Modulate = Colors.White;
                        btn.Modulate = UiTheme.Gold;
                        SetTooltip(detail);
                    }
                }
            };
            _ownedButtons.Add(btn);
            ownedBox.AddChild(btn);
        }
        for (int i = 0; i < gm.Save.BagEquipment.Count; i++)
        {
            var item = gm.Save.BagEquipment[i];
            var slotDef = ProgressionService.Slots.Find(s => s.Id == item.SlotId);
            var bagIndex = i;
            AddOwnedButton($"{slotDef?.Name ?? item.SlotId} · Lv{item.Level} — Trong túi",
                EquipmentService.DetailText(item.SlotId, item.Level, item.RefineIndex, "Trạng thái: Trong túi"),
                () =>
                {
                    // Phase 2.6: bag items can be equipped to ANY hero, owned or locked.
                    if (EquipmentService.EquipFromBag(gm.Save, _selectedHeroId, bagIndex))
                    { gm.SaveGame(); CallDeferred("BuildAll"); }
                });
        }
        foreach (var hero in HeroDatabase.Heroes)
        {
            foreach (var slot in ProgressionService.Slots)
            {
                var eq = gm.Save.HeroEquipment[hero.Id][slot.Id];
                if (eq.IsEmpty) continue;
                var heroId = hero.Id; var slotId = slot.Id;
                AddOwnedButton($"{slot.Name} · Lv{eq.Level} — {hero.Name}",
                    EquipmentService.DetailText(slotId, eq.Level, eq.RefineIndex, $"Đang mang: {hero.Name}"),
                    () =>
                    {
                        if (EquipmentService.Unequip(gm.Save, heroId, slotId)) { gm.SaveGame(); CallDeferred("BuildAll"); }
                    });
            }
        }
        if (_ownedButtons.Count == 0) ownedBox.AddChild(MakeLabel("Chưa sở hữu trang bị nào.", 14, 240));

        // Center: selected hero detail, fixed sizes.
        var heroDef = HeroDatabase.Get(_selectedHeroId);
        var p = gm.Save.Heroes[_selectedHeroId];
        var eff = ProgressionService.GetEffectiveHero(gm.Save, _selectedHeroId);
        bool selectedOwned = gm.Save.OwnedHeroIds.Contains(_selectedHeroId);
        // Hero selector: 2 rows x 3 buttons — 6 full names cannot fit one
        // 500px row at a readable font size (Gia Cát Lượng alone needs 104px).
        var heroListAll = HeroDatabase.Heroes;
        for (int rowIdx = 0; rowIdx < 2; rowIdx++)
        {
            var selectorBox = new HBoxContainer();
            detail.AddChild(selectorBox);
            for (int colIdx = 0; colIdx < 3; colIdx++)
            {
                var hero = heroListAll[rowIdx * 3 + colIdx];
                var btn = new Button { Text = hero.Name, CustomMinimumSize = new Vector2(160, 48) };
                UiTheme.StyleButton(btn, hero.Id == _selectedHeroId);
                btn.CustomMinimumSize = new Vector2(160, 48);
                var id = hero.Id;
                btn.Pressed += () => { _selectedHeroId = id; gm.SelectedHeroId = id; CallDeferred("BuildAll"); };
                selectorBox.AddChild(btn);
            }
        }
        detail.AddChild(MakeLabel($"{heroDef.Name} — Cấp {p.Level} — Loại: {HeroDatabase.RoleName(heroDef.Role)}", 20));
        var portrait = new ColorRect { Color = new Color(0.20f, 0.35f, 0.60f), CustomMinimumSize = new Vector2(480, 110) };
        detail.AddChild(portrait);
        detail.AddChild(MakeLabel($"Chân dung: {heroDef.Name} (placeholder, chưa có sprite final)", 13, 480));
        var equippedBox = new HBoxContainer();
        detail.AddChild(equippedBox);
        foreach (var slot in ProgressionService.Slots)
        {
            var eq = gm.Save.HeroEquipment[_selectedHeroId][slot.Id];
            var slotBtn = new Button
            {
                Text = eq.IsEmpty ? $"{slot.Name}\n(Trống)" : $"{slot.Name}\nĐang mang",
                Disabled = !selectedOwned,
                CustomMinimumSize = new Vector2(115, 56),
            };
            UiTheme.StyleButton(slotBtn, slot.Id == _selectedSlot);
            slotBtn.CustomMinimumSize = new Vector2(115, 56);
            var slotId = slot.Id;
            string slotDetail = eq.IsEmpty ? "Ô trống — chưa mang trang bị."
                : EquipmentService.DetailText(slotId, eq.Level, eq.RefineIndex, $"Đang mang: {heroDef.Name}");
            slotBtn.MouseEntered += () => SetTooltip(slotDetail);
            slotBtn.MouseExited += () => SetTooltip(_lastSelectedDetail);
            slotBtn.GuiInput += (InputEvent e) =>
            {
                if (e is InputEventMouseButton mb && mb.Pressed)
                {
                    if (mb.DoubleClick)
                    {
                        if (selectedOwned && EquipmentService.Unequip(gm.Save, _selectedHeroId, slotId)) { gm.SaveGame(); CallDeferred("BuildAll"); }
                    }
                    else
                    {
                        _selectedSlot = slotId;
                        _lastSelectedDetail = slotDetail;
                        foreach (var b in _equippedButtons) b.Modulate = Colors.White;
                        slotBtn.Modulate = UiTheme.Gold;
                        SetTooltip(slotDetail);
                    }
                }
            };
            _equippedButtons.Add(slotBtn);
            equippedBox.AddChild(slotBtn);
        }
        var unequipAll = new Button { Text = "Tháo tất cả trang bị", Disabled = !selectedOwned, CustomMinimumSize = new Vector2(480, 56) };
        UiTheme.StyleButton(unequipAll);
        unequipAll.CustomMinimumSize = new Vector2(480, 56);
        unequipAll.Pressed += () => { if (EquipmentService.UnequipAll(gm.Save, _selectedHeroId) >= 0) { gm.SaveGame(); CallDeferred("BuildAll"); } };
        detail.AddChild(unequipAll);
        detail.AddChild(MakeLabel($"EXP: {p.Exp}/{ProgressionService.ExpToNext(p.Level)} — Sao: {p.Stars}/5 — Mảnh: {gm.Save.Shards[_selectedHeroId]}" + (selectedOwned ? "" : " — CHƯA SỞ HỮU (cần 10 mảnh để mở khóa)"), 15, 480));
        var expBar = new ProgressBar { MinValue = 0, MaxValue = p.Level >= ProgressionService.MaxLevel ? 1 : ProgressionService.ExpToNext(p.Level), Value = p.Level >= ProgressionService.MaxLevel ? 1 : p.Exp, CustomMinimumSize = new Vector2(480, 20) };
        detail.AddChild(expBar);
        detail.AddChild(MakeLabel($"Đánh thường: {heroDef.NormalName} | Tuyệt kỹ: {heroDef.UltName} | Bị động: {heroDef.PassiveName} — {heroDef.PassiveDesc}", 15, 480));
        if (selectedOwned)
        {
            var id = _selectedHeroId;
            var openBtn = new Button { Text = "Mở trang bị & cường hóa cho tướng này", CustomMinimumSize = new Vector2(480, 56) };
            UiTheme.StyleButton(openBtn, true);
            openBtn.CustomMinimumSize = new Vector2(480, 56);
            openBtn.Pressed += () =>
            {
                gm.SelectedHeroId = id;
                var hub = UiTheme.FindHubScene(this);
                if (hub != null && hub.IsPopupOpen) hub.OpenPopup("res://features/hero/scenes/HeroScreen.tscn", HubScene.HeroPopupSize);
                else GetTree().ChangeSceneToFile("res://features/hero/scenes/HeroScreen.tscn");
            };
            detail.AddChild(openBtn);
        }

        // Right: hero stats + hint (tooltip panel itself is static in the .tscn).
        int power = eff.Hp + eff.Atk * 5;
        statsBox.AddChild(MakeLabel($"Chỉ số hiệu lực — Lực chiến ~{power}", 17, 320));
        statsBox.AddChild(MakeLabel($"HP {eff.Hp} / ATK {eff.Atk} / DEF {eff.Def} / Tốc {eff.Speed} / Bạo {eff.Crit:P0} / Né {eff.Dodge:P0}\nTướng Tinh: {gm.Save.StarNodesUnlocked}/12 ô", 15, 320));
        statsBox.AddChild(MakeLabel("Trái: trang bị sở hữu (mang được cho mọi tướng). Click đúp món trong túi để mang cho tướng đang chọn, click đúp món đang mang để tháo. Hover để xem chi tiết.", 13, 320));
    }
}
