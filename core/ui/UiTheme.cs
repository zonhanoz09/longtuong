using Godot;

namespace LongTuong;

/// <summary>Shared UI/UX system for Phase 2.2: dark background, gold accent,
/// consistent title/body sizes, 56px-min buttons, top-right currency bar.</summary>
public static class UiTheme
{
    public static readonly Color Background = new(0.07f, 0.07f, 0.13f);
    public static readonly Color Panel = new(0.14f, 0.14f, 0.24f);
    public static readonly Color Gold = new(0.91f, 0.72f, 0.29f);
    public static readonly Color Text = new(0.95f, 0.95f, 0.96f);
    public static readonly Color Muted = new(0.72f, 0.72f, 0.78f);
    public static readonly Color PlayerBlue = new(0.20f, 0.60f, 1.00f);
    public static readonly Color EnemyRed = new(1.00f, 0.25f, 0.25f);

    public static void ApplyBackground(Control root)
    {
        var bg = new ColorRect { Color = Background, Name = "UiThemeBackground" };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(bg);
        root.MoveChild(bg, 0);
    }

    public static void StyleTitle(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeColorOverride("font_color", Gold);
    }

    public static void StyleBody(Label label, int size = 16)
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
        button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X, 56);
        button.AddThemeFontSizeOverride("font_size", 17);
        button.AddThemeColorOverride("font_color", primary ? new Color(0.10f, 0.08f, 0.02f) : Text);
        button.AddThemeStyleboxOverride("normal", Box(primary ? Gold : Panel));
        button.AddThemeStyleboxOverride("hover", Box(primary ? new Color(1.00f, 0.82f, 0.45f) : new Color(0.20f, 0.20f, 0.32f)));
        button.AddThemeStyleboxOverride("pressed", Box(primary ? new Color(0.75f, 0.58f, 0.20f) : new Color(0.10f, 0.10f, 0.18f)));
        button.AddThemeStyleboxOverride("disabled", Box(new Color(0.25f, 0.25f, 0.28f)));
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
