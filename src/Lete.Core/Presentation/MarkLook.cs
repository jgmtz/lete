using Lete.Core.Systems;

namespace Lete.Core.Presentation;

/// <summary>
/// Como a Marca aparece no sprite de mapa e de batalha (GDD 2.3, <c>docs/PROTAGONISTA_SOMBRA.md</c> 6): o sprite não
/// troca de imagem, perde cor por faixa (shader, <i>proposta</i>) e, na faixa alta, deixa de projetar sombra.
/// Os quanto de perda de cor vêm de <c>content/rules/marca.json</c> (provisórios).
/// </summary>
public sealed record MarkLook(float MidDesaturation, float HighDesaturation)
{
    /// <summary>Quanto de cor o sprite perde (0 = nenhuma, 1 = cinza).</summary>
    public float DesaturationOf(MarkBand band) => band switch
    {
        MarkBand.High => HighDesaturation,
        MarkBand.Mid => MidDesaturation,
        _ => 0f,
    };

    /// <summary>Nas faixas altas o protagonista não projeta sombra (e não deixa pegadas, quando elas existirem).</summary>
    public static bool CastsShadow(MarkBand band) => band != MarkBand.High;

    public IEnumerable<string> Validate()
    {
        if (MidDesaturation < 0f || HighDesaturation > 1f || HighDesaturation < MidDesaturation)
        {
            yield return "A perda de cor da Marca precisa ser 0 <= média <= alta <= 1.";
        }
    }
}
