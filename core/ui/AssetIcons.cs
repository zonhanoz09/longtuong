using Godot;

namespace LongTuong;

/// <summary>
/// Phase 2.27: procedural "assets" (Huynh chose option 1c — no real art
/// files, improve placeholders by code). Everything here is drawn with
/// panels, borders and representative characters, so the game never
/// depends on image files and headless tests keep passing.
/// - SlotSymbol: 2-letter abbreviation per equipment slot.
/// - PortraitChar: the hero's given name (last word of the Vietnamese
///   name; custom hero = first character of the username, uppercased).
/// - MakeCoinIcon: gold/silver coin (circle + "V"/"B" letter).
/// - MakePortrait: SQUARE portrait (Huynh chose square) — tier-colored
///   border + big given-name character, gold for the tierless custom hero.
/// </summary>
public static class AssetIcons
{
    public static string SlotSymbol(string slotId) => slotId switch
    {
        "weapon" => "VK",
        "armor" => "GI",
        "helmet" => "MŨ",
        "boots" => "GY",
        "cloak" => "CH",
        "bracer" => "UY",
        _ => "?",
    };

    public static string PortraitChar(string heroId)
    {
        var name = HeroDatabase.Get(heroId).Name;
        if (string.IsNullOrEmpty(name)) return "?";
        if (heroId == "custom_hero") return name.Substring(0, 1).ToUpper();
        var parts = name.Split(' ');
        return parts[parts.Length - 1];
    }

    public static Color PortraitBorderColor(string heroId)
    {
        var tier = TavernData.TierOf(heroId);
        return tier == null ? UiTheme.Gold : UiTheme.TierColor(tier.Value);
    }

    public static PanelContainer MakeCoinIcon(bool gold, int size = 24)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(size, size) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = gold ? UiTheme.Gold : UiTheme.SilverCoin,
            CornerRadiusTopLeft = size / 2, CornerRadiusTopRight = size / 2,
            CornerRadiusBottomLeft = size / 2, CornerRadiusBottomRight = size / 2,
            ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0,
        });
        var label = new Label
        {
            Text = gold ? "V" : "B",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", size - 10);
        label.AddThemeColorOverride("font_color", UiTheme.DarkText);
        panel.AddChild(label);
        return panel;
    }

    public static PanelContainer MakePortrait(string heroId, Vector2 size)
    {
        var color = PortraitBorderColor(heroId);
        var panel = new PanelContainer { CustomMinimumSize = size };
        panel.SetSize(size);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UiTheme.Panel,
            BorderColor = color,
            BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0,
        });
        var label = new Label
        {
            Name = "PortraitCharLabel",
            Text = PortraitChar(heroId),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", (int)(size.Y / 3));
        label.AddThemeColorOverride("font_color", color);
        panel.AddChild(label);
        return panel;
    }
}
