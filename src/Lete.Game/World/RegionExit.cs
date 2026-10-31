using System;
using Godot;

namespace Lete.Game.World;

/// <summary>
/// Passagem para outra região: ao entrar na área, o jogador vai para <see cref="TargetRegion"/> e chega no marcador
/// <see cref="TargetEntry"/> de lá. A cena (Main) troca a região e salva (salvar ao trocar de região).
/// </summary>
public partial class RegionExit : Area3D
{
    /// <summary>Id da região de destino (<c>content/regions</c>).</summary>
    [Export] public string TargetRegion { get; set; } = "";

    /// <summary>Nome do marcador de chegada na cena de destino (no grupo <c>teleport_points</c>).</summary>
    [Export] public string TargetEntry { get; set; } = "";

    public static event Action<string, string>? Crossed;

    private bool _crossed;

    public override void _Ready() => BodyEntered += body =>
    {
        if (body is Player && !_crossed)
        {
            _crossed = true;
            Crossed?.Invoke(TargetRegion, TargetEntry);
        }
    };
}
