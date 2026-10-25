using System.Numerics;

namespace Lete.Core.Combat.Gestures;

/// <summary>
/// Medidas do quadro da Esquiva (GDD 4.4), em pixels da arte: tamanho do quadro, duração, velocidade e raio
/// do marcador, raio dos golpes, tempo de proteção depois de um acerto e quantos acertos zeram o valor.
/// </summary>
public sealed record DodgeSettings(
    float DurationSeconds,
    float BoxWidth,
    float BoxHeight,
    float MarkerSpeed,
    float MarkerRadius,
    float BulletRadius,
    float InvulnerableSeconds,
    int HitsToZero)
{
    public IEnumerable<string> Validate()
    {
        if (DurationSeconds <= 0f || BoxWidth <= 0f || BoxHeight <= 0f || MarkerSpeed <= 0f)
        {
            yield return "Duração, tamanho do quadro e velocidade do marcador precisam ser positivos.";
        }

        if (MarkerRadius * 2f >= Math.Min(BoxWidth, BoxHeight))
        {
            yield return "O marcador não cabe no quadro.";
        }

        if (HitsToZero < 1)
        {
            yield return "São precisos pelo menos 1 acerto para zerar o valor.";
        }
    }
}

/// <summary>Onde estão os golpes da sombra dentro do quadro num instante (coordenadas do quadro, y para baixo).</summary>
public interface IDodgePattern
{
    IEnumerable<Vector2> BulletsAt(float seconds);
}

/// <summary>
/// "Golpes redondos em onda" (o padeiro do GDD 4.4): colunas de golpes entram pela direita a cada
/// <see cref="SpawnInterval"/>, cada fileira ondulando com fase própria. Uma fileira de cada coluna vem vazia
/// (a passagem), e muda de lugar a cada coluna.
/// </summary>
public sealed record WavesPattern(
    DodgeSettings Box,
    float SpawnInterval,
    float Speed,
    int Rows,
    float Amplitude,
    float Frequency,
    float RowPhaseStep) : IDodgePattern
{
    public IEnumerable<Vector2> BulletsAt(float seconds)
    {
        var rowHeight = Box.BoxHeight / Rows;
        var lastColumn = (int)MathF.Floor(seconds / SpawnInterval);
        for (var column = 0; column <= lastColumn; column++)
        {
            var age = seconds - column * SpawnInterval;
            var x = Box.BoxWidth + Box.BulletRadius - Speed * age;
            if (x < -Box.BulletRadius)
            {
                continue;
            }

            var gap = GapRow(column);
            for (var row = 0; row < Rows; row++)
            {
                if (row == gap)
                {
                    continue;
                }

                var y = (row + 0.5f) * rowHeight + Amplitude * MathF.Sin(Frequency * age + row * RowPhaseStep);
                yield return new Vector2(x, y);
            }
        }
    }

    /// <summary>Fileira vazia da coluna: anda pelo quadro sem ficar no mesmo lugar duas vezes seguidas.</summary>
    public int GapRow(int column) => Rows <= 1 ? -1 : column * 2 % Rows;
}

/// <summary>
/// Uma Esquiva em andamento: move o marcador pela entrada, conta os acertos (com proteção logo depois de cada
/// um) e, ao fim, dá o valor de 0 a 1 usado por todos os gestos.
/// </summary>
public sealed class DodgeRun
{
    private readonly DodgeSettings _settings;
    private readonly IDodgePattern _pattern;
    private float _invulnerable;

    public DodgeRun(DodgeSettings settings, IDodgePattern pattern)
    {
        _settings = settings;
        _pattern = pattern;
        Marker = new Vector2(settings.BoxWidth / 2f, settings.BoxHeight / 2f);
    }

    public Vector2 Marker { get; private set; }
    public float Elapsed { get; private set; }
    public int Hits { get; private set; }
    public bool IsInvulnerable => _invulnerable > 0f;
    public bool IsFinished => Elapsed >= _settings.DurationSeconds;

    /// <summary>1 sem acertos, caindo até 0 em <see cref="DodgeSettings.HitsToZero"/> acertos.</summary>
    public float Score => Math.Max(0f, 1f - (float)Hits / _settings.HitsToZero);

    public IEnumerable<Vector2> Bullets => _pattern.BulletsAt(Elapsed);

    /// <summary>Avança o tempo e move o marcador (entrada da tela, comprimento até 1). Verdadeiro se levou um golpe agora.</summary>
    public bool Step(float delta, Vector2 input)
    {
        if (IsFinished)
        {
            return false;
        }

        Elapsed = Math.Min(_settings.DurationSeconds, Elapsed + delta);
        if (input.LengthSquared() > 1f)
        {
            input = Vector2.Normalize(input);
        }

        var r = _settings.MarkerRadius;
        var moved = Marker + input * _settings.MarkerSpeed * delta;
        Marker = Vector2.Clamp(moved, new Vector2(r, r), new Vector2(_settings.BoxWidth - r, _settings.BoxHeight - r));

        _invulnerable = Math.Max(0f, _invulnerable - delta);
        if (IsInvulnerable)
        {
            return false;
        }

        var reach = r + _settings.BulletRadius;
        if (_pattern.BulletsAt(Elapsed).Any(b => Vector2.DistanceSquared(b, Marker) < reach * reach))
        {
            Hits++;
            _invulnerable = _settings.InvulnerableSeconds;
            return true;
        }

        return false;
    }
}
