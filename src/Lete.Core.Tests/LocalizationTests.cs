using Lete.Core.Localization;
using Lete.Core.State;

namespace Lete.Core.Tests;

public class LocalizationTests
{
    [Theory]
    [InlineData(Gender.Masculine, "Você parece cansado.")]
    [InlineData(Gender.Feminine, "Você parece cansada.")]
    public void GenderMarkup_PicksFormByGender(Gender gender, string expected)
    {
        Assert.Equal(expected, GenderMarkup.Resolve("Você parece cansad{o|a}.", gender));
    }

    [Fact]
    public void GenderMarkup_ResolvesEveryMarkInText()
    {
        var text = "{o|a} viv{o|a}, sozinh{o|a}";

        Assert.Equal("a viva, sozinha", GenderMarkup.Resolve(text, Gender.Feminine));
    }

    [Fact]
    public void GenderMarkup_AllowsEmptyForm()
    {
        Assert.Equal("senhor", GenderMarkup.Resolve("senhor{|a}", Gender.Masculine));
        Assert.Equal("senhora", GenderMarkup.Resolve("senhor{|a}", Gender.Feminine));
    }

    [Fact]
    public void GenderMarkup_LeavesTextWithoutMarksUntouched()
    {
        Assert.Equal("Uma ânfora {rachada}.", GenderMarkup.Resolve("Uma ânfora {rachada}.", Gender.Feminine));
    }

    [Fact]
    public void Localizer_FromJson_ReadsKeysAndResolvesGender()
    {
        var localizer = Localizer.FromJson("""{ "a.b": "Bem-vind{o|a}" }""");

        Assert.True(localizer.Has("a.b"));
        Assert.Equal("Bem-vinda", localizer.Get("a.b", Gender.Feminine));
    }

    [Fact]
    public void Localizer_MissingKey_IsShownBetweenBrackets()
    {
        var localizer = new Localizer(new Dictionary<string, string>());

        Assert.Equal("[nao.existe]", localizer.Get("nao.existe", Gender.Masculine));
    }
}
