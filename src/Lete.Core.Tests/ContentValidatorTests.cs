using Lete.Core.Data;

namespace Lete.Core.Tests;

public class ContentValidatorTests
{
    private static readonly IReadOnlySet<string> Locale = new HashSet<string> { "dlg.a", "dlg.b" };

    private static ContentReport Validate(bool release = false, params (string File, string Json)[] files) =>
        ContentValidator.Validate(files.ToDictionary(f => f.File, f => f.Json), Locale, release);

    [Fact]
    public void Valid_HasNoErrors()
    {
        var report = Validate(false,
            ("dialogue/x.json", """{ "id": "x", "line_key": "dlg.a", "options": [ { "text_key": "dlg.b", "requires": { "flag": "f" } } ] }"""),
            ("locale/pt_BR.json", "isto não é lido"));
        Assert.True(report.Ok, string.Join("\n", report.Errors));
    }

    [Fact]
    public void Errors_SayFileFieldAndMessage()
    {
        var report = Validate(false,
            ("dialogue/x.json", """{ "nodes": [ { "line_key": "dlg.falta", "options": [ { "requires": { "voar": 1 } } ] } ] }"""),
            ("battle/b.json", """{ "shade": "nao_existe" }"""),
            ("shades/s.json", """{ "attacks": [ { "gesture": "anel" } ] }"""),
            ("ruim/r.json", "{ sem aspas }"));

        var errors = report.Errors.Select(e => e.ToString()).ToList();
        Assert.Contains("dialogue/x.json → nodes[0].line_key: chave de texto 'dlg.falta' não existe em pt_BR.", errors);
        Assert.Contains(errors, e => e.StartsWith("dialogue/x.json → nodes[0].options[0].requires.voar: predicado desconhecido"));
        Assert.Contains("battle/b.json → shade: 'nao_existe' não existe em shades.", errors);
        Assert.Contains("shades/s.json → attacks[0].gesture: 'anel' não existe em gestures.", errors);
        Assert.Contains(errors, e => e.StartsWith("ruim/r.json: JSON inválido"));
    }

    [Fact]
    public void ConditionIds_MustExist()
    {
        var report = Validate(false,
            ("characters/origens.json", """{ "origins": [] }"""),
            ("dialogue/x.json", """{ "requires": { "all": [ { "shade_state": { "id": "remador", "state": "freed" } }, { "item_owned": "lanterna" } ] } }"""),
            ("shades/remador.json", "{}"));

        var error = Assert.Single(report.Errors);
        Assert.Equal("requires", error.Field);
        Assert.Contains("'lanterna' não existe em items", error.Message);
    }

    [Fact]
    public void Tbd_IsWarningInDevelopmentAndErrorInRelease()
    {
        var file = ("rules/r.json", """{ "a": { "_tbd": true, "value": 1 } }""");

        var dev = Validate(false, file);
        Assert.True(dev.Ok);
        Assert.Equal("a", Assert.Single(dev.Warnings).Field);

        Assert.Equal("a", Assert.Single(Validate(true, file).Errors).Field);
    }
}
