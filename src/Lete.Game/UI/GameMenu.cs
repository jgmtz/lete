using System;
using System.Collections.Generic;
using Godot;

namespace Lete.Game.UI;

/// <summary>
/// Menu do jogo com abas (Diário, Mosaico e Inventário; decisão do João, 29/out/2026). Cada aba tem a sua tecla, que é só um
/// atalho: abre o menu direto nela, ou troca para ela se o menu já estiver aberto, ou fecha se ela já estiver na
/// frente. Dentro do menu, <c>menu_tab_prev</c>/<c>menu_tab_next</c> (ou o clique no nome) passam de uma aba para
/// outra, e <c>cancel</c> fecha.
/// </summary>
public partial class GameMenu : CanvasLayer
{
    /// <summary>Tamanho do painel em 1080p (o conteúdo da maior aba, a silhueta de 160 px a 3×, cabe nele).</summary>
    [Export] public Vector2 PanelSize { get; set; } = new(1280, 760);

    private readonly List<IMenuTab> _tabs = new();
    private readonly List<Button> _headers = new();
    private PanelContainer _panel = null!;
    private Control _content = null!;
    private Label _hint = null!;

    public bool IsOpen => _panel.Visible;

    public int Current { get; private set; }

    public event Action? OpenChanged;

    public override void _Ready()
    {
        Layer = 6;
        _tabs.Add(new JournalTab());
        _tabs.Add(new MosaicTab());
        _tabs.Add(new InventoryTab());

        _panel = UiStyle.MakePanel();
        _panel.CustomMinimumSize = PanelSize;
        AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center, Control.LayoutPresetMode.KeepSize);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 24);
        _panel.AddChild(column);

        var headers = new HBoxContainer();
        headers.AddThemeConstantOverride("separation", 48);
        column.AddChild(headers);
        for (var i = 0; i < _tabs.Count; i++)
        {
            var index = i;
            var header = new Button { Flat = true, FocusMode = Control.FocusModeEnum.None };
            header.AddThemeFontSizeOverride("font_size", UiStyle.TitleFontSize);
            header.Pressed += () => Show(index);
            headers.AddChild(header);
            _headers.Add(header);
        }

        _content = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(_content);
        foreach (var tab in _tabs)
        {
            tab.View.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _content.AddChild(tab.View);
        }

        _hint = UiStyle.MakeLabel("");
        _hint.HorizontalAlignment = HorizontalAlignment.Right;
        _hint.AddThemeColorOverride("font_color", UiStyle.Palette.IvoryDim);
        column.AddChild(_hint);

        _panel.Visible = false;
        GameRoot.Instance.StateReplaced += RefreshTexts;
    }

    public override void _ExitTree()
    {
        GameRoot.Instance.StateReplaced -= RefreshTexts;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (@event.IsActionPressed(_tabs[i].ShortcutAction))
            {
                if (IsOpen && Current == i)
                {
                    SetOpen(false);
                }
                else
                {
                    Open(i);
                }

                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (!IsOpen)
        {
            return;
        }

        if (@event.IsActionPressed("cancel"))
        {
            SetOpen(false);
        }
        else if (@event.IsActionPressed("menu_tab_prev"))
        {
            Show(Current - 1);
        }
        else if (@event.IsActionPressed("menu_tab_next"))
        {
            Show(Current + 1);
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    /// <summary>Abre o menu na aba do atalho pedido (<c>journal</c>, <c>inventory</c>).</summary>
    public void Open(string shortcutAction) => Open(_tabs.FindIndex(t => t.ShortcutAction == shortcutAction));

    public void Open(int index)
    {
        Show(index);
        if (!IsOpen)
        {
            SetOpen(true);
        }
    }

    public void SetOpen(bool open)
    {
        if (open)
        {
            RefreshTexts();
        }

        _panel.Visible = open;
        OpenChanged?.Invoke();
    }

    /// <summary>Redesenha os textos (depois de trocar o gênero no debug ou de carregar um save).</summary>
    public void RefreshTexts()
    {
        _hint.Text = GameRoot.Instance.Text("ui.menu.dica");
        for (var i = 0; i < _tabs.Count; i++)
        {
            _headers[i].Text = GameRoot.Instance.Text(_tabs[i].TitleKey);
        }

        _tabs[Current].Refresh();
    }

    private void Show(int index)
    {
        Current = (index % _tabs.Count + _tabs.Count) % _tabs.Count;
        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].View.Visible = i == Current;
            var color = i == Current ? UiStyle.Palette.Gold : UiStyle.Palette.IvoryDim;
            foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color" })
            {
                _headers[i].AddThemeColorOverride(state, color);
            }
        }

        _tabs[Current].Refresh();
    }
}
