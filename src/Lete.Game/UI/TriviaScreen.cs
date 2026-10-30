using System;
using System.Linq;
using Godot;

namespace Lete.Game.UI;

/// <summary>
/// Trívia de Hécate (GDD 3.6 e 3.7), provisória: chegar a acende; ali se descansa e salva, ou se segue uma tocha acesa
/// para outra Trívia (a Maré sobe um pouco). A cena faz o teleporte (<see cref="Traveled"/>).
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

        ShowChoices(text, destinations
            .Select(d => new Choice(string.Format(T("trivia.viajar"), T(d.NameKey)), () => Travel(d.Id)))
            .Prepend(new Choice(T("trivia.descansar"), Rest))
            .Append(new Choice(T("ui.sair"), Close)));
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
