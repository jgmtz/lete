using System;
using Godot;
using Lete.Game.World;

namespace Lete.Game.UI;

/// <summary>
/// HUD de exploração do M0: aviso de "Examinar", a frase do objeto examinado e o Diário placeholder.
/// Sem marcadores nem contagens: o Diário só lista frases.
/// </summary>
public partial class Hud : CanvasLayer
{
    private const double MessageSeconds = 5.0;

    private readonly Player _player;
    private Label _prompt = null!;
    private Label _message = null!;
    private PanelContainer _journalPanel = null!;
    private VBoxContainer _journalList = null!;
    private double _messageTimeLeft;

    public Hud(Player player) => _player = player;

    // Construtor sem parâmetros exigido pelo Godot para tipos de script.
    public Hud() => _player = null!;

    public bool JournalOpen => _journalPanel.Visible;

    public event Action? OpenChanged;

    public override void _Ready()
    {
        Layer = 5;

        _prompt = UiStyle.MakeLabel(GameRoot.Instance.Text("ui.prompt.examinar"));
        AddBottomBand(_prompt, 60, 40);

        _message = UiStyle.MakeLabel("");
        _message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddBottomBand(_message, 120, 90);

        BuildJournalPanel();

        _player.NearbyChanged += RefreshPrompt;
        Interactable.Examined += ShowExamined;
        RefreshPrompt();
        _message.Visible = false;
    }

    public override void _ExitTree()
    {
        Interactable.Examined -= ShowExamined;
    }

    public override void _Process(double delta)
    {
        if (_messageTimeLeft > 0)
        {
            _messageTimeLeft -= delta;
            _message.Visible = _messageTimeLeft > 0;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("journal"))
        {
            SetJournalOpen(!JournalOpen);
            GetViewport().SetInputAsHandled();
        }
        else if (JournalOpen && @event.IsActionPressed("cancel"))
        {
            SetJournalOpen(false);
            GetViewport().SetInputAsHandled();
        }
    }

    public void SetJournalOpen(bool open)
    {
        if (open)
        {
            RefreshJournal();
        }

        _journalPanel.Visible = open;
        if (open)
        {
            _messageTimeLeft = 0;
            _message.Visible = false;
        }

        RefreshPrompt();
        OpenChanged?.Invoke();
    }

    /// <summary>Redesenha os textos (por exemplo, depois de trocar o gênero no debug).</summary>
    public void RefreshTexts()
    {
        if (JournalOpen)
        {
            RefreshJournal();
        }
    }

    private void ShowExamined(string key)
    {
        _message.Text = GameRoot.Instance.Text(key);
        _message.Visible = true;
        _messageTimeLeft = MessageSeconds;
    }

    private void RefreshPrompt()
    {
        _prompt.Visible = !JournalOpen && _player.CurrentInteractable is not null;
    }

    private void BuildJournalPanel()
    {
        _journalPanel = UiStyle.MakePanel();
        _journalPanel.CustomMinimumSize = new Vector2(900, 500);
        AddChild(_journalPanel);
        _journalPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center, Control.LayoutPresetMode.KeepSize);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 16);
        _journalPanel.AddChild(column);

        column.AddChild(UiStyle.MakeLabel(GameRoot.Instance.Text("ui.diario.titulo"), UiStyle.TitleFontSize));

        _journalList = new VBoxContainer();
        _journalList.AddThemeConstantOverride("separation", 12);
        column.AddChild(_journalList);

        _journalPanel.Visible = false;
    }

    private void RefreshJournal()
    {
        foreach (var child in _journalList.GetChildren())
        {
            child.QueueFree();
        }

        var entries = GameRoot.Instance.Journal.Entries;
        if (entries.Count == 0)
        {
            _journalList.AddChild(UiStyle.MakeLabel(GameRoot.Instance.Text("ui.diario.vazio")));
            return;
        }

        foreach (var key in entries)
        {
            var line = UiStyle.MakeLabel("— " + GameRoot.Instance.Text(key));
            line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            line.CustomMinimumSize = new Vector2(850, 0);
            _journalList.AddChild(line);
        }
    }

    /// <summary>Faixa horizontal presa embaixo da tela, com o texto centralizado.</summary>
    private void AddBottomBand(Label label, float bottomMargin, float height)
    {
        const float sideMargin = 360;

        AddChild(label);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Bottom;
        label.AnchorLeft = 0;
        label.AnchorRight = 1;
        label.AnchorTop = 1;
        label.AnchorBottom = 1;
        label.OffsetLeft = sideMargin;
        label.OffsetRight = -sideMargin;
        label.OffsetTop = -bottomMargin - height;
        label.OffsetBottom = -bottomMargin;
    }
}
