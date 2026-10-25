namespace Lete.Core.Combat;

/// <summary>
/// Apoio do companheiro (GDD 4.10): age a cada <paramref name="IntervalTurns"/> turnos. Por enquanto só a
/// Postura do Aulo tem efeito: alarga as janelas da esquiva e do aparo em <paramref name="WindowWidening"/>
/// até o fim do turno da sombra seguinte.
/// </summary>
public sealed record CompanionSupport(string CompanionId, string SupportId, int IntervalTurns, float WindowWidening)
{
    public IEnumerable<string> Validate()
    {
        if (IntervalTurns < 1)
        {
            yield return $"Intervalo do apoio precisa ser pelo menos 1 turno (veio {IntervalTurns}).";
        }

        if (WindowWidening is < 0f or > 1f)
        {
            yield return $"Alargamento das janelas fora de 0..1 (veio {WindowWidening}).";
        }
    }
}
