using Lete.Core.Combat.Gestures;

namespace Lete.Core.Combat;

/// <summary>O que uma escuta pode revelar: uma pista solta, o nome ou a fixação (que acendem a Compreensão).</summary>
public enum ListenReveal
{
    Clue,
    Name,
    Fixation,
}

/// <summary>
/// O que a sombra murmura durante um golpe do ciclo (<paramref name="CycleStep"/>, contando do 0). Escutando
/// pela metade ouve-se <paramref name="PartialKey"/>; inteira, <paramref name="TextKey"/>.
/// </summary>
public sealed record ListenEntry(int CycleStep, ListenReveal Reveals, string TextKey, string PartialKey);

/// <summary>Resultado de uma escuta: a fala ouvida e o que ela acabou de revelar (se revelou).</summary>
public sealed record ListenResult(string TextKey, ListenReveal? Revealed, bool Partial);

/// <summary>
/// Escutar (GDD 4.6, gesto Segurar): o que se ouve depende do momento do ciclo da sombra (o golpe que ela vai
/// repetir), e a clareza do resultado do gesto. Perfeito ouve tudo; bom ouve metade (outra escuta boa completa);
/// falho só ouve o murmúrio. Ouvir por inteiro o nome ou a fixação acende a Compreensão.
/// </summary>
public sealed class ShadeListening
{
    private const int Heard = 2;

    private readonly IReadOnlyList<ListenEntry> _entries;
    private readonly string _murmurKey;
    private readonly ShadeUnderstanding _understanding;
    private readonly Dictionary<ListenEntry, int> _progress = new();

    public ShadeListening(IReadOnlyList<ListenEntry> entries, string murmurKey, ShadeUnderstanding understanding)
    {
        _entries = entries;
        _murmurKey = murmurKey;
        _understanding = understanding;
    }

    public ListenResult Listen(int cycleStep, GestureGrade grade)
    {
        var entry = _entries.FirstOrDefault(e => e.CycleStep == cycleStep);
        if (entry is null || grade == GestureGrade.Failed)
        {
            return new ListenResult(_murmurKey, null, Partial: false);
        }

        var before = _progress.GetValueOrDefault(entry);
        var now = Math.Min(Heard, before + (grade == GestureGrade.Perfect ? Heard : 1));
        _progress[entry] = now;
        if (now < Heard)
        {
            return new ListenResult(entry.PartialKey, null, Partial: true);
        }

        ListenReveal? revealed = before < Heard ? entry.Reveals : null;
        if (revealed == ListenReveal.Name)
        {
            _understanding.RevealName();
        }
        else if (revealed == ListenReveal.Fixation)
        {
            _understanding.RevealFixation();
        }

        return new ListenResult(entry.TextKey, revealed, Partial: false);
    }
}
