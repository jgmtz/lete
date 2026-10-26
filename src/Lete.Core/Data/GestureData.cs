using System.Text.Json;
using Lete.Core.Combat.Gestures;

namespace Lete.Core.Data;

/// <summary>Carrega os parâmetros de gesto dos arquivos de <c>content/</c>.</summary>
public static class GestureData
{
    /// <summary>
    /// Quantos anéis seguidos o gesto pede (campo opcional <c>repeats</c>; o Eco do Remador é um Anel duplo, no
    /// ritmo da remada). O valor final é a média dos anéis.
    /// </summary>
    public static int RingRepeats(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var repeats = doc.RootElement.TryGetProperty("repeats", out _) ? (int)TbdNumber.Read(doc.RootElement, "repeats") : 1;
        return repeats >= 1 ? repeats : throw new FormatException("'repeats' precisa ser pelo menos 1.");
    }

    /// <summary>Tempo de um Segurar (<c>content/gestures/&lt;id&gt;.json</c>, <c>gesture: "hold"</c>).</summary>
    public static HoldTiming HoldFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var timing = new HoldTiming(
            TbdNumber.Read(root, "fill_seconds"),
            TbdNumber.Read(root, "target"),
            TbdNumber.Read(root, "zero_score_distance"),
            TbdNumber.Read(root, "wait_seconds"));
        var errors = timing.Validate().ToList();
        return errors.Count == 0 ? timing : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Quadro e caminho de um Arrastar (<c>content/gestures/&lt;id&gt;.json</c>, <c>gesture: "drag"</c>).</summary>
    public static DragSettings DragFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("path", out var path))
        {
            throw new FormatException("Campo 'path' ausente.");
        }

        var settings = new DragSettings(
            TbdNumber.Read(root, "box_width"),
            TbdNumber.Read(root, "box_height"),
            path.EnumerateArray().Select(p => new System.Numerics.Vector2(TbdNumber.Read(p, "x"), TbdNumber.Read(p, "y"))).ToList(),
            TbdNumber.Read(root, "marker_speed"),
            TbdNumber.Read(root, "guide_speed"),
            TbdNumber.Read(root, "gap_to_zero"),
            TbdNumber.Read(root, "forward_threshold"),
            TbdNumber.Read(root, "max_seconds"));
        var errors = settings.Validate().ToList();
        return errors.Count == 0 ? settings : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Limiares de um modo de dificuldade (<c>content/difficulty/&lt;modo&gt;.json</c>, campo <c>gesture_windows</c>).</summary>
    public static GestureWindows WindowsFromDifficulty(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("gesture_windows", out var windows))
        {
            throw new FormatException("Campo 'gesture_windows' ausente no modo de dificuldade.");
        }

        return new GestureWindows(TbdNumber.Read(windows, "perfect"), TbdNumber.Read(windows, "good"));
    }

    /// <summary>Tempo de um Anel (<c>content/gestures/&lt;id&gt;.json</c>).</summary>
    public static RingTiming RingFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new RingTiming(
            TbdNumber.Read(root, "shrink_seconds"),
            TbdNumber.Read(root, "start_scale"),
            TbdNumber.Read(root, "zero_score_offset_seconds"));
    }

    /// <summary>Quadro e padrão de uma Esquiva (<c>content/gestures/&lt;id&gt;.json</c>, <c>gesture: "dodge"</c>).</summary>
    public static (DodgeSettings Settings, IDodgePattern Pattern) DodgeFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var settings = new DodgeSettings(
            TbdNumber.Read(root, "duration_seconds"),
            TbdNumber.Read(root, "box_width"),
            TbdNumber.Read(root, "box_height"),
            TbdNumber.Read(root, "marker_speed"),
            TbdNumber.Read(root, "marker_radius"),
            TbdNumber.Read(root, "bullet_radius"),
            TbdNumber.Read(root, "invulnerable_seconds"),
            (int)TbdNumber.Read(root, "hits_to_zero"));
        var errors = settings.Validate().ToList();
        if (errors.Count > 0)
        {
            throw new FormatException(string.Join(" ", errors));
        }

        if (!root.TryGetProperty("pattern", out var p) || !p.TryGetProperty("id", out var id))
        {
            throw new FormatException("Campo 'pattern' (com 'id') ausente.");
        }

        IDodgePattern pattern = id.GetString() switch
        {
            "ondas" => new WavesPattern(
                settings,
                TbdNumber.Read(p, "spawn_interval"),
                TbdNumber.Read(p, "speed"),
                (int)TbdNumber.Read(p, "rows"),
                TbdNumber.Read(p, "amplitude"),
                TbdNumber.Read(p, "frequency"),
                TbdNumber.Read(p, "row_phase_step")),
            var other => throw new FormatException($"Padrão de esquiva '{other}' desconhecido."),
        };

        return (settings, pattern);
    }
}
