using System;
using Godot;

namespace Lete.Game.World;

/// <summary>
/// Alguém com quem conversar: perto dele, <c>interact</c> abre a conversa <see cref="DialogueId"/>
/// (<c>content/dialogue</c>) em vez de examinar.
/// </summary>
public partial class Talkable : Interactable
{
    [Export] public string DialogueId { get; set; } = "";

    public static event Action<string>? Talked;

    public override string PromptKey => "ui.prompt.conversar";

    public override void Examine() => Talked?.Invoke(DialogueId);
}
