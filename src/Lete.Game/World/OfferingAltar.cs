using Godot;

namespace Lete.Game.World;

/// <summary>Altar das Oferendas (GDD 5.1): o que os vivos deixam; recolher respeita o limite por tipo, e o resto fica.</summary>
public partial class OfferingAltar : Interactable
{
    [Export] public string Kind { get; set; } = "mel";
    [Export] public int Amount { get; set; } = 1;

    public override string PromptKey => "ui.prompt.recolher";

    public override void Examine()
    {
        var taken = GameRoot.Instance.Offerings.Pick(Kind, Amount);
        Amount -= taken;
        Announce(taken > 0 ? "ui.altar.recolheu" : Amount > 0 ? "ui.altar.cheia" : "ui.altar.vazio");
    }
}
