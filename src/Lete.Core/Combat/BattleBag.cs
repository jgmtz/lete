using Lete.Core.Combat.Gestures;
using Lete.Core.State;

namespace Lete.Core.Combat;

/// <summary>
/// Uma provisão da Bolsa (GDD 4.8): quanto Fôlego devolve e quanto sobe a Marca (as dos vivos não sobem; as do
/// Além sobem). Usar é imediato.
/// </summary>
public sealed record ProvisionDefinition(string Id, string NameKey, string DescriptionKey, float BreathRestore, int MarkGain);

/// <summary>
/// O que o protagonista carrega para a luta: a Bolsa do <see cref="WorldState"/> (óbolos, oferendas por tipo e
/// provisões), vista pela luta. Na derrota, óbolos e oferendas ficam com a sombra (<see cref="ShadeHoard"/>).
/// </summary>
public sealed class BattleBag
{
    private readonly InventoryState _inventory;
    private readonly Dictionary<string, int> _offerings;
    private readonly Dictionary<string, int> _provisions;

    public BattleBag(InventoryState inventory, IReadOnlyList<ProvisionDefinition> provisions)
    {
        _inventory = inventory;
        _offerings = inventory.Offerings;
        _provisions = inventory.Provisions;
        Provisions = provisions;
    }

    /// <summary>Uma bolsa solta (testes).</summary>
    public BattleBag(int obols, IReadOnlyDictionary<string, int> offerings, IReadOnlyList<ProvisionDefinition> provisions,
        IReadOnlyDictionary<string, int> provisionCounts)
        : this(new InventoryState { Obols = obols }, provisions)
    {
        foreach (var (kind, count) in offerings)
        {
            _offerings[kind] = count;
        }

        foreach (var (id, count) in provisionCounts)
        {
            _provisions[id] = count;
        }
    }

    public int Obols
    {
        get => _inventory.Obols;
        private set => _inventory.Obols = value;
    }
    public IReadOnlyList<ProvisionDefinition> Provisions { get; }
    public IReadOnlyDictionary<string, int> Offerings => _offerings;

    public int OfferingCount(string kind) => _offerings.GetValueOrDefault(kind);

    public int ProvisionCount(string id) => _provisions.GetValueOrDefault(id);

    public bool HasAnyOffering => _offerings.Values.Any(c => c > 0);

    public bool HasAnyProvision => _provisions.Values.Any(c => c > 0);

    /// <summary>Gasta uma oferenda (certa ou errada: a errada também se perde).</summary>
    public void SpendOffering(string kind)
    {
        if (OfferingCount(kind) <= 0)
        {
            throw new InvalidOperationException($"Não há oferenda '{kind}' na bolsa.");
        }

        _offerings[kind]--;
    }

    public ProvisionDefinition UseProvision(string id)
    {
        if (ProvisionCount(id) <= 0)
        {
            throw new InvalidOperationException($"Não há provisão '{id}' na bolsa.");
        }

        _provisions[id]--;
        return Provisions.First(p => p.Id == id);
    }

    /// <summary>Tira tudo o que se perde na derrota (óbolos e oferendas) e devolve o que foi tirado.</summary>
    internal (int Obols, Dictionary<string, int> Offerings) TakeLosses()
    {
        var lost = (Obols, new Dictionary<string, int>(_offerings.Where(o => o.Value > 0)));
        Obols = 0;
        foreach (var kind in _offerings.Keys.ToList())
        {
            _offerings[kind] = 0;
        }

        return lost;
    }

    internal void Receive(int obols, IReadOnlyDictionary<string, int> offerings)
    {
        Obols += obols;
        foreach (var (kind, count) in offerings)
        {
            _offerings[kind] = OfferingCount(kind) + count;
        }
    }
}

/// <summary>
/// O que a sombra guarda depois de vencer o protagonista (GDD 4.9): os óbolos e as oferendas que ele carregava.
/// Só voltam se ela for vencida depois (qualquer via que encerre a luta sem derrota).
/// </summary>
public sealed class ShadeHoard
{
    private readonly Dictionary<string, int> _offerings = new();

    public int Obols { get; private set; }
    public bool IsEmpty => Obols == 0 && _offerings.Values.All(c => c == 0);

    /// <summary>Derrota: tudo o que se perde vai para a sombra. Devolve quantos óbolos foram.</summary>
    public int TakeFrom(BattleBag bag)
    {
        var (obols, offerings) = bag.TakeLosses();
        Obols += obols;
        foreach (var (kind, count) in offerings)
        {
            _offerings[kind] = _offerings.GetValueOrDefault(kind) + count;
        }

        return obols;
    }

    /// <summary>Vitória sobre ela: devolve tudo para a bolsa. Verdadeiro se havia algo.</summary>
    public bool ReturnTo(BattleBag bag)
    {
        if (IsEmpty)
        {
            return false;
        }

        bag.Receive(Obols, _offerings);
        Obols = 0;
        _offerings.Clear();
        return true;
    }
}

/// <summary>Assistência de gestos (GDD 4.4, acessibilidade ajustável a qualquer momento).</summary>
public enum GestureAssist
{
    Off,

    /// <summary>Janelas de perfeito/bom mais largas.</summary>
    Wider,

    /// <summary>O gesto se faz sozinho com resultado "bom" (nunca perfeito: a leve penalidade).</summary>
    Automatic,
}

/// <summary>Valores da assistência (por modo de dificuldade).</summary>
public sealed record AssistSettings(float WindowWidening, float AutoScore)
{
    /// <summary>O valor automático precisa dar "bom" nas janelas do modo (nunca perfeito, nunca falho).</summary>
    public IEnumerable<string> Validate(GestureWindows windows)
    {
        if (windows.Grade(AutoScore) != GestureGrade.Good)
        {
            yield return $"O valor automático {AutoScore} não dá 'bom' com as janelas do modo ({windows.Good} a {windows.Perfect}).";
        }

        if (WindowWidening is < 0f or > 1f)
        {
            yield return "O alargamento da assistência precisa ficar entre 0 e 1.";
        }
    }

    public GestureWindows WindowsFor(GestureAssist assist, GestureWindows windows) =>
        assist == GestureAssist.Wider ? windows.Widened(WindowWidening) : windows;

    public static GestureAssist Next(GestureAssist assist) => (GestureAssist)(((int)assist + 1) % 3);
}
