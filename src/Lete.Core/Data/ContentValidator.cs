using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Conditions;

namespace Lete.Core.Data;

/// <summary>Um problema no conteúdo: arquivo (relativo a <c>content/</c>), campo (caminho JSON) e mensagem.</summary>
public sealed record ContentIssue(string File, string Field, string Message)
{
    public override string ToString() => Field.Length == 0 ? $"{File}: {Message}" : $"{File} → {Field}: {Message}";
}

public sealed record ContentReport(IReadOnlyList<ContentIssue> Errors, IReadOnlyList<ContentIssue> Warnings)
{
    public bool Ok => Errors.Count == 0;
}

/// <summary>Um campo que cita outro arquivo de conteúdo: em <c>SourceFolder</c>, o valor de <c>Field</c> é um id de <c>TargetFolder</c>.</summary>
public sealed record ReferenceRule(string SourceFolder, string Field, string TargetFolder);

/// <summary>
/// Validação do conteúdo inteiro no carregamento (DADOS.md 9). Falha com arquivo, campo e mensagem quando:
/// o JSON não lê; uma chave de texto (<c>*_key</c>, <c>*_keys</c>) não existe no idioma padrão; uma condição
/// (<see cref="ConditionFields"/>) está mal escrita ou usa predicado desconhecido; um id citado não existe
/// (<see cref="References"/> e os ids das condições). Valores <c>_tbd</c> são aviso em desenvolvimento e erro em release.
/// As regras de chefe (saída sem dano total) e de sombra comum (<c>base_body</c>) entram quando esses dados existirem.
/// </summary>
public static class ContentValidator
{
    /// <summary>Campos que guardam uma condição (DADOS.md 1, 3, 5, 8).</summary>
    public static readonly IReadOnlySet<string> ConditionFields =
        new HashSet<string>(StringComparer.Ordinal) { "requires", "when", "gate", "hook", "condition" };

    /// <summary>Referências entre arquivos que o jogo segue ao carregar (só campos com esse significado em toda a pasta).</summary>
    public static readonly IReadOnlyList<ReferenceRule> References = new[]
    {
        new ReferenceRule("battle", "shade", "shades"),
        new ReferenceRule("battle", "companion", "companions"),
        new ReferenceRule("shades", "gesture", "gestures"),
        new ReferenceRule("recollections", "shade", "shades"),
        new ReferenceRule("items", "set", "item_sets"),
        new ReferenceRule("items", "unlocks_path", "path"),
        new ReferenceRule("merchants", "item", "items"),
        new ReferenceRule("encounters", "shade", "shades"),
        new ReferenceRule("encounters", "region", "regions"),
        new ReferenceRule("encounters", "battle", "battle"),
        new ReferenceRule("encounters", "after", "encounters"),
        new ReferenceRule("trivias", "dialogue", "dialogue"),
    };

    private const string LocaleFolder = "locale";

    /// <param name="files">Caminho relativo a <c>content/</c> (com <c>/</c>) → texto JSON. Os de <c>locale/</c> são ignorados.</param>
    /// <param name="localeKeys">Chaves do idioma padrão (pt_BR).</param>
    /// <param name="release">Em release, <c>_tbd</c> vira erro.</param>
    public static ContentReport Validate(IReadOnlyDictionary<string, string> files, IReadOnlySet<string> localeKeys, bool release)
    {
        var errors = new List<ContentIssue>();
        var warnings = new List<ContentIssue>();
        var documents = new Dictionary<string, JsonDocument>(StringComparer.Ordinal);
        try
        {
            foreach (var (file, text) in files.Where(f => Folder(f.Key) != LocaleFolder).OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                try
                {
                    documents[file] = JsonDocument.Parse(text);
                }
                catch (JsonException e)
                {
                    errors.Add(new ContentIssue(file, "", $"JSON inválido: {e.Message}"));
                }
            }

            var known = KnownIds(files.Keys, documents, errors);
            foreach (var (file, doc) in documents)
            {
                Walk(file, doc.RootElement, "", localeKeys, known, release, errors, warnings);
            }
        }
        finally
        {
            foreach (var doc in documents.Values)
            {
                doc.Dispose();
            }
        }

        return new ContentReport(errors, warnings);
    }

    /// <summary>Ids existentes por destino: um por arquivo de cada pasta, mais as origens e os companheiros do elenco.</summary>
    private static Dictionary<string, HashSet<string>> KnownIds(
        IEnumerable<string> files, IReadOnlyDictionary<string, JsonDocument> documents, List<ContentIssue> errors)
    {
        var known = files
            .GroupBy(Folder)
            .ToDictionary(g => g.Key, g => g.Select(f => Path.GetFileNameWithoutExtension(f)).ToHashSet(StringComparer.Ordinal));

        Add(ConditionTargets.Origins, "characters/origens.json", json => OriginData.FromJson(json).Select(o => o.Id));
        Add(ConditionTargets.Companions, "characters/elenco.json",
            json => CastData.FromJson(json).Values.Where(m => m.Kind == CastKind.Companion).Select(m => m.Id));
        return known;

        void Add(string target, string file, Func<string, IEnumerable<string>> read)
        {
            if (!documents.TryGetValue(file, out var doc))
            {
                return;
            }

            try
            {
                known[target] = read(doc.RootElement.GetRawText()).ToHashSet(StringComparer.Ordinal);
            }
            catch (Exception e) when (e is FormatException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                errors.Add(new ContentIssue(file, "", e.Message));
            }
        }
    }

    private static void Walk(
        string file, JsonElement element, string path, IReadOnlySet<string> localeKeys,
        IReadOnlyDictionary<string, HashSet<string>> known, bool release,
        List<ContentIssue> errors, List<ContentIssue> warnings)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var item in element.EnumerateArray())
            {
                Walk(file, item, $"{path}[{i++}]", localeKeys, known, release, errors, warnings);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (element.TryGetProperty("_tbd", out var tbd) && tbd.ValueKind == JsonValueKind.True)
        {
            (release ? errors : warnings).Add(new ContentIssue(file, path, "valor provisório (_tbd)."));
        }

        foreach (var property in element.EnumerateObject())
        {
            var at = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
            var value = property.Value;

            if (property.Name.EndsWith("_key", StringComparison.Ordinal) && value.ValueKind == JsonValueKind.String)
            {
                CheckKey(value.GetString()!, at);
            }
            else if (property.Name.EndsWith("_keys", StringComparison.Ordinal) && value.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var key in value.EnumerateArray())
                {
                    CheckKey(key.GetString() ?? "", $"{at}[{i++}]");
                }
            }
            else if (ConditionFields.Contains(property.Name))
            {
                CheckCondition(value, at);
            }
            else
            {
                if (value.ValueKind == JsonValueKind.String &&
                    References.FirstOrDefault(r => r.SourceFolder == Folder(file) && r.Field == property.Name) is { } rule)
                {
                    CheckId(rule.TargetFolder, value.GetString()!, at);
                }

                Walk(file, value, at, localeKeys, known, release, errors, warnings);
            }
        }

        void CheckKey(string key, string at)
        {
            if (!localeKeys.Contains(key))
            {
                errors.Add(new ContentIssue(file, at, $"chave de texto '{key}' não existe em pt_BR."));
            }
        }

        void CheckCondition(JsonElement value, string at)
        {
            Condition condition;
            try
            {
                condition = ConditionParser.Parse(value, at);
            }
            catch (ConditionException e)
            {
                errors.Add(new ContentIssue(file, e.Path, e.Message[(e.Path.Length + 2)..]));
                return;
            }

            foreach (var (target, id) in Predicates(condition).SelectMany(p => p.References()))
            {
                CheckId(target, id, at);
            }
        }

        void CheckId(string target, string id, string at)
        {
            if (!known.TryGetValue(target, out var ids) || !ids.Contains(id))
            {
                errors.Add(new ContentIssue(file, at, $"'{id}' não existe em {target}."));
            }
        }
    }

    private static IEnumerable<Predicate> Predicates(Condition condition) => condition switch
    {
        AllOf all => all.Items.SelectMany(Predicates),
        AnyOf any => any.Items.SelectMany(Predicates),
        NotOf not => Predicates(not.Inner),
        Predicate p => new[] { p },
        _ => Enumerable.Empty<Predicate>(),
    };

    private static string Folder(string file)
    {
        var slash = file.IndexOf('/');
        return slash < 0 ? "" : file[..slash];
    }
}
