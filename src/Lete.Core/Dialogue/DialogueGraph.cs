using Lete.Core.Conditions;

namespace Lete.Core.Dialogue;

/// <summary>Os três modos de conversa (GDD 5.2).</summary>
public enum DialogueMode
{
    /// <summary>Sombras: estados de lucidez (apagada, vaga, desperta).</summary>
    Shade,

    /// <summary>Seres fixos (Hécate, Hades, Caronte...): falam normalmente.</summary>
    Fixed,

    /// <summary>Cenas: sem escolha, só contam algo.</summary>
    Scene,
}

/// <summary>Posturas das opções (GDD 5.2), não "bom ou mau". No conteúdo: escutar, perguntar, pressionar, consolar, calar.</summary>
public enum Posture
{
    Listen,
    Ask,
    Press,
    Console,
    Silence,
}

/// <summary>Lucidez da sombra na conversa. No conteúdo e na condição <c>shade_lucidity</c>: apagada, vaga, desperta.</summary>
public enum ShadeLucidity
{
    Faded,
    Vague,
    Awake,
}

/// <summary>
/// Uma conversa (<c>content/dialogue/&lt;id&gt;.json</c>, DADOS.md 5): grafo de nós com condições. <see cref="Start"/>:
/// nós de entrada, na ordem; a conversa começa pelo primeiro cujo <c>when</c> vale.
/// </summary>
public sealed record DialogueGraph(
    string Id,
    DialogueMode Mode,
    string SpeakerKey,
    string? ShadeId,
    LucidityRules? Lucidity,
    IReadOnlyList<string> Start,
    IReadOnlyDictionary<string, DialogueNode> Nodes);

/// <summary>
/// Como a lucidez da sombra é decidida (decisão do João, 30/out/2026): o estado base do arquivo, subido pela primeira
/// condição de <see cref="Raises"/> que vale; a oferenda certa (<see cref="OfferingKind"/>, dada ou partilhada; nula =
/// qualquer uma) a deixa desperta até o fim da conversa seguinte.
/// </summary>
public sealed record LucidityRules(ShadeLucidity Base, IReadOnlyList<LucidityRaise> Raises, string? OfferingKind);

public sealed record LucidityRaise(ShadeLucidity To, Condition When);

/// <summary>
/// Um nó: a fala, as opções (sem opções, segue para <see cref="Next"/> ou termina), os efeitos ao entrar e, se houver,
/// o comentário do companheiro.
/// </summary>
public sealed record DialogueNode(
    string Id,
    Condition? When,
    string LineKey,
    IReadOnlyList<DialogueOption> Options,
    string? Next,
    IReadOnlyList<DialogueEffect> Effects,
    CompanionComment? Comment);

/// <summary>
/// Uma opção. <see cref="Next"/> nulo termina a conversa. Bloqueada (<see cref="Requires"/> falha): some, ou aparece
/// em cinza com a dica <see cref="HintKey"/> se <see cref="ShowBlocked"/> (ou se faltar só um predicado <c>soft</c>).
/// </summary>
public sealed record DialogueOption(Posture Posture, string TextKey, string? Next, Condition? Requires, string? HintKey, bool ShowBlocked);

/// <summary>Comentário do companheiro, uma vez só por partida (GDD 5.2): uma fala por companheiro que pode estar junto.</summary>
public sealed record CompanionComment(string Id, IReadOnlyDictionary<string, string> LineKeys);

/// <summary>Resposta do jogador ao comentário do companheiro (o vínculo que ela muda entra com o item Companheiro).</summary>
public enum CommentResponse
{
    Agree,
    Disagree,
    Ignore,
}

/// <summary>O que um nó faz ao ser alcançado.</summary>
public abstract record DialogueEffect;

public sealed record AddJournalEntry(string EntryKey) : DialogueEffect;

public sealed record SetFlag(string Flag) : DialogueEffect;

public sealed record GainOriginFragment(string FragmentId) : DialogueEffect;

/// <summary>A conversa é uma fonte de uma Lembrança (Mosaico).</summary>
public sealed record HearRecollectionSource(string RecollectionId, string SourceId) : DialogueEffect;

public sealed record SwearOath(string OathId) : DialogueEffect;

/// <summary>Perjúrio por um ato que não é via (ex.: revelar o nome confiado).</summary>
public sealed record BreakOath(string OathId) : DialogueEffect;
