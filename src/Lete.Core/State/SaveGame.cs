using System.Text.Json;
using System.Text.Json.Nodes;
using Lete.Core.Combat;
using Lete.Core.Conditions;

namespace Lete.Core.State;

/// <summary>
/// Save em JSON legível e versionado (ARQUITETURA.md 4.3). Gravar é escrever o <see cref="WorldState"/>; ler aplica
/// as migrações das versões antigas, uma por vez, até a atual. Chaves em português, como os ids de conteúdo.
/// </summary>
public static class SaveGame
{
    public const int CurrentVersion = 1;

    /// <summary>Migrações por versão de origem: a de chave N transforma um save da versão N em N+1.</summary>
    public static readonly IReadOnlyDictionary<int, Action<JsonObject>> Migrations = new Dictionary<int, Action<JsonObject>>();

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static string Write(WorldState state)
    {
        var json = new JsonObject
        {
            ["versao"] = CurrentVersion,
            ["corpo"] = state.ProtagonistId,
            ["genero"] = state.Gender == Gender.Feminine ? "f" : "m",
            ["origem"] = state.OriginId,
            ["companheiro"] = state.CompanionId,
            ["marca"] = state.Mark,
            ["marca_etapa"] = state.MarkStage,
            ["rosto_final"] = FaceToText(state.FinalFace),
            ["mare"] = state.Tide,
            ["mare_marcos"] = Array(state.TideMilestones.OrderBy(m => m, StringComparer.Ordinal)),
            ["memorias_pesadas"] = new JsonObject(state.HeavyMemories
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => KeyValuePair.Create(p.Key, (JsonNode?)(p.Value == HeavyMemoryChoice.Face ? "encarou" : "deixou")))),
            ["lembrancas"] = new JsonObject(state.Recollections
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => KeyValuePair.Create(p.Key, (JsonNode?)new JsonObject
                {
                    ["fontes"] = Array(p.Value.Sources.OrderBy(s => s, StringComparer.Ordinal)),
                    ["ato_pleno"] = p.Value.FullAct,
                    ["falsa"] = p.Value.False,
                }))),
            ["perfil"] = new JsonObject
            {
                ["vias"] = Counts(state.Profile.ViaTotals),
                ["tipos"] = Counts(state.Profile.KindTotals),
                ["recentes"] = new JsonArray(state.Profile.Recent.Select(e => (JsonNode?)new JsonObject
                {
                    ["via"] = e.Via is { } via ? ConditionParser.SnakeCase(via) : null,
                    ["tipos"] = Array(e.Kinds.Select(ConditionParser.SnakeCase)),
                }).ToArray()),
            },
            ["bolsa"] = new JsonObject
            {
                ["obolos"] = state.Inventory.Obols,
                ["oferendas"] = Ints(state.Inventory.Offerings),
                ["provisoes"] = Ints(state.Inventory.Provisions),
            },
            ["vitais"] = new JsonObject
            {
                ["folego_gasto"] = state.Vitals.BreathSpent,
                ["lucidez_gasta"] = state.Vitals.LuciditySpent,
                ["curas"] = Ints(state.Vitals.HealUses),
            },
            ["dialogo"] = new JsonObject
            {
                ["escolhidas"] = Array(state.Dialogue.Chosen.OrderBy(c => c, StringComparer.Ordinal)),
                ["comentarios"] = Array(state.Dialogue.CommentsSeen.OrderBy(c => c, StringComparer.Ordinal)),
                ["despertas"] = Array(state.Dialogue.Awakened.OrderBy(c => c, StringComparer.Ordinal)),
            },
            ["juramentos"] = new JsonObject(state.Oaths
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => KeyValuePair.Create(p.Key, (JsonNode?)OathToText(p.Value)))),
            ["caminho"] = new JsonObject(state.PathKeys
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => KeyValuePair.Create(p.Key, (JsonNode?)p.Value))),
            ["fragmentos_origem"] = Array(state.OriginFragments),
            ["diario"] = Array(state.JournalEntries),
            ["regiao"] = state.RegionId,
            ["posicao"] = state.Position is { } p ? new JsonArray(p.X, p.Y, p.Z) : null,
            ["flags"] = Array(state.Flags.OrderBy(f => f, StringComparer.Ordinal)),
        };
        return json.ToJsonString(Indented);
    }

    public static WorldState Read(string text) => Read(text, Migrations, CurrentVersion);

    /// <summary>Lê com uma tabela de migrações e versão atual próprias (os testes usam para simular versões futuras).</summary>
    public static WorldState Read(string text, IReadOnlyDictionary<int, Action<JsonObject>> migrations, int currentVersion)
    {
        var json = JsonNode.Parse(text) as JsonObject ?? throw new FormatException("O save não é um objeto JSON.");
        var version = json["versao"]?.GetValue<int>() ?? throw new FormatException("O save não tem 'versao'.");
        if (version > currentVersion)
        {
            throw new FormatException($"Save da versão {version}, mais nova que a do jogo ({currentVersion}).");
        }

        for (; version < currentVersion; version++)
        {
            if (!migrations.TryGetValue(version, out var migrate))
            {
                throw new FormatException($"Falta a migração da versão {version} para {version + 1}.");
            }

            migrate(json);
            json["versao"] = version + 1;
        }

        var state = new WorldState
        {
            ProtagonistId = Text(json, "corpo"),
            Gender = Text(json, "genero") == "f" ? Gender.Feminine : Gender.Masculine,
            OriginId = Text(json, "origem"),
            CompanionId = Text(json, "companheiro"),
            RegionId = Text(json, "regiao"),
            Position = json["posicao"] switch
            {
                null => null,
                JsonArray { Count: 3 } p => new System.Numerics.Vector3(
                    p[0]!.GetValue<float>(), p[1]!.GetValue<float>(), p[2]!.GetValue<float>()),
                _ => throw new FormatException("'posicao' precisa ser [x, y, z]."),
            },
            Mark = json["marca"]?.GetValue<int>() ?? 0,
            MarkStage = json["marca_etapa"]?.GetValue<int>() ?? 0,
            Tide = json["mare"]?.GetValue<decimal>() ?? 0m,
            FinalFace = Text(json, "rosto_final") switch
            {
                null => FinalFace.None,
                "devolvido" => FinalFace.Returned,
                "coberto" => FinalFace.Covered,
                var other => throw new FormatException($"'rosto_final' = '{other}' desconhecido."),
            },
        };

        if (json["memorias_pesadas"] is JsonObject heavy)
        {
            foreach (var (id, choice) in heavy)
            {
                state.HeavyMemories[id] = choice?.GetValue<string>() switch
                {
                    "encarou" => HeavyMemoryChoice.Face,
                    "deixou" => HeavyMemoryChoice.Leave,
                    var other => throw new FormatException($"Memória Pesada '{id}' com estado '{other}' desconhecido."),
                };
            }
        }

        state.TideMilestones.UnionWith(Strings(json, "mare_marcos"));
        if (json["lembrancas"] is JsonObject recollections)
        {
            foreach (var (id, node) in recollections)
            {
                var entry = node as JsonObject ?? throw new FormatException($"Lembrança '{id}' precisa ser um objeto.");
                var progress = new RecollectionProgress
                {
                    FullAct = Text(entry, "ato_pleno"),
                    False = entry["falsa"]?.GetValue<bool>() ?? false,
                };
                progress.Sources.UnionWith(Strings(entry, "fontes"));
                state.Recollections[id] = progress;
            }
        }

        if (json["perfil"] is JsonObject profile)
        {
            ReadCounts(profile, "vias", state.Profile.ViaTotals);
            ReadCounts(profile, "tipos", state.Profile.KindTotals);
            if (profile["recentes"] is JsonArray recent)
            {
                state.Profile.Recent.AddRange(recent.OfType<JsonObject>().Select(e => new ProfileEncounter(
                    Text(e, "via") is { } via ? EnumOf<Via>(via) : null,
                    Strings(e, "tipos").Select(EnumOf<RecollectionKind>).ToList())));
            }
        }

        if (json["bolsa"] is JsonObject bag)
        {
            state.Inventory.Obols = bag["obolos"]?.GetValue<int>() ?? 0;
            ReadInts(bag, "oferendas", state.Inventory.Offerings);
            ReadInts(bag, "provisoes", state.Inventory.Provisions);
        }

        if (json["vitais"] is JsonObject vitals)
        {
            state.Vitals.BreathSpent = vitals["folego_gasto"]?.GetValue<float>() ?? 0f;
            state.Vitals.LuciditySpent = vitals["lucidez_gasta"]?.GetValue<float>() ?? 0f;
            ReadInts(vitals, "curas", state.Vitals.HealUses);
        }

        if (json["dialogo"] is JsonObject dialogue)
        {
            state.Dialogue.Chosen.UnionWith(Strings(dialogue, "escolhidas"));
            state.Dialogue.CommentsSeen.UnionWith(Strings(dialogue, "comentarios"));
            state.Dialogue.Awakened.UnionWith(Strings(dialogue, "despertas"));
        }

        if (json["juramentos"] is JsonObject oaths)
        {
            foreach (var (id, status) in oaths)
            {
                state.Oaths[id] = status?.GetValue<string>() switch
                {
                    "ativo" => OathStatus.Active,
                    "quebrado" => OathStatus.Broken,
                    "cumprido" => OathStatus.Kept,
                    var other => throw new FormatException($"Juramento '{id}' com estado '{other}' desconhecido."),
                };
            }
        }

        if (json["caminho"] is JsonObject path)
        {
            foreach (var (key, way) in path)
            {
                state.PathKeys[key] = way?.GetValue<string>() ?? throw new FormatException($"Chave do Caminho '{key}' sem forma.");
            }
        }

        state.OriginFragments.AddRange(Strings(json, "fragmentos_origem"));
        state.JournalEntries.AddRange(Strings(json, "diario"));
        state.Flags.UnionWith(Strings(json, "flags"));
        return state;
    }

    private static string OathToText(OathStatus status) => status switch
    {
        OathStatus.Active => "ativo",
        OathStatus.Broken => "quebrado",
        _ => "cumprido",
    };

    private static string? FaceToText(FinalFace face) => face switch
    {
        FinalFace.Returned => "devolvido",
        FinalFace.Covered => "coberto",
        _ => null,
    };

    private static JsonObject Ints(Dictionary<string, int> values) => new(values
        .OrderBy(p => p.Key, StringComparer.Ordinal)
        .Select(p => KeyValuePair.Create(p.Key, (JsonNode?)p.Value)));

    private static void ReadInts(JsonObject json, string key, Dictionary<string, int> into)
    {
        if (json[key] is JsonObject values)
        {
            foreach (var (name, value) in values)
            {
                into[name] = value?.GetValue<int>() ?? 0;
            }
        }
    }

    private static JsonObject Counts<T>(Dictionary<T, int> counts) where T : struct, Enum => new(counts
        .OrderBy(p => p.Key)
        .Select(p => KeyValuePair.Create(ConditionParser.SnakeCase(p.Key), (JsonNode?)p.Value)));

    private static void ReadCounts<T>(JsonObject json, string key, Dictionary<T, int> into) where T : struct, Enum
    {
        if (json[key] is JsonObject counts)
        {
            foreach (var (name, value) in counts)
            {
                into[EnumOf<T>(name)] = value?.GetValue<int>() ?? 0;
            }
        }
    }

    /// <summary>snake_case do save (<c>free</c>, <c>childhood</c>) → membro do enum.</summary>
    private static T EnumOf<T>(string text) where T : struct, Enum
    {
        foreach (var value in System.Enum.GetValues<T>())
        {
            if (ConditionParser.SnakeCase(value) == text)
            {
                return value;
            }
        }

        throw new FormatException($"'{text}' não é um {typeof(T).Name} conhecido.");
    }

    private static JsonArray Array(IEnumerable<string> items) => new(items.Select(i => (JsonNode?)i).ToArray());

    private static string? Text(JsonObject json, string key) => json[key]?.GetValue<string>();

    private static IEnumerable<string> Strings(JsonObject json, string key) =>
        json[key] is JsonArray list ? list.Select(i => i?.GetValue<string>() ?? "") : Enumerable.Empty<string>();
}
