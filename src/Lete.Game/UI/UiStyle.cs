using Godot;

namespace Lete.Game.UI;

/// <summary>Estilo provisório da interface do M0 (cores da paleta "cerâmica dos mortos").</summary>
public static class UiStyle
{
    public static readonly Color Bone = new("#D9CBB0");
    public static readonly Color Charcoal = new("#25222B");
    public static readonly Color LetheWater = new("#4E7F86");

    public const int BodyFontSize = 28;
    public const int TitleFontSize = 36;

    public static Label MakeLabel(string text, int fontSize = BodyFontSize)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Bone);
        label.AddThemeColorOverride("font_outline_color", Charcoal);
        label.AddThemeConstantOverride("outline_size", 8);
        return label;
    }

    public static StyleBoxFlat MakePanelBox(float alpha = 0.88f) => new()
    {
        BgColor = new Color(Charcoal, alpha),
        BorderColor = LetheWater,
        BorderWidthBottom = 2,
        BorderWidthTop = 2,
        BorderWidthLeft = 2,
        BorderWidthRight = 2,
        ContentMarginLeft = 24,
        ContentMarginRight = 24,
        ContentMarginTop = 16,
        ContentMarginBottom = 16,
    };

    public static PanelContainer MakePanel(float alpha = 0.88f)
    {
        // Tema próprio para botões e campos herdarem um tamanho de fonte legível em 1080p.
        var panel = new PanelContainer { Theme = new Theme { DefaultFontSize = BodyFontSize } };
        panel.AddThemeStyleboxOverride("panel", MakePanelBox(alpha));
        return panel;
    }
}
