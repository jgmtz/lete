using System;
using Godot;

namespace Lete.Game.World;

/// <summary>Um lugar de julgamento no Tribunal (GDD 7.2): interagir abre o julgamento <see cref="TrialId"/>.</summary>
public partial class TribunalSeat : Interactable
{
    [Export] public string TrialId { get; set; } = "";

    public static event Action<string>? Opened;

    public override string PromptKey => "ui.prompt.julgamento";

    public override void Examine() => Opened?.Invoke(TrialId);
}
