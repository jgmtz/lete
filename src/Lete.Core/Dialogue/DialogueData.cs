using System.Text.Json;
using Lete.Core.Conditions;

namespace Lete.Core.Dialogue;

/// <summary>
/// Lê uma conversa (<c>content/dialogue/&lt;id&gt;.json</c>, DADOS.md 5): <c>id</c>, <c>mode</c> (shade | fixed | scene),
/// <c>speaker_key</c>, <c>shade</c> (id da sombra, para a oferenda), <c>lucidity</c> {<c>base</c>, <c>raise</c>
/// [{<c>to</c>, <c>when</c>}], <c>offering</c>} (só no modo sombra), <c>start</c> (nós de entrada, na ordem) e
/// <c>nodes</c>. Confere que todo <c>next</c> e <c>start</c> aponta para um nó que existe (<c>fim</c> encerra).
/// </summary>
public static class DialogueData
{
    public const string EndNode = "fim";

    private static readonly Dictionary<string, Posture> Postures = new(StringComparer.Ordinal)
    {
        ["escutar"] = Posture.Listen,
        ["perguntar"] = Posture.Ask,
        ["pressionar"] = Posture.Press,
        ["consolar"] = Posture.Console,
        ["calar"] = Posture.Silence,
    };

    private static readonly Dictionary<string, ShadeLucidity> Lucidities = new(StringComparer.Ordinal)
    {
        ["apagada"] = ShadeLucidity.Faded,
        ["vaga"] = ShadeLucidity.Vague,
        ["desperta"] = ShadeLucidity.Awake,
    };

    private static readonly Dictionary<string, DialogueMode> Modes = new(StringComparer.Ordinal)
    {
        ["shade"] = DialogueMode.Shade,
        ["fixed"] = DialogueMode.Fixed,
        ["scene"] = DialogueMode.Scene,
    };

    /// <summary>Os ids de lucidez do conteúdo (a condição <c>shade_lucidity</c> só aceita estes).</summary>
    public static IReadOnlyCollection<string> LucidityIds => Lucidities.Keys;

    public static string PostureId(Posture posture) => Postures.First(p => p.Value == posture).Key;

    public static string LucidityId(ShadeLucidity lucidity) => Lucidities.First(p => p.Value == lucidity).Key;

    public static DialogueGraph FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var mode = Lookup(Modes, String(root, "mode"), id, "mode");
        var shade = Optional(root, "shade");
        var nodes = root.GetProperty("nodes").EnumerateArray().Select(n => Node(id, shade, n)).ToList();
        if (nodes.Select(n => n.Id).Distinct().Count() != nodes.Count)
        {
            throw new FormatException($"Diálogo '{id}': ids de nó repetidos.");
        }

        var byId = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var start = root.GetProperty("start").EnumerateArray().Select(s => s.GetString()!).ToList();
        var targets = start.Concat(nodes.Select(n => n.Next)).Concat(nodes.SelectMany(n => n.Options.Select(o => o.Next)))
            .OfType<string>();
        if (start.Count == 0)
        {
            throw new FormatException($"Diálogo '{id}': falta 'start'.");
        }

        if (targets.FirstOrDefault(t => !byId.ContainsKey(t)) is { } missing)
        {
            throw new FormatException($"Diálogo '{id}': o nó '{missing}' não existe.");
        }

        if (mode == DialogueMode.Scene && nodes.Any(n => n.Options.Count > 0))
        {
            throw new FormatException($"Diálogo '{id}': uma cena não tem opções.");
        }

        var lucidity = root.TryGetProperty("lucidity", out var l) ? Lucidity(id, shade, l) : null;
        if ((mode == DialogueMode.Shade) != (lucidity is not null))
        {
            throw new FormatException($"Diálogo '{id}': 'lucidity' é só (e sempre) do modo 'shade'.");
        }

        return new DialogueGraph(id, mode, String(root, "speaker_key"), shade, lucidity, start, byId);
    }

    private static LucidityRules Lucidity(string id, string? shade, JsonElement l) => new(
        Lookup(Lucidities, String(l, "base"), id, "lucidity.base"),
        l.TryGetProperty("raise", out var raises)
            ? raises.EnumerateArray().Select((r, i) => new LucidityRaise(
                Lookup(Lucidities, String(r, "to"), id, $"lucidity.raise[{i}].to"),
                ConditionParser.Parse(r.GetProperty("when"), $"{id}.lucidity.raise[{i}].when"))).ToList()
            : new List<LucidityRaise>(),
        Optional(l, "offering"),
        l.TryGetProperty("share_effects", out var share)
            ? share.EnumerateArray().Select(e => Effect($"{id}.lucidity.share_effects", shade, e)).ToList()
            : new List<DialogueEffect>());

    private static DialogueNode Node(string dialogue, string? shade, JsonElement n)
    {
        var id = String(n, "id");
        var at = $"{dialogue}.{id}";
        var options = n.TryGetProperty("options", out var list)
            ? list.EnumerateArray().Select((o, i) => new DialogueOption(
                Lookup(Postures, String(o, "posture"), dialogue, $"{id}.options[{i}].posture"),
                String(o, "text_key"),
                Next(o),
                o.TryGetProperty("requires", out var requires) ? ConditionParser.Parse(requires, $"{at}.options[{i}].requires") : null,
                Optional(o, "hint_key"),
                o.TryGetProperty("show_blocked", out var show) && show.GetBoolean())).ToList()
            : new List<DialogueOption>();

        if (options.Any(o => o.ShowBlocked && o.HintKey is null))
        {
            throw new FormatException($"Diálogo '{dialogue}', nó '{id}': opção com 'show_blocked' precisa de 'hint_key'.");
        }

        return new DialogueNode(
            id,
            n.TryGetProperty("when", out var when) ? ConditionParser.Parse(when, $"{at}.when") : null,
            String(n, "line_key"),
            options,
            Next(n),
            n.TryGetProperty("effects", out var effects) ? effects.EnumerateArray().Select(e => Effect(at, shade, e)).ToList() : new List<DialogueEffect>(),
            n.TryGetProperty("comment", out var comment) ? Comment(comment) : null);
    }

    private static CompanionComment Comment(JsonElement c) => new(
        String(c, "id"),
        c.GetProperty("lines").EnumerateArray().ToDictionary(l => String(l, "companion"), l => String(l, "line_key"), StringComparer.Ordinal));

    /// <summary>
    /// Efeito: um objeto de uma chave só (<c>journal_key</c>, <c>flag</c>, <c>origin_fragment</c>, <c>hear</c>,
    /// <c>swear_oath</c>, <c>break_oath</c>, <c>hospitality</c>: true, que precisa de <c>shade</c> no diálogo).
    /// </summary>
    private static DialogueEffect Effect(string at, string? shade, JsonElement e)
    {
        var property = e.EnumerateObject().Single();
        return property.Name switch
        {
            "journal_key" => new AddJournalEntry(property.Value.GetString()!),
            "flag" => new SetFlag(property.Value.GetString()!),
            "origin_fragment" => new GainOriginFragment(property.Value.GetString()!),
            "hear" => new HearRecollectionSource(String(property.Value, "recollection"), String(property.Value, "source")),
            "swear_oath" => new SwearOath(property.Value.GetString()!),
            "break_oath" => new BreakOath(property.Value.GetString()!),
            "hospitality" => new OfferHospitality(shade ?? throw new FormatException($"{at}: 'hospitality' precisa de 'shade' no diálogo.")),
            var other => throw new FormatException($"{at}: efeito '{other}' desconhecido."),
        };
    }

    private static string? Next(JsonElement element) => Optional(element, "next") is { } next && next != EndNode ? next : null;

    private static T Lookup<T>(Dictionary<string, T> table, string value, string id, string field) =>
        table.TryGetValue(value, out var result)
            ? result
            : throw new FormatException($"Diálogo '{id}': '{field}' = '{value}' (use {string.Join(", ", table.Keys)}).");

    private static string? Optional(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string String(JsonElement element, string property) =>
        Optional(element, property) ?? throw new FormatException($"Campo de texto '{property}' ausente.");
}
