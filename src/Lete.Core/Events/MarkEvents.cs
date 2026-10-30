using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Events;

/// <summary>A Marca subiu <paramref name="Amount"/> e está na faixa <paramref name="Band"/>.</summary>
public sealed record MarkIncreased(int Amount, MarkBand Band);

/// <summary>O corpo chegou a uma etapa nova da Marca (1 a 3): a imagem do inventário muda.</summary>
public sealed record MarkStageReached(int Stage);

/// <summary>Uma sombra foi absorvida (via Absorver). <paramref name="ShadeId"/> é o id do conteúdo.</summary>
public sealed record ShadeAbsorbed(string ShadeId);

/// <summary>O jogador encarou ou deixou uma Memória Pesada (irreversível).</summary>
public sealed record HeavyMemoryChosen(string MemoryId, HeavyMemoryChoice Choice);

/// <summary>O rosto do fim foi decidido pela regra C.</summary>
public sealed record FinalFaceSettled(FinalFace Face);

/// <summary>Um fragmento de memória sobre a própria origem foi ganho.</summary>
public sealed record OriginFragmentGained(string FragmentId);
