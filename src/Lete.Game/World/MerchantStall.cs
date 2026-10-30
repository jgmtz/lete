using System;
using Godot;

namespace Lete.Game.World;

/// <summary>Um mercador no mundo (GDD 6.4): interagir abre a loja dele (<see cref="Opened"/>).</summary>
public partial class MerchantStall : Interactable
{
    [Export] public string MerchantId { get; set; } = "";

    public static event Action<string>? Opened;

    public override string PromptKey => "ui.prompt.negociar";

    public override void Examine() => Opened?.Invoke(MerchantId);
}
