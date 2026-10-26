namespace Lete.Core.Combat.Gestures;

/// <summary>As oito direções de um Traço (diagonais = duas direções apertadas juntas).</summary>
public enum StrokeDirection
{
    Up,
    UpRight,
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
    UpLeft,
}

/// <summary>
/// Gesto Traço (Memória, GDD 4.4 e 2.1): uma sequência curta de 2 a 4 direções, cada uma no tempo de um pulso.
/// O passo <c>i</c> deve ser apertado em <c>LeadInSeconds + i × BeatSeconds</c>; o valor de cada passo cai com a
/// distância ao pulso e zera em <see cref="ZeroScoreOffsetSeconds"/>. Direção errada vale 0.
/// </summary>
public sealed record StrokePattern(
    IReadOnlyList<StrokeDirection> Steps,
    float BeatSeconds,
    float ZeroScoreOffsetSeconds,
    float LeadInSeconds)
{
    public const int MinSteps = 2;
    public const int MaxSteps = 4;

    public float IdealTime(int step) => LeadInSeconds + step * BeatSeconds;

    /// <summary>Depois deste instante, o passo conta como perdido.</summary>
    public float Deadline(int step) => IdealTime(step) + ZeroScoreOffsetSeconds;

    public float StepScore(StrokeDirection expected, StrokeDirection pressed, float atSeconds, int step) =>
        expected != pressed ? 0f : Math.Max(0f, 1f - Math.Abs(atSeconds - IdealTime(step)) / ZeroScoreOffsetSeconds);

    public IEnumerable<string> Validate()
    {
        if (Steps.Count is < MinSteps or > MaxSteps)
        {
            yield return $"O Traço precisa de {MinSteps} a {MaxSteps} direções (veio {Steps.Count}).";
        }

        if (BeatSeconds <= 0f || ZeroScoreOffsetSeconds <= 0f || LeadInSeconds < ZeroScoreOffsetSeconds)
        {
            yield return "Pulso e tolerância precisam ser positivos, e a entrada não pode ser menor que a tolerância.";
        }

        if (ZeroScoreOffsetSeconds * 2f > BeatSeconds)
        {
            yield return "A tolerância não pode passar da metade do pulso (os passos se sobreporiam).";
        }
    }
}

/// <summary>
/// Um Traço em andamento. Cada aperto vale para o passo atual; passos sem aperto até o prazo valem 0. O valor final
/// é a média dos passos.
/// </summary>
public sealed class StrokeRun
{
    private readonly List<float> _scores = new();

    public StrokeRun(StrokePattern pattern) => Pattern = pattern;

    public StrokePattern Pattern { get; }
    public float Elapsed { get; private set; }
    public IReadOnlyList<float> StepScores => _scores;
    public int CurrentStep => _scores.Count;
    public bool IsFinished => _scores.Count >= Pattern.Steps.Count;
    public float Score => _scores.Count == 0 ? 0f : _scores.Sum() / Pattern.Steps.Count;

    public void Advance(float delta)
    {
        Elapsed += delta;
        while (!IsFinished && Elapsed > Pattern.Deadline(CurrentStep))
        {
            _scores.Add(0f);
        }
    }

    /// <summary>Um aperto no instante <paramref name="atSeconds"/> (desde o começo do gesto).</summary>
    public void Press(StrokeDirection direction, float atSeconds)
    {
        if (IsFinished)
        {
            return;
        }

        // Aperto cedo demais (antes da janela do passo) é ignorado: não pune quem antecipa sem querer.
        if (atSeconds < Pattern.IdealTime(CurrentStep) - Pattern.ZeroScoreOffsetSeconds)
        {
            return;
        }

        _scores.Add(Pattern.StepScore(Pattern.Steps[CurrentStep], direction, atSeconds, CurrentStep));
    }

    public static StrokeDirection? FromAxes(int x, int y) => (x, y) switch
    {
        (0, -1) => StrokeDirection.Up,
        (1, -1) => StrokeDirection.UpRight,
        (1, 0) => StrokeDirection.Right,
        (1, 1) => StrokeDirection.DownRight,
        (0, 1) => StrokeDirection.Down,
        (-1, 1) => StrokeDirection.DownLeft,
        (-1, 0) => StrokeDirection.Left,
        (-1, -1) => StrokeDirection.UpLeft,
        _ => null,
    };
}
