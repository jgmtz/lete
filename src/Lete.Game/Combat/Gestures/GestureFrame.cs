using Godot;
using Lete.Game.UI;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Moldura comum dos quadros de gesto (Esquiva, Segurar, Arrastar): fundo azul-noite translúcido, filete de ouro
/// e um quadrado de meandro em cada canto (<c>art/ui/canto_meandro.png</c>), na identidade de barro e ouro.
/// </summary>
public static class GestureFrame
{
    private const string CornerPath = "res://art/ui/canto_meandro.png";

    private static Texture2D? _corner;

    private static Texture2D Corner => _corner ??= GD.Load<Texture2D>(CornerPath);

    public static void Draw(CanvasItem canvas, Rect2 box, float backgroundAlpha)
    {
        var palette = UiStyle.Palette;
        canvas.DrawRect(box, new Color(palette.Night, backgroundAlpha));
        var border = box.Grow(1f);
        canvas.DrawRect(border, palette.Gold, filled: false);

        // Cantos de meandro espelhados para cada lado, por cima do filete.
        var size = Corner.GetSize();
        var left = border.Position.X - 1f;
        var top = border.Position.Y - 1f;
        var right = border.End.X - size.X + 1f;
        var bottom = border.End.Y - size.Y + 1f;
        canvas.DrawTextureRect(Corner, new Rect2(left, top, size.X, size.Y), tile: false);
        canvas.DrawTextureRect(Corner, new Rect2(right + size.X, top, -size.X, size.Y), tile: false);
        canvas.DrawTextureRect(Corner, new Rect2(left, bottom + size.Y, size.X, -size.Y), tile: false);
        canvas.DrawTextureRect(Corner, new Rect2(right + size.X, bottom + size.Y, -size.X, -size.Y), tile: false);
    }
}
