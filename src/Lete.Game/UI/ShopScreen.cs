using System.Linq;
using Lete.Core.World;

namespace Lete.Game.UI;

/// <summary>
/// Loja provisória de um mercador (GDD 6.4): o estoque de agora (o de outro caminho, mais caro e esmaecido), os óbolos
/// do jogador e o resultado de cada compra.
/// </summary>
public partial class ShopScreen : ChoiceScreen
{
    private string _merchantId = "";

    public void Open(string merchantId)
    {
        _merchantId = merchantId;
        var merchant = GameRoot.Instance.Merchants.Definitions[merchantId];
        ShowPanel(T(merchant.NameKey));
        ShowStock(T(merchant.GreetingKey));
    }

    private void ShowStock(string message)
    {
        var root = GameRoot.Instance;
        var offers = root.Merchants.Offers(_merchantId);
        var text = message + "\n" + string.Format(T("mercador.obolos"), root.State.Inventory.Obols);
        ShowChoices(text, offers.Select(o => new Choice(Label(o), () => Buy(o.Ware), Enabled: o.Remaining > 0, Dim: !o.OnPath))
            .Append(new Choice(T("ui.sair"), Close)));
    }

    private void Buy(Ware ware)
    {
        var result = GameRoot.Instance.Merchants.Buy(_merchantId, ware);
        ShowStock(T(result switch
        {
            PurchaseResult.Bought => "mercador.comprou",
            PurchaseResult.NotEnoughObols => "mercador.sem_obolos",
            PurchaseResult.BagFull => "mercador.bolsa_cheia",
            _ => "mercador.esgotado",
        }));
    }

    private static string Label(WareOffer offer)
    {
        var root = GameRoot.Instance;
        var name = offer.Ware.Kind switch
        {
            WareKind.Item => T(root.Items.Definitions[offer.Ware.Id].NameKey),
            WareKind.Offering => T($"oferenda.{offer.Ware.Id}"),
            _ => T(root.Provisions.First(p => p.Id == offer.Ware.Id).NameKey),
        };
        var label = offer.Ware.Kind == WareKind.Item
            ? string.Format(T("mercador.oferta"), name, offer.Price)
            : string.Format(T("mercador.oferta.restam"), name, offer.Price, offer.Remaining);
        return offer.OnPath ? label : $"{label} {T("mercador.oferta.outro_caminho")}";
    }
}
