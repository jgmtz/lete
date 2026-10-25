using Godot;
using Lete.Game.UI;

namespace Lete.Game.Combat;

/// <summary>
/// Fonte e medidas do HUD de combate em pixel art (as cores vêm da paleta central, <see cref="UiPalette"/>). Medidas em pixels da arte (o HUD fica dentro do
/// viewport de baixa resolução) ou em frações da tela. Editável no Inspector, sem números no código.
/// Regra da paleta: nenhuma barra em vermelho (a romã é o único vermelho forte, ARTE.md 3).
/// </summary>
[GlobalClass]
public partial class BattleHudStyle : Resource
{
    /// <summary>Cores: sempre da paleta central (barro e ouro), nunca soltas aqui.</summary>
    public UiPalette Palette => UiStyle.Palette;

    [ExportGroup("Barras")]
    /// <summary>Largura e altura interna da barra (sem as tampas), em pixels da arte.</summary>
    [Export] public Vector2I BarSize { get; set; } = new(58, 3);
    [Export] public float BarTweenSeconds { get; set; } = 0.35f;

    [ExportGroup("Animações")]
    /// <summary>Tempo do friso de meandro "se desenhando" quando o menu abre.</summary>
    [Export] public float MenuOpenSeconds { get; set; } = 0.3f;

    /// <summary>Abaixo desta fração o Fôlego entra em perigo: a barra pulsa em terracota (nunca vermelho).</summary>
    [Export] public float DangerFraction { get; set; } = 0.3f;
    [Export] public float DangerPulseSeconds { get; set; } = 0.8f;

    [ExportGroup("Posições")]
    /// <summary>Canto dos indicadores do protagonista, em frações da tela.</summary>
    [Export] public Vector2 HeroIndicatorsAt { get; set; } = new(0.03f, 0.04f);

    /// <summary>Faixa da legenda (x, y, largura, altura em frações da tela): no alto, sem cobrir os personagens.</summary>
    [Export] public Rect2 CaptionRect { get; set; } = new(0.27f, 0.025f, 0.36f, 0.21f);

    /// <summary>Opacidade do fundo da faixa da legenda.</summary>
    [Export] public float CaptionAlpha { get; set; } = 0.72f;

    /// <summary>Espaço entre o topo da cabeça da sombra e os indicadores dela, em pixels da arte.</summary>
    [Export] public int ShadeIndicatorGap { get; set; } = 3;

    /// <summary>Espaço entre as linhas de barra, entre o ícone e a barra, e entre a cabeça e o louro do turno (pixels da arte).</summary>
    [Export] public int BarRowGap { get; set; } = 2;
    [Export] public int IconGap { get; set; } = 2;
    [Export] public int LaurelGap { get; set; } = 2;

    /// <summary>Espaço entre as barras do protagonista e o selo do apoio do companheiro, em pixels da arte.</summary>
    [Export] public int SupportGap { get; set; } = 4;

    /// <summary>Espaço entre o retrato do personagem ativo e a caixa de ações, em pixels da arte.</summary>
    [Export] public int PortraitGap { get; set; } = 3;
}
