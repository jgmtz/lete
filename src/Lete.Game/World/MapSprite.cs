using Godot;

namespace Lete.Game.World;

/// <summary>
/// Sprite parado de mapa (uma sombra com quem se conversa, por exemplo): de frente para a câmera e iluminado como o
/// jogador (mesmos ajustes do Sprite3D dele), com os pés na origem do nó e a mesma densidade de pixel dos personagens
/// (o <c>Main</c> ajusta o grupo <see cref="Group"/>).
/// </summary>
public partial class MapSprite : Sprite3D
{
    public const string Group = "map_sprite";

    public override void _Ready()
    {
        AddToGroup(Group);
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        Shaded = true;
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        AlphaCut = AlphaCutMode.Discard;
        if (Texture is { } texture)
        {
            Offset = new Vector2(0, texture.GetHeight() / 2f);
        }
    }
}
