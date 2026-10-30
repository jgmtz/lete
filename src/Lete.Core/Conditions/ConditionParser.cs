using Lete.Core.Recollections;
using Lete.Core.Combat;
using System.Text.Json;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Conditions;

/// <summary>
/// Lê uma condição do JSON (DADOS.md 1). Erros dizem o caminho do campo (ex.: <c>requires.all[1].mark_band.min</c>).
/// Um predicado é um objeto com uma chave só (o nome) e, opcionalmente, <c>"soft": true</c>.
/// </summary>
public static class ConditionParser
{
    public const string SoftField = "soft";

    /// <summary>Nomes de todos os predicados conhecidos (os da lista de DADOS.md 1 e <c>shade_lucidity</c> dos diálogos).</summary>
    public static readonly IReadOnlySet<string> PredicateNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "origin", "gender", "recollection", "recollection_kind_full_count", "heavy_memory", "mark_band", "tide_min",
        "flag", "path_key", "oath_active", "oath_broken", "offering_shared", "shade_state", "companion", "bond_min",
        "difficulty", "profile", "item_owned", "trivia_lit", "region_visited", "quest_stage", "shade_lucidity",
    };

    public static Condition Parse(string json, string path = "condition")
    {
        using var doc = JsonDocument.Parse(json);
        return Parse(doc.RootElement, path);
    }

    public static Condition Parse(JsonElement element, string path) => Parse(element, path, insideNot: false);

    private static Condition Parse(JsonElement element, string path, bool insideNot)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Error(path, "uma condição precisa ser um objeto.");
        }

        var soft = false;
        JsonProperty? only = null;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name == SoftField)
            {
                if (property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    throw Error($"{path}.{SoftField}", "precisa ser true ou false.");
                }

                soft = property.Value.GetBoolean();
                continue;
            }

            if (only is not null)
            {
                throw Error(path, $"tem mais de uma chave ('{only.Value.Name}' e '{property.Name}'); use 'all' ou 'any'.");
            }

            only = property;
        }

        if (only is not { } entry)
        {
            throw Error(path, "condição vazia.");
        }

        var name = entry.Name;
        var value = entry.Value;
        var at = $"{path}.{name}";
        switch (name)
        {
            case "all":
            case "any":
                if (soft)
                {
                    throw Error(path, "'soft' só vale em predicados, não em 'all'/'any'.");
                }

                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
                {
                    throw Error(at, "precisa ser uma lista com pelo menos uma condição.");
                }

                var items = value.EnumerateArray().Select((item, i) => Parse(item, $"{at}[{i}]", insideNot)).ToList();
                return name == "all" ? new AllOf(items) : new AnyOf(items);

            case "not":
                if (soft)
                {
                    throw Error(path, "'soft' só vale em predicados, não em 'not'.");
                }

                return new NotOf(Parse(value, at, insideNot: true));
        }

        if (!PredicateNames.Contains(name))
        {
            throw Error(at, $"predicado desconhecido '{name}'.");
        }

        if (soft && insideNot)
        {
            throw Error(path, "um predicado 'soft' não pode ficar dentro de 'not' (a dica ficaria ao contrário).");
        }

        var predicate = ParsePredicate(name, value, at);
        return soft ? predicate with { Soft = true } : predicate;
    }

    private static Predicate ParsePredicate(string name, JsonElement value, string at) => name switch
    {
        "origin" => new OriginIs(Id(value, at)),
        "gender" => new GenderIs(Id(value, at) switch
        {
            "m" => Gender.Masculine,
            "f" => Gender.Feminine,
            var other => throw Error(at, $"'{other}' não é 'm' nem 'f'."),
        }),
        "recollection" => new RecollectionAt(
            Id(Field(value, at, "id"), $"{at}.id"),
            OptionalEnum<RecollectionStage>(value, at, "min_stage"),
            OptionalEnum<RecollectionCertainty>(value, at, "state")),
        "recollection_kind_full_count" => new RecollectionKindFullCount(Count(Field(value, at, "min"), $"{at}.min")),
        "heavy_memory" => new HeavyMemoryIs(
            Id(Field(value, at, "id"), $"{at}.id"),
            Enum<HeavyMemoryState>(Field(value, at, "state"), $"{at}.state")),
        "mark_band" => ParseMarkBand(value, at),
        "tide_min" => new TideMin(Count(value, at)),
        "flag" => new FlagSet(Id(value, at)),
        "path_key" => new PathKeyOwned(Id(value, at)),
        "oath_active" => new OathIsActive(Id(value, at)),
        "oath_broken" => new OathIsBroken(Id(value, at)),
        "offering_shared" => new OfferingWasShared(
            Id(Field(value, at, "shade"), $"{at}.shade"),
            value.TryGetProperty("kind", out var kind) ? Id(kind, $"{at}.kind") : null),
        "shade_state" => new ShadeIs(
            Id(Field(value, at, "id"), $"{at}.id"),
            Enum<ShadeState>(Field(value, at, "state"), $"{at}.state")),
        "companion" => new CompanionIs(Id(value, at)),
        "bond_min" => new BondMin(Count(value, at)),
        "difficulty" => new DifficultyIs(Id(value, at)),
        "profile" => ParseProfile(value, at),
        "item_owned" => new ItemOwned(Id(value, at)),
        "trivia_lit" => new TriviaIsLit(Id(value, at)),
        "region_visited" => new RegionVisited(Id(value, at)),
        "quest_stage" => new QuestStageMin(
            Id(Field(value, at, "id"), $"{at}.id"),
            Count(Field(value, at, "min"), $"{at}.min")),
        "shade_lucidity" => Lete.Core.Dialogue.DialogueData.LucidityIds.Contains(Id(value, at))
            ? new ShadeLucidityIs(Id(value, at))
            : throw Error(at, $"use {string.Join(", ", Lete.Core.Dialogue.DialogueData.LucidityIds)}."),
        _ => throw Error(at, $"predicado desconhecido '{name}'."),
    };

    private static MarkBandIn ParseMarkBand(JsonElement value, string at)
    {
        var band = new MarkBandIn(OptionalEnum<MarkBand>(value, at, "min"), OptionalEnum<MarkBand>(value, at, "max"));
        if (band.Min is null && band.Max is null)
        {
            throw Error(at, "precisa de 'min' e/ou 'max' (low, mid, high).");
        }

        return band.Min > band.Max ? throw Error(at, "'min' maior que 'max'.") : band;
    }

    private static ProfileIs ParseProfile(JsonElement value, string at)
    {
        RequireObject(value, at);
        var profile = new ProfileIs(
            OptionalEnum<Via>(value, at, "top_via"),
            OptionalEnum<RecollectionKind>(value, at, "top_kind"));
        return profile.TopVia is null && profile.TopKind is null
            ? throw Error(at, "precisa de 'top_via' e/ou 'top_kind'.")
            : profile;
    }

    private static JsonElement Field(JsonElement value, string at, string field)
    {
        RequireObject(value, at);
        return value.TryGetProperty(field, out var inner) ? inner : throw Error(at, $"falta o campo '{field}'.");
    }

    private static void RequireObject(JsonElement value, string at)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw Error(at, "precisa ser um objeto.");
        }
    }

    private static string Id(JsonElement value, string at) =>
        value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } id
            ? id
            : throw Error(at, "precisa ser um id (texto não vazio).");

    private static int Count(JsonElement value, string at) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number >= 0
            ? number
            : throw Error(at, "precisa ser um número inteiro >= 0.");

    private static T? OptionalEnum<T>(JsonElement value, string at, string field) where T : struct, Enum
    {
        RequireObject(value, at);
        return value.TryGetProperty(field, out var inner) ? Enum<T>(inner, $"{at}.{field}") : null;
    }

    /// <summary>Valores em snake_case no JSON (<c>min_stage: "recalled"</c>) → membro do enum (<c>Recalled</c>).</summary>
    private static T Enum<T>(JsonElement value, string at) where T : struct, Enum
    {
        var text = Id(value, at);
        var names = System.Enum.GetValues<T>().ToDictionary(SnakeCase, v => v, StringComparer.Ordinal);
        return names.TryGetValue(text, out var parsed)
            ? parsed
            : throw Error(at, $"'{text}' não é um de: {string.Join(", ", names.Keys)}.");
    }

    public static string SnakeCase<T>(T value) where T : struct, Enum =>
        string.Concat(value.ToString().Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    private static ConditionException Error(string path, string message) => new(path, message);
}

/// <summary>Condição mal escrita no conteúdo, com o caminho do campo.</summary>
public sealed class ConditionException : FormatException
{
    public ConditionException(string path, string message) : base($"{path}: {message}")
    {
        Path = path;
    }

    public string Path { get; }
}
