using System;
using System.Linq;
using Godot;

namespace Lete.Game.UI;

/// <summary>
/// Trívia de Hécate (GDD 3.6 e 3.7), provisória: chegar a acende; ali se descansa e salva, ou se segue uma tocha acesa
/// para outra Trívia (a Maré sobe um pouco). A cena faz o teleporte (<see cref="Traveled"/>). Com o companheiro, dá
/// para conversar (uma vez por degrau da Maré) e partilhar uma oferenda com ele: as duas coisas aprofundam o vínculo
/// (GDD 4.10).
/// </summary>
public partial class TriviaScreen : ChoiceScreen
{
    private string _triviaId = "";

    /// <summary>A viagem aconteceu: a cena leva o jogador até a Trívia de destino.</summary>
    public event Action<string>? Traveled;

    public void Open(string triviaId)
    {
        _triviaId = triviaId;
        var trivias = GameRoot.Instance.Trivias;
        ShowPanel(T(trivias.Definitions[triviaId].NameKey));
        ShowMenu(trivias.Light(triviaId) ? T("trivia.acesa") : T("trivia.titulo"));
    }

    private void ShowMenu(string text)
    {
        var destinations = GameRoot.Instance.Trivias.Destinations(_triviaId);
        if (destinations.Count == 0)
        {
            text += "\n" + T("trivia.sem_tochas");
        }

        var choices = new System.Collections.Generic.List<Choice> { new(T("trivia.descansar"), Rest) };
        var companions = GameRoot.Instance.Companions;
        if (companions.Current is { } companion)
        {
            var name = T(companion.NameKey);
            choices.Add(new Choice(string.Format(T("trivia.conversar"), name), Talk, companions.CanTalk));
            choices.Add(new Choice(string.Format(T("trivia.partilhar"), name), ShowShare, GameRoot.Instance.Offerings.Carried.Any()));
        }

        choices.AddRange(destinations.Select(d => new Choice(string.Format(T("trivia.viajar"), T(d.NameKey)), () => Travel(d.Id))));
        choices.Add(new Choice(T("ui.sair"), Close));
        ShowChoices(text, choices);
    }

    private void Talk() => ShowMenu(GameRoot.Instance.Companions.Talk() is { } line ? T(line) : T("trivia.titulo"));

    private void ShowShare()
    {
        var companion = GameRoot.Instance.Companions.Current!;
        ShowChoices(string.Format(T("trivia.partilhar.qual"), T(companion.NameKey)), GameRoot.Instance.Offerings.Carried
            .Select(kind => new Choice(T($"oferenda.{kind}"), () =>
            {
                GameRoot.Instance.Offerings.Share(companion.Id, kind);
                ShowMenu(string.Format(T("trivia.partilhou"), T(companion.NameKey), T($"oferenda.{kind}")));
            }))
            .Append(new Choice(T("ui.voltar"), () => ShowMenu(T("trivia.titulo"))))
            .ToList());
    }

    private void Rest()
    {
        GameRoot.Instance.Healing.Rest();
        ShowMenu(T(GameRoot.Instance.Save() == Error.Ok ? "ui.trivia.descansou" : "ui.trivia.nao_salvou"));
    }

    private void Travel(string toId)
    {
        GameRoot.Instance.Trivias.Travel(_triviaId, toId);
        Close();
        Traveled?.Invoke(toId);
    }
}
