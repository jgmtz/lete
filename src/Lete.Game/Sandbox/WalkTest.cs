using Godot;
using Lete.Core.Presentation;
using Lete.Game.World;

namespace Lete.Game.Sandbox;

/// <summary>
/// Cena de teste do andar (M0_PRIMEIRO_TESTE.md): chão branco, câmera diagonal e Aulo/Ismena.
/// Aplica a densidade de pixel dos sprites de 56 px ao personagem e à câmera.
/// </summary>
public partial class WalkTest : Node3D
{
    [Export] public int SpriteHeightPx { get; set; } = 56;
    [Export] public float CharacterHeightMeters { get; set; } = 1.7f;

    /// <summary>Pixels de tela (em 1080p) por pixel de arte; inteiro para os pixels ficarem iguais.</summary>
    [Export] public int ScreenPixelsPerArtPixel { get; set; } = 3;

    public override void _Ready()
    {
        var viewportHeight = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
        var density = new PixelDensity(
            SpriteHeightPx, CharacterHeightMeters, SpriteHeightPx * ScreenPixelsPerArtPixel, viewportHeight);

        GetNode<WalkingCharacter>("Character").PixelSize = density.PixelSize;
        GetNode<CameraRig>("CameraRig").Camera.Size = density.CameraSize;

        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--demo") >= 0)
        {
            AddChild(new WalkDemo());
        }
    }
}
