using Godot;

namespace Lete.Game.Combat;

/// <summary>
/// Barra fina greco-romana, sem número: tampas de meandro nas pontas, moldura em ouro apagado,
/// fundo carvão e preenchimento da cor pedida, sempre em pixels inteiros.
/// </summary>
public partial class PixelBar : Control
{
    private static readonly Texture2D Cap = GD.Load<Texture2D>("res://art/ui/barra_tampa.png");

    private float _value = 1f;

    public BattleHudStyle Style { get; set; } = null!;
    public Color Fill { get; set; } = Colors.White;

    /// <summary>Fração de 0 a 1.</summary>
    public float Value
    {
        get => _value;
        set
        {
            _value = Mathf.Clamp(value, 0f, 1f);
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        var height = Mathf.Max(Cap.GetHeight(), Style.BarSize.Y + 2);
        CustomMinimumSize = new Vector2(Style.BarSize.X + 2 + Cap.GetWidth() * 2, height);
    }

    public void TweenTo(float value) =>
        CreateTween().TweenMethod(Callable.From<float>(v => Value = v), _value, Mathf.Clamp(value, 0f, 1f), Style.BarTweenSeconds);

    public override void _Draw()
    {
        var capW = Cap.GetWidth();
        var top = (Cap.GetHeight() - (Style.BarSize.Y + 2)) / 2;
        var frame = new Rect2(capW, top, Style.BarSize.X + 2, Style.BarSize.Y + 2);

        DrawTextureRect(Cap, new Rect2(0, 0, capW, Cap.GetHeight()), false, Style.Palette.Gold);
        DrawTextureRect(Cap, new Rect2(frame.End.X + capW, 0, -capW, Cap.GetHeight()), false, Style.Palette.Gold);

        DrawRect(frame, Style.Palette.Gold);
        var inner = frame.Grow(-1);
        DrawRect(inner, Style.Palette.Night);
        var filled = Mathf.Round(inner.Size.X * _value);
        if (filled > 0)
        {
            DrawRect(new Rect2(inner.Position, new Vector2(filled, inner.Size.Y)), Fill);
        }
    }
}
