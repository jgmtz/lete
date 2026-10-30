using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Diário: guarda o que o jogador viu e ouviu, em frases soltas. Sem marcadores de objetivo.
/// Examinar o mesmo objeto de novo não repete a frase. Também escreve o que outros sistemas anunciam (uma chave do
/// Caminho obtida).
/// </summary>
public sealed class Journal : IDisposable
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly IDisposable _pathKeys;

    public Journal(WorldState state, EventBus events)
    {
        _state = state;
        _events = events;
        _pathKeys = events.Subscribe<PathKeyObtained>(e => Add(e.JournalKey));
    }

    public void Dispose() => _pathKeys.Dispose();

    public IReadOnlyList<string> Entries => _state.JournalEntries;

    /// <returns><c>true</c> se a entrada era nova.</returns>
    public bool Add(string entryKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryKey);

        if (_state.JournalEntries.Contains(entryKey))
        {
            return false;
        }

        _state.JournalEntries.Add(entryKey);
        _events.Publish(new JournalEntryAdded(entryKey));
        return true;
    }
}
