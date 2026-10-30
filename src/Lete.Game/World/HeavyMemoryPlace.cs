using System;
using Godot;

namespace Lete.Game.World;

/// <summary>
/// Lugar onde uma Memória Pesada aflora (GDD 2.2). Quando o jogador entra na área e ela é a próxima da fila com a
/// condição valendo, avisa a cena (que mostra o aviso de Hécate ou do companheiro). Se o jogador adiar, o aviso
/// volta na próxima vez que ele entrar.
/// </summary>
public partial class HeavyMemoryPlace : Area3D
{
    /// <summary>Id da Pesada (<c>content/recollections</c>, <c>scope: heavy</c>).</summary>
    [Export] public string MemoryId { get; set; } = "";

    public static event Action<string>? Reached;

    public override void _Ready()
    {
        BodyEntered += body =>
        {
            var root = GameRoot.Instance;
            if (body is Player && root.HeavyMemories.CanSurface(MemoryId, root.Facts))
            {
                Reached?.Invoke(MemoryId);
            }
        };
    }
}
