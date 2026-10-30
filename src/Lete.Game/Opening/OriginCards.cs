using System;
using System.Collections.Generic;
using Godot;
using Lete.Core.State;
using Lete.Game.UI;

namespace Lete.Game.Opening;

/// <summary>
/// Escolha da origem, logo depois do reflexo (decisão de 26/out/2026): 4 cartas lado a lado, cada uma com o símbolo
/// da origem, o nome, o ofício, a ambição e 2 a 3 linhas de "quem foi" (sem revelar o ato). A carta em foco fica
/// acesa e as outras, apagadas. Só desenha e troca o foco; a abertura cuida da entrada e do tempo.
/// </summary>
public partial class OriginCards : Control
{
    /// <summary>Largura de cada carta e o espaço entre elas, em fração da largura da tela.</summary>
    [Export] public float CardWidthFraction { get; set; } = 0.21f;
    [Export] public float CardGapFraction { get; set; } = 0.015f;

    /// <summary>Distância das cartas até a base da tela, em fração da altura (o reflexo continua visível em cima).</summary>
    [Export] public float BottomFraction { get; set; } = 0.06f;

    /// <summary>Ampliação do símbolo (pixel art de 64 px, sem filtro).</summary>
    [Export] public int SymbolScale { get; set; } = 3;

    /// <summary>Cor das cartas fora de foco.</summary>
    [Export] public Color DimmedColor { get; set; } = new(0.55f, 0.55f, 0.6f, 0.85f);

    private readonly IReadOnlyList<Origin> _origins;
    private readonly List<Control> _cards = new();
    private readonly List<(Label Name, Label Craft, Label Ambition, Label Who)> _labels = new();
    private Label _title = null!;
    private Label _hint = null!;

    public int Selected { get; private set; }

    public Origin Current => _origins[Selected];

    public OriginCards(IReadOnlyList<Origin> origins)
    {
        _origins = origins;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    // Construtor sem argumentos exigido pelo Godot para nós criados em C#.
    public OriginCards() : this(Array.Empty<Origin>())
    {
    }

    public override void _Ready()
    {
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.SetAnchorsPreset(LayoutPreset.CenterBottom);
        row.GrowHorizontal = GrowDirection.Both;
        row.GrowVertical = GrowDirection.Begin;
        AddChild(row);

        foreach (var origin in _origins)
        {
            var card = UiStyle.MakePanel();
            var column = new VBoxContainer();
            card.AddChild(column);

            var symbol = new TextureRect
            {
                Texture = GD.Load<Texture2D>(origin.Symbol),
                TextureFilter = TextureFilterEnum.Nearest,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            symbol.CustomMinimumSize = symbol.Texture.GetSize() * SymbolScale;
            symbol.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            column.AddChild(symbol);

            var name = UiStyle.MakeLabel("", UiStyle.TitleFontSize);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            var craft = UiStyle.MakeLabel("");
            craft.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            var ambition = UiStyle.MakeLabel("");
            ambition.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            var who = UiStyle.MakeLabel("");
            who.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            who.AddThemeColorOverride("font_color", UiStyle.Palette.IvoryDim);
            column.AddChild(name);
            column.AddChild(craft);
            column.AddChild(ambition);
            column.AddChild(who);

            row.AddChild(card);
            _cards.Add(card);
            _labels.Add((name, craft, ambition, who));
        }

        _title = UiStyle.MakeLabel("", UiStyle.TitleFontSize);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _title.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
        _title.GrowHorizontal = GrowDirection.Both;
        AddChild(_title);

        _hint = UiStyle.MakeLabel("");
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        _hint.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        _hint.GrowHorizontal = GrowDirection.Both;
        _hint.GrowVertical = GrowDirection.Begin;
        AddChild(_hint);

        Resized += () => Layout(row);
        Layout(row);
        Select(0);
    }

    /// <summary>Escreve os textos (depois da escolha do corpo, para o gênero das palavras sair certo).</summary>
    public void Refresh(Func<string, string> text)
    {
        _title.Text = text("origem.titulo");
        _hint.Text = text("origem.dica");
        for (var i = 0; i < _origins.Count; i++)
        {
            var origin = _origins[i];
            var labels = _labels[i];
            labels.Name.Text = text(origin.NameKey);
            labels.Craft.Text = $"{text("origem.oficio")}: {text(origin.CraftKey)}";
            labels.Ambition.Text = $"{text("origem.ambicao")}: {text(origin.AmbitionKey)}";
            labels.Who.Text = text(origin.WhoKey);
        }
    }

    public void Select(int index)
    {
        if (_origins.Count == 0)
        {
            return;
        }

        Selected = (index % _origins.Count + _origins.Count) % _origins.Count;
        for (var i = 0; i < _cards.Count; i++)
        {
            _cards[i].Modulate = i == Selected ? Colors.White : DimmedColor;
        }
    }

    private void Layout(HBoxContainer row)
    {
        var screen = Size;
        row.AddThemeConstantOverride("separation", Mathf.RoundToInt(screen.X * CardGapFraction));
        foreach (var card in _cards)
        {
            card.CustomMinimumSize = new Vector2(Mathf.Round(screen.X * CardWidthFraction), 0f);
        }

        var bottom = Mathf.Round(screen.Y * BottomFraction);
        row.OffsetBottom = -bottom;
        _hint.OffsetBottom = -bottom / 4f;
        _title.OffsetTop = bottom;
    }
}
