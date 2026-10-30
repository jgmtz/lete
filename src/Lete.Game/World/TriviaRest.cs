using Godot;

namespace Lete.Game.World;

/// <summary>Trívia de Hécate (GDD 3.7): descansar devolve Fôlego e Lucidez e salva o jogo (só se salva nas Trívias).</summary>
public partial class TriviaRest : Interactable
{
    public override string PromptKey => "ui.prompt.descansar";

    public override void Examine()
    {
        GameRoot.Instance.Healing.Rest();
        Announce(GameRoot.Instance.Save() == Error.Ok ? "ui.trivia.descansou" : "ui.trivia.nao_salvou");
    }
}
