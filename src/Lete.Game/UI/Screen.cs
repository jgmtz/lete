using Godot;

namespace Lete.Game.UI;

/// <summary>
/// Tamanho lógico da tela. O projeto usa <c>canvas_items</c> com proporção <c>expand</c>: a base é 1920×1080 e,
/// em telas mais estreitas (16:10, 4:3), a altura cresce em vez de aparecer faixa preta. Por isso o HUD usa
/// frações da tela (âncoras) e as câmeras usam esta altura, não a do projeto.
/// </summary>
public static class Screen
{
    public static Vector2 LogicalSize(Node node) => node.GetTree().Root.GetVisibleRect().Size;

    public static int LogicalHeight(Node node) => Mathf.RoundToInt(LogicalSize(node).Y);
}
