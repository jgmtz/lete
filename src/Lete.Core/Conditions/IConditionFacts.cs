using Lete.Core.Recollections;
using Lete.Core.Combat;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Conditions;

/// <summary>
/// O que as condições perguntam ao jogo (uma pergunta por predicado de DADOS.md 1). <see cref="WorldFacts"/> responde
/// pelo <see cref="WorldState"/>; os testes usam respostas prontas.
/// </summary>
public interface IConditionFacts
{
    string? Origin { get; }
    Gender Gender { get; }
    string? Companion { get; }
    string? Difficulty { get; }
    MarkBand MarkBand { get; }
    /// <summary>Degrau inteiro da Maré.</summary>
    int Tide { get; }
    int Bond { get; }
    int FullRecollectionKinds { get; }
    Via? TopVia { get; }
    RecollectionKind? TopKind { get; }

    /// <summary>Lucidez da sombra do diálogo atual; nula fora de um diálogo.</summary>
    string? ShadeLucidity { get; }

    bool HasFlag(string flag);
    RecollectionStage? RecollectionStageOf(string id);
    RecollectionCertainty? RecollectionCertaintyOf(string id);
    HeavyMemoryState HeavyMemory(string id);
    bool HasPathKey(string key);
    bool IsOathActive(string oath);
    bool IsOathBroken(string oath);
    bool WasOfferingShared(string shade, string? kind);
    bool ShadeIs(string shade, ShadeState state);
    bool OwnsItem(string item);
    bool IsTriviaLit(string trivia);
    bool WasRegionVisited(string region);

    /// <summary>Estágio atual da missão (índice em <c>stages</c>); nulo se ela ainda não apareceu.</summary>
    int? QuestStage(string quest);
}
