namespace Lete.Core.World;

/// <summary>
/// Um encontro marcado no mapa (<c>content/encounters/&lt;id&gt;.json</c>): ao chegar perto da sombra, a luta começa e,
/// no fim, o jogo volta ao mapa. Os encontros do tutorial (o Remador na descida de Tênaro, "uma coisa por luta")
/// mostram só as ações já ensinadas e terminam quando a lição se cumpre; sem lição, é a luta inteira.
/// </summary>
/// <param name="Battle">Id da configuração da batalha (<c>content/battle</c>): palco, grupo e sombra.</param>
/// <param name="Actions">As opções do menu que aparecem (ids de <see cref="Combat.BattleActions.Menu"/>); nulo = todas.</param>
/// <param name="Lesson">O que o jogador precisa fazer para a sombra recuar (opções do menu, aparar ou esquivar); vazio = luta inteira.</param>
/// <param name="HecateKey">A linha de Hécate na legenda quando a luta começa.</param>
/// <param name="WithdrawKey">A legenda quando a sombra recua (lição cumprida).</param>
/// <param name="After">O encontro que precisa ter terminado antes (até lá, a sombra não está neste lugar).</param>
/// <param name="NeedsOffering">Tipo de oferenda que precisa estar na Bolsa para a luta começar; sem ela, só a fala <paramref name="WaitKey"/>.</param>
/// <param name="FreeDefeat">Derrota sem custo: Hécate diz uma linha e o encontro recomeça descansado.</param>
/// <param name="Requires">Condição para a sombra estar no lugar, além de <paramref name="After"/> (ex.: Hécate já avisou da romã).</param>
public sealed record Encounter(
    string Id,
    string ShadeId,
    string RegionId,
    string Battle,
    IReadOnlyList<string>? Actions,
    IReadOnlyList<string> Lesson,
    string HecateKey,
    string? WithdrawKey,
    string? After,
    string? NeedsOffering,
    string? WaitKey,
    bool FreeDefeat,
    Conditions.Condition? Requires = null)
{
    public bool IsLesson => Lesson.Count > 0;

    /// <summary>A opção do menu aparece neste encontro.</summary>
    public bool Allows(string action) => Actions is null || Actions.Contains(action, StringComparer.Ordinal);
}
