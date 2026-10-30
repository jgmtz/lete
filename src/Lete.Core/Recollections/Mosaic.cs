using Lete.Core.Combat;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Recollections;

/// <summary>
/// O Mosaico (GDD 2.1): o que o protagonista lembra, sem XP. Guarda no <see cref="WorldState"/> só os fatos (fontes
/// ouvidas, ato pleno, falsa) e deriva estágio e confiança das regras:
/// <list type="bullet">
/// <item>uma fonte → Fragmento; uma segunda fonte, diferente, com a Lembrança confirmada → Recordação;</item>
/// <item>um ato de <see cref="Recollection.FullBy"/> numa Recordação → Memória plena;</item>
/// <item>só fontes não confiáveis → incerta (funciona como Fragmento e não avança); uma confiável a confirma;</item>
/// <item>uma fonte que contradiz uma incerta → falsa: a pedra some e a habilidade junto (decisão do João, 29/out/2026).</item>
/// </list>
/// Escuta <see cref="ShadeFreed"/> e <see cref="OfferingShared"/>; as outras fontes chegam por <see cref="Hear"/>.
/// As Pesadas não têm fontes: encarada, a Pesada é plena (e a Memória dela é a Culpa); deixada, a pedra fica
/// escura e coberta (<see cref="LeftBehind"/>), sem habilidade (GDD 2.2; decisão do João, 30/out/2026).
/// </summary>
public sealed class Mosaic : IDisposable
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly IReadOnlyDictionary<string, Recollection> _recollections;
    private readonly MosaicRules _rules;
    private readonly List<IDisposable> _subscriptions = new();

    public Mosaic(WorldState state, EventBus events, IReadOnlyDictionary<string, Recollection> recollections, MosaicRules rules)
    {
        _state = state;
        _events = events;
        _recollections = recollections;
        _rules = rules;
        _subscriptions.Add(events.Subscribe<ShadeFreed>(e => HearAll(s => s.Type == SourceType.FreeShade && s.Shade == e.ShadeId)));
        _subscriptions.Add(events.Subscribe<OfferingShared>(e => HearAll(s =>
            s.Type == SourceType.SharedOffering && s.Shade == e.ShadeId && (s.OfferingKind is null || s.OfferingKind == e.Kind))));
        _subscriptions.Add(events.Subscribe<HeavyMemoryChosen>(e =>
        {
            if (e.Choice == HeavyMemoryChoice.Face && _recollections.ContainsKey(e.MemoryId))
            {
                _events.Publish(new RecollectionGained(e.MemoryId, RecollectionStage.Full));
            }
        }));
    }

    public IReadOnlyDictionary<string, Recollection> Definitions => _recollections;

    /// <summary>Lembranças que o jogador tem agora (falsas não entram: a pedra sumiu), com as Pesadas encaradas.</summary>
    public IEnumerable<Recollection> Known => _state.Recollections
        .Where(p => !p.Value.False && _recollections.ContainsKey(p.Key))
        .Select(p => _recollections[p.Key])
        .Concat(HeavyWith(HeavyMemoryChoice.Face));

    /// <summary>Pesadas deixadas: a pedra fica no lugar, escura e coberta, sem habilidade.</summary>
    public IEnumerable<Recollection> LeftBehind => HeavyWith(HeavyMemoryChoice.Leave);

    public RecollectionStage? StageOf(string id)
    {
        if (_recollections.TryGetValue(id, out var heavy) && heavy.Scope == RecollectionScope.Heavy)
        {
            return _state.HeavyMemories.TryGetValue(id, out var choice) && choice == HeavyMemoryChoice.Face
                ? RecollectionStage.Full
                : null;
        }

        if (!_state.Recollections.TryGetValue(id, out var progress) || progress.False || !_recollections.TryGetValue(id, out var recollection))
        {
            return null;
        }

        if (progress.Sources.Count == 0 || CertaintyOf(recollection, progress) != RecollectionCertainty.Certain || progress.Sources.Count < 2)
        {
            return RecollectionStage.Fragment;
        }

        return progress.FullAct is null ? RecollectionStage.Recalled : RecollectionStage.Full;
    }

    /// <summary>Nula se o jogador não tem a Lembrança (e nunca teve); <see cref="RecollectionCertainty.False"/> continua lembrada pelo Tribunal.</summary>
    public RecollectionCertainty? CertaintyOf(string id) =>
        _recollections.TryGetValue(id, out var heavy) && heavy.Scope == RecollectionScope.Heavy
            ? StageOf(id) is null ? null : RecollectionCertainty.Certain
            : _state.Recollections.TryGetValue(id, out var progress) && _recollections.TryGetValue(id, out var recollection)
            ? CertaintyOf(recollection, progress)
            : null;

    /// <summary>A Memória (habilidade) da Lembrança; nula para falsa, desconhecida ou sem habilidade.</summary>
    public string? SkillOf(string id) => StageOf(id) is null ? null : _recollections[id].Skill;

    /// <summary>
    /// As Memórias de combate que o Mosaico dá agora, na ordem do Mosaico: uma por Lembrança conhecida com habilidade,
    /// com a força multiplicada pelo estágio (Fragmento fraca, Recordação média, plena máxima; GDD 2.1). A incerta
    /// funciona como Fragmento; a falsa não entra.
    /// </summary>
    public IReadOnlyList<CombatMemory> CombatMemories(IEnumerable<MemorySkill> skills)
    {
        var byId = skills.ToDictionary(s => s.Id, StringComparer.Ordinal);
        return Known
            .Where(r => r.Skill is not null)
            .OrderBy(r => r.MosaicY).ThenBy(r => r.MosaicX).ThenBy(r => r.Id, StringComparer.Ordinal)
            .Select(r =>
            {
                var skill = byId.TryGetValue(r.Skill!, out var found)
                    ? found
                    : throw new KeyNotFoundException($"A Lembrança '{r.Id}' dá a Memória '{r.Skill}', que não existe.");
                var stage = StageOf(r.Id)!.Value;
                return new CombatMemory(
                    skill with { Power = skill.Power.Scaled(_rules.StagePower[stage]) }, r.Id, stage, r.Scope == RecollectionScope.Heavy);
            })
            .ToList();
    }

    /// <summary>Tipos com pelo menos uma Memória plena (Lembranças comuns e da origem; Pesadas não contam).</summary>
    public int FullKinds => Known
        .Where(r => r.Scope != RecollectionScope.Heavy && StageOf(r.Id) == RecollectionStage.Full)
        .Select(r => r.Kind)
        .Distinct()
        .Count();

    /// <summary>Chave de Memória: abre o Vale do Lete (o jogador nunca vê o número).</summary>
    public bool HasMemoryKey => FullKinds >= _rules.KeyKinds;

    /// <summary>Uma fonte foi ouvida (lugar-âncora, sonho, Mnemósine, fala). Falso se não mudou nada.</summary>
    public bool Hear(string recollectionId, string sourceId)
    {
        var recollection = Get(recollectionId);
        var source = recollection.Sources.FirstOrDefault(s => s.Id == sourceId)
            ?? throw new ArgumentException($"A Lembrança '{recollectionId}' não tem a fonte '{sourceId}'.", nameof(sourceId));
        return Apply(recollection, source);
    }

    /// <summary>O ato que leva uma Recordação à Memória plena. Falso se ela ainda não é Recordação ou já é plena.</summary>
    public bool CompleteAct(string recollectionId, string actId)
    {
        var recollection = Get(recollectionId);
        if (!recollection.FullBy.Contains(actId))
        {
            throw new ArgumentException($"A Lembrança '{recollectionId}' não fica plena por '{actId}'.", nameof(actId));
        }

        if (StageOf(recollectionId) != RecollectionStage.Recalled)
        {
            return false;
        }

        _state.Recollections[recollectionId].FullAct = actId;
        _events.Publish(new RecollectionAdvanced(recollectionId, RecollectionStage.Full));
        return true;
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private IEnumerable<Recollection> HeavyWith(HeavyMemoryChoice choice) => _state.HeavyMemories
        .Where(p => p.Value == choice && _recollections.TryGetValue(p.Key, out var r) && r.Scope == RecollectionScope.Heavy)
        .Select(p => _recollections[p.Key]);

    private void HearAll(Func<RecollectionSource, bool> match)
    {
        foreach (var recollection in _recollections.Values.Where(r => r.Scope != RecollectionScope.Heavy))
        {
            foreach (var source in recollection.Sources.Where(match))
            {
                Apply(recollection, source);
            }
        }
    }

    private bool Apply(Recollection recollection, RecollectionSource source)
    {
        _state.Recollections.TryGetValue(recollection.Id, out var progress);
        if (progress is { False: true } || progress?.Sources.Contains(source.Id) == true)
        {
            return false;
        }

        if (source.Contradicts)
        {
            // Só desmente o que ainda é incerto; sem a Lembrança, a outra versão não ensina nada.
            if (progress is null || CertaintyOf(recollection, progress) != RecollectionCertainty.Uncertain)
            {
                return false;
            }

            progress.False = true;
            _events.Publish(new RecollectionFalsified(recollection.Id));
            return true;
        }

        var before = StageOf(recollection.Id);
        if (progress is null)
        {
            progress = new RecollectionProgress();
            _state.Recollections[recollection.Id] = progress;
        }

        progress.Sources.Add(source.Id);
        var after = StageOf(recollection.Id)!.Value;
        if (before is null)
        {
            _events.Publish(new RecollectionGained(recollection.Id, after));
        }
        else if (after > before)
        {
            _events.Publish(new RecollectionAdvanced(recollection.Id, after));
        }

        return true;
    }

    private static RecollectionCertainty CertaintyOf(Recollection recollection, RecollectionProgress progress)
    {
        if (progress.False)
        {
            return RecollectionCertainty.False;
        }

        return recollection.Sources.Any(s => s.Reliable && progress.Sources.Contains(s.Id))
            ? RecollectionCertainty.Certain
            : RecollectionCertainty.Uncertain;
    }

    /// <summary>Uma Lembrança que avança por fontes e atos (as Pesadas avançam só por Encarar).</summary>
    private Recollection Get(string id) => _recollections.TryGetValue(id, out var recollection)
        ? recollection.Scope == RecollectionScope.Heavy
            ? throw new ArgumentException($"'{id}' é Pesada: só avança ao ser encarada.", nameof(id))
            : recollection
        : throw new ArgumentException($"Lembrança '{id}' não existe.", nameof(id));
}

/// <summary>
/// Uma Memória de combate vinda do Mosaico: a habilidade já na força do estágio, e de qual Lembrança veio.
/// <see cref="Guilt"/>: é a Culpa de uma Pesada encarada (aparece marcada no menu Memória).
/// </summary>
public sealed record CombatMemory(MemorySkill Skill, string RecollectionId, RecollectionStage Stage, bool Guilt = false);

/// <summary>
/// Números do Mosaico (<c>content/rules/mosaico.json</c>): tipos plenos para a Chave de Memória e o multiplicador
/// de força da Memória em cada estágio.
/// </summary>
public sealed record MosaicRules(int KeyKinds, IReadOnlyDictionary<RecollectionStage, float> StagePower)
{
    public IEnumerable<string> Validate()
    {
        if (KeyKinds < 1 || KeyKinds > Enum.GetValues<RecollectionKind>().Length)
        {
            yield return "A Chave de Memória precisa de 1 a 5 tipos.";
        }

        var powers = Enum.GetValues<RecollectionStage>().Select(s => StagePower.TryGetValue(s, out var p) ? p : -1f).ToList();
        if (powers.Any(p => p <= 0f) || powers.Zip(powers.Skip(1)).Any(pair => pair.Second < pair.First))
        {
            yield return "A força por estágio precisa existir para os três, ser positiva e não cair de um estágio para o seguinte.";
        }
    }
}
