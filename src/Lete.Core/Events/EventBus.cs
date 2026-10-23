namespace Lete.Core.Events;

/// <summary>
/// Barramento de eventos tipados. Sistemas publicam o que aconteceu e outros apenas escutam,
/// sem um conhecer o outro (ARQUITETURA.md, seção 2).
/// </summary>
public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            list = new List<Delegate>();
            _handlers[typeof(TEvent)] = list;
        }

        list.Add(handler);
        return new Subscription(() => list.Remove(handler));
    }

    public void Publish<TEvent>(TEvent evt)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            return;
        }

        // Cópia: um ouvinte pode se desinscrever durante a publicação.
        foreach (var handler in list.ToArray())
        {
            ((Action<TEvent>)handler)(evt);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _unsubscribe;

        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

        public void Dispose()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }
    }
}
