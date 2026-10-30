using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;
using Lete.Game.UI;

namespace Lete.Game.Debug;

/// <summary>
/// Painel de debug (ação "debug_toggle", F1): render, gênero, teleporte, flags, Diário, Marca, Memórias Pesadas,
/// regra C e o save. Maré, Lembranças e juramentos entram com os sistemas (M2).
/// </summary>
public partial class DebugPanel : CanvasLayer
{
    private readonly Main _main;
    private PanelContainer _panel = null!;
    private Button _renderButton = null!;
    private Button _genderButton = null!;
    private VBoxContainer _flagList = null!;
    private LineEdit _flagInput = null!;
    private Label _markState = null!;
    private Label _heavyState = null!;
    private Label _tideState = null!;
    private Label _profileState = null!;
    private Label _pathState = null!;
    private Label _oathState = null!;
    private Label _dialogueState = null!;
    private Button _companionButton = null!;
    private Label _mosaicState = null!;
    private OptionButton _sourcePicker = null!;
    private readonly List<(string Recollection, string Source)> _sources = new();
    private Label _saveResult = null!;

    /// <summary>Distância das bordas da tela e largura do painel, em pixels 1080p.</summary>
    private const int Margin = 24;
    private const int Width = 520;

    public DebugPanel(Main main) => _main = main;

    // Construtor sem parâmetros exigido pelo Godot para tipos de script.
    public DebugPanel() => _main = null!;

    public bool IsOpen => _panel.Visible;

    public event Action? OpenChanged;

    private static string T(string key) => GameRoot.Instance.Text(key);

    public override void _Ready()
    {
        Layer = 10;

        _panel = UiStyle.MakePanel();
        AddChild(_panel);

        // Coluna à esquerda na altura da tela, com rolagem: o painel cresce a cada sistema novo.
        _panel.SetAnchorsPreset(Control.LayoutPreset.LeftWide);
        _panel.OffsetLeft = Margin;
        _panel.OffsetTop = Margin;
        _panel.OffsetBottom = -Margin;
        _panel.OffsetRight = Margin + Width;

        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _panel.AddChild(scroll);
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(column);

        column.AddChild(UiStyle.MakeLabel(T("debug.titulo"), UiStyle.TitleFontSize));

        column.AddChild(UiStyle.MakeLabel(T("debug.render")));
        _renderButton = new Button();
        _renderButton.Pressed += () => _main.SetLowResRender(!_main.LowResRender);
        column.AddChild(_renderButton);

        column.AddChild(UiStyle.MakeLabel(T("debug.genero")));
        _genderButton = new Button();
        _genderButton.Pressed += ToggleGender;
        column.AddChild(_genderButton);

        column.AddChild(UiStyle.MakeLabel(T("debug.teleporte")));
        var teleports = new HFlowContainer();
        foreach (var point in _main.TeleportPoints)
        {
            var button = new Button { Text = point.Name };
            button.Pressed += () => _main.TeleportPlayer(point);
            teleports.AddChild(button);
        }
        column.AddChild(teleports);

        column.AddChild(UiStyle.MakeLabel(T("debug.flags")));
        _flagList = new VBoxContainer();
        column.AddChild(_flagList);
        var addRow = new HBoxContainer();
        _flagInput = new LineEdit { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _flagInput.TextSubmitted += _ => AddFlag();
        var addButton = new Button { Text = T("debug.flags.adicionar") };
        addButton.Pressed += AddFlag;
        addRow.AddChild(_flagInput);
        addRow.AddChild(addButton);
        column.AddChild(addRow);

        var clearJournal = new Button { Text = T("debug.diario.limpar") };
        clearJournal.Pressed += () => GameRoot.Instance.State.JournalEntries.Clear();
        column.AddChild(clearJournal);

        column.AddChild(UiStyle.MakeLabel(T("debug.marca")));
        _markState = UiStyle.MakeLabel("");
        _markState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_markState);
        var markRow = new HFlowContainer();
        AddButton(markRow, "debug.marca.somar", () => GameRoot.Instance.Mark.Add(1));
        AddButton(markRow, "debug.marca.absorver", () => GameRoot.Instance.Events.Publish(new ShadeAbsorbed("debug")));
        AddButton(markRow, "debug.marca.rosto", () => GameRoot.Instance.Mark.SettleFace());
        column.AddChild(markRow);
        _heavyState = UiStyle.MakeLabel("");
        _heavyState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_heavyState);
        var heavyRow = new HFlowContainer();
        AddButton(heavyRow, "debug.pesada.aflorar", () =>
        {
            if (GameRoot.Instance.HeavyMemories.Next is { } next)
            {
                SetOpen(false);
                _main.SurfaceHeavyMemory(next.Id);
            }
        });
        AddButton(heavyRow, "debug.pesada.encarar", () => ChooseHeavy(HeavyMemoryChoice.Face));
        AddButton(heavyRow, "debug.pesada.deixar", () => ChooseHeavy(HeavyMemoryChoice.Leave));
        column.AddChild(heavyRow);

        column.AddChild(UiStyle.MakeLabel(T("debug.mare")));
        _tideState = UiStyle.MakeLabel("");
        column.AddChild(_tideState);
        var tideRow = new HFlowContainer();
        AddButton(tideRow, "debug.mare.marco", () =>
            GameRoot.Instance.Tide.Milestone($"marco_debug_{GameRoot.Instance.State.TideMilestones.Count + 1}"));
        AddButton(tideRow, "debug.mare.viagem", () => GameRoot.Instance.Tide.Travel());
        AddButton(tideRow, "debug.mare.derrota", () => GameRoot.Instance.Tide.Defeat(1));
        column.AddChild(tideRow);

        column.AddChild(UiStyle.MakeLabel(T("debug.dialogo")));
        _dialogueState = UiStyle.MakeLabel("");
        _dialogueState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_dialogueState);
        var dialogueRow = new HFlowContainer();
        AddButton(dialogueRow, "debug.dialogo.oferenda", () => GameRoot.Instance.Events.Publish(new OfferingShared("padeiro", "farinha")));
        _companionButton = new Button();
        _companionButton.Pressed += () =>
        {
            // Debug: troca o companheiro à vontade (no jogo a escolha é irreversível).
            var state = GameRoot.Instance.State;
            var all = new[] { "aulo", "ismena", "dimas", "septimia", "calias", "lia" };
            var index = Array.IndexOf(all, state.CompanionId);
            state.CompanionId = index + 1 < all.Length ? all[index + 1] : null;
            Refresh();
        };
        dialogueRow.AddChild(_companionButton);
        column.AddChild(dialogueRow);

        column.AddChild(UiStyle.MakeLabel(T("debug.juramento")));
        _oathState = UiStyle.MakeLabel("");
        _oathState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_oathState);
        var oathRow = new HFlowContainer();
        AddButton(oathRow, "debug.juramento.jurar", () =>
        {
            var oaths = GameRoot.Instance.Oaths;
            if (oaths.Definitions.Keys.OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault(oaths.CanSwear) is { } id)
            {
                oaths.Swear(id);
            }
        });
        AddButton(oathRow, "debug.juramento.quebrar", () =>
        {
            if (GameRoot.Instance.Oaths.Active.FirstOrDefault() is { } oath)
            {
                GameRoot.Instance.Oaths.Break(oath.Id);
            }
        });
        column.AddChild(oathRow);

        column.AddChild(UiStyle.MakeLabel(T("debug.caminho")));
        _pathState = UiStyle.MakeLabel("");
        _pathState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_pathState);

        column.AddChild(UiStyle.MakeLabel(T("debug.perfil")));
        _profileState = UiStyle.MakeLabel("");
        _profileState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_profileState);
        var profileRow = new HFlowContainer();
        foreach (var via in Enum.GetValues<Via>())
        {
            AddButton(profileRow, $"debug.perfil.via.{ConditionParser.SnakeCase(via)}", () => PublishVia(via));
        }

        AddButton(profileRow, "debug.perfil.derrota", () => GameRoot.Instance.Events.Publish(new PlayerDefeated("debug")));
        column.AddChild(profileRow);

        column.AddChild(UiStyle.MakeLabel(T("debug.mosaico")));
        _sourcePicker = new OptionButton();
        foreach (var recollection in GameRoot.Instance.Mosaic.Definitions.Values.OrderBy(r => r.Id))
        {
            foreach (var source in recollection.Sources)
            {
                _sources.Add((recollection.Id, source.Id));
                _sourcePicker.AddItem($"{recollection.Id} ← {source.Id}{(source.Reliable ? "" : " (?)")}{(source.Contradicts ? " (≠)" : "")}");
            }
        }
        column.AddChild(_sourcePicker);
        var mosaicRow = new HFlowContainer();
        AddButton(mosaicRow, "debug.mosaico.ouvir", () =>
        {
            if (_sourcePicker.Selected >= 0)
            {
                var (recollection, source) = _sources[_sourcePicker.Selected];
                GameRoot.Instance.Mosaic.Hear(recollection, source);
            }
        });
        AddButton(mosaicRow, "debug.mosaico.ato", () =>
        {
            if (_sourcePicker.Selected >= 0)
            {
                var mosaic = GameRoot.Instance.Mosaic;
                var definition = mosaic.Definitions[_sources[_sourcePicker.Selected].Recollection];
                if (definition.FullBy.Count > 0)
                {
                    mosaic.CompleteAct(definition.Id, definition.FullBy[0]);
                }
            }
        });
        column.AddChild(mosaicRow);
        _mosaicState = UiStyle.MakeLabel("");
        column.AddChild(_mosaicState);

        column.AddChild(UiStyle.MakeLabel(T("debug.save")));
        var saveRow = new HBoxContainer();
        AddButton(saveRow, "debug.save.salvar", () => ShowSaveResult("debug.save.salvar", GameRoot.Instance.Save()));
        AddButton(saveRow, "debug.save.carregar", () => ShowSaveResult("debug.save.carregar", GameRoot.Instance.Load()));
        column.AddChild(saveRow);
        _saveResult = UiStyle.MakeLabel("");
        column.AddChild(_saveResult);

        _panel.Visible = false;
        Refresh();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_toggle"))
        {
            SetOpen(!IsOpen);
            GetViewport().SetInputAsHandled();
        }
    }

    public void SetOpen(bool open)
    {
        _panel.Visible = open;
        Refresh();
        OpenChanged?.Invoke();
    }

    public void Refresh()
    {
        _renderButton.Text = T(_main.LowResRender ? "debug.render.baixa" : "debug.render.direto");
        _genderButton.Text = T(GameRoot.Instance.State.Gender == Gender.Feminine ? "debug.genero.f" : "debug.genero.m");
        RefreshFlags();
        RefreshMark();
        var state = GameRoot.Instance.State;
        _tideState.Text = string.Format(T("debug.mare.estado"), state.Tide, GameRoot.Instance.Tide.Step, state.TideMilestones.Count);
        RefreshMosaic();
        RefreshProfile();
        RefreshPath();
        RefreshOaths();
        var dialogue = GameRoot.Instance.State.Dialogue;
        _dialogueState.Text = string.Format(T("debug.dialogo.estado"),
            dialogue.Awakened.Count == 0 ? "-" : string.Join(", ", dialogue.Awakened), dialogue.Chosen.Count, dialogue.CommentsSeen.Count);
        _companionButton.Text = string.Format(T("debug.dialogo.companheiro"), GameRoot.Instance.State.CompanionId ?? T("debug.dialogo.nenhum"));
    }

    private void AddButton(Container row, string key, Action action)
    {
        var button = new Button { Text = T(key) };
        button.Pressed += () =>
        {
            action();
            Refresh();
        };
        row.AddChild(button);
    }

    /// <summary>Decide a próxima Pesada da fila sem a cena (aflorar pelo botão mostra a cena provisória).</summary>
    private static void ChooseHeavy(HeavyMemoryChoice choice)
    {
        var heavy = GameRoot.Instance.HeavyMemories;
        if (heavy.Next is { } next)
        {
            heavy.Choose(next.Id, choice);
        }
    }

    private void ShowSaveResult(string actionKey, Error result)
    {
        _saveResult.Text = string.Format(T("debug.save.resultado"), T(actionKey), result);
        _main.OnTextsChanged();
    }

    /// <summary>Um encontro de mentira encerrado pela via (Absorver também sobe a Marca, como no jogo).</summary>
    private static void PublishVia(Via via)
    {
        var events = GameRoot.Instance.Events;
        const string shade = "debug";
        switch (via)
        {
            case Via.Fight: events.Publish(new ShadeDestroyed(shade)); break;
            case Via.Free: events.Publish(new ShadeFreed(shade)); break;
            case Via.Absorb: events.Publish(new ShadeAbsorbed(shade)); break;
            case Via.Witness: events.Publish(new ShadeWitnessed(shade)); break;
            case Via.Spare: events.Publish(new ShadeSpared(shade)); break;
            case Via.Flee: events.Publish(new ShadeFled(shade)); break;
            case Via.Yield: events.Publish(new ShadeYielded(shade)); break;
        }
    }

    /// <summary>Cada juramento com o estado (ativo, quebrado, cumprido ou não jurado).</summary>
    private void RefreshOaths()
    {
        var oaths = GameRoot.Instance.Oaths;
        var lines = oaths.Definitions.Keys.OrderBy(k => k, StringComparer.Ordinal)
            .Select(id => string.Format(T("debug.juramento.estado"), id, oaths.StatusOf(id)?.ToString() ?? T("debug.juramento.livre")));
        if (oaths.ConditionsWaitingForSystems.Count > 0)
        {
            lines = lines.Append(string.Format(T("debug.juramento.esperando"), string.Join(", ", oaths.ConditionsWaitingForSystems)));
        }

        _oathState.Text = string.Join("\n", lines);
    }

    /// <summary>Cada chave do Caminho com a forma que a deu (ou "falta"). Uma flag nova (acima) já pode dar uma chave.</summary>
    private void RefreshPath()
    {
        var path = GameRoot.Instance.PathKeys;
        var lines = path.Keys.Keys.OrderBy(k => k, StringComparer.Ordinal)
            .Select(k => string.Format(T("debug.caminho.estado"), k, path.WayOf(k) ?? T("debug.caminho.falta")));
        if (path.WaysWaitingForSystems.Count > 0)
        {
            lines = lines.Append(string.Format(T("debug.caminho.esperando"), string.Join(", ", path.WaysWaitingForSystems)));
        }

        _pathState.Text = string.Join("\n", lines);
    }

    /// <summary>O Perfil em números: só no debug (o jogador só o sente pelo mundo).</summary>
    private void RefreshProfile()
    {
        var profile = GameRoot.Instance.Profile;
        string Totals<T>(IReadOnlyDictionary<T, int> counts) where T : struct, Enum =>
            counts.Count == 0 ? "-" : string.Join(", ", counts.OrderBy(p => p.Key).Select(p => $"{ConditionParser.SnakeCase(p.Key)} {p.Value}"));
        _profileState.Text = string.Format(T("debug.perfil.estado"),
            profile.TopVia is { } via ? ConditionParser.SnakeCase(via) : "-",
            profile.TopKind is { } kind ? ConditionParser.SnakeCase(kind) : "-",
            GameRoot.Instance.State.Profile.Recent.Count,
            Totals(profile.ViaTotals),
            Totals(profile.KindTotals));
    }

    /// <summary>Estágio e confiança de cada Lembrança começada (falsas também), e a Chave de Memória.</summary>
    private void RefreshMosaic()
    {
        var mosaic = GameRoot.Instance.Mosaic;
        var lines = GameRoot.Instance.State.Recollections.Keys.OrderBy(id => id).Select(id => string.Format(
            T("debug.mosaico.estado"), id, mosaic.StageOf(id)?.ToString() ?? "-", mosaic.CertaintyOf(id)));
        _mosaicState.Text = string.Join("\n", lines.Append(string.Format(T("debug.mosaico.chave"), mosaic.FullKinds, mosaic.HasMemoryKey)));
    }

    /// <summary>Aqui a Marca aparece como número: só no debug, nunca para o jogador (princípio 1).</summary>
    private void RefreshMark()
    {
        var root = GameRoot.Instance;
        var state = root.State;
        _markState.Text = string.Format(T("debug.marca.estado"), state.Mark, root.Mark.Band, state.MarkStage, state.FinalFace,
            state.HeavyMemories.Values.Count(c => c == HeavyMemoryChoice.Face),
            state.HeavyMemories.Values.Count(c => c == HeavyMemoryChoice.Leave));
        var heavy = root.HeavyMemories;
        _heavyState.Text = string.Format(T("debug.pesada.estado"),
            heavy.Next?.Id ?? T("debug.pesada.nenhuma"), heavy.Lightness.LucidityMax, heavy.Lightness.Protections);
    }

    private void ToggleGender()
    {
        var state = GameRoot.Instance.State;
        state.Gender = state.Gender == Gender.Feminine ? Gender.Masculine : Gender.Feminine;
        _main.OnTextsChanged();
        Refresh();
    }

    private void AddFlag()
    {
        var flag = _flagInput.Text.Trim();
        if (flag.Length > 0)
        {
            GameRoot.Instance.State.Flags.Add(flag);
            _flagInput.Clear();
            // Mudar o estado sem evento: o Caminho confere de novo (uma flag pode dar uma chave).
            GameRoot.Instance.PathKeys.Refresh();
            GameRoot.Instance.Oaths.Refresh();
            Refresh();
        }
    }

    private void RefreshFlags()
    {
        foreach (var child in _flagList.GetChildren())
        {
            child.QueueFree();
        }

        var flags = GameRoot.Instance.State.Flags.OrderBy(f => f, StringComparer.Ordinal).ToList();
        if (flags.Count == 0)
        {
            _flagList.AddChild(UiStyle.MakeLabel(T("debug.flags.nenhuma")));
            return;
        }

        foreach (var flag in flags)
        {
            var box = new CheckBox { Text = flag, ButtonPressed = true };
            box.Toggled += on =>
            {
                if (!on)
                {
                    GameRoot.Instance.State.Flags.Remove(flag);
                    CallDeferred(MethodName.RefreshFlags);
                }
            };
            _flagList.AddChild(box);
        }
    }
}
