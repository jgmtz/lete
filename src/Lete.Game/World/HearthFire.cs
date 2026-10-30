using Godot;

namespace Lete.Game.World;

/// <summary>Fogo do Lar (GDD 4.8): aceso com uma oferenda, devolve parte do Fôlego, uma vez por degrau da Maré.</summary>
public partial class HearthFire : Interactable
{
    /// <summary>Id do altar (cada fogo cura uma vez por degrau).</summary>
    [Export] public string FireId { get; set; } = "";

    public override string PromptKey => "ui.prompt.acender";

    public override void Examine()
    {
        var healing = GameRoot.Instance.Healing;
        Announce(!healing.CanUse(FireId) ? "ui.fogo.apagado"
            : healing.LightHearth(FireId) ? "ui.fogo.aceso"
            : "ui.fogo.sem_oferenda");
    }
}
