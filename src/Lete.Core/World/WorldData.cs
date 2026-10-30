using System.Text.Json;
using Lete.Core.Data;

namespace Lete.Core.World;

/// <summary>
/// Lê mercadores (<c>content/merchants</c>), Trívias (<c>content/trivias</c>), julgamentos (<c>content/trials</c>) e as
/// regras deles (<c>content/rules/mercadores.json</c>, <c>tribunal.json</c>).
/// </summary>
public static class WorldData
{
    private static readonly Dictionary<string, ShadeDestination> Destinations = new(StringComparer.Ordinal)
    {
        ["asfodelos"] = ShadeDestination.Asphodel,
        ["elisios"] = ShadeDestination.Elysium,
        ["tartaro"] = ShadeDestination.Tartarus,
    };

    /// <summary>Mercador: <c>id</c>, <c>name_key</c>, <c>greeting_key</c> e <c>stock</c> [{<c>item</c> | <c>offering</c> | <c>provision</c>, <c>price</c>, <c>count</c>, <c>tags</c>}].</summary>
    public static Merchant MerchantFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var stock = root.GetProperty("stock").EnumerateArray().Select((w, i) =>
        {
            var (kind, ware) = w.TryGetProperty("item", out var item) ? (WareKind.Item, item.GetString()!)
                : w.TryGetProperty("offering", out var offering) ? (WareKind.Offering, offering.GetString()!)
                : w.TryGetProperty("provision", out var provision) ? (WareKind.Provision, provision.GetString()!)
                : throw new FormatException($"Mercador '{id}', stock[{i}]: diga 'item', 'offering' ou 'provision'.");
            var count = kind == WareKind.Item ? 1 : (int)TbdNumber.Read(w, "count");
            var tags = w.TryGetProperty("tags", out var t) ? t.EnumerateArray().Select(x => x.GetString()!).ToList() : new List<string>();
            return new Ware(kind, ware, (int)TbdNumber.Read(w, "price"), count, tags);
        }).ToList();

        if (stock.Any(w => w.Price < 0 || w.Count < 1) || stock.Select(w => w.Key).Distinct().Count() != stock.Count)
        {
            throw new FormatException($"Mercador '{id}': preço negativo, quantidade zero ou entrada repetida.");
        }

        return new Merchant(id, String(root, "name_key"), String(root, "greeting_key"), stock);
    }

    public static MerchantRules MerchantRulesFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rules = new MerchantRules(TbdNumber.Read(doc.RootElement, "off_path_price_factor"));
        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Trívia: <c>id</c>, <c>region</c>, <c>name_key</c>, <c>torches</c> (até 3) e <c>hub</c> (a de Asfódelos).</summary>
    public static Trivia TriviaFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var torches = root.TryGetProperty("torches", out var t) ? t.EnumerateArray().Select(x => x.GetString()!).ToList() : new List<string>();
        if (torches.Count > 3 || torches.Contains(id))
        {
            throw new FormatException($"Trívia '{id}': até três tochas, nenhuma para ela mesma.");
        }

        return new Trivia(id, String(root, "region"), String(root, "name_key"), torches,
            root.TryGetProperty("hub", out var hub) && hub.GetBoolean());
    }

    /// <summary>
    /// Julgamento: <c>id</c>, <c>judge_key</c>, <c>shade</c>, <c>intro_key</c>, <c>slots</c>, <c>evidence</c>
    /// [{<c>journal</c> | <c>recollection</c>, <c>weight</c>}], <c>perjury_if_uncertain</c> (Lembranças) e
    /// <c>verdicts</c> [{<c>destination</c>, <c>min</c>, <c>text_key</c>}] do melhor ao pior (o último sem <c>min</c>).
    /// </summary>
    public static Trial TrialFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var weights = root.GetProperty("evidence").EnumerateArray().Select(e => new WeightedEvidence(
            e.TryGetProperty("journal", out var journal) ? new Evidence(EvidenceKind.Journal, journal.GetString()!)
                : new Evidence(EvidenceKind.Recollection, String(e, "recollection")),
            (int)TbdNumber.Read(e, "weight"))).ToList();
        var verdicts = root.GetProperty("verdicts").EnumerateArray().Select(v => new Verdict(
            Destinations.TryGetValue(String(v, "destination"), out var d) ? d : throw new FormatException($"Julgamento '{id}': destino desconhecido (use asfodelos, elisios, tartaro)."),
            v.TryGetProperty("min", out _) ? (int)TbdNumber.Read(v, "min") : null,
            String(v, "text_key"))).ToList();

        if (verdicts.Count == 0 || verdicts[^1].Min is not null || verdicts.SkipLast(1).Any(v => v.Min is null)
            || verdicts.SkipLast(1).Zip(verdicts.Skip(1).SkipLast(1)).Any(p => p.Second.Min >= p.First.Min))
        {
            throw new FormatException($"Julgamento '{id}': vereditos do maior mínimo ao menor, e só o último sem 'min'.");
        }

        return new Trial(id, String(root, "judge_key"), String(root, "shade"), String(root, "intro_key"),
            (int)TbdNumber.Read(root, "slots"), weights,
            root.TryGetProperty("perjury_if_uncertain", out var p) ? p.EnumerateArray().Select(x => x.GetString()!).ToList() : new List<string>(),
            verdicts);
    }

    public static TribunalRules TribunalRulesFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return new TribunalRules((int)TbdNumber.Read(doc.RootElement, "defend_bonus"), (int)TbdNumber.Read(doc.RootElement, "accuse_bonus"));
    }

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
