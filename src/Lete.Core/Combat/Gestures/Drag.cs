using System.Numerics;

namespace Lete.Core.Combat.Gestures;

/// <summary>
/// Medidas do gesto Arrastar (GDD 4.4: conduzir um ponto devagar por um caminho, sem pressa), em pixels da arte.
/// A luz anda <b>presa no caminho</b> (as curvas são automáticas): qualquer direção que aponte para a frente do trecho
/// atual a faz avançar a <paramref name="MarkerSpeed"/>; para trás, recua. Uma luz-guia anda à frente a
/// <paramref name="GuideSpeed"/>, mais devagar: o valor vem de acompanhá-la, sem correr nem ficar para trás.
/// <paramref name="Path"/>: pontos em frações do quadro (0 a 1).
/// <paramref name="GapToZero"/>: distância média da guia (ao longo do caminho) que zera o valor.
/// <paramref name="ForwardThreshold"/>: o quanto a direção precisa apontar para a frente (cosseno, 0 a 1).
/// <paramref name="MaxSeconds"/>: limite generoso para o gesto não ficar aberto para sempre.
/// </summary>
public sealed record DragSettings(
    float BoxWidth,
    float BoxHeight,
    IReadOnlyList<Vector2> Path,
    float MarkerSpeed,
    float GuideSpeed,
    float GapToZero,
    float ForwardThreshold,
    float MaxSeconds)
{
    /// <summary>O caminho em pixels do quadro.</summary>
    public IReadOnlyList<Vector2> PathPixels => Path.Select(p => new Vector2(p.X * BoxWidth, p.Y * BoxHeight)).ToList();

    public IEnumerable<string> Validate()
    {
        if (BoxWidth <= 0f || BoxHeight <= 0f || MarkerSpeed <= 0f || GuideSpeed <= 0f || GapToZero <= 0f || MaxSeconds <= 0f)
        {
            yield return "Medidas, velocidades, distância que zera e tempo máximo precisam ser positivos.";
        }

        if (MarkerSpeed <= GuideSpeed)
        {
            yield return "A luz precisa ser mais rápida que a guia (senão nunca alcança quem ficou para trás).";
        }

        if (ForwardThreshold is < 0f or >= 1f)
        {
            yield return "O limiar de 'para a frente' precisa ficar entre 0 e 1.";
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
/// Um Arrastar em andamento, no trilho: a luz e a guia são posições ao longo do caminho. O valor cai com a distância
/// média entre as duas; chegar ao fim termina o gesto; estourar o tempo dá falho.
/// </summary>
public sealed class DragRun
{
    private readonly DragSettings _settings;
    private readonly IReadOnlyList<Vector2> _path;
    private readonly float[] _starts;
    private float _gapTime;

    /// <summary>Recuo mínimo para saber de qual trecho a luz veio quando está exatamente num canto.</summary>
    private const float CornerEpsilon = 1e-3f;

    public DragRun(DragSettings settings)
    {
        _settings = settings;
        _path = settings.PathPixels;
        _starts = new float[_path.Count];
        for (var i = 1; i < _path.Count; i++)
        {
            _starts[i] = _starts[i - 1] + Vector2.Distance(_path[i - 1], _path[i]);
        }

        Length = _starts[^1];
    }

    /// <summary>Comprimento do caminho, em pixels da arte.</summary>
    public float Length { get; }

    /// <summary>Quanto a luz e a guia já andaram pelo caminho.</summary>
    public float Progress { get; private set; }
    public float GuideProgress => Math.Min(Length, _settings.GuideSpeed * Elapsed);

    public float Elapsed { get; private set; }
    public bool ReachedEnd => Progress >= Length;
    public bool IsFinished => ReachedEnd || Elapsed >= _settings.MaxSeconds;

    /// <summary>Luz menos guia, ao longo do caminho: positivo = à frente (com pressa), negativo = atrás.</summary>
    public float Gap => Progress - GuideProgress;

    public Vector2 Marker => PointAt(Progress);
    public Vector2 Guide => PointAt(GuideProgress);

    /// <summary>Direção do trecho onde a luz está (para onde é "para a frente").</summary>
    public Vector2 Tangent => TangentAt(Progress);

    public float Score => !ReachedEnd || Elapsed <= 0f ? 0f : Math.Max(0f, 1f - _gapTime / Elapsed / _settings.GapToZero);

    public void Step(float delta, Vector2 input)
    {
        if (IsFinished)
        {
            return;
        }

        Elapsed += delta;
        if (input != Vector2.Zero)
        {
            // Para a frente vale o trecho seguinte (no canto, já o novo); para trás, o trecho de onde veio.
            // Um passo nunca atravessa um canto: a luz para nele e a próxima direção decide o resto.
            var direction = Vector2.Normalize(input);
            var ahead = Segment(Progress);
            var behind = Segment(Math.Max(0f, Progress - CornerEpsilon));
            if (Vector2.Dot(direction, TangentOf(ahead)) > _settings.ForwardThreshold)
            {
                Progress = Math.Min(_starts[ahead + 1], Progress + _settings.MarkerSpeed * delta);
            }
            else if (Vector2.Dot(direction, TangentOf(behind)) < -_settings.ForwardThreshold)
            {
                Progress = Math.Max(_starts[behind], Progress - _settings.MarkerSpeed * delta);
            }
        }

        _gapTime += Math.Abs(Gap) * delta;
    }

    private Vector2 PointAt(float distance)
    {
        var i = Segment(distance);
        var length = _starts[i + 1] - _starts[i];
        var t = length == 0f ? 0f : Math.Clamp((distance - _starts[i]) / length, 0f, 1f);
        return Vector2.Lerp(_path[i], _path[i + 1], t);
    }

    private Vector2 TangentAt(float distance) => TangentOf(Segment(distance));

    private Vector2 TangentOf(int i)
    {
        var segment = _path[i + 1] - _path[i];
        return segment == Vector2.Zero ? Vector2.UnitX : Vector2.Normalize(segment);
    }

    private int Segment(float distance)
    {
        // No fim de um trecho já vale o próximo (para a curva ser automática ao chegar no canto).
        for (var i = 0; i < _path.Count - 2; i++)
        {
            if (distance < _starts[i + 1])
            {
                return i;
            }
        }

        return _path.Count - 2;
    }
}
