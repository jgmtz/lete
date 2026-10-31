using Godot;

namespace Lete.Game.World;

/// <summary>Altar das Oferendas (GDD 5.1): o que os vivos deixam; recolher respeita o limite por tipo, e o resto fica.</summary>
public partial class OfferingAltar : Interactable
{
    [Export] public string Kind { get; set; } = "mel";
    [Export] public int Amount { get; set; } = 1;

    /// <summary>A frase ao recolher (uma poça não é um altar); a <see cref="Interactable.JournalKey"/>, se houver, vai para o Diário.</summary>
    [Export] public string TakenKey { get; set; } = "ui.altar.recolheu";

    public override string PromptKey => "ui.prompt.recolher";

    public override void Examine()
    {
        var taken = GameRoot.Instance.Offerings.Pick(Kind, Amount);
        Amount -= taken;
        if (taken > 0 && JournalKey != "")
        {
            GameRoot.Instance.Journal.Add(JournalKey);
        }

        Announce(taken > 0 ? TakenKey : Amount > 0 ? "ui.altar.cheia" : "ui.altar.vazio");
    }
}
