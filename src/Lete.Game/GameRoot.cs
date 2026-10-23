using Godot;
using Lete.Core.Events;
using Lete.Core.Localization;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Game;

/// <summary>
/// Autoload que guarda os serviços do Core (estado, eventos, sistemas, textos) durante o jogo inteiro.
/// É a ponte entre a camada Godot e a lógica pura de <c>Lete.Core</c>.
/// </summary>
public partial class GameRoot : Node
{
    private const string LocalePath = "res://content/locale/pt_BR.json";

    public static GameRoot Instance { get; private set; } = null!;

    public EventBus Events { get; } = new();
    public WorldState State { get; } = new();
    public Journal Journal { get; private set; } = null!;
    public Localizer Localizer { get; private set; } = null!;

    public override void _EnterTree()
    {
        Instance = this;
        Journal = new Journal(State, Events);
        Localizer = Localizer.FromJson(FileAccess.GetFileAsString(LocalePath));
    }

    /// <summary>Texto da chave, já com o gênero do protagonista resolvido.</summary>
    public string Text(string key) => Localizer.Get(key, State.Gender);
}
