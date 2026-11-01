using Godot;

namespace LongTuong;

/// <summary>Shared icon fallbacks and portrait presentation. PNGs are optional.</summary>
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
        ApplyPortrait(panel, heroId, label);
        return panel;
    }

    /// <summary>Fits a portrait without changing the host's minimum size or layout.</summary>
    public static bool ApplyPortrait(Control host, string heroId, Label? fallback = null, int bottomInset = 0, int padding = 4)
    {
        var texture = AssetCatalog.Portrait(heroId);
        var view = host.GetNodeOrNull<Control>("AssetPortrait");
        if (view == null && texture != null)
        {
            view = new Control { Name = "AssetPortrait", MouseFilter = Control.MouseFilterEnum.Ignore, ClipContents = true };
            host.AddChild(view);
            host.MoveChild(view, 0);
            view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            view.OffsetBottom = -bottomInset;
            var image = new TextureRect
            {
                Name = "Image", MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            };
            view.AddChild(image);
            image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            image.OffsetLeft = image.OffsetTop = padding;
            image.OffsetRight = image.OffsetBottom = -padding;
        }
        if (view != null)
        {
            view.GetNode<TextureRect>("Image").Texture = texture;
            view.Visible = texture != null;
        }
        if (fallback != null) fallback.Visible = texture == null;
        return texture != null;
    }
}
