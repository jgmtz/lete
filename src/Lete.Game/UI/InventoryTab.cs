using System.Collections.Generic;
using System.Linq;
using Godot;
using Lete.Core.Combat;
using Lete.Core.Data;
using Lete.Core.Presentation;
using Lete.Core.State;
using Lete.Game.Combat;
using Lete.Game.Opening;

namespace Lete.Game.UI;

/// <summary>
/// Aba do inventário (<c>docs/PROTAGONISTA_SOMBRA.md</c> 6): a silhueta do corpo escolhido, trocando de imagem pela
/// etapa da Marca (e, no fim, pelo rosto da regra C), e o selo da origem. A Marca nunca aparece como número.
/// </summary>
public partial class InventoryTab : HBoxContainer, IMenuTab
{
    /// <summary>Ampliação da silhueta (pixel art de 160 px, sem filtro).</summary>
    [Export] public int BodyScale { get; set; } = 3;

    /// <summary>Ampliação do símbolo da origem (pixel art de 64 px).</summary>
    [Export] public int SymbolScale { get; set; } = 2;

    private IReadOnlyDictionary<string, CastMember> _cast = null!;
    private IReadOnlyList<Origin> _origins = null!;
    private TextureRect _body = null!;
    private TextureRect _symbol = null!;
    private Label _bodyName = null!;
    private Label _originName = null!;
    private Label _craft = null!;
    private Label _bag = null!;

    public string TitleKey => "ui.inventario.titulo";

    public string ShortcutAction => "inventory";

    public Control View => this;

    public override void _Ready()
    {
        _cast = CastData.FromJson(FileAccess.GetFileAsString(BattleStage.CastPath));
        _origins = OriginData.FromJson(FileAccess.GetFileAsString(OpeningDirector.OriginsPath));
        AddThemeConstantOverride("separation", 48);

        _body = new TextureRect
        {
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        AddChild(_body);

        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 16);
        AddChild(column);

        _bodyName = UiStyle.MakeLabel("");
        _bodyName.AddThemeColorOverride("font_color", UiStyle.Palette.IvoryDim);
        column.AddChild(_bodyName);

        var seal = new HBoxContainer();
        seal.AddThemeConstantOverride("separation", 16);
        _symbol = new TextureRect
        {
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        seal.AddChild(_symbol);
        var names = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _originName = UiStyle.MakeLabel("", UiStyle.TitleFontSize);
        _craft = UiStyle.MakeLabel("");
        _craft.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        names.AddChild(_originName);
        names.AddChild(_craft);
        seal.AddChild(names);
        column.AddChild(seal);

        column.AddChild(UiStyle.MakeLabel(GameRoot.Instance.Text("ui.inventario.bolsa"), UiStyle.TitleFontSize));
        _bag = UiStyle.MakeLabel("");
        _bag.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_bag);
    }

    public void Refresh()
    {
        var state = GameRoot.Instance.State;
        var body = state.ProtagonistId is { } id && _cast.TryGetValue(id, out var member) ? member : null;
        _body.Texture = body?.InventoryFolder is { } folder
            ? GD.Load<Texture2D>(MarkArt.ImageFor(folder, state.MarkStage, state.FinalFace))
            : null;
        _body.CustomMinimumSize = _body.Texture is { } texture ? texture.GetSize() * BodyScale : Vector2.Zero;
        _bodyName.Text = body is null ? "" : GameRoot.Instance.Text(body.NameKey);

        var origin = _origins.FirstOrDefault(o => o.Id == state.OriginId);
        _symbol.Texture = origin is null ? null : GD.Load<Texture2D>(origin.Symbol);
        _symbol.CustomMinimumSize = _symbol.Texture is { } symbol ? symbol.GetSize() * SymbolScale : Vector2.Zero;
        _originName.Text = origin is null ? GameRoot.Instance.Text("ui.inventario.sem_origem") : GameRoot.Instance.Text(origin.NameKey);
        _craft.Text = origin is null ? "" : $"{GameRoot.Instance.Text("origem.oficio")}: {GameRoot.Instance.Text(origin.CraftKey)}";
        _bag.Text = BagText();
    }

    /// <summary>Oferendas e provisões com a quantidade, e os óbolos (itens contados são da Bolsa, não progresso).</summary>
    private static string BagText()
    {
        var root = GameRoot.Instance;
        var inventory = root.State.Inventory;
        var lines = root.Offerings.Carried
            .Select(k => string.Format(root.Text("ui.inventario.item"), root.Text($"oferenda.{k}"), root.Offerings.Count(k)))
            .Concat(root.Provisions.Where(p => inventory.Provisions.GetValueOrDefault(p.Id) > 0)
                .Select(p => string.Format(root.Text("ui.inventario.item"), root.Text(p.NameKey), inventory.Provisions[p.Id])))
            .Append(string.Format(root.Text("ui.inventario.obolos"), inventory.Obols))
            .ToList();
        return string.Join("\n", lines);
    }
}
