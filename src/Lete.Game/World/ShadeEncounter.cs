using System;
using Godot;

namespace Lete.Game.World;

/// <summary>
/// Uma sombra parada no mapa que abre a luta quando o jogador chega perto (<see cref="EncounterId"/>, de
/// <c>content/encounters</c>). Só aparece enquanto o encontro está disponível; se ele pede uma oferenda que o jogador
/// não tem, mostra a fala de espera em vez de lutar. Não se examina: o aviso de "Examinar" não aparece.
/// </summary>
public partial class ShadeEncounter : Interactable
{
    [Export] public string EncounterId { get; set; } = "";

    /// <summary>A que distância do centro o jogador volta ao mapa depois da luta (fora do alcance, para não recomeçar).</summary>
    [Export] public float ReturnDistance { get; set; } = 2.5f;

    /// <summary>O encontro começa: o id e onde o jogador fica quando voltar ao mapa.</summary>
    public static event Action<string, Vector3>? Started;

    private bool _started;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _Process(0);
    }

    /// <summary>Aparece e some com o encontro (terminar o anterior, carregar um save, o painel de debug).</summary>
    public override void _Process(double delta)
    {
        var available = GameRoot.Instance.Encounters.IsAvailable(EncounterId);
        if (available != Visible || available != Monitoring)
        {
            Visible = available;
            Monitoring = available;
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Player player || _started || !GameRoot.Instance.Encounters.IsAvailable(EncounterId))
        {
            return;
        }

        if (!GameRoot.Instance.Encounters.CanStart(EncounterId))
        {
            Announce(GameRoot.Instance.Encounters.Definitions[EncounterId].WaitKey!);
            return;
        }

        _started = true;
        var away = player.GlobalPosition - GlobalPosition;
        away.Y = 0f;
        var direction = away.LengthSquared() > 0f ? away.Normalized() : Vector3.Back;
        Started?.Invoke(EncounterId, GlobalPosition + direction * ReturnDistance);
    }
}
