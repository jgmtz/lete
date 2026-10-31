using System.Text.RegularExpressions;
using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Data;
using Lete.Core.Events;
using Lete.Core.Items;
using Lete.Core.Localization;
using Lete.Core.State;

namespace Lete.Core.Tests;

/// <summary>O idioma do jogo e a conferência dos arquivos de idioma (ARQUITETURA 7).</summary>
public class LocaleTests
{
    [Fact]
    public void Localizer_TranslationFallsBackToTheStandardLanguage()
    {
        var standard = Localizer.FromJson("""{ "a": "Cansad{o|a}", "b": "Só em português" }""");
        var english = Localizer.FromJson("""{ "a": "Tired" }""", standard);

        Assert.Equal("Tired", english.Get("a", Gender.Feminine));
        Assert.Equal("Só em português", english.Get("b", Gender.Feminine));
        Assert.True(english.Has("b"));
        Assert.Equal(new[] { "a", "b" }, english.Keys.OrderBy(k => k));
    }

    [Theory]
    [InlineData("Pront{o|a}, {0}.", 0)]
    [InlineData("Senhor{|a}", 0)]
    [InlineData("Sem nada", 0)]
    [InlineData("{o|a|e} três formas", 1)]
    [InlineData("Uma ânfora {rachada}.", 1)]
    [InlineData("Solta } aqui", 1)]
    [InlineData("Aninhada {a{o|a}}", 1)]
    [InlineData("  ", 1)]
    public void Validator_AcceptsPlaceholdersAndTwoFormMarks(string text, int problems)
    {
        Assert.Equal(problems, LocaleValidator.Problems(text).Count());
    }

    [Fact]
    public void Validator_ComparesATranslationWithTheStandard()
    {
        var standard = new Dictionary<string, string> { ["a"] = "{0} de {1}", ["b"] = "b", ["c"] = "c" };
        var translated = new Dictionary<string, string> { ["a"] = "{0} of", ["b"] = "b", ["x"] = "x" };

        Assert.Equal(new[] { "c: falta a tradução", "x: chave que o idioma padrão não tem", "a: lugares de valor diferentes do padrão" },
            LocaleValidator.Compare(standard, translated));
    }

    // ---- O idioma do jogo ----

    private static readonly IReadOnlyDictionary<string, string> Strings =
        Localizer.Read(File.ReadAllText(Path.Combine(RepoRoot(), "content", "locale", "pt_BR.json")));

    [Fact]
    public void Game_EveryTextIsWellFormed()
    {
        Assert.Empty(LocaleValidator.Validate(Strings));
    }

    /// <summary>Textos entre aspas que parecem chave mas não são (nome de campo, arquivo de opções).</summary>
    private static readonly HashSet<string> NotKeys = new(StringComparer.Ordinal) { "lucidity.base", "opcoes.cfg" };

    private static readonly string[] FileExtensions = { ".json", ".png", ".tscn", ".tres", ".cs" };

    [Fact]
    public void Game_HasEveryKeyWrittenInTheCode()
    {
        var literal = new Regex(@"""([a-z][a-z0-9_]*(?:\.[a-z0-9_]+)+)""");
        var separator = Path.DirectorySeparatorChar;
        var missing = new List<string>();
        foreach (var folder in new[] { "Lete.Game", "Lete.Core" })
        {
            var files = Directory.GetFiles(Path.Combine(RepoRoot(), "src", folder), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{separator}bin{separator}") && !f.Contains($"{separator}obj{separator}"));
            foreach (var file in files)
            {
                foreach (Match match in literal.Matches(File.ReadAllText(file)))
                {
                    var key = match.Groups[1].Value;
                    if (!NotKeys.Contains(key) && !FileExtensions.Any(key.EndsWith) && !Strings.ContainsKey(key))
                    {
                        missing.Add($"{Path.GetFileName(file)}: {key}");
                    }
                }
            }
        }

        Assert.Empty(missing);
    }

    /// <summary>As chaves que o código monta com um valor (<c>$"oferenda.{tipo}"</c>) existem para todos os valores.</summary>
    [Fact]
    public void Game_HasEveryKeyTheCodeBuilds()
    {
        var content = Path.Combine(RepoRoot(), "content");
        var expected = new List<string>();
        expected.AddRange(BagData.OfferingRulesFromJson(File.ReadAllText(Path.Combine(content, "rules", "oferendas.json"))).Kinds
            .Select(k => $"oferenda.{k}"));
        expected.AddRange(Enum.GetValues<ItemStage>().Select(s => $"item.estagio.{ConditionParser.SnakeCase(s)}"));
        expected.AddRange(Enum.GetValues<EquipSlot>().Select(s => $"item.espaco.{ConditionParser.SnakeCase(s)}"));
        expected.AddRange(Enum.GetValues<RecollectionKind>().Select(k => $"dialogo.tipo.{ConditionParser.SnakeCase(k)}"));
        expected.AddRange(Enum.GetValues<Via>().Select(v => $"debug.perfil.via.{ConditionParser.SnakeCase(v)}"));
        expected.AddRange(Enum.GetValues<GestureAssist>().Select(a => $"combate.assistencia.{a.ToString().ToLowerInvariant()}"));
        // O golpe (Strike) não tem fala: o efeito se vê na sombra.
        expected.AddRange(Enum.GetValues<MemoryEffectKind>().Where(e => e != MemoryEffectKind.Strike)
            .Select(e => $"combate.memoria.efeito.{e.ToString().ToLowerInvariant()}"));
        foreach (var file in Directory.GetFiles(Path.Combine(content, "companions"), "*.json"))
        {
            var companion = CompanionData.FromJson(File.ReadAllText(file));
            expected.Add($"combate.apoio.{companion.SupportId}");
            expected.Add($"combate.apoio.{companion.SupportId}.age");
        }

        var missing = expected.Where(k => !Strings.ContainsKey(k)).ToList();
        Assert.Empty(missing);
    }

    /// <summary>Um idioma além do padrão (quando existir) tem as mesmas chaves e lugares de valor.</summary>
    [Fact]
    public void Game_OtherLanguagesMatchTheStandard()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot(), "content", "locale"), "*.json")
            .Where(f => Path.GetFileName(f) != "pt_BR.json"))
        {
            var translated = Localizer.Read(File.ReadAllText(file));
            Assert.Empty(LocaleValidator.Validate(translated));
            Assert.Empty(LocaleValidator.Compare(Strings, translated));
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("project.godot não encontrado.");
    }
}
