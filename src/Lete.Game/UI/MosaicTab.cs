using System.Collections.Generic;
using System.Linq;
using Godot;
using Lete.Core.Combat;
using Lete.Core.Data;
using Lete.Core.Recollections;

namespace Lete.Game.UI;

/// <summary>
/// Aba do Mosaico (GDD 2.1), versão provisória até haver arte (decisão do João, 29/out/2026): cada Lembrança
/// conhecida é um trecho de pedras na cor do tipo, no lugar de <c>mosaic_position</c>. O Fragmento acende um terço
/// das pedras, a Recordação dois terços, a Memória plena todas; a incerta tem uma rachadura. Sem moldura, sem
/// números e sem pedras apagadas do que ainda não se lembra (o jogador não vê quanto falta). As Pesadas (GDD 2.2):
/// encarada, o trecho inteiro em pedra escura com a borda partida; deixada, a pedra escura fica coberta pela água do
/// Lete, sem título (decisão do João, 30/out/2026). As pendentes não aparecem.
/// </summary>
public partial class MosaicTab : HBoxContainer, IMenuTab
{
    private const string RulesPath = "res://content/rules/mosaico.json";

    /// <summary>Pedras por lado de cada trecho (trecho de 3×3).</summary>
    [Export] public int StonesPerSide { get; set; } = 3;

    /// <summary>Tamanho de uma pedra e o rejunte, em pixels de tela 1080p.</summary>
    [Export] public int StonePx { get; set; } = 40;
    [Export] public int GroutPx { get; set; } = 6;

    /// <summary>Quanto a cor do tipo escurece na pedra da Pesada, e a água que cobre a deixada.</summary>
    [Export] public float HeavyDarkening { get; set; } = 0.65f;
    [Export] public float LeftWaterAlpha { get; set; } = 0.6f;

    private IReadOnlyDictionary<RecollectionKind, Color> _colors = null!;
    private Control _canvas = null!;
    private VBoxContainer _titles = null!;

    public string TitleKey => "ui.mosaico.titulo";

    public string ShortcutAction => "mosaic";

    public Control View => this;

    public override void _Ready()
    {
        _colors = RecollectionData.StoneColorsFromJson(FileAccess.GetFileAsString(RulesPath))
            .ToDictionary(p => p.Key, p => new Color(p.Value));
        AddThemeConstantOverride("separation", 48);

        _canvas = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _canvas.Draw += DrawStones;
        AddChild(_canvas);

        _titles = new VBoxContainer { CustomMinimumSize = new Vector2(360, 0) };
        _titles.AddThemeConstantOverride("separation", 12);
        AddChild(_titles);
    }

    public void Refresh()
    {
        foreach (var child in _titles.GetChildren())
        {
            child.QueueFree();
        }

        var mosaic = GameRoot.Instance.Mosaic;
        var known = mosaic.Known.OrderBy(r => r.MosaicY).ThenBy(r => r.MosaicX).ToList();
        if (known.Count == 0)
        {
            _titles.AddChild(UiStyle.MakeLabel(GameRoot.Instance.Text("ui.mosaico.vazio")));
        }

        foreach (var recollection in known)
        {
            var title = UiStyle.MakeLabel(GameRoot.Instance.Text(recollection.TitleKey));
            title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            title.AddThemeColorOverride("font_color", _colors[recollection.Kind].Lightened(0.25f));
            _titles.AddChild(title);
        }

        _canvas.QueueRedraw();
    }

    private void DrawStones()
    {
        var mosaic = GameRoot.Instance.Mosaic;
        var pitch = StonePx + GroutPx;

        // Centraliza pelo desenho inteiro (todas as Lembranças do conteúdo), para nada mudar de lugar quando uma
        // pedra nova aparece; as que faltam não são desenhadas.
        var definitions = mosaic.Definitions.Values.ToList();
        var extent = definitions.Count == 0
            ? Vector2.Zero
            : new Vector2(definitions.Max(r => r.MosaicX) + StonesPerSide, definitions.Max(r => r.MosaicY) + StonesPerSide) * pitch;
        var offset = ((_canvas.Size - extent) / 2).Floor();
        foreach (var recollection in mosaic.LeftBehind)
        {
            var origin = offset + new Vector2(recollection.MosaicX, recollection.MosaicY) * pitch;
            DrawHeavy(origin, _colors[recollection.Kind].Darkened(HeavyDarkening));
            var size = StonesPerSide * pitch - GroutPx;
            _canvas.DrawRect(new Rect2(origin, new Vector2(size, size)), new Color(UiStyle.Palette.Lapis, LeftWaterAlpha));
        }

        foreach (var recollection in mosaic.Known)
        {
            if (recollection.Scope == RecollectionScope.Heavy)
            {
                DrawHeavy(offset + new Vector2(recollection.MosaicX, recollection.MosaicY) * pitch, _colors[recollection.Kind].Darkened(HeavyDarkening));
                continue;
            }

            var stage = mosaic.StageOf(recollection.Id)!.Value;
            var total = StonesPerSide * StonesPerSide;
            var lit = total * ((int)stage + 1) / 3;
            var origin = offset + new Vector2(recollection.MosaicX, recollection.MosaicY) * pitch;
            var color = _colors[recollection.Kind];

            // Ordem de acender: do centro para fora, para o trecho crescer como uma pedra que se completa.
            var center = (StonesPerSide - 1) / 2f;
            var order = Enumerable.Range(0, total)
                .OrderBy(i => Mathf.Abs(i % StonesPerSide - center) + Mathf.Abs(i / StonesPerSide - center))
                .ThenBy(i => i)
                .Take(lit);
            foreach (var i in order)
            {
                var position = origin + new Vector2(i % StonesPerSide, i / StonesPerSide) * pitch;
                _canvas.DrawRect(new Rect2(position, new Vector2(StonePx, StonePx)), color);
            }

            if (mosaic.CertaintyOf(recollection.Id) == RecollectionCertainty.Uncertain)
            {
                var size = StonesPerSide * pitch - GroutPx;
                _canvas.DrawLine(origin + new Vector2(size * 0.2f, 0), origin + new Vector2(size * 0.55f, size * 0.5f), UiStyle.Palette.Outline, GroutPx);
                _canvas.DrawLine(origin + new Vector2(size * 0.55f, size * 0.5f), origin + new Vector2(size * 0.4f, size), UiStyle.Palette.Outline, GroutPx);
            }
        }
    }

    /// <summary>Trecho inteiro de pedra escura; a pedra do canto de cima, à direita, partida na diagonal.</summary>
    private void DrawHeavy(Vector2 origin, Color color)
    {
        var pitch = StonePx + GroutPx;
        var corner = StonesPerSide - 1;
        for (var i = 0; i < StonesPerSide * StonesPerSide; i++)
        {
            var position = origin + new Vector2(i % StonesPerSide, i / StonesPerSide) * pitch;
            if (i == corner)
            {
                _canvas.DrawColoredPolygon(new[] { position, position + new Vector2(StonePx, StonePx), position + new Vector2(0, StonePx) }, color);
                continue;
            }

            _canvas.DrawRect(new Rect2(position, new Vector2(StonePx, StonePx)), color);
        }
    }
}
