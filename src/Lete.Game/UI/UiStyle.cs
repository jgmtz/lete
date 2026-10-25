using Godot;

namespace Lete.Game.UI;

/// <summary>
/// Tema central da interface (identidade de barro e ouro, GDD 10). Monta o <see cref="Theme"/> do Godot a partir
/// de <see cref="UiPalette"/>: fonte pixel (Pixelify Sans) sem suavização, texto marfim com contorno escuro e
/// sombra de 1 pixel da arte (lê sobre qualquer cena), títulos em ouro e painéis com moldura dupla (barro por
/// fora, filete de ouro por dentro, cantos de meandro, fundo azul-noite translúcido).
/// Duas escalas: a batalha desenha a 1 pixel da arte (viewport de baixa resolução); a exploração, a 3× em 1080p.
/// </summary>
public static class UiStyle
{
    private const string PalettePath = "res://src/Lete.Game/UI/ui_paleta.tres";
    private const string FontPath = "res://art/fonts/PixelifySans.ttf";
    private const string FramePath = "res://art/ui/moldura.png";
    private const string FrameExplorationPath = "res://art/ui/moldura_3x.png";

    /// <summary>Tamanho da fonte pixel em 1 pixel da arte; a exploração usa este valor × <see cref="ExplorationScale"/>.</summary>
    public const int PixelFontSize = 10;
    public const int TitlePixelFontSize = 13;

    /// <summary>Quantos pixels de tela há em cada pixel da arte na exploração (decidido no M0, PENDENCIAS 13).</summary>
    public const int ExplorationScale = 3;

    /// <summary>Margem do 9-patch das molduras (em pixels da arte) e margem interna do conteúdo.</summary>
    public const int FrameMargin = 5;
    public const int ContentMarginX = 7;
    public const int ContentMarginY = 5;
    public const int ButtonPaddingX = 4;
    public const int ButtonPaddingY = 2;

    public const int BodyFontSize = PixelFontSize * ExplorationScale;
    public const int TitleFontSize = TitlePixelFontSize * ExplorationScale;

    private static UiPalette? _palette;
    private static FontFile? _font;

    public static UiPalette Palette => _palette ??= GD.Load<UiPalette>(PalettePath);

    /// <summary>Fonte pixel sem suavização nem posicionamento subpixel (pixels duros em qualquer escala inteira).</summary>
    public static FontFile PixelFont
    {
        get
        {
            if (_font is null)
            {
                _font = GD.Load<FontFile>(FontPath);
                _font.Antialiasing = TextServer.FontAntialiasing.None;
                _font.Hinting = TextServer.Hinting.None;
                _font.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
            }

            return _font;
        }
    }

    /// <summary>
    /// Tema para uma escala de pixel (1 na batalha, <see cref="ExplorationScale"/> na exploração): fonte, cores de
    /// texto, contorno, sombra, botões e a moldura dupla dos painéis.
    /// </summary>
    public static Theme BuildTheme(int scale)
    {
        var p = Palette;
        var theme = new Theme { DefaultFont = PixelFont, DefaultFontSize = PixelFontSize * scale };

        foreach (var type in new[] { "Label", "Button", "LineEdit", "CheckBox", "OptionButton" })
        {
            theme.SetColor("font_color", type, p.Ivory);
            theme.SetColor("font_outline_color", type, p.Outline);
            theme.SetConstant("outline_size", type, 2 * scale);
        }

        theme.SetColor("font_shadow_color", "Label", new Color(p.Outline, 0.7f));
        theme.SetConstant("shadow_offset_x", "Label", scale);
        theme.SetConstant("shadow_offset_y", "Label", scale);

        theme.SetColor("font_hover_color", "Button", p.Gold);
        theme.SetColor("font_focus_color", "Button", p.Gold);
        theme.SetColor("font_pressed_color", "Button", p.Gold);
        theme.SetColor("font_disabled_color", "Button", p.ClayGray);

        var frame = MakeFrame(scale);
        theme.SetStylebox("panel", "PanelContainer", frame);
        theme.SetStylebox("panel", "Panel", frame);
        foreach (var state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
        {
            theme.SetStylebox(state, "Button", MakeButtonBox(scale, state));
        }

        return theme;
    }

    /// <summary>Moldura dupla de barro e ouro com fundo azul-noite translúcido (a mesma arte em 1× ou 3×).</summary>
    public static StyleBoxTexture MakeFrame(int scale) => new()
    {
        Texture = GD.Load<Texture2D>(scale == 1 ? FramePath : FrameExplorationPath),
        TextureMarginLeft = FrameMargin * scale,
        TextureMarginTop = FrameMargin * scale,
        TextureMarginRight = FrameMargin * scale,
        TextureMarginBottom = FrameMargin * scale,
        ContentMarginLeft = ContentMarginX * scale,
        ContentMarginRight = ContentMarginX * scale,
        ContentMarginTop = ContentMarginY * scale,
        ContentMarginBottom = ContentMarginY * scale,
    };

    /// <summary>Botão: fundo azul-noite com filete de ouro (escuro parado, claro em foco), sem caixa preta lisa.</summary>
    private static StyleBoxFlat MakeButtonBox(int scale, string state)
    {
        var p = Palette;
        var highlighted = state is "hover" or "focus" or "pressed";
        return new StyleBoxFlat
        {
            BgColor = new Color(p.Night, p.PanelAlpha),
            BorderColor = state == "disabled" ? p.ClayGray : highlighted ? p.Gold : p.GoldDark,
            BorderWidthLeft = scale,
            BorderWidthTop = scale,
            BorderWidthRight = scale,
            BorderWidthBottom = scale,
            ContentMarginLeft = ButtonPaddingX * scale,
            ContentMarginRight = ButtonPaddingX * scale,
            ContentMarginTop = ButtonPaddingY * scale,
            ContentMarginBottom = ButtonPaddingY * scale,
        };
    }

    // ---- Atalhos da exploração (1080p, escala 3×) ----------------------------------------------------

    public static Label MakeLabel(string text, int fontSize = BodyFontSize)
    {
        var label = new Label { Text = text };
        label.AddThemeFontOverride("font", PixelFont);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", fontSize >= TitleFontSize ? Palette.Gold : Palette.Ivory);
        label.AddThemeColorOverride("font_outline_color", Palette.Outline);
        label.AddThemeConstantOverride("outline_size", 2 * ExplorationScale);
        label.AddThemeColorOverride("font_shadow_color", new Color(Palette.Outline, 0.7f));
        label.AddThemeConstantOverride("shadow_offset_x", ExplorationScale);
        label.AddThemeConstantOverride("shadow_offset_y", ExplorationScale);
        return label;
    }

    public static PanelContainer MakePanel()
    {
        var panel = new PanelContainer { Theme = BuildTheme(ExplorationScale) };
        panel.AddThemeStyleboxOverride("panel", MakeFrame(ExplorationScale));
        return panel;
    }
}
