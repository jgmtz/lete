using System;
using Godot;

namespace Lete.Game.World;

/// <summary>
/// Objeto examinável: quando o jogador está perto e aperta "interact", a frase vai para o Diário.
/// A área de alcance é este <see cref="Area3D"/>.
/// </summary>
public partial class Interactable : Area3D
{
    /// <summary>Chave de localização da frase que vai para o Diário.</summary>
    [Export] public string JournalKey { get; set; } = "";

    /// <summary>Disparado ao examinar (mesmo se a frase já estiver no Diário), para a interface mostrar o texto.</summary>
    public static event Action<string>? Examined;

    public override void _Ready()
    {
        BodyEntered += body => (body as Player)?.AddNearby(this);
        BodyExited += body => (body as Player)?.RemoveNearby(this);
    }

    public void Examine()
    {
        GameRoot.Instance.Journal.Add(JournalKey);
        Examined?.Invoke(JournalKey);
    }
}
