using System.Text.Json;
using Lete.Core.Combat;

namespace Lete.Core.Data;

/// <summary>Carrega a escuta e a libertação de uma sombra (<c>content/shades/&lt;id&gt;.json</c>).</summary>
public static class ShadeData
{
    /// <summary>Campos <c>listen</c> (o que ela murmura em cada golpe do ciclo) e <c>murmur_key</c>.</summary>
    public static (IReadOnlyList<ListenEntry> Entries, string MurmurKey) ListeningFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var cycleLength = Required(root, "attacks").GetArrayLength();

        var entries = Required(root, "listen").EnumerateArray()
            .Select(e => new ListenEntry(
                (int)TbdNumber.Read(e, "cycle_step"),
                String(e, "reveals") switch
                {
                    "clue" => ListenReveal.Clue,
                    "name" => ListenReveal.Name,
                    "fixation" => ListenReveal.Fixation,
                    var other => throw new FormatException($"Escuta revela '{other}', que não existe (use clue, name ou fixation)."),
                },
                String(e, "text_key"),
                String(e, "partial_key")))
            .ToList();

        var errors = new List<string>();
        if (entries.Any(e => e.CycleStep < 0 || e.CycleStep >= cycleLength))
        {
            errors.Add($"Escuta num golpe fora do ciclo (o ciclo tem {cycleLength} golpes, contando do 0).");
        }

        if (entries.GroupBy(e => e.CycleStep).Any(g => g.Count() > 1))
        {
            errors.Add("Dois murmúrios no mesmo golpe do ciclo.");
        }

        foreach (var reveal in new[] { ListenReveal.Name, ListenReveal.Fixation })
        {
            if (entries.Count(e => e.Reveals == reveal) != 1)
            {
                errors.Add($"É preciso exatamente uma escuta que revele {reveal}.");
            }
        }

        return errors.Count == 0
            ? (entries, String(root, "murmur_key"))
            : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Campos <c>fixation</c> (a verdadeira e as opções) e <c>liberation</c>.</summary>
    public static LiberationRules LiberationFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var fixation = Required(root, "fixation");
        var liberation = Required(root, "liberation");
        var gain = Required(liberation, "peace_gain");

        var rules = new LiberationRules(
            String(fixation, "id"),
            Required(fixation, "options").EnumerateArray()
                .Select(o => new FixationOption(String(o, "id"), String(o, "label_key"), OptionalString(o, "wrong_key") ?? ""))
                .ToList(),
            TbdNumber.Read(liberation, "peace"),
            new GradeValues(TbdNumber.Read(gain, "perfect"), TbdNumber.Read(gain, "good"), TbdNumber.Read(gain, "failed")),
            TbdNumber.Read(liberation, "lucidity_cost"),
            TbdNumber.Read(liberation, "enraged_damage_multiplier"),
            String(liberation, "progress_key"),
            String(liberation, "freed_key"));

        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Campos <c>absorb</c> (quando e quanto pesa absorver) e <c>echo</c> (o Eco que a sombra deixa).</summary>
    public static AbsorbRules AbsorbFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var absorb = Required(root, "absorb");
        var echo = Required(root, "echo");

        var rules = new AbsorbRules(
            TbdNumber.Read(absorb, "resolve_fraction_max"),
            TbdNumber.Read(absorb, "weak_fill_factor"),
            Grades(Required(absorb, "mark_gain")),
            new EchoDefinition(
                String(echo, "id"),
                String(echo, "name_key"),
                String(echo, "description_key"),
                String(echo, "gesture"),
                Grades(Required(echo, "resolve_damage")),
                (int)TbdNumber.Read(echo, "mark_on_use")));

        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>
    /// Campos narrativos: <c>voices</c>, <c>phases</c>, <c>story_keys</c> (Presenciar), <c>flee_clue_key</c>,
    /// <c>yield</c> (falas e custo), <c>defeat_clue_keys</c> (pistas de Hécate), <c>spare_key</c> e <c>offering</c> (opcional).
    /// </summary>
    public static ShadeStory StoryFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var yield = Required(root, "yield");

        var story = new ShadeStory(
            Required(root, "voices").EnumerateArray()
                .Select(v => new ShadeVoice(TbdNumber.Read(v, "resolve_fraction_below"), String(v, "text_key")))
                .ToList(),
            Required(root, "phases").EnumerateArray()
                .Select(p => new ShadePhase(
                    TbdNumber.Read(p, "resolve_fraction_below"),
                    CombatData.ReadAttacks(Required(p, "attacks")),
                    String(p, "caption_key"),
                    OptionalString(p, "reveals") switch
                    {
                        null => null,
                        "name" => ListenReveal.Name,
                        "fixation" => ListenReveal.Fixation,
                        var other => throw new FormatException($"Fase revela '{other}', que não existe (use name ou fixation)."),
                    }))
                .ToList(),
            Keys(root, "story_keys"),
            String(root, "flee_clue_key"),
            Keys(yield, "text_keys"),
            TbdNumber.Read(yield, "lucidity_cost"),
            Keys(root, "defeat_clue_keys"),
            String(root, "spare_key"),
            root.TryGetProperty("offering", out var offering)
                ? new ShadeOffering(String(offering, "accepts"), String(offering, "accept_key"), String(offering, "refuse_key"))
                : null);

        var errors = story.Validate().ToList();
        return errors.Count == 0 ? story : throw new FormatException(string.Join(" ", errors));
    }

    private static IReadOnlyList<string> Keys(JsonElement element, string property) =>
        Required(element, property).EnumerateArray().Select(k => k.GetString() ?? "").ToList();

    private static GradeValues Grades(JsonElement e) =>
        new(TbdNumber.Read(e, "perfect"), TbdNumber.Read(e, "good"), TbdNumber.Read(e, "failed"));

    private static JsonElement Required(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value : throw new FormatException($"Campo '{property}' ausente.");

    private static string? OptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
