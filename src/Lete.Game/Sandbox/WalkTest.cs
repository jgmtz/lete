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

    /// <summary>
    /// Pixels de tela (em 1080p) por pixel de arte; inteiro para os pixels ficarem iguais.
    /// 4× = 56 × 4 = 224 px de altura ≈ 20,7% de 1080 (câmera com size ≈ 8,2 m).
    /// </summary>
    [Export] public int ScreenPixelsPerArtPixel { get; set; } = 4;

    /// <summary>Escalas que as ações de debug <c>debug_zoom_in</c>/<c>debug_zoom_out</c> percorrem (+ e -).</summary>
    [Export] public int MinScale { get; set; } = 2;
    [Export] public int MaxScale { get; set; } = 4;

    public override void _Ready()
    {
        ApplyScale();

        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--demo") >= 0)
        {
            AddChild(new WalkDemo());
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        var step = @event.IsActionPressed("debug_zoom_in") ? 1 : @event.IsActionPressed("debug_zoom_out") ? -1 : 0;
        if (step != 0)
        {
            ScreenPixelsPerArtPixel = Mathf.Clamp(ScreenPixelsPerArtPixel + step, MinScale, MaxScale);
            ApplyScale();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>size da câmera = altura da tela ÷ (escala × pixels de arte por metro).</summary>
    private void ApplyScale()
    {
        var viewportHeight = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
        var density = new PixelDensity(
            SpriteHeightPx, CharacterHeightMeters, SpriteHeightPx * ScreenPixelsPerArtPixel, viewportHeight);

        GetNode<WalkingCharacter>("Character").PixelSize = density.PixelSize;
        GetNode<CameraRig>("CameraRig").Camera.Size = density.CameraSize;
        GD.Print($"Escala {ScreenPixelsPerArtPixel}x: personagem com {SpriteHeightPx * ScreenPixelsPerArtPixel} px, size {density.CameraSize:0.###} m");
    }
}
