using System.Collections.Generic;
using System.Linq;
using Lete.Core.World;

namespace Lete.Game.UI;

/// <summary>
/// Julgamento provisório no Tribunal (GDD 7.2): o jogador marca até N provas (entradas do Diário e Lembranças que tem ou
/// teve) e escolhe Defender, Acusar ou Calar; depois vê o veredito (e o perjúrio, se houve).
/// </summary>
public partial class TrialScreen : ChoiceScreen
{
    private readonly List<Evidence> _chosen = new();
    private string _trialId = "";

    public void Open(string trialId)
    {
        _trialId = trialId;
        _chosen.Clear();
        var tribunal = GameRoot.Instance.Tribunal;
        var trial = tribunal.Definitions[trialId];
        ShowPanel(T(trial.JudgeKey));
        if (tribunal.VerdictOf(trialId) is not null)
        {
            ShowChoices(T("julgamento.ja_houve"), new[] { new Choice(T("ui.sair"), Close) });
            return;
        }

        ShowEvidence();
    }

    private void ShowEvidence()
    {
        var tribunal = GameRoot.Instance.Tribunal;
        var trial = tribunal.Definitions[_trialId];
        var text = T(trial.IntroKey) + "\n" + string.Format(T("julgamento.provas"), _chosen.Count, trial.Slots);
        var evidence = tribunal.AvailableEvidence().Select(e =>
        {
            var chosen = _chosen.Contains(e);
            return new Choice((chosen ? "• " : "") + EvidenceText(e), () => Toggle(e, trial.Slots),
                Enabled: chosen || _chosen.Count < trial.Slots, Dim: !chosen);
        });
        var stances = new[]
        {
            new Choice(T("julgamento.defender"), () => Judge(TrialStance.Defend)),
            new Choice(T("julgamento.acusar"), () => Judge(TrialStance.Accuse)),
            new Choice(T("julgamento.calar"), () => Judge(TrialStance.Silence)),
        };
        ShowChoices(text, stances.Concat(evidence));
    }

    private void Toggle(Evidence evidence, int slots)
    {
        if (!_chosen.Remove(evidence) && _chosen.Count < slots)
        {
            _chosen.Add(evidence);
        }

        ShowEvidence();
    }

    private void Judge(TrialStance stance)
    {
        var outcome = GameRoot.Instance.Tribunal.Judge(_trialId, stance, _chosen);
        var text = T(outcome.Verdict.TextKey);
        if (outcome.Perjuries.Count > 0)
        {
            text = T("julgamento.perjurio") + "\n\n" + text;
        }

        ShowChoices(text, new[] { new Choice(T("ui.sair"), Close) });
    }

    private static string EvidenceText(Evidence evidence) => evidence.Kind == EvidenceKind.Journal
        ? T(evidence.Id)
        : string.Format(T("julgamento.prova.lembranca"),
            GameRoot.Instance.Mosaic.Definitions.TryGetValue(evidence.Id, out var r) ? T(r.TitleKey) : evidence.Id);
}
