using Godot;

namespace Lete.Game.World;

/// <summary>Um item único no mundo (GDD 6): pegar dá o item ao jogador uma vez; depois o lugar fica vazio.</summary>
public partial class ItemPickup : Interactable
{
    [Export] public string ItemId { get; set; } = "";

    public override string PromptKey => "ui.prompt.pegar";

    public override void _Ready()
    {
        base._Ready();
        Visible = !GameRoot.Instance.Items.Owns(ItemId);
        Monitoring = Visible;
    }

    public override void Examine()
    {
        if (GameRoot.Instance.Items.Acquire(ItemId))
        {
            Announce("ui.item.pegou");
        }

        Visible = false;
        SetDeferred(Area3D.PropertyName.Monitoring, false);
    }
}
