using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>Shared UI/UX system for Phase 2.2: dark background, gold accent,
/// consistent title/body sizes, 56px-min buttons, top-right currency bar.
/// Phase 2.27: the SINGLE source of truth for palette, font scale and
/// standard sizes — no scene may hardcode its own colors, font sizes
/// or icon/slot sizes anymore (see docs/KE_HOACH_CHUANHOA_ASSET.md).</summary>
public static class UiTheme
{
    public static readonly Color Background = new(0.07f, 0.07f, 0.13f);
    public static readonly Color Panel = new(0.14f, 0.14f, 0.24f);
    public static readonly Color Gold = new(0.91f, 0.72f, 0.29f);
    public static readonly Color Text = new(0.95f, 0.95f, 0.96f);
    public static readonly Color Muted = new(0.72f, 0.72f, 0.78f);
    public static readonly Color PlayerBlue = new(0.20f, 0.60f, 1.00f);
    public static readonly Color EnemyRed = new(1.00f, 0.25f, 0.25f);

    // --- Phase 2.27: centralized palette (moved from scene-local colors) ---
    public static readonly Color HpColor = new(0.25f, 0.75f, 0.30f);
    public static readonly Color BarBackground = new(0.15f, 0.15f, 0.20f);
    public static readonly Color SlotEmpty = new(0.12f, 0.12f, 0.16f);
    public static readonly Color GoldHover = new(1.00f, 0.82f, 0.45f);
    public static readonly Color GoldPressed = new(0.75f, 0.58f, 0.20f);
    public static readonly Color PanelHover = new(0.20f, 0.20f, 0.32f);
    public static readonly Color PanelPressed = new(0.10f, 0.10f, 0.18f);
    public static readonly Color DisabledBg = new(0.25f, 0.25f, 0.28f);
    public static readonly Color DangerHover = new(0.30f, 0.15f, 0.15f);
    public static readonly Color DeadGrey = new(0.5f, 0.5f, 0.5f);
    public static readonly Color SilverCoin = new(0.75f, 0.75f, 0.78f);
    public static readonly Color DarkText = new(0.10f, 0.08f, 0.02f);

    public static readonly Dictionary<string, Color> SlotColors = new()
    {
        ["weapon"] = new Color(0.65f, 0.25f, 0.20f),
        ["armor"] = new Color(0.20f, 0.35f, 0.60f),
        ["helmet"] = new Color(0.25f, 0.55f, 0.30f),
        ["boots"] = new Color(0.65f, 0.55f, 0.20f),
        ["cloak"] = new Color(0.50f, 0.30f, 0.75f),
        ["bracer"] = new Color(0.20f, 0.60f, 0.60f),
    };

    public static Color TierColor(TavernTier tier) => tier switch
    {
        TavernTier.Lam => new Color(0.15f, 0.55f, 1f),
        TavernTier.Tim => new Color(0.85f, 0.25f, 0.95f),
        TavernTier.Cam => new Color(1f, 0.6f, 0.1f),
        _ => new Color(0.90f, 0.15f, 0.15f),
    };

    // --- Phase 2.27: font scale ---
    public const int FontTitle = 28;
    public const int FontBody = 16;
    public const int FontSmall = 13;
    public const int FontButton = 17;
    // Small destructive buttons (sketch scale, e.g. Dismiss) — a
    // deliberate exception to FontButton, kept as a named constant.
    public const int FontSmallButton = 15;

    // --- Phase 2.27: standard sizes ---
    public const int ButtonHeight = 56;
    public const int PopupCloseSize = 40;
    public const int IconGridSize = 64;
    public const int IconEquippedSize = 56;
    public const int FormationSlotSize = 96;
    public const int TierDotSize = 20;
    public const int TabWidth = 160;
    public static readonly Vector2 HpBarSize = new(132, 14);
    public static readonly Vector2 RageBarSize = new(132, 10);

    public static void ApplyBackground(Control root)
    {
        var bg = new ColorRect { Color = Background, Name = "UiThemeBackground" };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(bg);
        root.MoveChild(bg, 0);
    }

    public static void StyleTitle(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", FontTitle);
        label.AddThemeColorOverride("font_color", Gold);
    }

    public static void StyleBody(Label label, int size = FontBody)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", Text);
    }

    private static StyleBoxFlat Box(Color color)
    {
        return new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 };
    }

    public static void StyleButton(Button button, bool primary = false)
    {
        button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X, ButtonHeight);
        button.AddThemeFontSizeOverride("font_size", FontButton);
        button.AddThemeColorOverride("font_color", primary ? DarkText : Text);
        button.AddThemeStyleboxOverride("normal", Box(primary ? Gold : Panel));
        button.AddThemeStyleboxOverride("hover", Box(primary ? GoldHover : PanelHover));
        button.AddThemeStyleboxOverride("pressed", Box(primary ? GoldPressed : PanelPressed));
        button.AddThemeStyleboxOverride("disabled", Box(DisabledBg));
        button.AddThemeColorOverride("font_disabled_color", Muted);
    }

    // True when a widget scene was opened inside the Hub popup host.
    public static bool IsInHubPopup(Control node) => node.GetParent()?.Name == "PopupContent";

    // Find the Hub that hosts this widget, via its parent chain first
    // (works even when Hub is not the current scene, e.g. in tests), else via CurrentScene.
    public static HubScene? FindHubScene(Control node)
    {
        Node? p = node.GetParent();
        while (p != null)
        {
            if (p is HubScene hub) return hub;
            p = p.GetParent();
        }
        return node.GetTree().CurrentScene as HubScene;
    }

    // Close the Hub popup if this widget is inside one. Returns true when it closed.
    public static bool TryCloseHubPopup(Control node)
    {
        var hub = FindHubScene(node);
        return hub != null && hub.ClosePopup();
    }

    public static Label AddCurrencyBar(Control root, GameManager gm)
    {
        var label = new Label
        {
            Text = $"Vàng: {gm.Save.Gold}  |  Bạc: {gm.Save.Silver}",
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        label.SetPosition(new Vector2(880, 10));
        label.SetSize(new Vector2(380, 30));
        StyleBody(label, 16);
        label.AddThemeColorOverride("font_color", Gold);
        root.AddChild(label);
        return label;
    }
}
