using System.Numerics;

namespace Lete.Core.Combat.Gestures;

/// <summary>
/// Medidas do gesto Arrastar (GDD 4.4: conduzir um ponto devagar por um caminho, sem pressa), em pixels da
/// arte. O caminho vem em frações do quadro (0 a 1). O marcador é lento de propósito: a pressa não ajuda.
/// <paramref name="DeviationToZero"/>: distância média do caminho que zera o valor.
/// <paramref name="MaxSeconds"/>: limite generoso só para o gesto não ficar aberto para sempre.
/// </summary>
public sealed record DragSettings(
    float BoxWidth,
    float BoxHeight,
    IReadOnlyList<Vector2> Path,
    float MarkerSpeed,
    float EndRadius,
    float DeviationToZero,
    float MaxSeconds)
{
    /// <summary>O caminho em pixels do quadro.</summary>
    public IReadOnlyList<Vector2> PathPixels => Path.Select(p => new Vector2(p.X * BoxWidth, p.Y * BoxHeight)).ToList();

    public IEnumerable<string> Validate()
    {
        if (BoxWidth <= 0f || BoxHeight <= 0f || MarkerSpeed <= 0f || EndRadius <= 0f || DeviationToZero <= 0f || MaxSeconds <= 0f)
        {
            yield return "Medidas, velocidade, raio do fim, desvio que zera e tempo máximo precisam ser positivos.";
        }

        if (Path.Count < 2)
        {
            yield return "O caminho precisa de pelo menos 2 pontos.";
        }

        if (Path.Any(p => p.X is < 0f or > 1f || p.Y is < 0f or > 1f))
        {
            yield return "Os pontos do caminho precisam estar dentro do quadro (0 a 1).";
        }
    }
}

/// <summary>
/// Um Arrastar em andamento: o marcador começa no início do caminho; o valor cai com o desvio médio
/// (medido só enquanto o marcador se move). Chegar ao fim termina o gesto; estourar o tempo dá falho.
/// </summary>
public sealed class DragRun
{
    private readonly DragSettings _settings;
    private readonly IReadOnlyList<Vector2> _path;
    private float _deviationTime;
    private float _movingTime;

    public DragRun(DragSettings settings)
    {
        _settings = settings;
        _path = settings.PathPixels;
        Marker = _path[0];
    }

    public Vector2 Marker { get; private set; }
    public float Elapsed { get; private set; }
    public bool ReachedEnd { get; private set; }
    public bool IsFinished => ReachedEnd || Elapsed >= _settings.MaxSeconds;

    /// <summary>Distância atual do marcador até o caminho.</summary>
    public float Deviation => DistanceToPath(Marker);

    public float Score => !ReachedEnd
        ? 0f
        : Math.Max(0f, 1f - (_movingTime > 0f ? _deviationTime / _movingTime : 0f) / _settings.DeviationToZero);

    public void Step(float delta, Vector2 input)
    {
        if (IsFinished)
        {
            return;
        }

        Elapsed += delta;
        if (input.LengthSquared() > 1f)
        {
            input = Vector2.Normalize(input);
        }

        if (input != Vector2.Zero)
        {
            var moved = Marker + input * _settings.MarkerSpeed * delta;
            Marker = Vector2.Clamp(moved, Vector2.Zero, new Vector2(_settings.BoxWidth, _settings.BoxHeight));
            _movingTime += delta;
            _deviationTime += DistanceToPath(Marker) * delta;
        }

        ReachedEnd = Vector2.Distance(Marker, _path[^1]) <= _settings.EndRadius;
    }

    private float DistanceToPath(Vector2 point)
    {
        var best = float.MaxValue;
        for (var i = 0; i < _path.Count - 1; i++)
        {
            best = Math.Min(best, DistanceToSegment(point, _path[i], _path[i + 1]));
        }

        return best;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = ab.LengthSquared() == 0f ? 0f : Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }
}
