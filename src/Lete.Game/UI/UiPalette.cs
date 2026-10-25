using Godot;

namespace Lete.Game.UI;

/// <summary>
/// Paleta única da interface, identidade greco-romana de barro e ouro (GDD 10, ARTE.md 3). Todas as telas
/// leem daqui (por <see cref="UiStyle"/>), e a arte da UI em <c>art/ui/</c> é gerada com estas mesmas cores
/// por <c>tools/ui_arte.py</c>. Regras: nenhuma barra em vermelho; o perigo usa terracota, nunca vermelho forte
/// (a romã é o único, CLAUDE.md princípio 8); o azul-lápis é só da Lucidez e da água.
/// </summary>
[GlobalClass]
public partial class UiPalette : Resource
{
    [ExportGroup("Fundo e texto")]
    /// <summary>Azul-noite dos painéis e do fundo das faixas (semitransparente, ver <see cref="PanelAlpha"/>).</summary>
    [Export] public Color Night { get; set; } = new("#1E2430");
    [Export] public float PanelAlpha { get; set; } = 0.82f;

    /// <summary>Contorno e sombra do texto, para ler sobre qualquer cena.</summary>
    [Export] public Color Outline { get; set; } = new("#120E10");

    /// <summary>Texto principal e opção escolhida (marfim), opção disponível (marfim apagado), desativada (cinza-barro).</summary>
    [Export] public Color Ivory { get; set; } = new("#E9DFC7");
    [Export] public Color IvoryDim { get; set; } = new("#B9AE98");
    [Export] public Color ClayGray { get; set; } = new("#6E5E58");

    [ExportGroup("Barro e ouro")]
    /// <summary>Barro escuro (bordas e molduras) e terracota clara (destaques e perigo).</summary>
    [Export] public Color Clay { get; set; } = new("#7A3E2B");
    [Export] public Color Terracotta { get; set; } = new("#B8664A");

    /// <summary>Ouro envelhecido (filetes, ícones, títulos, resultado perfeito) e ouro escuro (sombra do filete).</summary>
    [Export] public Color Gold { get; set; } = new("#C9A24B");
    [Export] public Color GoldDark { get; set; } = new("#8C6B2A");

    /// <summary>Fundo da tabuinha de barro do menu de ações.</summary>
    [Export] public Color ClayTablet { get; set; } = new("#3A2219");

    [ExportGroup("Barras")]
    /// <summary>Azul-lápis: só Lucidez e água.</summary>
    [Export] public Color Lapis { get; set; } = new("#4E7F86");

    /// <summary>Firmeza da sombra (tom frio, nunca vermelho) e paz do Libertar (ouro claro, quente).</summary>
    [Export] public Color ShadeFill { get; set; } = new("#8C9BAD");
    [Export] public Color PeaceFill { get; set; } = new("#D8C48E");
}
