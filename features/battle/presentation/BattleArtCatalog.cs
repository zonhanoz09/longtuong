using Godot;
using System.Collections.Generic;

namespace LongTuong;

public enum BattleWeapon { Sword, TwinSwords, Glaive, Spear, Halberd, TwinHalberds, Fan, Ribbon, Scroll, Lute, Knives }
public enum BattleMotif { Slash, Dragon, Roar, Thrust, Trigram, Petals, Cross, Tiger, Stone, Poison, Raid, Water, Fire, FireKnife, Charge }
public sealed record BattleArt(Color Accent, BattleWeapon Weapon, BattleMotif Motif, bool Ranged = false, bool Plumes = false);

// Presentation only. Art choices never determine damage, targets, or status effects.
public static class BattleArtCatalog
{
    private static readonly IReadOnlyDictionary<string, BattleArt> Heroes = new Dictionary<string, BattleArt>
    {
        ["custom_hero"] = new(UiTheme.Gold, BattleWeapon.Sword, BattleMotif.Slash),
        ["luu_bi"] = new(UiTheme.Jade.Lightened(0.25f), BattleWeapon.TwinSwords, BattleMotif.Cross),
        ["quan_vu"] = new(UiTheme.Jade, BattleWeapon.Glaive, BattleMotif.Dragon),
        ["truong_phi"] = new(UiTheme.Iron.Lightened(0.4f), BattleWeapon.Spear, BattleMotif.Roar),
        ["trieu_van"] = new(UiTheme.SilverCoin, BattleWeapon.Spear, BattleMotif.Thrust, Plumes: true),
        ["gia_cat_luong"] = new(UiTheme.GoldHover, BattleWeapon.Fan, BattleMotif.Trigram, true),
        ["dieu_thuyen"] = new(UiTheme.Petal, BattleWeapon.Ribbon, BattleMotif.Petals, true),
        ["lu_bo"] = new(UiTheme.EnemyRed, BattleWeapon.Halberd, BattleMotif.Cross, Plumes: true),
        ["ton_kien"] = new(UiTheme.GoldHover, BattleWeapon.Glaive, BattleMotif.Tiger),
        ["dien_vi"] = new(UiTheme.Wood.Lightened(0.35f), BattleWeapon.TwinHalberds, BattleMotif.Stone),
        ["gia_hu"] = new(UiTheme.SlotColors["cloak"].Lightened(0.2f), BattleWeapon.Scroll, BattleMotif.Poison, true),
        ["truong_lieu"] = new(UiTheme.SlotColors["bracer"], BattleWeapon.Halberd, BattleMotif.Raid),
        ["tu_thu"] = new(UiTheme.PlayerBlue.Lightened(0.3f), BattleWeapon.Scroll, BattleMotif.Water, true),
        ["chu_du"] = new(UiTheme.EnemyRed.Lightened(0.25f), BattleWeapon.Lute, BattleMotif.Fire, true),
        ["chuc_dung"] = new(UiTheme.SlotColors["boots"].Lightened(0.25f), BattleWeapon.Knives, BattleMotif.FireKnife, true),
        ["ma_sieu"] = new(UiTheme.Gold.Lightened(0.35f), BattleWeapon.Spear, BattleMotif.Charge, Plumes: true),
    };

    public static bool HasHero(string id) => Heroes.ContainsKey(id);
    public static BattleArt For(HeroDef hero, bool player)
    {
        if (Heroes.TryGetValue(hero.Id, out var art)) return art;
        var color = player ? UiTheme.Gold : UiTheme.EnemyRed;
        return hero.Role switch
        {
            HeroRole.Strategist => new(color, BattleWeapon.Fan, BattleMotif.Trigram, true),
            HeroRole.Support => new(color, BattleWeapon.Scroll, BattleMotif.Water, true),
            _ => new(color, BattleWeapon.Spear, BattleMotif.Thrust),
        };
    }
}
