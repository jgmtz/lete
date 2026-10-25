using Godot;

namespace Lete.Game.Combat;

/// <summary>
/// Anel de alvo no chão sob quem age agora, pulsando devagar: aro de ouro envelhecido com desgaste e um quadrado
/// de meandro nos quatro pontos (<c>art/ui/anel_alvo.png</c>, visto de cima). Parâmetros em <see cref="BattleAnimTuning"/>.
/// </summary>
public partial class TurnRing : MeshInstance3D
{
    private const string TexturePath = "res://art/ui/anel_alvo.png";

    /// <summary>Raio do aro dentro da imagem, em frações da metade do lado (47,5 px de 64 em <c>anel_alvo.png</c>, ver <c>tools/ui_arte.py</c>).</summary>
    private const float RingRadiusInTexture = 47.5f / 64f;

    public BattleAnimTuning Tuning { get; set; } = null!;

    public override void _Ready()
    {
        CastShadow = ShadowCastingSetting.Off;
        var side = 2f * Tuning.TurnRingRadius / RingRadiusInTexture;
        Mesh = new PlaneMesh
        {
            Size = new Vector2(side, side),
            Material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoTexture = GD.Load<Texture2D>(TexturePath),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor,
            },
        };

        var pulse = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine);
        pulse.TweenProperty(this, "scale", Vector3.One * (1f + Tuning.TurnRingPulseScale), Tuning.TurnRingPulseSeconds / 2f);
        pulse.TweenProperty(this, "scale", Vector3.One, Tuning.TurnRingPulseSeconds / 2f);
    }

    /// <summary>Vai para baixo de quem age (só a posição no chão; o ator pode estar escalado).</summary>
    public void MoveUnder(Node3D actor) => Position = actor.Position with { Y = Tuning.TurnRingThickness };
}
