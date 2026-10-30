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

    public static event Action<string>? Reached;

    public override string PromptKey => "ui.prompt.descansar";

    public override void _Ready()
    {
        base._Ready();
        AddToGroup(Group);
    }

    public override void Examine() => Reached?.Invoke(TriviaId);
}
