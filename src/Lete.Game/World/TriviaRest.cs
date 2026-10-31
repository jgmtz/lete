using System;
using Godot;

namespace Lete.Game.World;

/// <summary>
/// Trívia de Hécate (GDD 3.6 e 3.7): chegar e interagir a acende e abre o que ela oferece (descansar e salvar, seguir
/// uma tocha). A cena abre a tela (<see cref="Reached"/>).
/// </summary>
public partial class TriviaRest : Interactable
{
    /// <summary>Id da Trívia (<c>content/trivias</c>).</summary>
    public const string Group = "trivias";

    [Export] public string TriviaId { get; set; } = "";

    /// <summary>Quanto a chama cresce enquanto Hécate fala (sem arte dela, ela é a chama e a voz) e em quanto tempo.</summary>
    [Export] public float PresenceEnergyFactor { get; set; } = 2.2f;
    [Export] public float PresenceSeconds { get; set; } = 0.8f;

    public static event Action<string>? Reached;

    public override string PromptKey => "ui.prompt.descansar";

    public override void _Ready()
    {
        base._Ready();
        AddToGroup(Group);
    }

    public override void Examine() => Reached?.Invoke(TriviaId);

    private float? _baseEnergy;

    /// <summary>A chama da Trívia (o nó irmão "&lt;nome&gt;Chama", se houver) cresce enquanto Hécate está presente.</summary>
    public void SetPresence(bool present)
    {
        if (GetParent()?.GetNodeOrNull<OmniLight3D>($"{Name}Chama") is not { } flame)
        {
            return;
        }

        _baseEnergy ??= flame.LightEnergy;
        CreateTween().TweenProperty(flame, "light_energy", _baseEnergy.Value * (present ? PresenceEnergyFactor : 1f), PresenceSeconds);
    }
}
