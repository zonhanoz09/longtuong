using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.5 hero list, laid out like the reference image: left hero list,
/// center hero detail (portrait + equipped slots + unequip-all + EXP + skills),
/// right detailed stats + bag grid. Interaction: DOUBLE-CLICK an equipped item
/// to unequip it, DOUBLE-CLICK a bag item to equip it (swap if occupied).
/// Single click only selects/highlights, never equips/unequips — so items use
/// GuiInput DoubleClick, not Button.Pressed, for the action.
/// </summary>
[GlobalClass]
public partial class HeroListScene : Control
{
    private string _selectedHeroId = "";
    private string _selectedSlot = "";
    private int _selectedBagIndex = -1;
    private readonly List<Button> _equippedButtons = new();
    private readonly List<Button> _bagButtons = new();

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
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

    private Label MakeLabel(string text, int size = 15)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        UiTheme.StyleBody(l, size);
        return l;
    }

    private void BuildAll()
    {
        // Deferred rebuilds can fire after the popup was closed; do nothing then.
        if (!IsInsideTree()) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        _equippedButtons.Clear();
        _bagButtons.Clear();
        var left = GetNode<VBoxContainer>("HeroListBox");
        var detail = GetNode<VBoxContainer>("DetailBox");
        var right = GetNode<VBoxContainer>("RightBox");
        ClearBox(left); ClearBox(detail); ClearBox(right);

        // Left: hero list.
        foreach (var hero in HeroDatabase.Heroes)
        {
            bool owned = gm.Save.OwnedHeroIds.Contains(hero.Id);
            var btn = new Button { Text = owned ? hero.Name : $"{hero.Name} (Khóa)", ButtonPressed = hero.Id == _selectedHeroId };
            UiTheme.StyleButton(btn, hero.Id == _selectedHeroId);
            var id = hero.Id;
            btn.Pressed += () => { _selectedHeroId = id; gm.SelectedHeroId = id; CallDeferred("BuildAll"); };
            left.AddChild(btn);
        }
        left.AddChild(MakeLabel($"Sở hữu: {gm.Save.OwnedHeroIds.Count}/{HeroDatabase.Heroes.Count} tướng", 14));

        // Center: detail for selected hero.
        var heroDef = HeroDatabase.Get(_selectedHeroId);
        var p = gm.Save.Heroes[_selectedHeroId];
        var eff = ProgressionService.GetEffectiveHero(gm.Save, _selectedHeroId);
        bool selectedOwned = gm.Save.OwnedHeroIds.Contains(_selectedHeroId);
        detail.AddChild(MakeLabel($"{heroDef.Name} — Cấp {p.Level} — Loại: {HeroDatabase.RoleName(heroDef.Role)}", 20));
        var portrait = new ColorRect { Color = new Color(0.20f, 0.35f, 0.60f), CustomMinimumSize = new Vector2(480, 160) };
        detail.AddChild(portrait);
        detail.AddChild(MakeLabel($"Chân dung: {heroDef.Name} (placeholder, chưa có sprite final)", 13));
        var equippedBox = new HBoxContainer();
        detail.AddChild(equippedBox);
        foreach (var slot in ProgressionService.Slots)
        {
            var eq = gm.Save.HeroEquipment[_selectedHeroId][slot.Id];
            var slotBtn = new Button
            {
                Text = eq.IsEmpty ? $"{slot.Name}\n(Trống)" : $"{slot.Name}\nCấp {eq.Level} — {ProgressionService.RefineNames[eq.RefineIndex]}",
                Disabled = !selectedOwned,
                CustomMinimumSize = new Vector2(115, 56),
            };
            UiTheme.StyleButton(slotBtn, slot.Id == _selectedSlot);
            var slotId = slot.Id;
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
                        foreach (var b in _equippedButtons) b.Modulate = Colors.White;
                        slotBtn.Modulate = UiTheme.Gold;
                    }
                }
            };
            _equippedButtons.Add(slotBtn);
            equippedBox.AddChild(slotBtn);
        }
        var unequipAll = new Button { Text = "Tháo tất cả trang bị", Disabled = !selectedOwned };
        UiTheme.StyleButton(unequipAll);
        unequipAll.Pressed += () => { if (EquipmentService.UnequipAll(gm.Save, _selectedHeroId) >= 0) { gm.SaveGame(); CallDeferred("BuildAll"); } };
        detail.AddChild(unequipAll);
        detail.AddChild(MakeLabel($"EXP: {p.Exp}/{ProgressionService.ExpToNext(p.Level)} — Sao: {p.Stars}/5 — Mảnh: {gm.Save.Shards[_selectedHeroId]}" + (selectedOwned ? "" : " — CHƯA SỞ HỮU (cần 10 mảnh để mở khóa)")));
        var expBar = new ProgressBar { MinValue = 0, MaxValue = p.Level >= ProgressionService.MaxLevel ? 1 : ProgressionService.ExpToNext(p.Level), Value = p.Level >= ProgressionService.MaxLevel ? 1 : p.Exp, CustomMinimumSize = new Vector2(480, 20) };
        detail.AddChild(expBar);
        detail.AddChild(MakeLabel($"Đánh thường: {heroDef.NormalName} | Tuyệt kỹ: {heroDef.UltName} | Bị động: {heroDef.PassiveName} — {heroDef.PassiveDesc}"));
        if (selectedOwned)
        {
            var id = _selectedHeroId;
            var openBtn = new Button { Text = "Mở trang bị & cường hóa cho tướng này" };
            UiTheme.StyleButton(openBtn, true);
            openBtn.Pressed += () =>
            {
                gm.SelectedHeroId = id;
                var hub = UiTheme.FindHubScene(this);
                if (hub != null && hub.IsPopupOpen) hub.OpenPopup("res://features/hero/scenes/HeroScreen.tscn", HubScene.HeroPopupSize);
                else GetTree().ChangeSceneToFile("res://features/hero/scenes/HeroScreen.tscn");
            };
            detail.AddChild(openBtn);
        }

        // Right: detailed stats + bag grid (double-click to equip).
        int power = eff.Hp + eff.Atk * 5;
        right.AddChild(MakeLabel($"Chỉ số hiệu lực — Lực chiến ~{power}", 17));
        right.AddChild(MakeLabel($"HP {eff.Hp} / ATK {eff.Atk} / DEF {eff.Def} / Tốc {eff.Speed} / Bạo {eff.Crit:P0} / Né {eff.Dodge:P0}\nTướng Tinh: {gm.Save.StarNodesUnlocked}/12 ô"));
        right.AddChild(MakeLabel("Trang bị có thể dùng (click đúp để mang):", 17));
        for (int i = 0; i < gm.Save.BagEquipment.Count; i++)
        {
            var item = gm.Save.BagEquipment[i];
            var slotDef = ProgressionService.Slots.Find(s => s.Id == item.SlotId);
            var bagBtn = new Button { Text = $"{slotDef?.Name ?? item.SlotId} — Cấp {item.Level} — {ProgressionService.RefineNames[item.RefineIndex]}", Disabled = !selectedOwned };
            UiTheme.StyleButton(bagBtn, i == _selectedBagIndex);
            var bagIndex = i;
            bagBtn.GuiInput += (InputEvent e) =>
            {
                if (e is InputEventMouseButton mb && mb.Pressed)
                {
                    if (mb.DoubleClick)
                    {
                        if (selectedOwned && EquipmentService.EquipFromBag(gm.Save, _selectedHeroId, bagIndex)) { gm.SaveGame(); CallDeferred("BuildAll"); }
                    }
                    else
                    {
                        _selectedBagIndex = bagIndex;
                        foreach (var b in _bagButtons) b.Modulate = Colors.White;
                        bagBtn.Modulate = UiTheme.Gold;
                    }
                }
            };
            _bagButtons.Add(bagBtn);
            right.AddChild(bagBtn);
        }
        if (gm.Save.BagEquipment.Count == 0) right.AddChild(MakeLabel("Túi trang bị trống.", 14));
        right.AddChild(MakeLabel("Click đúp món đang mang để tháo, click đúp món trong túi để mang. Click đơn chỉ chọn.", 13));
    }
}
