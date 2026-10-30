namespace Lete.Core.State;

/// <summary>O que as conversas guardam no <see cref="WorldState"/>.</summary>
public sealed class DialogueState
{
    /// <summary>Opções já escolhidas (<c>diálogo/text_key</c>): aparecem esmaecidas da próxima vez.</summary>
    public HashSet<string> Chosen { get; } = new(StringComparer.Ordinal);

    /// <summary>Comentários do companheiro já feitos (cada um só uma vez por partida).</summary>
    public HashSet<string> CommentsSeen { get; } = new(StringComparer.Ordinal);

    /// <summary>Sombras despertas pela oferenda certa, até o fim da próxima conversa com elas.</summary>
    public HashSet<string> Awakened { get; } = new(StringComparer.Ordinal);
}
