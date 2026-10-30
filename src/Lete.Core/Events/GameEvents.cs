using Lete.Core.Combat;
using Lete.Core.Recollections;
using Lete.Core.Conditions;

namespace Lete.Core.Events;

// Eventos da lista inicial de ARQUITETURA.md 4.2 (os da Marca ficam em MarkEvents.cs e o do Diário em
// JournalEntryAdded.cs). Ids são os do conteúdo. Muitos ainda não têm quem publique: entram com os sistemas do M2.

// ---- Sombras (vias de resolução, GDD 4.5) ----

/// <summary>Uma sombra foi libertada (via Libertar).</summary>
public sealed record ShadeFreed(string ShadeId);

/// <summary>Uma sombra foi desfeita (via Combater).</summary>
public sealed record ShadeDestroyed(string ShadeId);

/// <summary>O jogador ficou até o fim do ciclo dela e viu a história inteira (via Presenciar).</summary>
public sealed record ShadeWitnessed(string ShadeId);

/// <summary>A sombra foi deixada ir, ainda por resolver (via Poupar).</summary>
public sealed record ShadeSpared(string ShadeId);

/// <summary>O jogador fugiu da luta.</summary>
public sealed record ShadeFled(string ShadeId);

/// <summary>O jogador escolheu perder (Deixar-se vencer; não é a derrota involuntária).</summary>
public sealed record ShadeYielded(string ShadeId);

/// <summary>Derrota involuntária (Fôlego zerado): volta à Trívia com custo (GDD 4.9).</summary>
public sealed record PlayerDefeated(string ShadeId);

/// <summary>A sombra passou para outra fase da luta (a luta começa na fase 1).</summary>
public sealed record BossPhaseChanged(string ShadeId, int Phase);

/// <summary>
/// O jogador obteve uma chave do Caminho (<paramref name="KeyId"/>) pela forma <paramref name="WayId"/>; o Diário
/// escreve <paramref name="JournalKey"/>.
/// </summary>
public sealed record PathKeyObtained(string KeyId, string WayId, string JournalKey);

/// <summary>Uma Memória (habilidade) foi usada em combate; o Perfil conta o tipo dela.</summary>
public sealed record MemoryUsed(string MemoryId, RecollectionKind Kind);

// ---- Mosaico ----

public sealed record RecollectionGained(string RecollectionId, RecollectionStage Stage);

public sealed record RecollectionAdvanced(string RecollectionId, RecollectionStage Stage);

/// <summary>Uma Lembrança confirmada por uma fonte que a contradiz virou falsa.</summary>
public sealed record RecollectionFalsified(string RecollectionId);

// ---- Maré ----

/// <summary>Por que a Maré subiu (ARQUITETURA 4.2).</summary>
public enum TideCause
{
    Milestone,
    Travel,
    Defeat,
}

/// <summary>A Maré subiu <paramref name="Amount"/> (um marco soma 1; a viagem, uma fração).</summary>
public sealed record TideAdvanced(decimal Amount, TideCause Cause);

/// <summary>A Maré cruzou um degrau inteiro: as tabelas das regiões reagem.</summary>
public sealed record TideStepReached(int Step);

// ---- Oferendas, juramentos, Tribunal ----

/// <summary>Oferenda dada a uma sombra (tipo: mel, vinho, leite, agua, farinha).</summary>
public sealed record OfferingGiven(string ShadeId, string Kind);

/// <summary>Oferenda partilhada com uma sombra (comer junto).</summary>
public sealed record OfferingShared(string ShadeId, string Kind);

/// <summary>O protagonista descansou numa Trívia (Fôlego e Lucidez cheios).</summary>
public sealed record Rested;

/// <summary>Uma fonte limitada (Fogo do Lar, hospitalidade) devolveu <paramref name="Breath"/> de Fôlego.</summary>
public sealed record Healed(string SourceId, float Breath);

public sealed record OathSworn(string OathId);

/// <summary>Perjúrio: a Marca sobe e a bênção some (DADOS.md 8).</summary>
public sealed record OathBroken(string OathId);

/// <summary>O juramento foi cumprido (chegou ao prazo sem quebrar): termina e libera o espaço.</summary>
public sealed record OathKept(string OathId);

public sealed record TestimonyGiven(string TrialId, string RecollectionId);

// ---- Mundo ----

public sealed record TriviaLit(string TriviaId);

public sealed record FastTravelUsed(string FromTriviaId, string ToTriviaId);

public sealed record RegionEntered(string RegionId);

/// <summary>Chegou aos Portões (os finais compostos começam).</summary>
public sealed record GateReached;

// ---- Itens ----

/// <summary>Evolução de item (GDD: Gasto → Reconhecido → Desperto).</summary>
public enum ItemStage
{
    Worn,
    Recognized,
    Awakened,
}

public sealed record ItemAcquired(string ItemId);

/// <summary>O item subiu de estágio; <paramref name="StoryKey"/> é a linha de história que vai para o Diário.</summary>
public sealed record ItemEvolved(string ItemId, ItemStage Stage, string? StoryKey);

/// <summary>Todas as peças de um conjunto de dono reunidas; <paramref name="StoryKey"/> conta a história inteira.</summary>
public sealed record ItemSetCompleted(string SetId, string StoryKey);

// ---- Companheiro e diálogo ----

/// <summary>Escolha irreversível do companheiro (um dos 6).</summary>
public sealed record CompanionChosen(string CompanionId);

/// <summary>A origem foi escolhida na abertura (uma vez; dá, por exemplo, a forma "origem" de uma chave do Caminho).</summary>
public sealed record OriginChosen(string OriginId);

public sealed record CompanionBondChanged(string CompanionId, int Bond);

/// <summary>Opção escolhida num diálogo (postura: escutar, perguntar, pressionar, consolar, calar).</summary>
public sealed record DialogueChoiceMade(string DialogueId, string NodeId, string Posture);

/// <summary>Uma conversa terminou (o despertar da oferenda acaba aqui).</summary>
public sealed record DialogueEnded(string DialogueId);

/// <summary>O jogador respondeu ao comentário do companheiro (o vínculo escuta quando existir).</summary>
public sealed record CompanionCommented(string CommentId, string CompanionId, Lete.Core.Dialogue.CommentResponse Response);
