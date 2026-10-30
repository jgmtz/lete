using Godot;

namespace Lete.Game.UI;

/// <summary>
/// Uma aba do menu do jogo (<see cref="GameMenu"/>). Cada aba tem um atalho próprio no Input Map que abre o menu
/// direto nela; dentro do menu, as abas se alternam entre si.
/// </summary>
public interface IMenuTab
{
    /// <summary>Chave de localização do nome da aba.</summary>
    string TitleKey { get; }

    /// <summary>Ação do Input Map que abre o menu nesta aba (ex.: <c>journal</c>, <c>inventory</c>).</summary>
    string ShortcutAction { get; }

    Control View { get; }

    /// <summary>Redesenha com o estado atual (chamado ao mostrar a aba e ao trocar textos ou o estado).</summary>
    void Refresh();
}
