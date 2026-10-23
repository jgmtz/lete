using Godot;

namespace Lete.Game.World;

/// <summary>
/// Câmera ortográfica diagonal fixa (estilo Hades) que segue um alvo.
/// O rig gira no eixo Y (yaw), o nó "Pitch" inclina, e a câmera fica afastada no eixo Z local.
/// </summary>
public partial class CameraRig : Node3D
{
    [Export] public float YawDegrees { get; set; } = 45f;
    [Export] public float PitchDegrees { get; set; } = -35f;
    [Export] public float Distance { get; set; } = 30f;
    [Export] public float FollowSharpness { get; set; } = 8f;
    [Export] public Node3D? Target { get; set; }

    public Camera3D Camera { get; private set; } = null!;

    public override void _Ready()
    {
        var pitch = GetNode<Node3D>("Pitch");
        Camera = pitch.GetNode<Camera3D>("Camera3D");

        RotationDegrees = new Vector3(0, YawDegrees, 0);
        pitch.RotationDegrees = new Vector3(PitchDegrees, 0, 0);
        Camera.Position = new Vector3(0, 0, Distance);
        Camera.Projection = Camera3D.ProjectionType.Orthogonal;
        SnapToTarget();
    }

    public override void _Process(double delta)
    {
        if (Target is null)
        {
            return;
        }

        var t = 1f - Mathf.Exp(-FollowSharpness * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition, t);
    }

    /// <summary>Pula direto para o alvo (usado no início e no teleporte).</summary>
    public void SnapToTarget()
    {
        if (Target is not null)
        {
            GlobalPosition = Target.GlobalPosition;
        }
    }
}
