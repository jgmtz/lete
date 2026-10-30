using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Lete.Game.UI;

namespace Lete.Game.Combat;

/// <summary>
/// Uma opção do menu: identificador, chave do rótulo, chave da descrição, se está disponível e se vem depois de
/// uma linha separadora (como Fugir, que fica fora do grupo, GDD 4.7).
/// </summary>
/// <summary>Uma opção do menu. <see cref="Guilt"/>: Culpa de uma Pesada encarada, em terracota (GDD 2.2).</summary>
public sealed record MenuEntry(
    string Id, string LabelKey, string DescriptionKey, bool Available = true, bool SeparatorBefore = false, bool Guilt = false);

/// <summary>
/// Caixa de ações do combate em pixel art (GDD 4.7): moldura de vaso grego, faixa de meandro, fonte pixelada
/// e cursor dourado. Fica dentro do viewport de baixa resolução, para ter o mesmo pixel da cena.
/// Mostra uma página de opções por vez (menu principal, Aproximar, escolha da fixação); quem usa decide as
/// páginas. Cores: escolhida em osso claro, disponível em tom normal, indisponível em cinza (a descrição ainda
/// aparece, para explicar o porquê).
/// </summary>
public partial class ActionMenu : Control
{
    private const string FramePath = "res://art/ui/painel_acoes.png";
    private const string MeanderPath = "res://art/ui/meandro.png";
    private const string CursorPath = "res://art/ui/cursor.png";

    [Export] public BattleHudStyle Style { get; set; } = null!;

    private VBoxContainer _box = null!;
    private VBoxContainer _column = null!;
    private TextureRect _friezeTop = null!;
    private TextureRect _friezeBottom = null!;
    private readonly List<(MenuEntry Entry, Button Button)> _buttons = new();
    private string? _focusId;
    private Rect2 _rect;

    /// <summary>Opção confirmada (id) e se ela está disponível.</summary>
    public event Action<string, bool>? ActionChosen;

    /// <summary>A opção selecionada mudou: chave de localização da descrição.</summary>
    public event Action<string>? DescriptionChanged;

    /// <summary>A caixa mudou de tamanho ou de lugar (para o retrato acompanhar).</summary>
    public event Action? Relaid;

    /// <summary>O jogador pediu para voltar (<c>cancel</c>) com o menu aberto.</summary>
    public event Action? BackRequested;

    public bool MenuVisible => Visible;

    /// <summary>Id da opção com o cursor, ou nulo.</summary>
    public string? FocusedId => _focusId;

    /// <summary>Posição da opção na página atual (-1 se não estiver nela).</summary>
    public int IndexOf(string? id) => _buttons.FindIndex(b => b.Entry.Id == id);

    /// <summary>A opção está na página atual e disponível.</summary>
    public bool IsAvailable(string id) => _buttons.Any(b => b.Entry.Id == id && b.Entry.Available);

    /// <summary>Onde a caixa ficou na tela (pixels do viewport), depois do layout.</summary>
    public Rect2 BoxRect => new(_box.Position, _box.Size);

    private static string T(string key) => GameRoot.Instance.Text(key);

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Theme = UiStyle.BuildTheme(1);

        _box = new VBoxContainer();
        _box.AddThemeConstantOverride("separation", 0);
        AddChild(_box);

        _box.AddChild(MakeFrieze(out _friezeTop));

        var frame = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        frame.AddThemeStyleboxOverride("panel", new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>(FramePath),
            TextureMarginLeft = 5,
            TextureMarginTop = 5,
            TextureMarginRight = 5,
            TextureMarginBottom = 5,
            ContentMarginLeft = 7,
            ContentMarginTop = 5,
            ContentMarginRight = 10,
            ContentMarginBottom = 5,
        });
        _box.AddChild(frame);
        _box.AddChild(MakeFrieze(out _friezeBottom));

        _column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        _column.AddThemeConstantOverride("separation", 1);
        frame.AddChild(_column);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Visible && @event.IsActionPressed("cancel"))
        {
            GetViewport().SetInputAsHandled();
            BackRequested?.Invoke();
        }
    }

    /// <summary>Troca as opções do menu. O cursor vai para <paramref name="focusId"/> (ou para a primeira opção).</summary>
    public void ShowPage(IReadOnlyList<MenuEntry> entries, string? focusId = null)
    {
        foreach (var child in _column.GetChildren())
        {
            _column.RemoveChild(child);
            child.QueueFree();
        }

        _buttons.Clear();
        foreach (var entry in entries)
        {
            if (entry.SeparatorBefore)
            {
                // Filete de ouro antes de Fugir (fora do grupo, GDD 4.7).
                _column.AddChild(new ColorRect { Color = Style.Palette.Gold, CustomMinimumSize = new Vector2(0, 1) });
            }

            _buttons.Add((entry, AddEntry(entry)));
        }

        _focusId = entries.Any(e => e.Id == focusId) ? focusId : entries.FirstOrDefault()?.Id;
        CallDeferred(MethodName.Relayout);
        if (Visible)
        {
            FocusCurrent();
        }
    }

    /// <summary>
    /// Posiciona a caixa no retângulo do layout (pixels do viewport): no tamanho natural, encostada no canto
    /// inferior direito dele.
    /// </summary>
    public void ApplyLayout(Rect2 rect)
    {
        _rect = rect;
        CallDeferred(MethodName.Relayout);
    }

    public void ShowMenu(bool visible)
    {
        var opening = visible && !Visible;
        Visible = visible;
        if (visible)
        {
            FocusCurrent();
        }

        if (opening)
        {
            DrawFriezes();
        }
    }

    /// <summary>Ao abrir, os frisos de meandro se desenham da esquerda para a direita.</summary>
    private void DrawFriezes()
    {
        var tween = CreateTween().SetParallel();
        foreach (var frieze in new[] { _friezeTop, _friezeBottom })
        {
            var width = frieze.GetParent<Control>().Size.X;
            frieze.Size = frieze.Size with { X = 0f };
            tween.TweenProperty(frieze, "size:x", width, Style.MenuOpenSeconds);
        }
    }

    /// <summary>Faixa de meandro com recorte, para poder crescer da esquerda para a direita.</summary>
    private static Control MakeFrieze(out TextureRect frieze)
    {
        var meander = GD.Load<Texture2D>(MeanderPath);
        var clip = new Control { ClipContents = true, CustomMinimumSize = new Vector2(0, meander.GetHeight()) };
        frieze = new TextureRect
        {
            Texture = meander,
            StretchMode = TextureRect.StretchModeEnum.Tile,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Size = new Vector2(0, meander.GetHeight()),
        };
        clip.AddChild(frieze);
        clip.Resized += () => clip.GetChild<TextureRect>(0).Size = clip.Size;
        return clip;
    }

    private void FocusCurrent() => _buttons.FirstOrDefault(b => b.Entry.Id == _focusId).Button?.GrabFocus();

    private void Relayout()
    {
        _box.Size = Vector2.Zero;
        var size = _box.GetCombinedMinimumSize();
        _box.Size = size;
        _box.Position = (_rect.End - size).Round();
        Relaid?.Invoke();
    }

    private Button AddEntry(MenuEntry entry)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 3);
        _column.AddChild(row);

        var cursorTexture = GD.Load<Texture2D>(CursorPath);
        var cursor = new TextureRect
        {
            Texture = cursorTexture,
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            CustomMinimumSize = new Vector2(cursorTexture.GetWidth(), 0),
            Modulate = Colors.Transparent,
        };
        row.AddChild(cursor);

        var button = new Button { Text = T(entry.LabelKey), Alignment = HorizontalAlignment.Left, FocusMode = FocusModeEnum.All };
        foreach (var style in new[] { "normal", "hover", "pressed", "focus", "disabled" })
        {
            button.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
        }

        var normal = !entry.Available ? Style.Palette.ClayGray : entry.Guilt ? Style.Palette.Terracotta : Style.Palette.IvoryDim;
        var selected = entry.Available ? Style.Palette.Ivory : Style.Palette.ClayGray;
        button.AddThemeColorOverride("font_color", normal);
        button.AddThemeColorOverride("font_hover_color", selected);
        button.AddThemeColorOverride("font_focus_color", selected);
        button.AddThemeColorOverride("font_pressed_color", selected);
        button.AddThemeColorOverride("font_hover_pressed_color", selected);

        button.FocusEntered += () =>
        {
            cursor.Modulate = Colors.White;
            _focusId = entry.Id;
            DescriptionChanged?.Invoke(entry.DescriptionKey);
        };
        button.FocusExited += () => cursor.Modulate = Colors.Transparent;
        button.MouseEntered += button.GrabFocus;
        button.Pressed += () => ActionChosen?.Invoke(entry.Id, entry.Available);
        row.AddChild(button);
        return button;
    }
}
