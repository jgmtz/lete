using Lete.Core.Combat;
using Lete.Core.Conditions;

namespace Lete.Core.Systems;

/// <summary>
/// Um juramento do Estige (GDD 7.1, <c>content/oaths/&lt;id&gt;.json</c>): a promessa, a bênção e o calcanhar. Quebra
/// por ato (<see cref="BreaksOn"/>, as vias que o jogador escolhe) ou pelo prazo vencido (<see cref="ExpiresWhen"/>
/// passa antes de <see cref="KeptWhen"/>); cumprido, termina (decisões do João, 30/out/2026). Atos que não são vias
/// (revelar um nome confiado) chegam pela cena, com <see cref="Oaths.Break"/>.
/// </summary>
public sealed record Oath(
    string Id,
    string NameKey,
    string PromiseKey,
    string BlessingKey,
    string HeelKey,
    IReadOnlyList<Via> BreaksOn,
    Condition? KeptWhen,
    Condition? ExpiresWhen,
    OathEffects Blessing,
    OathEffects Heel);

/// <summary>
/// O que um juramento ativo muda no jogo. Por enquanto só os efeitos que a batalha já lê; os outros dos exemplos do GDD
/// (névoa, Fogos do Lar, Libertar mais difícil, passagem bloqueada) entram com os sistemas deles.
/// </summary>
public sealed record OathEffects(float LucidityMax, bool EchoesBlocked)
{
    public static readonly OathEffects None = new(0f, false);

    public OathEffects Plus(OathEffects other) => new(LucidityMax + other.LucidityMax, EchoesBlocked || other.EchoesBlocked);
}

/// <summary>Números dos juramentos (<c>content/rules/juramentos.json</c>): quantos ao mesmo tempo (3, GDD 7.1).</summary>
public sealed record OathRules(int MaxActive)
{
    public IEnumerable<string> Validate()
    {
        if (MaxActive < 1)
        {
            yield return "Precisa caber pelo menos 1 juramento ativo.";
        }
    }
}
