using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

// Drag source for the owned-hero list: dragging starts with "heroId|" data.
public partial class FormationDragButton : Button
{
    public string HeroId { get; set; } = "";

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (string.IsNullOrEmpty(HeroId)) return default;
        var preview = new Label { Text = HeroDatabase.Get(HeroId).Name };
        preview.AddThemeFontSizeOverride("font_size", 17);
        preview.AddThemeColorOverride("font_color", UiTheme.Gold);
        SetDragPreview(preview);
        return $"{HeroId}|";
    }
}

// A formation slot: a round (96x96, radius 48) button. Dragging a filled slot
// starts with "heroId|slotId" data; dropping onto it calls OnSlotDrop.
public partial class FormationSlotButton : Button
{
    public string SlotId { get; set; } = "";
    public string HeroId { get; set; } = "";
    public System.Action<string, string?>? OnSlotDrop;

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (string.IsNullOrEmpty(HeroId)) return default;
        var preview = new Label { Text = HeroDatabase.Get(HeroId).Name };
        preview.AddThemeFontSizeOverride("font_size", 17);
        preview.AddThemeColorOverride("font_color", UiTheme.Gold);
        SetDragPreview(preview);
        return $"{HeroId}|{SlotId}";
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.VariantType == Variant.Type.String && data.AsString().Contains("|");
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var parts = data.AsString().Split('|');
        if (parts.Length < 2) return;
        string? from = string.IsNullOrEmpty(parts[1]) ? null : parts[1];
        OnSlotDrop?.Invoke(parts[0], from);
    }
}

// The owned-hero list area: dropping a slot hero here removes it from the
// formation (back to the list). Dropping a list hero here is a no-op.
public partial class FormationListBox : VBoxContainer
{
    public System.Action<string>? OnListDrop;

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.VariantType == Variant.Type.String && data.AsString().Contains("|");
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var parts = data.AsString().Split('|');
        if (parts.Length < 2) return;
        if (string.IsNullOrEmpty(parts[1])) return; // came from the list itself
        OnListDrop?.Invoke(parts[0]);
    }
}

/// <summary>
/// Phase 2.20 formation popup (900x540, inside the Hub popup host).
/// Left: owned heroes NOT yet deployed (deployed heroes do not show here).
/// Right: the 7-slot grid from the sketch (Hậu 3, Trung 3, Tiền 1), with the
/// column names underneath. Drag list -> empty slot to deploy, list -> filled
/// slot to swap (the occupant goes back to the list), slot -> slot to swap,
/// slot -> list or click a filled slot to remove (main hero excepted).
/// The main hero (tướng tự tạo) always deploys and can only sit in the Trung
/// column (top/middle/bottom). Every change saves immediately; there is no
/// Save button — close with X or ESC like the other Hub popups.
/// </summary>
[GlobalClass]
public partial class FormationScene : Control
{
    private FormationListBox? _listBox;
    private GridContainer? _grid;
    private Label? _countLabel;
    private Label? _noteLabel;
    private readonly Dictionary<string, FormationSlotButton> _slots = new();

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        FormationService.EnsureFormation(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        UiTheme.StyleBody(GetNode<Label>("ListHeaderLabel"), 17);
        UiTheme.StyleBody(GetNode<Label>("CountLabel"), 17);
        foreach (var label in GetNode<HBoxContainer>("ColumnLabels").GetChildren().OfType<Label>())
            UiTheme.StyleBody(label, 16);
        _countLabel = GetNode<Label>("CountLabel");
        _noteLabel = GetNode<Label>("NoteLabel");
        UiTheme.StyleBody(_noteLabel, 15);
        _grid = GetNode<GridContainer>("SlotGrid");

        _listBox = new FormationListBox
        {
            Name = "ListBox",
            CustomMinimumSize = new Vector2(220, 400),
            SizeFlagsHorizontal = SizeFlags.Fill,
        };
        _listBox.OnListDrop = heroId => TryRemoveHero(heroId);
        GetNode<ScrollContainer>("ListScroll").AddChild(_listBox);

        foreach (var cell in FormationService.GridOrder)
        {
            if (cell == null)
            {
                _grid.AddChild(new Control { CustomMinimumSize = new Vector2(UiTheme.FormationSlotSize, UiTheme.FormationSlotSize) });
                continue;
            }
            var slotId = cell;
            var btn = new FormationSlotButton
            {
                SlotId = slotId,
                CustomMinimumSize = new Vector2(UiTheme.FormationSlotSize, UiTheme.FormationSlotSize),
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.Fill,
            };
            btn.OnSlotDrop = (heroId, fromSlot) => TryPlaceHero(heroId, slotId, fromSlot);
            btn.Pressed += () => OnSlotClicked(slotId);
            _slots[slotId] = btn;
            _grid.AddChild(btn);
        }
        Refresh();
        SetNote("Kéo tướng từ danh sách vào ô. Main luôn ở cột Trung.");
    }

    private void SetNote(string text)
    {
        if (_noteLabel != null) _noteLabel.Text = text;
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var form = gm.Save.Formation ?? new Dictionary<string, string>();
        if (_countLabel != null)
            _countLabel.Text = $"Số tướng ra trận {form.Count}/5";
        if (_listBox != null)
        {
            foreach (var child in _listBox.GetChildren()) child.QueueFree();
            foreach (var heroId in gm.Save.OwnedHeroIds)
            {
                if (form.ContainsValue(heroId)) continue; // deployed: not in list
                var hero = HeroDatabase.Get(heroId);
                var p = gm.Save.Heroes[heroId];
                var btn = new FormationDragButton
                {
                    HeroId = heroId,
                    Text = $"{hero.Name} — Cấp {p.Level}",
                    SizeFlagsHorizontal = SizeFlags.Fill,
                };
                UiTheme.StyleButton(btn);
                _listBox.AddChild(btn);
            }
        }
        foreach (var (slotId, btn) in _slots)
        {
            form.TryGetValue(slotId, out var heroId);
            btn.HeroId = heroId ?? "";
            btn.Text = string.IsNullOrEmpty(heroId) ? "" : HeroDatabase.Get(heroId).Name;
            btn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmall);
            var box = new StyleBoxFlat
            {
                CornerRadiusTopLeft = 48, CornerRadiusTopRight = 48,
                CornerRadiusBottomLeft = 48, CornerRadiusBottomRight = 48,
            };
            if (string.IsNullOrEmpty(heroId))
            {
                box.BgColor = UiTheme.SlotEmpty;
                box.BorderColor = UiTheme.Muted;
                box.BorderWidthLeft = box.BorderWidthRight = box.BorderWidthTop = box.BorderWidthBottom = 1;
                btn.AddThemeColorOverride("font_color", UiTheme.Muted);
            }
            else if (FormationService.IsMain(heroId))
            {
                box.BgColor = UiTheme.Gold;
                btn.AddThemeColorOverride("font_color", UiTheme.DarkText);
            }
            else
            {
                // Phase 2.27: filled slots get a tier-colored ring.
                box.BgColor = UiTheme.Panel;
                var tier = TavernData.TierOf(heroId);
                box.BorderColor = tier == null ? UiTheme.Gold : UiTheme.TierColor(tier.Value);
                box.BorderWidthLeft = box.BorderWidthRight = box.BorderWidthTop = box.BorderWidthBottom = 2;
                btn.AddThemeColorOverride("font_color", UiTheme.Text);
            }
            btn.AddThemeStyleboxOverride("normal", box);
            btn.AddThemeStyleboxOverride("hover", box);
            btn.AddThemeStyleboxOverride("pressed", box);
        }
    }

    // Shared by drag drops and by tests (which drive the same logic directly).
    public bool TryPlaceHero(string heroId, string slotId, string? fromSlot)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!FormationService.TryPlace(gm.Save, heroId, slotId, fromSlot, out var error))
        {
            SetNote(error);
            return false;
        }
        gm.SelectedTeamIds = new List<string>(gm.Save.TeamIds);
        gm.SaveGame();
        Refresh();
        SetNote("Đã lưu đội hình.");
        return true;
    }

    public bool TryRemoveHero(string heroId)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!FormationService.TryRemove(gm.Save, heroId, out var error))
        {
            SetNote(error);
            return false;
        }
        gm.SelectedTeamIds = new List<string>(gm.Save.TeamIds);
        gm.SaveGame();
        Refresh();
        SetNote("Đã lưu đội hình.");
        return true;
    }

    private void OnSlotClicked(string slotId)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var form = gm.Save.Formation ?? new Dictionary<string, string>();
        if (!form.TryGetValue(slotId, out var heroId) || string.IsNullOrEmpty(heroId)) return;
        TryRemoveHero(heroId);
    }
}
