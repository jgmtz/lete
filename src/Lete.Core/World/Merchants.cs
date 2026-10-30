using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.Items;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.World;

/// <summary>O que uma entrada do mercador vende.</summary>
public enum WareKind
{
    Item,
    Offering,
    Provision,
}

/// <summary>
/// Uma entrada do estoque: um item único, uma oferenda ou uma provisão, com preço em óbolos. <see cref="Count"/>: quantas
/// por degrau da Maré (oferendas e provisões; o item único é um só). <see cref="Tags"/>: vias e tipos de Lembrança a que
/// ela combina; sem etiqueta, é neutra.
/// </summary>
public sealed record Ware(WareKind Kind, string Id, int Price, int Count, IReadOnlyList<string> Tags)
{
    /// <summary>Chave da entrada no estado (<c>item:remo</c>, <c>oferenda:mel</c>, <c>provisao:pao</c>).</summary>
    public string Key => $"{Kind switch { WareKind.Item => "item", WareKind.Offering => "oferenda", _ => "provisao" }}:{Id}";
}

/// <summary>Um mercador (<c>content/merchants/&lt;id&gt;.json</c>, GDD 6.4): sombra comerciante com o estoque dela.</summary>
public sealed record Merchant(string Id, string NameKey, string GreetingKey, IReadOnlyList<Ware> Stock);

/// <summary>Uma entrada como o jogador a vê agora: o preço (mais caro fora do caminho) e quantas restam.</summary>
public sealed record WareOffer(Ware Ware, int Price, int Remaining, bool OnPath);

public enum PurchaseResult
{
    Bought,
    NotEnoughObols,
    BagFull,
    SoldOut,
}

/// <summary>
/// Mercadores (GDD 6.4). O estoque segue o Perfil <b>recente</b>: o neutro sempre aparece pelo preço normal; o que
/// combina com a via ou o tipo de Memória predominante também; o de outros caminhos aparece mais caro, como tentação
/// (com o Perfil neutro, todo o etiquetado é "de outro caminho"). Oferendas e provisões acabam e repõem a cada degrau
/// da Maré; o item único sai do estoque quando o jogador o tem (decisão do João, 30/out/2026).
/// </summary>
public sealed class Merchants
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly MerchantRules _rules;
    private readonly Profile _profile;
    private readonly Offerings _offerings;
    private readonly ItemTracker _items;
    private readonly Func<int> _tideStep;

    public Merchants(WorldState state, EventBus events, IReadOnlyDictionary<string, Merchant> merchants, MerchantRules rules,
        Profile profile, Offerings offerings, ItemTracker items, Func<int> tideStep)
    {
        _state = state;
        _events = events;
        Definitions = merchants;
        _rules = rules;
        _profile = profile;
        _offerings = offerings;
        _items = items;
        _tideStep = tideStep;
    }

    public IReadOnlyDictionary<string, Merchant> Definitions { get; }

    /// <summary>O estoque visível agora (sem os itens que o jogador já tem).</summary>
    public IReadOnlyList<WareOffer> Offers(string merchantId)
    {
        var tags = new[] { _profile.TopVia is { } via ? ConditionParser.SnakeCase(via) : null, _profile.TopKind is { } kind ? ConditionParser.SnakeCase(kind) : null }
            .OfType<string>().ToHashSet(StringComparer.Ordinal);
        return Definitions[merchantId].Stock
            .Where(w => w.Kind != WareKind.Item || !_items.Owns(w.Id))
            .Select(w =>
            {
                var onPath = w.Tags.Count == 0 || w.Tags.Any(tags.Contains);
                var price = onPath ? w.Price : (int)MathF.Ceiling(w.Price * _rules.OffPathPriceFactor);
                return new WareOffer(w, price, Remaining(merchantId, w), onPath);
            })
            .ToList();
    }

    public PurchaseResult Buy(string merchantId, Ware ware)
    {
        var offer = Offers(merchantId).FirstOrDefault(o => o.Ware == ware)
            ?? throw new ArgumentException($"O mercador '{merchantId}' não vende '{ware.Key}'.", nameof(ware));
        if (offer.Remaining <= 0)
        {
            return PurchaseResult.SoldOut;
        }

        if (_state.Inventory.Obols < offer.Price)
        {
            return PurchaseResult.NotEnoughObols;
        }

        if (ware.Kind == WareKind.Offering && _offerings.Pick(ware.Id, 1) == 0)
        {
            return PurchaseResult.BagFull;
        }

        _state.Inventory.Obols -= offer.Price;
        switch (ware.Kind)
        {
            case WareKind.Item:
                _items.Acquire(ware.Id);
                break;
            case WareKind.Provision:
                _state.Inventory.Provisions[ware.Id] = _state.Inventory.Provisions.GetValueOrDefault(ware.Id) + 1;
                break;
        }

        var key = $"{merchantId}/{ware.Key}";
        var step = _tideStep();
        var sold = _state.Merchants.TryGetValue(key, out var record) && record.Step == step ? record.Sold : 0;
        _state.Merchants[key] = new MerchantSale(sold + 1, step);
        _events.Publish(new WareBought(merchantId, ware.Key, offer.Price));
        return PurchaseResult.Bought;
    }

    private int Remaining(string merchantId, Ware ware) =>
        _state.Merchants.TryGetValue($"{merchantId}/{ware.Key}", out var record) && record.Step == _tideStep()
            ? Math.Max(0, ware.Count - record.Sold)
            : ware.Count;
}

/// <summary>Quanto a mais custa o que é de outro caminho (<c>content/rules/mercadores.json</c>).</summary>
public sealed record MerchantRules(float OffPathPriceFactor)
{
    public IEnumerable<string> Validate()
    {
        if (OffPathPriceFactor < 1f)
        {
            yield return "O que é de outro caminho não pode custar menos.";
        }
    }
}
