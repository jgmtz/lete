using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;
using Lete.Core.Data;
using Lete.Game.Combat;
using Lete.Game.Combat.Gestures;

namespace Lete.Game.Sandbox;

/// <summary>
/// Batalha de teste com a máquina de turnos (GDD 4.1), na arte escolhida (pixel_160) e com o HUD em pixel art.
/// Lia é a protagonista; Aulo é o companheiro e dá Postura a cada 2 rodadas. A sombra (o Remador,
/// em rascunho) repete um ciclo de golpes: direto (aparar com o Anel) e de padrão (Esquiva).
/// Vias: Atacar (Combater) e Aproximar → Escutar (gesto Segurar; o que se ouve depende do golpe que ela vai
/// repetir) e Libertar (escolher a fixação e conduzir o Arrastar; custa Lucidez). Fôlego zerado: a luta recomeça.
/// Teclas de debug: <c>debug_turn</c> (T) passa a vez do jogador, <c>debug_reveal</c> (N) revela nome e depois
/// fixação, <c>debug_gesture_area</c> (G) mostra a área dos gestos.
/// </summary>
public partial class BattleAttackTest : Node
{
    private const string DifficultyPath = "res://content/difficulty/estige.json";
    private const string AttackPath = "res://content/gestures/anel_basico.json";
    private const string ListenPath = "res://content/gestures/escutar.json";
    private const string FreePath = "res://content/gestures/arrastar_libertar.json";
    private const string HudLayoutsPath = "res://content/battle/hud_layouts.json";
    private const string GesturesFolder = "res://content/gestures";
    private const string CompanionsFolder = "res://content/companions";
    private const string ShadesFolder = "res://content/shades";

    // Ids das opções do menu (as chaves de texto ficam em MenuEntry).
    private const string AttackId = "atacar";
    private const string ApproachId = "aproximar";
    private const string ListenId = "escutar";
    private const string FreeId = "libertar";
    private const string BackId = "voltar";
    private const string AbsorbId = "absorver";
    private const string EchoId = "eco";
    private const string FixationPrefix = "fixacao:";
    private const string EchoPrefix = "eco:";
    private const string AbsorbPath = "res://content/gestures/absorver.json";

    /// <summary>Raio e espessura do Anel na tela (1080p); convertidos para o viewport de baixa resolução.</summary>
    [Export] public float RingScreenRadiusPx { get; set; } = 90f;
    [Export] public float RingScreenLineWidthPx { get; set; } = 6f;

    /// <summary>Altura do centro do Anel no corpo de quem recebe o golpe (0 = pés, 1 = topo da cabeça).</summary>
    [Export] public float RingBodyHeight { get; set; } = 0.55f;

    /// <summary>Pausas da cena, em segundos.</summary>
    [Export] public float ResultHoldSeconds { get; set; } = 0.4f;
    [Export] public float ShadeTellSeconds { get; set; } = 1.2f;
    [Export] public float SupportSeconds { get; set; } = 1.4f;
    [Export] public float LineSeconds { get; set; } = 2.8f;
    [Export] public float RageLineSeconds { get; set; } = 1.6f;
    [Export] public float DefeatFadeSeconds { get; set; } = 1.2f;
    [Export] public float DefeatHoldSeconds { get; set; } = 1.5f;

    [Export] public BattleHudStyle HudStyle { get; set; } = null!;

    private GestureWindows _windows = null!;
    private GradeValues _damage = null!;
    private RingTiming _attackRing = null!;
    private HoldTiming _listenHold = null!;
    private DragSettings _freeDrag = null!;
    private float _shadeMaxResolve;
    private IReadOnlyList<ShadeAttack> _shadeAttacks = null!;
    private IReadOnlyList<ListenEntry> _listenEntries = null!;
    private string _murmurKey = "";
    private LiberationRules _liberationRules = null!;
    private AbsorbRules _absorbRules = null!;
    private HoldTiming _absorbHold = null!;
    private ProtagonistStats _protagonistStats = null!;
    private CompanionSupport? _support;
    private HudLayoutSet _layouts = null!;
    private readonly Dictionary<string, string> _gestures = new();

    private BattleFlow _flow = null!;
    private ShadeUnderstanding _understanding = null!;
    private ShadeListening _listening = null!;

    private BattleStage _stage = null!;
    private RingGesture _ring = null!;
    private DodgeGesture _dodge = null!;
    private HoldGesture _hold = null!;
    private DragGesture _drag = null!;
    private BattleHud _hud = null!;
    private ActionMenu _menu = null!;
    private TurnRing _turnRing = null!;
    private TaskCompletionSource? _playerAction;
    private string _mainFocus = AttackId;
    private MenuPage _page;

    private enum MenuPage
    {
        Main,
        Approach,
        Fixation,
        Echo,
    }

    // Ecos e Marca duram a sessão inteira do teste (entre uma luta e outra); no jogo, vão para o WorldState (M2).
    private static readonly EchoSlots Echoes = new();
    private static readonly MarkCounter Mark = new();

    private static string T(string key) => GameRoot.Instance.Text(key);

    public override void _Ready()
    {
        _windows = GestureData.WindowsFromDifficulty(FileAccess.GetFileAsString(DifficultyPath));
        var attack = FileAccess.GetFileAsString(AttackPath);
        _damage = CombatData.ResolveDamageFromJson(attack);
        _attackRing = GestureData.RingFromJson(attack);
        _listenHold = GestureData.HoldFromJson(FileAccess.GetFileAsString(ListenPath));
        _freeDrag = GestureData.DragFromJson(FileAccess.GetFileAsString(FreePath));
        _layouts = HudData.FromJson(FileAccess.GetFileAsString(HudLayoutsPath));

        var container = GetNode<SubViewportContainer>("ViewportContainer");
        _stage = container.GetNode<BattleStage>("SubViewport/Stage");
        _ring = container.GetNode<RingGesture>("SubViewport/RingLayer/Ring");
        _dodge = container.GetNode<DodgeGesture>("SubViewport/RingLayer/Dodge");
        _hold = container.GetNode<HoldGesture>("SubViewport/RingLayer/Hold");
        _drag = container.GetNode<DragGesture>("SubViewport/RingLayer/Drag");
        _hud = container.GetNode<BattleHud>("SubViewport/HudLayer/BattleHud");
        _menu = container.GetNode<ActionMenu>("SubViewport/HudLayer/ActionMenu");

        var battle = FileAccess.GetFileAsString(_stage.SetupPath);
        _protagonistStats = CombatData.ProtagonistFromJson(battle);
        if (_stage.Setup.ProtagonistSlotError(_protagonistStats.Id) is { } slotError)
        {
            GD.PushError(slotError);
        }

        if (CombatData.CompanionIdFromJson(battle) is { } companionId)
        {
            _support = CombatData.CompanionFromJson(FileAccess.GetFileAsString($"{CompanionsFolder}/{companionId}.json"));
        }

        var shadeId = CombatData.ShadeIdFromJson(battle)
            ?? throw new System.FormatException("A batalha precisa do campo 'shade'.");
        var shade = FileAccess.GetFileAsString($"{ShadesFolder}/{shadeId}.json");
        _shadeMaxResolve = CombatData.ShadeResolveFromJson(shade);
        _shadeAttacks = CombatData.ShadeAttacksFromJson(shade);
        (_listenEntries, _murmurKey) = ShadeData.ListeningFromJson(shade);
        _liberationRules = ShadeData.LiberationFromJson(shade);
        _absorbRules = ShadeData.AbsorbFromJson(shade);
        _absorbHold = GestureData.HoldFromJson(FileAccess.GetFileAsString(AbsorbPath));
        _gestures[_absorbRules.Echo.Gesture] = FileAccess.GetFileAsString($"{GesturesFolder}/{_absorbRules.Echo.Gesture}.json");
        foreach (var gesture in _shadeAttacks.Select(a => a.Gesture).Distinct())
        {
            _gestures[gesture] = FileAccess.GetFileAsString($"{GesturesFolder}/{gesture}.json");
        }

        // Encolhe o render até 1 pixel da arte = 1 pixel renderizado (640 px na tela ÷ 160 px da arte = 4).
        var art = GD.Load<Texture2D>($"{_stage.ArtRoot}/{_stage.Setup.Styles[_stage.Style].Folder}/{_stage.Party[0].Art.Art}.png");
        var shrink = Mathf.Max(1, Mathf.RoundToInt(_stage.ScreenHeightPx * _stage.Party[0].Art.Height / art.GetHeight()));
        container.StretchShrink = shrink;

        _ring.CircleRadius = Mathf.Round(RingScreenRadiusPx / shrink);
        _ring.LineWidth = Mathf.Max(1f, Mathf.Round(RingScreenLineWidthPx / shrink));
        foreach (var gesture in new Control[] { _ring, _dodge, _hold, _drag })
        {
            gesture.Visible = false;
        }

        _turnRing = new TurnRing { Tuning = _stage.Tuning };
        _stage.AddChild(_turnRing);

        _hud.Track(_stage.Camera, Shade);

        _menu.DescriptionChanged += key => _hud.SetDescription(T(key));
        _menu.ActionChosen += OnActionChosen;
        _menu.BackRequested += GoBack;
        _menu.Relaid += () => _hud.PlacePortrait(_menu.BoxRect);
        _menu.ShowMenu(false);

        ApplyLayout();

        NewBattle();
        RunBattle();

        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--demo") >= 0)
        {
            RunDemo();
        }
        Debug.ScreenshotArgs.Run(this);
    }

    private BattleActor Protagonist => ActorById(_protagonistStats.Id);
    private BattleActor? Companion => _support is null ? null : ActorById(_support.CompanionId);
    private BattleActor Shade => _stage.Enemies[0].Node;
    private Liberation Liberation => _flow.Liberation!;

    /// <summary>Layout escolhido: o normal (decisão do João, 25/out/2026).</summary>
    private HudLayout Layout => _layouts.Layouts[0];

    private BattleActor ActorById(string id) => _stage.Party.First(p => p.Art.Id == id).Node;

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_turn") && _menu.MenuVisible)
        {
            // Passa a vez sem agir.
            _menu.ShowMenu(false);
            _playerAction?.TrySetResult();
        }
        else if (@event.IsActionPressed("debug_reveal"))
        {
            if (!_understanding.NameRevealed)
            {
                _understanding.RevealName();
            }
            else
            {
                _understanding.RevealFixation();
            }

            RefreshShadeKnowledge();
        }
        else if (@event.IsActionPressed("debug_gesture_area"))
        {
            _hud.ToggleGestureAreaGuide();
        }
    }

    private void NewBattle()
    {
        _understanding = new ShadeUnderstanding();
        _listening = new ShadeListening(_listenEntries, _murmurKey, _understanding);
        _flow = new BattleFlow(
            new Gauge(_protagonistStats.Breath),
            new Gauge(_protagonistStats.Lucidity),
            new ShadeResolve(_shadeMaxResolve),
            _shadeAttacks,
            _support,
            new Liberation(_liberationRules));
        _hud.SetBreath(_flow.Breath.Fraction);
        _hud.SetLucidity(_flow.Lucidity.Fraction);
        _hud.SetResolve(_flow.Resolve.Fraction);
        _hud.SetPeace(0f);
        _hud.SetTargetMarked(true);
        _mainFocus = AttackId;
        Echoes.NewBattle();
        RefreshShadeKnowledge();
        RefreshSupport();
    }

    /// <summary>Conduz a luta passo a passo, como a máquina de turnos manda.</summary>
    private async void RunBattle()
    {
        while (IsInsideTree())
        {
            switch (_flow.Phase)
            {
                case BattlePhase.PlayerTurn:
                    await PlayerTurn();
                    _flow.EndPlayerAction();
                    break;
                case BattlePhase.CompanionSupport:
                    await SupportTurn();
                    _flow.EndSupport();
                    break;
                case BattlePhase.ShadeTurn:
                    await ShadeTurn();
                    _flow.EndShadeTurn();
                    break;
                case BattlePhase.Victory:
                    await Victory();
                    break;
                case BattlePhase.Defeat:
                    await Defeat();
                    break;
            }

            RefreshSupport();
        }
    }

    // ---- Vez do jogador e menu -------------------------------------------------------------------------

    private async Task PlayerTurn()
    {
        _turnRing.MoveUnder(Protagonist);
        _hud.SetTurn(Protagonist, shadeTurn: false);
        var portrait = _stage.Party.First(p => p.Art.Id == _protagonistStats.Id).Art.Portrait;
        if (portrait is not null)
        {
            _hud.SetPortrait(GD.Load<Texture2D>(portrait));
        }

        // A legenda anuncia o próximo gesto da sombra: é a principal fonte de informação (GDD 4.7),
        // e diz em que momento do ciclo o Escutar vai cair.
        _hud.SetCaption(T(_flow.NextShadeAttack.CaptionKey));
        _hud.SetGestureActive(false);
        _playerAction = new TaskCompletionSource();
        ShowMainPage();
        _menu.ShowMenu(true);
        await _playerAction.Task;
    }

    private void ShowMainPage()
    {
        _page = MenuPage.Main;
        var entries = new List<MenuEntry>
        {
            new(AttackId, "combate.acao.atacar", "combate.acao.atacar.descricao"),
            new("memoria", "combate.acao.memoria", "combate.acao.memoria.descricao"),
            new(ApproachId, "combate.acao.aproximar", "combate.acao.aproximar.descricao"),
        };

        // Absorver só aparece com a Firmeza baixa (escolha do João: no menu principal, 25/out/2026).
        if (_absorbRules.CanAbsorb(_flow.Resolve))
        {
            entries.Add(new MenuEntry(AbsorbId, "combate.acao.absorver", "combate.acao.absorver.descricao"));
        }

        var echoDescription = Echoes.Echoes.Count == 0 ? "combate.acao.eco.indisponivel"
            : Echoes.HasUsable ? "combate.acao.eco.descricao"
            : "combate.acao.eco.usado";
        entries.Add(new MenuEntry(EchoId, "combate.acao.eco", echoDescription, Available: Echoes.HasUsable));
        entries.Add(new MenuEntry("bolsa", "combate.acao.bolsa", "combate.acao.bolsa.descricao"));
        entries.Add(new MenuEntry("fugir", "combate.acao.fugir", "combate.acao.fugir.descricao", SeparatorBefore: true));
        _menu.ShowPage(entries, _mainFocus);
    }

    /// <summary>Aproximar (GDD 4.7) agrupa os outros caminhos; Libertar fica aqui, indisponível até a Compreensão completa.</summary>
    private void ShowApproachPage(string? focus = null)
    {
        _page = MenuPage.Approach;
        var freeDescription = !_understanding.IsComplete ? "combate.acao.libertar.indisponivel"
            : !Liberation.CanAttempt(_understanding, _flow.Lucidity) ? "combate.acao.libertar.sem_lucidez"
            : "combate.acao.libertar.descricao";
        _menu.ShowPage(new[]
        {
            new MenuEntry(ListenId, "combate.acao.escutar", "combate.acao.escutar.descricao"),
            new MenuEntry(FreeId, "combate.acao.libertar", freeDescription, Available: Liberation.CanAttempt(_understanding, _flow.Lucidity)),
            new MenuEntry("oferenda", "combate.acao.oferenda", "combate.acao.oferenda.descricao"),
            new MenuEntry("presenciar", "combate.acao.presenciar", "combate.acao.presenciar.descricao"),
            new MenuEntry("poupar", "combate.acao.poupar", "combate.acao.poupar.descricao"),
            new MenuEntry(BackId, "combate.acao.voltar", "combate.acao.voltar.descricao", SeparatorBefore: true),
        }, focus);
    }

    /// <summary>Escolha da fixação: as opções que a sombra ainda não corrigiu.</summary>
    private void ShowFixationPage()
    {
        _page = MenuPage.Fixation;
        var entries = Liberation.RemainingOptions
            .Select(o => new MenuEntry(FixationPrefix + o.Id, o.LabelKey, "combate.libertar.escolha.descricao"))
            .Append(new MenuEntry(BackId, "combate.acao.voltar", "combate.acao.voltar.descricao", SeparatorBefore: true))
            .ToList();
        _menu.ShowPage(entries);
    }

    /// <summary>Os Ecos guardados (até 3); cada um uma vez por batalha.</summary>
    private void ShowEchoPage()
    {
        _page = MenuPage.Echo;
        var entries = Echoes.Echoes
            .Select(e => new MenuEntry(
                EchoPrefix + e.Id,
                e.NameKey,
                Echoes.CanUse(e.Id) ? e.DescriptionKey : "combate.acao.eco.usado",
                Available: Echoes.CanUse(e.Id)))
            .Append(new MenuEntry(BackId, "combate.acao.voltar", "combate.acao.voltar.descricao", SeparatorBefore: true))
            .ToList();
        _menu.ShowPage(entries);
    }

    private void GoBack()
    {
        switch (_page)
        {
            case MenuPage.Fixation:
                ShowApproachPage(FreeId);
                break;
            case MenuPage.Approach or MenuPage.Echo:
                ShowMainPage();
                break;
        }
    }

    private void OnActionChosen(string id, bool available)
    {
        if (!available || _flow.Phase != BattlePhase.PlayerTurn)
        {
            return;
        }

        if (_page == MenuPage.Main)
        {
            _mainFocus = id;
        }

        switch (id)
        {
            case AttackId:
                DoAttack();
                break;
            case ApproachId:
                ShowApproachPage();
                break;
            case ListenId:
                DoListen();
                break;
            case FreeId:
                ShowFixationPage();
                break;
            case AbsorbId:
                DoAbsorb();
                break;
            case EchoId:
                ShowEchoPage();
                break;
            case BackId:
                GoBack();
                break;
            case var fixation when fixation.StartsWith(FixationPrefix):
                DoFree(fixation[FixationPrefix.Length..]);
                break;
            case var echo when echo.StartsWith(EchoPrefix):
                DoEcho(echo[EchoPrefix.Length..]);
                break;
            default:
                _hud.SetDescription(T("combate.teste.ainda_nao"));
                break;
        }
    }

    // ---- Ações do jogador -----------------------------------------------------------------------------

    private async void DoAttack()
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true);
        var grade = _windows.Grade(await RunRing(_attackRing, Shade, _windows));

        // O falho ainda cumpre a ação (GDD 4.4): o golpe acontece, só mais fraco.
        await Protagonist.PlayAdvance(Shade.Position, () =>
        {
            _flow.Resolve.Reduce(_damage.For(grade));
            _hud.SetResolve(_flow.Resolve.Fraction);
            return Shade.PlayHit(HitStrength(grade));
        });

        _playerAction?.TrySetResult();
    }

    /// <summary>Escutar: o que se ouve vem do golpe que ela vai repetir agora (o que a legenda anunciou).</summary>
    private async void DoListen()
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.escutar"));
        var grade = _windows.Grade(await RunHold(_listenHold));

        var heard = _listening.Listen(_flow.NextShadeAttackIndex, grade);
        RefreshShadeKnowledge();
        await ShowLine(T(heard.TextKey), LineSeconds);
        _playerAction?.TrySetResult();
    }

    /// <summary>Libertar acreditando na fixação escolhida: gasta Lucidez; a errada enfurece a sombra.</summary>
    private async void DoFree(string fixation)
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.libertar"));
        var grade = _windows.Grade(await RunDrag());

        var result = Liberation.Attempt(fixation, grade, _flow.Lucidity);
        _hud.SetLucidity(_flow.Lucidity.Fraction);
        _hud.SetPeace(Liberation.Fraction);
        await ShowLine(T(result.TextKey), LineSeconds);
        if (!result.Correct)
        {
            _flow.Enrage();
            await ShowLine(T("combate.enfurecida"), RageLineSeconds);
        }

        _playerAction?.TrySetResult();
    }

    /// <summary>
    /// Absorver (gesto Segurar): quanto mais fraca a sombra, mais rápido a barra enche. Mesmo falho, absorve
    /// (GDD 4.4: a ação se cumpre), mas a Marca pesa mais. O Eco dela vai para um espaço livre.
    /// </summary>
    private async void DoAbsorb()
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.absorver"));
        var factor = _absorbRules.FillFactor(_flow.Resolve.Fraction);
        var grade = _windows.Grade(await RunHold(_absorbHold with { FillSeconds = _absorbHold.FillSeconds * factor }));

        AddMark((int)_absorbRules.MarkGain.For(grade));
        var echo = _absorbRules.Echo;
        var line = Echoes.Echoes.Any(e => e.Id == echo.Id) ? T("combate.eco.repetido")
            : Echoes.Add(echo) ? string.Format(T("combate.eco.obtido"), T(echo.NameKey))
            : T("combate.eco.cheio");
        _flow.AbsorbShade();
        await ShowLine(line, LineSeconds);
        _playerAction?.TrySetResult();
    }

    /// <summary>Eco: o golpe da sombra de origem, no gesto dela (o Remador: dois anéis no ritmo da remada).</summary>
    private async void DoEcho(string id)
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.eco"));
        var echo = Echoes.Use(id);
        var gesture = _gestures[echo.Gesture];
        var timing = GestureData.RingFromJson(gesture);
        var repeats = GestureData.RingRepeats(gesture);
        var total = 0f;
        for (var i = 0; i < repeats; i++)
        {
            total += await RunRing(timing, Shade, _windows);
        }

        var grade = _windows.Grade(total / repeats);
        AddMark(echo.MarkOnUse);
        await Protagonist.PlayAdvance(Shade.Position, () =>
        {
            _flow.Resolve.Reduce(echo.ResolveDamage.For(grade));
            _hud.SetResolve(_flow.Resolve.Fraction);
            return Shade.PlayHit(HitStrength(grade));
        });

        _playerAction?.TrySetResult();
    }

    /// <summary>A Marca nunca aparece como número para o jogador (princípio 1): só no console de debug.</summary>
    private static void AddMark(int amount)
    {
        Mark.Add(amount);
        GD.Print($"[debug] Marca: {Mark.Value}");
    }

    // ---- Apoio e vez da sombra ------------------------------------------------------------------------

    private async Task SupportTurn()
    {
        if (Companion is not { } companion || _support is null)
        {
            return;
        }

        _turnRing.MoveUnder(companion);
        _hud.SetTurn(companion, shadeTurn: false);
        _hud.SetCaption(T($"combate.apoio.{_support.SupportId}.age"));
        await Wait(SupportSeconds);
    }

    private async Task ShadeTurn()
    {
        var attack = _flow.NextShadeAttack;
        _turnRing.MoveUnder(Shade);
        _hud.SetTurn(null, shadeTurn: true);
        _hud.SetCaption(T(attack.CaptionKey));
        _hud.SetGestureActive(true);
        await Wait(ShadeTellSeconds);

        var windows = _flow.SupportActive && _support is not null ? _windows.Widened(_support.WindowWidening) : _windows;
        var gesture = _gestures[attack.Gesture];

        if (attack.Kind == ShadeAttackKind.Direct)
        {
            _hud.SetGestureActive(true, T("combate.gesto.aparar"));
            var grade = windows.Grade(await RunRing(GestureData.RingFromJson(gesture), Protagonist, windows));
            await Shade.PlayAdvance(Protagonist.Position, () => TakeHit(attack, grade));
        }
        else
        {
            _hud.SetGestureActive(true, T("combate.gesto.esquivar"));
            var (settings, pattern) = GestureData.DodgeFromJson(gesture);
            var grade = windows.Grade(await RunDodge(settings, pattern));
            await TakeHit(attack, grade);
        }
    }

    /// <summary>Dano no Fôlego conforme a defesa (mais forte com a sombra enfurecida); o aparo perfeito não deixa o golpe chegar.</summary>
    private Task TakeHit(ShadeAttack attack, GestureGrade grade)
    {
        var rage = _flow.Enraged ? _liberationRules.EnragedDamageMultiplier : 1f;
        _flow.Breath.Reduce(attack.BreathDamage.For(grade) * rage);
        _hud.SetBreath(_flow.Breath.Fraction);
        return grade == GestureGrade.Perfect
            ? Task.CompletedTask
            // Quanto pior a defesa, mais forte o tremor: o bom treme como um golpe fraco, o falho como um médio.
            : Protagonist.PlayHit(grade == GestureGrade.Good ? _stage.Tuning.HitStrengthFailed : _stage.Tuning.HitStrengthGood);
    }

    // ---- Fim da luta ----------------------------------------------------------------------------------

    private async Task Victory()
    {
        _hud.SetTurn(null, shadeTurn: false);
        _hud.SetTargetMarked(false);
        if (_flow.Outcome == BattleOutcome.Freed)
        {
            _hud.SetCaption(T("combate.sombra.libertada"));
            await Shade.PlayDepart();
        }
        else if (_flow.Outcome == BattleOutcome.Absorbed)
        {
            _hud.SetCaption(T("combate.sombra.absorvida"));
            await Shade.PlayAbsorbInto(Protagonist.Position);
        }
        else
        {
            _hud.SetCaption(T("combate.sombra.desfeita"));
            await Shade.PlayDisintegrate();
        }

        await Wait(DefeatHoldSeconds);
        await Restart();
    }

    private async Task Defeat()
    {
        _hud.SetTurn(null, shadeTurn: false);
        _hud.SetCaption(T("combate.derrota"));
        await Protagonist.PlayDefeat();
        await _hud.Fade(toBlack: true, DefeatFadeSeconds);
        await Wait(DefeatHoldSeconds);
        await Restart();
    }

    /// <summary>Teste: tudo volta ao início para lutar de novo (o custo real da derrota vem com as Trívias).</summary>
    private async Task Restart()
    {
        Shade.Revive();
        Protagonist.Revive();
        NewBattle();
        _hud.SetCaption(T("combate.recomeca"));
        await _hud.Fade(toBlack: false, DefeatFadeSeconds);
        await Wait(ResultHoldSeconds);
    }

    // ---- Gestos ---------------------------------------------------------------------------------------

    private async Task<float> RunRing(RingTiming timing, BattleActor target, GestureWindows windows)
    {
        _ring.Timing = timing;
        _ring.Center = _stage.Camera.UnprojectPosition(target.PointAtHeight(RingBodyHeight));
        _ring.Visible = true;
        var score = await RunGesture(_ring);
        _ring.CircleColor = ResultColor(windows.Grade(score));
        if (windows.Grade(score) == GestureGrade.Perfect)
        {
            _ring.PlayPerfectBurst();
        }

        await Wait(ResultHoldSeconds);
        _ring.Visible = false;
        _hud.SetGestureActive(true); // tira a dica: o gesto acabou
        return score;
    }

    private async Task<float> RunDodge(DodgeSettings settings, IDodgePattern pattern)
    {
        _dodge.Settings = settings;
        _dodge.Pattern = pattern;
        _dodge.Center = BetweenPartyAndShade();
        void OnHit() => _ = Protagonist.PlayHit(_stage.Tuning.HitStrengthFailed);
        _dodge.Hit += OnHit;
        var score = await RunGesture(_dodge);
        _dodge.Hit -= OnHit;
        await Wait(ResultHoldSeconds);
        _dodge.Visible = false;
        _hud.SetGestureActive(true); // tira a dica: o gesto acabou
        return score;
    }

    private async Task<float> RunHold(HoldTiming timing)
    {
        _hold.Timing = timing;
        _hold.Center = GestureAreaCenter();
        var score = await RunGesture(_hold);
        await Wait(ResultHoldSeconds);
        _hold.Visible = false;
        _hud.SetGestureActive(true); // tira a dica: o gesto acabou
        return score;
    }

    private async Task<float> RunDrag()
    {
        _drag.Settings = _freeDrag;
        _drag.Center = GestureAreaCenter();
        var score = await RunGesture(_drag);
        await Wait(ResultHoldSeconds);
        _drag.Visible = false;
        _hud.SetGestureActive(true); // tira a dica: o gesto acabou
        return score;
    }

    private static Task<float> RunGesture(IGesture gesture)
    {
        var done = new TaskCompletionSource<float>();
        void OnCompleted(float score)
        {
            gesture.Completed -= OnCompleted;
            done.TrySetResult(score);
        }

        gesture.Completed += OnCompleted;
        gesture.Begin();
        return done.Task;
    }

    // ---- HUD ------------------------------------------------------------------------------------------

    private async Task ShowLine(string text, float seconds)
    {
        _hud.SetCaption(text);
        await Wait(seconds);
    }

    /// <summary>
    /// Centro do quadro da Esquiva: no meio do caminho entre quem se defende e a sombra (na altura do peito),
    /// sem sair da área central reservada aos gestos.
    /// </summary>
    private Vector2 BetweenPartyAndShade()
    {
        var camera = _stage.Camera;
        var middle = (camera.UnprojectPosition(Protagonist.PointAtHeight(RingBodyHeight))
            + camera.UnprojectPosition(Shade.PointAtHeight(RingBodyHeight))) / 2f;
        var screen = _hud.GetViewportRect().Size;
        var area = _layouts.GestureArea;
        var min = new Vector2(area.X * screen.X, area.Y * screen.Y);
        var max = new Vector2(area.Right * screen.X, area.Bottom * screen.Y);
        return middle.Clamp(min, max).Round();
    }

    private Vector2 GestureAreaCenter()
    {
        var screen = _hud.GetViewportRect().Size;
        var area = _layouts.GestureArea;
        return new Vector2((area.X + area.Width / 2f) * screen.X, (area.Y + area.Height / 2f) * screen.Y).Round();
    }

    private float HitStrength(GestureGrade grade) => grade switch
    {
        GestureGrade.Perfect => _stage.Tuning.HitStrengthPerfect,
        GestureGrade.Good => _stage.Tuning.HitStrengthGood,
        _ => _stage.Tuning.HitStrengthFailed,
    };

    private Color ResultColor(GestureGrade grade) => grade switch
    {
        GestureGrade.Perfect => HudStyle.Palette.Gold,
        GestureGrade.Good => HudStyle.Palette.Ivory,
        _ => HudStyle.Palette.ClayGray,
    };

    private void RefreshSupport()
    {
        if (_support is null)
        {
            return;
        }

        var status = _flow.SupportActive ? T("combate.apoio.ativo")
            : _flow.RoundsUntilSupport switch
            {
                0 => T("combate.apoio.agora"),
                1 => T("combate.apoio.em_um_turno"),
                var n => string.Format(T("combate.apoio.em_turnos"), n),
            };
        _hud.SetSupport($"{T($"companheiro.{_support.CompanionId}.nome")} · {T($"combate.apoio.{_support.SupportId}")}", status);
    }

    private void ApplyLayout()
    {
        var screen = _hud.GetViewportRect().Size;
        var menu = Layout.Menu;
        var rect = BattleHud.Snap(new Rect2(menu.X * screen.X, menu.Y * screen.Y, menu.Width * screen.X, menu.Height * screen.Y));

        _hud.ApplyLayout(Layout, _layouts.GestureArea);
        // A caixa se ajusta no quadro seguinte e avisa (Relaid); o retrato vai ao lado dela.
        _menu.ApplyLayout(rect);
    }

    private void RefreshShadeKnowledge()
    {
        var nameKey = _stage.Enemies[0].Art.NameKey;
        _hud.SetShadeName(T(_understanding.NameRevealed && nameKey is not null ? nameKey : "combate.sombra.nome_oculto"));
        _hud.SetUnderstanding(_understanding.Points);
    }

    // ---- Modo demo ------------------------------------------------------------------------------------

    /// <summary>Período (em ms, dividido por 2π) do sobe e desce do marcador da Esquiva no modo demo.</summary>
    private const float DemoDodgeSwingMs = 600f;

    /// <summary>Distância (pixels da arte) em que o demo considera um ponto do caminho do Arrastar alcançado.</summary>
    private const float DemoDragReach = 2f;

    /// <summary>
    /// Com <c>-- --demo</c>: joga sozinho para gravar vídeo, mostrando a via Libertar inteira. Escuta nos três
    /// momentos do ciclo (pista, nome, fixação), tenta Libertar com a fixação errada (a sombra corrige e se
    /// enfurece) e depois com a certa até ela partir. Na luta seguinte só ataca. Apara no tempo e foge dos golpes
    /// subindo e descendo (de propósito, nem sempre consegue).
    /// </summary>
    private async void RunDemo()
    {
        var script = new Queue<string[]>(new[]
        {
            new[] { ApproachId, ListenId },
            new[] { ApproachId, ListenId },
            new[] { ApproachId, ListenId },
            new[] { ApproachId, FreeId, FixationPrefix + "saudade" },
            new[] { ApproachId, FreeId, FixationPrefix + "culpa" },
            new[] { ApproachId, FreeId, FixationPrefix + "culpa" },
        });

        while (IsInsideTree())
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (_menu.MenuVisible && _flow.Phase == BattlePhase.PlayerTurn)
            {
                await Wait(1f);
                var choices = script.Count > 0 ? script.Dequeue()
                    : _menu.IsAvailable(AbsorbId) ? new[] { AbsorbId }
                    : Echoes.HasUsable ? new[] { EchoId, EchoPrefix + Echoes.Echoes[0].Id }
                    : new[] { AttackId };
                foreach (var choice in choices)
                {
                    await Choose(choice);
                }
            }
            else if (_ring.IsRunning)
            {
                await PressRingAt(0.02f, _ring.Timing);
            }
            else if (_hold.IsRunning)
            {
                await HoldUntil(_hold.Timing.FillSeconds * _hold.Timing.Target);
            }
            else if (_drag.IsRunning)
            {
                await FollowPath();
            }
            else
            {
                // Na Esquiva, sobe e desce devagar: às vezes escapa, às vezes leva golpe. Fora dela, solta tudo.
                var up = _dodge.IsRunning && Mathf.Sin(Time.GetTicksMsec() / DemoDodgeSwingMs) > 0f;
                var down = _dodge.IsRunning && !up;
                Input.ParseInputEvent(new InputEventAction { Action = "move_up", Pressed = up });
                Input.ParseInputEvent(new InputEventAction { Action = "move_down", Pressed = down });
            }
        }
    }

    /// <summary>Como o jogador: sobe ou desce o cursor até a opção e confirma.</summary>
    private async Task Choose(string id)
    {
        for (var tries = 0; _menu.FocusedId != id && tries < 12; tries++)
        {
            await Press(_menu.IndexOf(id) > _menu.IndexOf(_menu.FocusedId) ? "ui_down" : "ui_up");
        }

        await Press("ui_accept");
        await Wait(0.4f);
    }

    private async Task PressRingAt(float offset, RingTiming timing)
    {
        await Wait(Mathf.Max(0f, timing.ShrinkSeconds + offset - _ring.Elapsed - 0.02f));
        await Press("gesture_confirm");
        while (_ring.Visible)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private async Task HoldUntil(float seconds)
    {
        await Wait(0.3f);
        Input.ParseInputEvent(new InputEventAction { Action = "gesture_confirm", Pressed = true });
        await Wait(seconds);
        Input.ParseInputEvent(new InputEventAction { Action = "gesture_confirm", Pressed = false });
        while (_hold.Visible)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>Conduz a luz ponto a ponto pelo caminho, com o analógico simulado (força de cada direção).</summary>
    private async Task FollowPath()
    {
        var points = _drag.PathPoints;
        var next = 1;
        while (_drag.IsRunning)
        {
            var toTarget = points[next] - _drag.MarkerPosition;
            if (toTarget.Length() < DemoDragReach && next < points.Length - 1)
            {
                next++;
                continue;
            }

            var dir = toTarget.Normalized();
            SetAxis("move_left", "move_right", dir.X);
            SetAxis("move_up", "move_down", dir.Y);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        SetAxis("move_left", "move_right", 0f);
        SetAxis("move_up", "move_down", 0f);
        while (_drag.Visible)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void SetAxis(string negative, string positive, float value)
    {
        Input.ParseInputEvent(new InputEventAction { Action = negative, Pressed = value < 0f, Strength = Mathf.Max(0f, -value) });
        Input.ParseInputEvent(new InputEventAction { Action = positive, Pressed = value > 0f, Strength = Mathf.Max(0f, value) });
    }

    private async Task Press(string action)
    {
        // Como uma tecla de verdade: o evento entra na fila e vale no começo do próximo quadro.
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private SignalAwaiter Wait(float seconds) =>
        ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
}
