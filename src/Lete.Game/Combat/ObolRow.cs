using Godot;

namespace Lete.Game.Combat;

/// <summary>Compreensão da sombra como óbolos (moedas) em ouro apagado: vazios em contorno, cheios preenchidos.</summary>
public partial class ObolRow : HBoxContainer
{
    private static readonly Texture2D Empty = GD.Load<Texture2D>("res://art/ui/obolo_vazio.png");
    private static readonly Texture2D Full = GD.Load<Texture2D>("res://art/ui/obolo_cheio.png");

    public int Count { get; set; } = 3;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 1);
        for (var i = 0; i < Count; i++)
        {
            AddChild(new TextureRect { Texture = Empty, StretchMode = TextureRect.StretchModeEnum.KeepCentered });
        }
    }

    public void SetFilled(int filled)
    {
        for (var i = 0; i < GetChildCount(); i++)
        {
            GetChild<TextureRect>(i).Texture = i < filled ? Full : Empty;
        }
    }
}
