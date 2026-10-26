using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class FormationScene : Control
{
    private readonly List<string> _selected = new();
    private Label? _infoLabel;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        _selected.AddRange(gm.SelectedTeamIds);
        _infoLabel = GetNode<Label>("InfoLabel");
        var box = GetNode<VBoxContainer>("HeroBox");
        foreach (var hero in HeroDatabase.Heroes)
        {
            var eff = ProgressionService.GetEffectiveHero(gm.Save, hero.Id);
            var btn = new Button { Text = $"{hero.Name} — Cấp {gm.Save.Heroes[hero.Id].Level} — Sao {gm.Save.Heroes[hero.Id].Stars} — Lực chiến ~{eff.Hp + eff.Atk * 5}", ToggleMode = true, ButtonPressed = _selected.Contains(hero.Id) };
            var id = hero.Id;
            btn.Pressed += () =>
            {
                if (btn.ButtonPressed)
                {
                    if (_selected.Count >= 3) { btn.ButtonPressed = false; Refresh("Chỉ chọn tối đa 3 tướng!"); return; }
                    _selected.Add(id);
                }
                else _selected.Remove(id);
                Refresh();
            };
            box.AddChild(btn);
        }
        Refresh();
    }

    private void Refresh(string? note = null)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        int power = _selected.Sum(id => { var e = ProgressionService.GetEffectiveHero(gm.Save, id); return e.Hp + e.Atk * 5; });
        var names = _selected.Select(id => HeroDatabase.Get(id).Name).ToList();
        if (_infoLabel != null)
            _infoLabel.Text = $"Đội hình ({_selected.Count}/3): {string.Join(", ", names)} — Tổng lực chiến ~{power}{(note != null ? $" | {note}" : "")}";
    }

    private void OnSavePressed()
    {
        if (_selected.Count != 3) { Refresh("Phải chọn đúng 3 tướng!"); return; }
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.SelectedTeamIds = new List<string>(_selected);
        gm.Save.TeamIds = new List<string>(_selected);
        gm.SaveGame();
        Refresh("Đã lưu đội hình!");
    }

    private void OnBackPressed() => GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
}
