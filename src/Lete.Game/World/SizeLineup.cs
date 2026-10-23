using Godot;

namespace Lete.Game.World;

/// <summary>
/// Fileira do teste de tamanho: o mesmo personagem em 32, 48 e 64 px, lado a lado,
/// todos com a mesma altura no mundo. Cada um usa o próprio tamanho de pixel.
/// </summary>
public partial class SizeLineup : Node3D
{
    private const string SpritePathFormat = "res://art/sprites/placeholder/protagonista_frente_{0}px.png";

    [Export] public int[] Heights { get; set; } = { 32, 48, 64 };
    [Export] public float Spacing { get; set; } = 1.2f;
    [Export] public float CharacterHeightMeters { get; set; } = 1.75f;

    public override void _Ready()
    {
        for (var i = 0; i < Heights.Length; i++)
        {
            var heightPx = Heights[i];
            var texture = GD.Load<Texture2D>(string.Format(SpritePathFormat, heightPx));
            var position = new Vector3(i * Spacing, 0, 0);

            AddChild(new Sprite3D
            {
                Name = $"Sprite{heightPx}",
                Texture = texture,
                PixelSize = CharacterHeightMeters / heightPx,
                Offset = new Vector2(0, texture.GetHeight() / 2f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Shaded = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Position = position,
            });

            // Rótulo técnico do teste (só desenvolvimento, não é texto do jogo).
            AddChild(new Label3D
            {
                Text = $"{heightPx}",
                PixelSize = 0.01f,
                FontSize = 48,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true,
                Position = position + new Vector3(0.3f, 0.05f, 0.3f),
                Modulate = new Color(0.85f, 0.8f, 0.69f),
            });
        }
    }
}
