using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;
using Lete.Core.Data;
using Lete.Core.Events;
using Lete.Core.Presentation;
using Lete.Core.State;
using Lete.Core.Systems;
using Lete.Game.Combat;
using Lete.Game.Combat.Gestures;

namespace Lete.Game.Sandbox;

/// <summary>
/// Batalha de teste com a máquina de turnos (GDD 4.1), na arte escolhida (pixel_160) e com o HUD em pixel art.
/// A protagonista-sombra luta com o companheiro do save (sem escolha, Aulo), que apoia a cada 2 rodadas (com vínculo alto, a
/// cada rodada) e fica relutante depois de uma via que ele reprova. A sombra (o Remador,
/// em rascunho) repete um ciclo de golpes: direto (aparar com o Anel) e de padrão (Esquiva).
/// Vias: Atacar (Combater) e Aproximar → Escutar (gesto Segurar; o que se ouve depende do golpe que ela vai
/// repetir) e Libertar (escolher a fixação e conduzir o Arrastar; custa Lucidez). Fôlego zerado: a luta recomeça.
/// Teclas de debug: <c>debug_turn</c> (T) passa a vez do jogador, <c>debug_reveal</c> (N) revela nome e depois
/// fixação, <c>debug_gesture_area</c> (G) mostra a área dos gestos.
/// </summary>
public partial class BattleAttackTest : Node
{
    private const string DifficultyFolder = "res://content/difficulty";
    private const string AttackPath = "res://content/gestures/anel_basico.json";
    private const string ListenPath = "res://content/gestures/escutar.json";
    private const string FreePath = "res://content/gestures/arrastar_libertar.json";
    private const string HudLayoutsPath = "res://content/battle/hud_layouts.json";
    private const string GesturesFolder = "res://content/gestures";
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
    private const string MemoryId = "memoria";
    private const string MemoryPrefix = "memoria:";
    private const string MemoriesDir = "res://content/memories";
    private const string AbsorbPath = "res://content/gestures/absorver.json";
    private const string WitnessPath = "res://content/gestures/presenciar.json";
    private const string FleePath = "res://content/gestures/fuga.json";
    private const string WitnessId = "presenciar";
    private const string KeepWitnessingId = "presenciar_continuar";
    private const string StopWitnessingId = "presenciar_parar";
    private const string SpareId = "poupar";
    private const string YieldId = "ceder";
    private const string OfferingId = "oferenda";
    private const string BagId = "bolsa";
    private const string FleeId = "fugir";
    private const string OfferingPrefix = "oferenda:";
    private const string ProvisionPrefix = "provisao:";

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
    private string _shadeId = "";
    private MemoryBook _memories = null!;
    private IReadOnlyList<MemorySkill> _allMemories = null!;
    private HashSet<string> _guiltIds = new();
    private ShadeStory _story = null!;
    private ShadeNarration _narration = null!;
    private HoldTiming _witnessHold = null!;
    private (DodgeSettings Settings, IDodgePattern Pattern, EscapeExit Exit) _flee;
    private AssistSettings _assistSettings = null!;
    private int _defeatTideSteps;
    private HoldTiming _absorbHold = null!;
    private ProtagonistStats _protagonistStats = null!;
    private CompanionSupport? _support;
    private string? _companionId;
    private bool _reluctanceSaid;
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
    private StrokeGesture _stroke = null!;
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
        Memory,
        Offering,
        Bag,
        Witness,
    }

    // Os Ecos guardados e o espólio de cada sombra estão no WorldState (ids e quantidades); aqui ficam os objetos da luta.
    private EchoSlots _echoes = new();
    private Dictionary<string, EchoDefinition> _echoDefinitions = new();

    // As derrotas (para a pista de Hécate) duram a sessão do teste. A Bolsa, o Fôlego, a Lucidez e a Maré estão no WorldState.
    private static BattleBag? _bag;
    private static int _defeats;

    /// <summary>O que esta sombra guarda de uma derrota anterior (no estado, por sombra).</summary>
    private ShadeHoard Hoard
    {
        get
        {
            var hoards = GameRoot.Instance.State.Hoards;
            if (!hoards.TryGetValue(_shadeId, out var hoard))
            {
                hoard = new HoardState();
                hoards[_shadeId] = hoard;
            }

            return new ShadeHoard(hoard);
        }
    }

    /// <summary>O modo da partida (WorldState): as janelas, a assistência e a Maré da derrota vêm dele.</summary>
    private static string DifficultyPath => $"{DifficultyFolder}/{GameRoot.Instance.State.Difficulty}.json";

    /// <summary>Os Ecos de todas as sombras (para reconstruir os espaços a partir dos ids do save).</summary>
    private static Dictionary<string, EchoDefinition> AllEchoes()
    {
        var echoes = new Dictionary<string, EchoDefinition>();
        foreach (var file in DirAccess.GetFilesAt(ShadesFolder).Where(f => f.EndsWith(".json", System.StringComparison.Ordinal)))
        {
            var json = FileAccess.GetFileAsString($"{ShadesFolder}/{file}");
            if (json.Contains("\"absorb\"") && json.Contains("\"echo\""))
            {
                var echo = ShadeData.AbsorbFromJson(json).Echo;
                echoes[echo.Id] = echo;
            }
        }

        return echoes;
    }
    private static GestureAssist _assist = GestureAssist.Off;

    private static BattleBag Bag => _bag!;

    private static string T(string key) => GameRoot.Instance.Text(key);

    /// <summary>
    /// Sem nada no Mosaico (o teste aberto direto, sem save), dá as Lembranças de teste em estágios variados, para a
    /// batalha ter Memórias fracas, médias e plenas. A "Água parada" fica em Fragmento confiável: libertar o Remador
    /// a leva à Recordação. Encara a primeira Pesada de teste e deixa a segunda (Culpa e Leveza). Com
    /// <c>-- --carregar</c>, usa o Mosaico do save.
    /// </summary>
    private static void SeedTestMosaic()
    {
        var mosaic = GameRoot.Instance.Mosaic;
        void Hear(string id, params string[] sources)
        {
            foreach (var source in sources)
            {
                if (mosaic.Definitions.TryGetValue(id, out var recollection) && recollection.Sources.Any(s => s.Id == source))
                {
                    mosaic.Hear(id, source);
                }
            }
        }

        Hear("colo_da_avo", "fonte_teste_a", "fonte_teste_b");
        Hear("noite_das_tochas", "fonte_teste_a", "fonte_teste_b");
        Hear("mesa_cheia", "fonte_teste_a");
        Hear("agua_parada", "fonte_teste_b");
        Hear("maos_no_barro", "fonte_teste_a", "fonte_teste_b");
        foreach (var id in new[] { "colo_da_avo", "maos_no_barro" })
        {
            if (mosaic.Definitions.TryGetValue(id, out var recollection) && recollection.FullBy.Count > 0)
            {
                mosaic.CompleteAct(id, recollection.FullBy[0]);
            }
        }

        // Pesadas de teste: a primeira encarada (a Culpa aparece no menu Memória), a segunda deixada (Leveza).
        var heavy = GameRoot.Instance.HeavyMemories;
        foreach (var choice in new[] { HeavyMemoryChoice.Face, HeavyMemoryChoice.Leave })
        {
            if (heavy.Next is { } next)
            {
                heavy.Choose(next.Id, choice);
            }
        }

        GD.Print("[debug] Mosaico de teste: plena, Recordação, Fragmento e as Pesadas (uma encarada, uma deixada).");
    }

    public override void _Ready()
    {
        _windows = GestureData.WindowsFromDifficulty(FileAccess.GetFileAsString(DifficultyPath));
        _echoDefinitions = AllEchoes();
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
        _stroke = container.GetNode<StrokeGesture>("SubViewport/RingLayer/Stroke");
        _hud = container.GetNode<BattleHud>("SubViewport/HudLayer/BattleHud");
        _menu = container.GetNode<ActionMenu>("SubViewport/HudLayer/ActionMenu");

        var battle = FileAccess.GetFileAsString(_stage.SetupPath);
        _protagonistStats = CombatData.ProtagonistFromJson(battle);
        // O palco põe no grupo o corpo escolhido no save; os números são os do arquivo.
        if (_stage.Party.Select(p => p.Art.Id).FirstOrDefault(id => _stage.Cast.TryGetValue(id, out var m) && m.Kind == CastKind.Protagonist) is { } body)
        {
            _protagonistStats = _protagonistStats with { Id = body };
        }

        if (_stage.Setup.ProtagonistSlotError(_protagonistStats.Id) is { } slotError)
        {
            GD.PushError(slotError);
        }

        // O companheiro é quem o palco pôs no grupo (o do save ou o do arquivo da batalha).
        _companionId = _stage.Party.Select(p => p.Art.Id)
            .FirstOrDefault(id => _stage.Cast.TryGetValue(id, out var m) && m.Kind == CastKind.Companion);

        _shadeId = CombatData.ShadeIdFromJson(battle)
            ?? throw new System.FormatException("A batalha precisa do campo 'shade'.");
        var shade = FileAccess.GetFileAsString($"{ShadesFolder}/{_shadeId}.json");
        _shadeMaxResolve = CombatData.ShadeResolveFromJson(shade);
        _shadeAttacks = CombatData.ShadeAttacksFromJson(shade);
        (_listenEntries, _murmurKey) = ShadeData.ListeningFromJson(shade);
        _liberationRules = ShadeData.LiberationFromJson(shade);
        _absorbRules = ShadeData.AbsorbFromJson(shade);
        _story = ShadeData.StoryFromJson(shade);
        foreach (var gesture in _story.Phases.SelectMany(p => p.Attacks).Select(a => a.Gesture).Distinct())
        {
            _gestures[gesture] = FileAccess.GetFileAsString($"{GesturesFolder}/{gesture}.json");
        }
        _absorbHold = GestureData.HoldFromJson(FileAccess.GetFileAsString(AbsorbPath));
        _allMemories = DirAccess.GetFilesAt(MemoriesDir)
            .Where(f => f.EndsWith(".json", System.StringComparison.Ordinal))
            .SelectMany(f => MemoryData.FromJson(FileAccess.GetFileAsString($"{MemoriesDir}/{f}")))
            .ToList();
        if (OS.GetCmdlineUserArgs().Contains("--carregar"))
        {
            GD.Print($"carregar: {GameRoot.Instance.Load()}");
        }
        else if (!GameRoot.Instance.Mosaic.Known.Any())
        {
            SeedTestMosaic();
        }

        // Teste: -- --jurar=mao_aberta começa com o juramento ativo (a opção avisa; absorver é perjúrio).
        if (System.Array.Find(OS.GetCmdlineUserArgs(), x => x.StartsWith("--jurar=")) is { } oathArg)
        {
            GD.Print($"jurar: {GameRoot.Instance.Oaths.Swear(oathArg["--jurar=".Length..])}");
        }

        _witnessHold = GestureData.HoldFromJson(FileAccess.GetFileAsString(WitnessPath));
        _flee = BagData.EscapeFromJson(FileAccess.GetFileAsString(FleePath));
        var difficulty = FileAccess.GetFileAsString(DifficultyPath);
        _echoes = new EchoSlots();
        foreach (var id in GameRoot.Instance.State.Echoes)
        {
            if (_echoDefinitions.TryGetValue(id, out var stored))
            {
                _echoes.Add(stored);
            }
        }

        _assistSettings = BagData.AssistFromDifficulty(difficulty);
        _defeatTideSteps = BagData.DefeatTideSteps(difficulty);
        // A Bolsa é a do estado; vazia (o teste aberto direto), recebe a bolsa de teste.
        var inventory = GameRoot.Instance.State.Inventory;
        if (inventory.Obols == 0 && inventory.Offerings.Values.All(c => c == 0) && inventory.Provisions.Values.All(c => c == 0))
        {
            BagData.FillFromJson(FileAccess.GetFileAsString(GameRoot.TestBagPath), inventory);
        }

        _bag = new BattleBag(inventory, GameRoot.Instance.Provisions);
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
        foreach (var gesture in new Control[] { _ring, _dodge, _hold, _drag, _stroke })
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
        ApplyMarkLook();
        RunBattle();

        if (System.Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--demo")) is { } demo)
        {
            RunDemo(demo["--demo".Length..].TrimStart('='));
        }
        Debug.ScreenshotArgs.Run(this);
    }

    private BattleActor Protagonist => ActorById(_protagonistStats.Id);
    private BattleActor? Companion => _support is null ? null : ActorById(_support.CompanionId);

    /// <summary>Absorver com o Surrupio de Dimas ativo aceita a sombra com mais Firmeza.</summary>
    private AbsorbRules Absorb => _absorbRules with
    {
        ResolveFractionMax = _absorbRules.ResolveFractionMax + _flow.SupportPower(SupportEffect.EaseAbsorb),
    };
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
        else if (@event.IsActionPressed("assist_toggle"))
        {
            // Acessibilidade ajustável a qualquer momento (GDD 4.4); vira opção do menu quando ele existir.
            _assist = AssistSettings.Next(_assist);
            _hud.SetCaption(T($"combate.assistencia.{_assist.ToString().ToLowerInvariant()}"));
        }
    }

    /// <summary>O Fôlego que sobrou da luta anterior (GDD 4.8: só volta nas fontes de cura).</summary>
    private Gauge Breath()
    {
        var gauge = new Gauge(_protagonistStats.Breath + GameRoot.Instance.Items.Modifiers.BreathMax);
        gauge.Reduce(System.Math.Min(GameRoot.Instance.State.Vitals.BreathSpent, gauge.Max - 1f));
        return gauge;
    }

    /// <summary>
    /// A Lucidez que sobrou; a Leveza das Pesadas deixadas, a bênção dos juramentos e os itens mudam o máximo (GDD 2.2,
    /// 7.1, 6).
    /// </summary>
    private Gauge Lucidity()
    {
        var gauge = new Gauge(_protagonistStats.Lucidity + GameRoot.Instance.HeavyMemories.Lightness.LucidityMax
            + GameRoot.Instance.Oaths.Effects.LucidityMax + GameRoot.Instance.Items.Modifiers.LucidityMax);
        gauge.Reduce(System.Math.Min(GameRoot.Instance.State.Vitals.LuciditySpent, gauge.Max));
        return gauge;
    }

    /// <summary>Fim da luta sem derrota: o que foi gasto fica para a próxima.</summary>
    private void KeepVitals()
    {
        var vitals = GameRoot.Instance.State.Vitals;
        vitals.BreathSpent = _flow.Breath.Max - _flow.Breath.Current;
        vitals.LuciditySpent = _flow.Lucidity.Max - _flow.Lucidity.Current;
    }

    private void NewBattle()
    {
        _understanding = new ShadeUnderstanding();
        _listening = new ShadeListening(_listenEntries, _murmurKey, _understanding);
        // O intervalo do apoio depende do vínculo, que cresce entre uma luta e outra.
        _support = _companionId is null ? null : GameRoot.Instance.Companions.Support(_companionId);
        _reluctanceSaid = false;
        _flow = new BattleFlow(
            Breath(),
            // A Leveza das Pesadas deixadas aumenta a Lucidez máxima (GDD 2.2).
            Lucidity(),
            new ShadeResolve(_shadeMaxResolve),
            _shadeAttacks,
            _support,
            new Liberation(_liberationRules));
        // Teste: -- --folego=N começa a luta com N de Fôlego (para conferir a derrota sem jogar muito).
        if (System.Array.Find(OS.GetCmdlineUserArgs(), x => x.StartsWith("--folego=")) is { } breathArg
            && float.TryParse(breathArg["--folego=".Length..], System.Globalization.CultureInfo.InvariantCulture, out var startBreath))
        {
            _flow.Breath.Reduce(System.Math.Max(0f, _flow.Breath.Max - startBreath));
        }

        // Teste: -- --firmeza=N começa a sombra com N de Firmeza (para conferir Absorver sem jogar muito).
        if (System.Array.Find(OS.GetCmdlineUserArgs(), x => x.StartsWith("--firmeza=")) is { } resolveArg
            && float.TryParse(resolveArg["--firmeza=".Length..], System.Globalization.CultureInfo.InvariantCulture, out var startResolve))
        {
            _flow.Resolve.Reduce(System.Math.Max(0f, _flow.Resolve.Max - startResolve));
        }

        _hud.SetBreath(_flow.Breath.Fraction);
        _hud.SetLucidity(_flow.Lucidity.Fraction);
        _hud.SetResolve(_flow.Resolve.Fraction);
        _hud.SetPeace(0f);
        _hud.SetTargetMarked(true);
        _hud.ResetShadePanel();
        _mainFocus = AttackId;
        _echoes.NewBattle();
        // As Memórias vêm do Mosaico (GDD 2.1): uma por Lembrança, com a força do estágio, e a Culpa de cada Pesada
        // encarada. Recomeçar a luta é descansar na Trívia: nada fica esquecido e as proteções da Leveza voltam.
        var combatMemories = GameRoot.Instance.Mosaic.CombatMemories(_allMemories);
        _guiltIds = combatMemories.Where(m => m.Guilt).Select(m => m.Skill.Id).ToHashSet();
        _memories = new MemoryBook(
            combatMemories.Select(m => m.Skill).ToList(), GameRoot.Instance.HeavyMemories.Lightness.Protections);
        GD.Print($"[debug] Memórias: {string.Join(", ", combatMemories.Select(m => m.Guilt ? $"{m.Skill.Id} (Culpa)" : m.Skill.Id))}; " +
            $"Lucidez máxima {_flow.Lucidity.Max}; proteções da Leveza {_memories.ProtectionsLeft}");
        _narration = new ShadeNarration(_story);
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
        // Com o Canto de Cálias, também o golpe depois do próximo.
        _hud.SetCaption(_flow.SupportPower(SupportEffect.Foretell) > 0f
            ? string.Format(T("combate.apoio.canto.depois"), T(_flow.NextShadeAttack.CaptionKey), T(_flow.ShadeAttackAfterNext.CaptionKey))
            : T(_flow.NextShadeAttack.CaptionKey));
        _hud.SetGestureActive(false);
        _playerAction = new TaskCompletionSource();
        if (_flow.IsWitnessing)
        {
            ShowWitnessPage();
        }
        else
        {
            ShowMainPage();
        }

        _menu.ShowMenu(true);
        await _playerAction.Task;
        await Narrate();
    }

    /// <summary>
    /// Conforme a Firmeza cai, a sombra recupera clareza e fala (GDD 4.3); na metade, o Remador muda de gesto e a
    /// fixação escapa. Só enquanto a luta continua.
    /// </summary>
    private async Task Narrate()
    {
        if (_flow.IsEnding || _flow.Resolve.IsBroken || _flow.Liberation is { IsFreed: true })
        {
            return;
        }

        foreach (var voice in _narration.NewVoices(_flow.Resolve.Fraction))
        {
            await ShowLine(T(voice.TextKey), LineSeconds);
        }

        if (_narration.NewPhase(_flow.Resolve.Fraction) is { } phase)
        {
            // A luta começa na fase 1; cada limiar de ShadeStory.Phases leva à seguinte.
            GameRoot.Instance.Events.Publish(new BossPhaseChanged(_shadeId, _story.Phases.ToList().IndexOf(phase) + 2));
            _flow.ChangeCycle(phase.Attacks);
            if (phase.Reveals == ListenReveal.Fixation)
            {
                _understanding.RevealFixation();
            }
            else if (phase.Reveals == ListenReveal.Name)
            {
                _understanding.RevealName();
            }

            RefreshShadeKnowledge();
            await ShowLine(T(phase.CaptionKey), LineSeconds);
        }
    }

    private void ShowMainPage()
    {
        _page = MenuPage.Main;
        var entries = new List<MenuEntry>
        {
            new(AttackId, "combate.acao.atacar", OathWarning(Via.Fight, "combate.acao.atacar.descricao")),
            new(MemoryId, "combate.acao.memoria", "combate.acao.memoria.descricao",
                Available: _memories.Skills.Any(s => _memories.CanUse(s.Id, _flow.Lucidity))),
            new(ApproachId, "combate.acao.aproximar", "combate.acao.aproximar.descricao"),
        };

        // Absorver só aparece com a Firmeza baixa (escolha do João: no menu principal, 25/out/2026).
        if (Absorb.CanAbsorb(_flow.Resolve))
        {
            entries.Add(new MenuEntry(AbsorbId, "combate.acao.absorver", OathWarning(Via.Absorb, "combate.acao.absorver.descricao")));
        }

        var echoesBlocked = GameRoot.Instance.Oaths.Effects.EchoesBlocked;
        var echoDescription = echoesBlocked ? "combate.acao.eco.juramento"
            : _echoes.Echoes.Count == 0 ? "combate.acao.eco.indisponivel"
            : _echoes.HasUsable ? "combate.acao.eco.descricao"
            : "combate.acao.eco.usado";
        entries.Add(new MenuEntry(EchoId, "combate.acao.eco", echoDescription, Available: _echoes.HasUsable && !echoesBlocked));
        entries.Add(new MenuEntry(BagId, "combate.acao.bolsa",
            Bag.HasAnyProvision ? "combate.acao.bolsa.descricao" : "combate.bolsa.vazia", Available: Bag.HasAnyProvision));
        entries.Add(new MenuEntry(FleeId, "combate.acao.fugir", OathWarning(Via.Flee, "combate.acao.fugir.descricao"), SeparatorBefore: true));
        _menu.ShowPage(entries, _mainFocus);
    }

    /// <summary>
    /// Antes do ato, a opção avisa que ele quebra um juramento ativo (decisão do João, 30/out/2026): escolher já é o
    /// perjúrio, sem caixa de confirmação.
    /// </summary>
    private static string OathWarning(Via via, string descriptionKey) =>
        GameRoot.Instance.Oaths.WouldBreak(via).Count > 0 ? "combate.juramento.quebra" : descriptionKey;

    /// <summary>Aproximar (GDD 4.7) agrupa os outros caminhos; Libertar fica aqui, indisponível até a Compreensão completa.</summary>
    private void ShowApproachPage(string? focus = null)
    {
        _page = MenuPage.Approach;
        var freeDescription = !_understanding.IsComplete ? "combate.acao.libertar.indisponivel"
            : !Liberation.CanAttempt(_understanding, _flow.Lucidity) ? "combate.acao.libertar.sem_lucidez"
            : OathWarning(Via.Free, "combate.acao.libertar.descricao");
        _menu.ShowPage(new[]
        {
            new MenuEntry(ListenId, "combate.acao.escutar", "combate.acao.escutar.descricao"),
            new MenuEntry(FreeId, "combate.acao.libertar", freeDescription, Available: Liberation.CanAttempt(_understanding, _flow.Lucidity)),
            new MenuEntry(OfferingId, "combate.acao.oferenda",
                Bag.HasAnyOffering ? "combate.acao.oferenda.descricao" : "combate.acao.oferenda.vazia", Available: Bag.HasAnyOffering),
            new MenuEntry(WitnessId, "combate.acao.presenciar", OathWarning(Via.Witness, "combate.acao.presenciar.descricao")),
            new MenuEntry(SpareId, "combate.acao.poupar", OathWarning(Via.Spare, "combate.acao.poupar.descricao")),
            new MenuEntry(YieldId, "combate.acao.ceder",
                _flow.Lucidity.Current >= _story.YieldLucidityCost ? OathWarning(Via.Yield, "combate.acao.ceder.descricao") : "combate.acao.ceder.sem_lucidez",
                Available: _flow.Lucidity.Current >= _story.YieldLucidityCost),
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

    /// <summary>As Memórias: cada uma com seu Traço; esquecidas ou sem Lucidez aparecem em cinza, explicando o porquê.</summary>
    private void ShowMemoryPage()
    {
        _page = MenuPage.Memory;
        var entries = _memories.Skills
            .Select(s => new MenuEntry(
                MemoryPrefix + s.Id,
                s.NameKey,
                _memories.IsForgotten(s.Id) ? "combate.memoria.esquecida"
                    : !_memories.CanUse(s.Id, _flow.Lucidity) ? "combate.memoria.sem_lucidez"
                    : s.DescriptionKey,
                Available: _memories.CanUse(s.Id, _flow.Lucidity),
                Guilt: _guiltIds.Contains(s.Id)))
            .Append(new MenuEntry(BackId, "combate.acao.voltar", "combate.acao.voltar.descricao", SeparatorBefore: true))
            .ToList();
        _menu.ShowPage(entries);
    }

    /// <summary>As oferendas da bolsa (só os tipos que ainda tem).</summary>
    private void ShowOfferingPage()
    {
        _page = MenuPage.Offering;
        var entries = Bag.Offerings.Where(o => o.Value > 0)
            .Select(o => new MenuEntry(OfferingPrefix + o.Key, $"oferenda.{o.Key}", "combate.oferenda.descricao"))
            .Append(new MenuEntry(BackId, "combate.acao.voltar", "combate.acao.voltar.descricao", SeparatorBefore: true))
            .ToList();
        _menu.ShowPage(entries);
    }

    /// <summary>As provisões da bolsa.</summary>
    private void ShowBagPage()
    {
        _page = MenuPage.Bag;
        var entries = Bag.Provisions.Where(p => Bag.ProvisionCount(p.Id) > 0)
            .Select(p => new MenuEntry(ProvisionPrefix + p.Id, p.NameKey, p.DescriptionKey))
            .Append(new MenuEntry(BackId, "combate.acao.voltar", "combate.acao.voltar.descricao", SeparatorBefore: true))
            .ToList();
        _menu.ShowPage(entries);
    }

    /// <summary>Presenciando: só manter a atenção ou parar (e voltar a lutar).</summary>
    private void ShowWitnessPage()
    {
        _page = MenuPage.Witness;
        _menu.ShowPage(new[]
        {
            new MenuEntry(KeepWitnessingId, "combate.presenciar.continuar", "combate.presenciar.continuar.descricao"),
            new MenuEntry(StopWitnessingId, "combate.presenciar.parar", "combate.presenciar.parar.descricao", SeparatorBefore: true),
        });
    }

    /// <summary>Os Ecos guardados (até 3); cada um uma vez por batalha.</summary>
    private void ShowEchoPage()
    {
        _page = MenuPage.Echo;
        var entries = _echoes.Echoes
            .Select(e => new MenuEntry(
                EchoPrefix + e.Id,
                e.NameKey,
                _echoes.CanUse(e.Id) ? e.DescriptionKey : "combate.acao.eco.usado",
                Available: _echoes.CanUse(e.Id)))
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
            case MenuPage.Approach or MenuPage.Echo or MenuPage.Memory or MenuPage.Bag:
                ShowMainPage();
                break;
            case MenuPage.Offering:
                ShowApproachPage(OfferingId);
                break;
            case MenuPage.Witness:
                // Presenciando não há para onde voltar: Esc não faz nada.
                break;
            case MenuPage.Main:
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
            case OfferingId:
                ShowOfferingPage();
                break;
            case BagId:
                ShowBagPage();
                break;
            case WitnessId:
                DoStartWitnessing();
                break;
            case KeepWitnessingId:
                DoKeepWitnessing();
                break;
            case StopWitnessingId:
                _flow.StopWitnessing();
                ShowMainPage();
                break;
            case SpareId:
                DoSpare();
                break;
            case YieldId:
                DoYield();
                break;
            case FleeId:
                DoFlee();
                break;
            case var offering when offering.StartsWith(OfferingPrefix):
                DoOffering(offering[OfferingPrefix.Length..]);
                break;
            case var provision when provision.StartsWith(ProvisionPrefix):
                DoProvision(provision[ProvisionPrefix.Length..]);
                break;
            case MemoryId:
                ShowMemoryPage();
                break;
            case var memory when memory.StartsWith(MemoryPrefix):
                DoMemory(memory[MemoryPrefix.Length..]);
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
        var grade = Windows.Grade(await RunRing(_attackRing, Shade, Windows));

        // O falho ainda cumpre a ação (GDD 4.4): o golpe acontece, só mais fraco.
        await Protagonist.PlayAdvance(Shade.Position, () =>
        {
            _flow.Resolve.Reduce(_damage.For(grade));
            _hud.SetResolve(_flow.Resolve.Fraction);
            return Shade.PlayHit(HitStrength(grade));
        });

        await Judge(Via.Fight);
        _playerAction?.TrySetResult();
    }

    /// <summary>
    /// Escutar: o que se ouve vem do golpe que ela vai repetir agora (o que a legenda anunciou). O Lamento de Ismena
    /// alarga as janelas.
    /// </summary>
    private async void DoListen()
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.escutar"));
        var grade = Windows.Widened(_flow.SupportPower(SupportEffect.WidenListen)).Grade(await RunHold(_listenHold));

        var heard = _listening.Listen(_flow.NextShadeAttackIndex, grade);
        RefreshShadeKnowledge();
        await ShowLine(T(heard.TextKey), LineSeconds);
        await Judge(Via.Free);
        _playerAction?.TrySetResult();
    }

    /// <summary>Libertar acreditando na fixação escolhida: gasta Lucidez; a errada enfurece a sombra.</summary>
    private async void DoFree(string fixation)
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.libertar"));
        var grade = Windows.Grade(await RunDrag());

        var result = Liberation.Attempt(fixation, grade, _flow.Lucidity);
        _hud.SetLucidity(_flow.Lucidity.Fraction);
        _hud.SetPeace(Liberation.Fraction);
        await ShowLine(T(result.TextKey), LineSeconds);
        await CheckForgetting();
        if (!result.Correct)
        {
            _flow.Enrage();
            await ShowLine(T("combate.enfurecida"), RageLineSeconds);
        }

        await Judge(Via.Free);
        _playerAction?.TrySetResult();
    }

    /// <summary>
    /// Memória (gesto Traço): gasta Lucidez (mais se falhar) e faz o efeito do tipo da Lembrança. Cura e golpe valem
    /// na hora; proteção, lentidão e foco valem até o fim do próximo golpe da sombra.
    /// </summary>
    private async void DoMemory(string id)
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.traco"));
        var skill = _memories.Skills.First(s => s.Id == id);
        // O Esboço de Lia alarga o Traço; a Brasa de Septímia barateia a Lucidez.
        var grade = Windows.Widened(_flow.SupportPower(SupportEffect.WidenStroke)).Grade(await RunStroke(skill.Stroke));
        var use = _memories.Use(id, grade, _flow.Lucidity, 1f - _flow.SupportPower(SupportEffect.CheaperMemory));
        GameRoot.Instance.Events.Publish(new MemoryUsed(use.Skill.Id, use.Skill.Kind));
        _hud.SetLucidity(_flow.Lucidity.Fraction);

        switch (skill.Effect)
        {
            case MemoryEffectKind.Heal:
                _flow.Breath.Restore(use.Power);
                _hud.SetBreath(_flow.Breath.Fraction);
                await ShowLine(T("combate.memoria.efeito.heal"), LineSeconds);
                break;
            case MemoryEffectKind.Strike:
                await Protagonist.PlayAdvance(Shade.Position, () =>
                {
                    _flow.Resolve.Reduce(use.Power);
                    _hud.SetResolve(_flow.Resolve.Fraction);
                    return Shade.PlayHit(HitStrength(grade));
                });
                break;
            default:
                _flow.ApplyUntilShadeTurnEnds(skill.Effect, use.Power);
                await ShowLine(T($"combate.memoria.efeito.{skill.Effect.ToString().ToLowerInvariant()}"), LineSeconds);
                break;
        }

        await CheckForgetting();
        _playerAction?.TrySetResult();
    }

    /// <summary>Lucidez zerada: a Memória mais usada fica esquecida (GDD 4.2), ou a Leveza a segura; a legenda conta.</summary>
    private async Task CheckForgetting()
    {
        if (_memories.CheckForgetting(_flow.Lucidity) is { } forgetting)
        {
            var key = forgetting.Resisted ? "combate.memoria.leveza" : "combate.memoria.esqueceu";
            await ShowLine(string.Format(T(key), T(forgetting.Skill.NameKey)), LineSeconds);
        }
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
        var grade = Windows.Grade(await RunHold(_absorbHold with { FillSeconds = _absorbHold.FillSeconds * factor }));

        AddMark((int)_absorbRules.MarkGain.For(grade));
        var echo = _absorbRules.Echo;
        var line = _echoes.Echoes.Any(e => e.Id == echo.Id) ? T("combate.eco.repetido")
            : _echoes.Add(echo) ? string.Format(T("combate.eco.obtido"), T(echo.NameKey))
            : T("combate.eco.cheio");
        if (_echoes.Echoes.Any(e => e.Id == echo.Id) && !GameRoot.Instance.State.Echoes.Contains(echo.Id))
        {
            GameRoot.Instance.State.Echoes.Add(echo.Id);
        }
        _flow.AbsorbShade();
        GameRoot.Instance.Events.Publish(new ShadeAbsorbed(_shadeId));
        await ShowLine(line, LineSeconds);
        await Judge(Via.Absorb);
        _playerAction?.TrySetResult();
    }

    /// <summary>Eco: o golpe da sombra de origem, no gesto dela (o Remador: dois anéis no ritmo da remada).</summary>
    private async void DoEcho(string id)
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.eco"));
        var echo = _echoes.Use(id);
        var gesture = _gestures[echo.Gesture];
        var timing = GestureData.RingFromJson(gesture);
        var repeats = GestureData.RingRepeats(gesture);
        var total = 0f;
        for (var i = 0; i < repeats; i++)
        {
            total += await RunRing(timing, Shade, Windows);
        }

        var grade = Windows.Grade(total / repeats);
        AddMark(echo.MarkOnUse);
        await Protagonist.PlayAdvance(Shade.Position, () =>
        {
            _flow.Resolve.Reduce(echo.ResolveDamage.For(grade));
            _hud.SetResolve(_flow.Resolve.Fraction);
            return Shade.PlayHit(HitStrength(grade));
        });

        await Judge(Via.Absorb);
        _playerAction?.TrySetResult();
    }

    /// <summary>Oferenda (gesto Arrastar, sem pressa): a certa a faz lembrar nome e fixação; a errada se perde.</summary>
    private async void DoOffering(string kind)
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.libertar"));
        await RunDrag();
        GameRoot.Instance.Offerings.Give(_shadeId, kind);
        if (_story.Offering is { } offering && offering.AcceptsKind == kind)
        {
            _understanding.RevealName();
            _understanding.RevealFixation();
            RefreshShadeKnowledge();
            await ShowLine(T(offering.AcceptKey), LineSeconds);
        }
        else
        {
            await ShowLine(T(_story.Offering?.RefuseKey ?? "combate.teste.ainda_nao"), LineSeconds);
        }

        await Judge(Via.Free);
        _playerAction?.TrySetResult();
    }

    /// <summary>Provisão da Bolsa: imediata, sem gesto. As do Além sobem a Marca (GDD 4.8).</summary>
    private async void DoProvision(string id)
    {
        _menu.ShowMenu(false);
        var provision = Bag.UseProvision(id);
        _flow.Breath.Restore(provision.BreathRestore);
        _hud.SetBreath(_flow.Breath.Fraction);
        if (provision.MarkGain > 0)
        {
            AddMark(provision.MarkGain);
        }

        await ShowLine(string.Format(T("combate.bolsa.usou"), T(provision.NameKey)), LineSeconds);
        _playerAction?.TrySetResult();
    }

    private async void DoStartWitnessing()
    {
        _menu.ShowMenu(false);
        _flow.StartWitnessing();
        await ShowLine(T("combate.presenciar.comeca"), LineSeconds);
        await Judge(Via.Witness);
        _playerAction?.TrySetResult();
    }

    /// <summary>Presenciando: a cada vez, um Segurar curto mantém a atenção; falhar quebra o Presenciar.</summary>
    private async void DoKeepWitnessing()
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.presenciar"));
        var grade = Windows.Grade(await RunHold(_witnessHold));
        if (grade == GestureGrade.Failed)
        {
            _flow.StopWitnessing();
            await ShowLine(T("combate.presenciar.distraiu"), LineSeconds);
        }

        await Judge(Via.Witness);
        _playerAction?.TrySetResult();
    }

    private async void DoSpare()
    {
        _menu.ShowMenu(false);
        await ShowLine(T(_story.SpareKey), LineSeconds);
        await Judge(Via.Spare);
        _flow.EndBattle(BattleOutcome.Spared);
        _playerAction?.TrySetResult();
    }

    /// <summary>Deixar-se vencer (escolha, não a derrota): custa Lucidez e mostra uma cena curta e uma lembrança dela.</summary>
    private async void DoYield()
    {
        _menu.ShowMenu(false);
        _flow.Lucidity.Reduce(_story.YieldLucidityCost);
        _hud.SetLucidity(_flow.Lucidity.Fraction);
        foreach (var key in _story.YieldKeys)
        {
            await ShowLine(T(key), LineSeconds);
        }

        await CheckForgetting();
        await Judge(Via.Yield);
        _flow.EndBattle(BattleOutcome.Yielded);
        _playerAction?.TrySetResult();
    }

    /// <summary>Fugir: Esquiva até a saída. Chegou sem levar golpes demais, escapa (e ouve algo); senão, perde a vez.</summary>
    private async void DoFlee()
    {
        _menu.ShowMenu(false);
        _hud.SetGestureActive(true, T("combate.gesto.fugir"));
        _dodge.Exit = _flee.Exit;
        var grade = Windows.Grade(await RunDodge(_flee.Settings, _flee.Pattern));
        _dodge.Exit = null;
        if (grade == GestureGrade.Failed)
        {
            await ShowLine(T("combate.fuga.falhou"), LineSeconds);
        }
        else
        {
            await ShowLine(T(_story.FleeClueKey), LineSeconds);
            _flow.EndBattle(BattleOutcome.Fled);
        }

        await Judge(Via.Flee);
        _playerAction?.TrySetResult();
    }

    /// <summary>A Marca nunca aparece como número para o jogador (princípio 1): só no console de debug.</summary>
    private void AddMark(int amount)
    {
        GameRoot.Instance.Mark.Add(amount);
        GD.Print($"[debug] Marca: {GameRoot.Instance.State.Mark} (etapa {GameRoot.Instance.State.MarkStage})");
        ApplyMarkLook();
    }

    /// <summary>A Marca no sprite do protagonista: perde cor por faixa e, na alta, fica sem sombra no chão.</summary>
    private void ApplyMarkLook()
    {
        var band = GameRoot.Instance.Mark.Band;
        Protagonist.SetMarkLook(GameRoot.Instance.MarkLook.DesaturationOf(band), MarkLook.CastsShadow(band));
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
        var acts = T($"combate.apoio.{_support.SupportId}.age");
        await ShowLine(_flow.SupportReluctant ? string.Format(T("combate.apoio.relutante"), acts) : acts, SupportSeconds);

        if (_support.Effect == SupportEffect.WidenListen && !_understanding.NameRevealed
            && _listenEntries.FirstOrDefault(e => e.Reveals == ListenReveal.Name) is { } name)
        {
            // Ismena sopra a pista do nome: o pedaço que se ouve na escuta pela metade.
            await ShowLine(string.Format(T("combate.apoio.lamento.sopra"), T(name.PartialKey)), LineSeconds);
        }
        else if (_support.Effect == SupportEffect.EaseAbsorb && !_flow.SupportReluctant
            && GameRoot.Instance.Companions.Definitions.TryGetValue(_support.CompanionId, out var dimas) && dimas.FoundObols > 0)
        {
            GameRoot.Instance.State.Inventory.Obols += dimas.FoundObols;
            await ShowLine(T("combate.apoio.surrupio.achou"), LineSeconds);
        }
    }

    /// <summary>
    /// A ação do turno pela via dela (HISTORIA 5.1): se o companheiro reprova, o próximo apoio sai relutante e ele comenta
    /// (uma vez por luta).
    /// </summary>
    private async Task Judge(Via via)
    {
        if (_support is null || GameRoot.Instance.Companions.ApprovalOf(_support.CompanionId, via) != Approval.Reprove)
        {
            return;
        }

        _flow.ReproveAction();
        if (!_reluctanceSaid && GameRoot.Instance.Companions.Definitions.TryGetValue(_support.CompanionId, out var companion))
        {
            _reluctanceSaid = true;
            await ShowLine(T(companion.ReluctantKey), LineSeconds);
        }
    }

    private async Task ShadeTurn()
    {
        var attack = _flow.NextShadeAttack;
        _turnRing.MoveUnder(Shade);
        _hud.SetTurn(null, shadeTurn: true);
        _hud.SetCaption(T(attack.CaptionKey));
        _hud.SetGestureActive(true);
        await Wait(ShadeTellSeconds);

        var widening = _flow.SupportPower(SupportEffect.WidenDefense) + _flow.FocusWidening;
        var items = GameRoot.Instance.Items.Modifiers;
        var windows = Windows.Widened(widening + (attack.Kind == ShadeAttackKind.Direct ? items.ParryWindow : 0f));
        var slow = _flow.ShadeSlowFactor;
        var gesture = _gestures[attack.Gesture];

        if (attack.Kind == ShadeAttackKind.Direct)
        {
            _hud.SetGestureActive(true, T("combate.gesto.aparar"));
            var timing = GestureData.RingFromJson(gesture);
            var grade = windows.Grade(await RunRing(timing with { ShrinkSeconds = timing.ShrinkSeconds * slow }, Protagonist, windows));
            await Shade.PlayAdvance(Protagonist.Position, () => TakeHit(attack, grade));
        }
        else
        {
            _hud.SetGestureActive(true, T("combate.gesto.esquivar"));
            var (settings, pattern) = GestureData.DodgeFromJson(gesture);
            // A veste aumenta o quadro da Esquiva (GDD 6.1).
            var box = 1f + items.DodgeBox;
            var grade = windows.Grade(await RunDodge(
                settings with { BoxWidth = settings.BoxWidth * box, BoxHeight = settings.BoxHeight * box }, pattern.Slowed(slow)));
            await TakeHit(attack, grade);
        }
    }

    /// <summary>Dano no Fôlego conforme a defesa (mais forte com a sombra enfurecida); o aparo perfeito não deixa o golpe chegar.</summary>
    private Task TakeHit(ShadeAttack attack, GestureGrade grade)
    {
        var rage = _flow.Enraged ? _liberationRules.EnragedDamageMultiplier : 1f;
        _flow.Breath.Reduce(attack.BreathDamage.For(grade) * rage * (1f - _flow.GuardFraction));
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
            _hud.FadeShadePanel(_stage.Tuning.AbsorbSeconds);
            await Shade.PlayAbsorbInto(Protagonist.Position);
        }
        else if (_flow.Outcome == BattleOutcome.Witnessed)
        {
            // A história inteira, que só quem fica até o fim do ciclo vê (GDD 4.5).
            foreach (var key in _story.StoryKeys)
            {
                await ShowLine(T(key), LineSeconds);
            }

            _hud.SetCaption(T("combate.sombra.vista"));
            await Shade.PlayDepart();
        }
        else if (_flow.Outcome is BattleOutcome.Spared or BattleOutcome.Fled or BattleOutcome.Yielded)
        {
            _hud.SetCaption(T(_flow.Outcome switch
            {
                BattleOutcome.Spared => "combate.sombra.poupada",
                BattleOutcome.Fled => "combate.fuga.escapou",
                _ => "combate.sombra.cedeu",
            }));
            await Shade.PlayDepart();
        }
        else
        {
            _hud.SetCaption(T("combate.sombra.desfeita"));
            await Shade.PlayDisintegrate();
        }

        PublishOutcome();
        KeepVitals();

        // Vencida (por qualquer via), a sombra devolve o que guardava de uma derrota anterior.
        if (_flow.Outcome is not (BattleOutcome.Spared or BattleOutcome.Fled or BattleOutcome.Yielded) && Hoard.ReturnTo(Bag))
        {
            await ShowLine(T("combate.espolio.devolvido"), LineSeconds);
        }

        await Wait(DefeatHoldSeconds);
        await Restart();
    }

    /// <summary>
    /// O fim da luta como evento (ARQUITETURA 4.2). Absorver já publica na hora da absorção, junto com a Marca.
    /// </summary>
    private void PublishOutcome()
    {
        var events = GameRoot.Instance.Events;
        switch (_flow.Outcome)
        {
            case BattleOutcome.Freed: events.Publish(new ShadeFreed(_shadeId)); break;
            case BattleOutcome.Destroyed: events.Publish(new ShadeDestroyed(_shadeId)); break;
            case BattleOutcome.Witnessed: events.Publish(new ShadeWitnessed(_shadeId)); break;
            case BattleOutcome.Spared: events.Publish(new ShadeSpared(_shadeId)); break;
            case BattleOutcome.Fled: events.Publish(new ShadeFled(_shadeId)); break;
            case BattleOutcome.Yielded: events.Publish(new ShadeYielded(_shadeId)); break;
        }
    }

    private async Task Defeat()
    {
        _hud.SetTurn(null, shadeTurn: false);
        _hud.SetCaption(T("combate.derrota"));
        await Protagonist.PlayDefeat();
        await _hud.Fade(toBlack: true, DefeatFadeSeconds);

        // Derrota com custo (GDD 4.9): acorda na Trívia, perde óbolos e oferendas (ficam com ela), a Maré sobe e
        // Hécate dá uma pista nova. O Diário mantém o que viu.
        _defeats++;
        GameRoot.Instance.Events.Publish(new PlayerDefeated(_shadeId));
        GameRoot.Instance.Healing.Rest();
        await ShowLine(T("combate.derrota.trivia"), LineSeconds);
        var lostObols = Hoard.TakeFrom(Bag);
        if (lostObols > 0 || !Hoard.IsEmpty)
        {
            await ShowLine(T("combate.derrota.perdas"), LineSeconds);
        }

        if (_defeatTideSteps > 0)
        {
            GameRoot.Instance.Tide.Defeat(_defeatTideSteps);
            GD.Print($"[debug] Maré: {GameRoot.Instance.State.Tide}");
            await ShowLine(T("combate.derrota.mare"), LineSeconds);
        }

        await ShowLine(T(ShadeNarration.DefeatClue(_story, _defeats)), LineSeconds);
        await Restart();
    }

    /// <summary>
    /// Teste: a luta recomeça (depois da derrota, o protagonista acordou descansado na Trívia; depois de uma vitória,
    /// segue com o Fôlego e a Lucidez que sobraram).
    /// </summary>
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

    private async Task<float> RunStroke(StrokePattern pattern)
    {
        _stroke.Pattern = pattern;
        _stroke.Center = GestureAreaCenter();
        var score = await RunGesture(_stroke);
        await Wait(ResultHoldSeconds);
        _stroke.Visible = false;
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

    /// <summary>Janelas do modo, com a assistência de gestos aplicada.</summary>
    private GestureWindows Windows => _assistSettings.WindowsFor(_assist, _windows);

    private Task<float> RunGesture(IGesture gesture)
    {
        // Assistência automática: o gesto se faz sozinho, sempre "bom" (nunca perfeito).
        if (_assist == GestureAssist.Automatic)
        {
            return Task.FromResult(_assistSettings.AutoScore);
        }

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

        var status = _flow.SupportActive ? T(_flow.SupportReluctant ? "combate.apoio.ativo_relutante" : "combate.apoio.ativo")
            : _flow.RoundsUntilSupport switch
            {
                0 => T("combate.apoio.agora"),
                1 => T("combate.apoio.em_um_turno"),
                var n => string.Format(T("combate.apoio.em_turnos"), n),
            };
        var name = GameRoot.Instance.Companions.Definitions.TryGetValue(_support.CompanionId, out var companion) ? T(companion.NameKey) : _support.CompanionId;
        _hud.SetSupport($"{name} · {T($"combate.apoio.{_support.SupportId}")}", status);
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

    /// <summary>
    /// Com <c>-- --demo</c>: joga sozinho para gravar vídeo, mostrando a via Libertar inteira (escuta nos três
    /// momentos do ciclo, erra a fixação, acerta até ela partir); na luta seguinte usa as cinco Memórias e ataca até
    /// absorver; depois usa o Eco. Com <c>-- --demo=vias</c>: uma luta por via nova (oferenda e Presenciar; provisão
    /// e Poupar; Fugir; Deixar-se vencer). Apara no tempo e foge dos golpes subindo e descendo (nem sempre consegue).
    /// </summary>
    private async void RunDemo(string mode)
    {
        var vias = mode == "vias";
        // --demo=absorver: sem roteiro (absorve assim que der; use com --firmeza=25).
        var script = vias ? ViasScript() : mode == "absorver" ? new Queue<string[]>() : new Queue<string[]>(new[]
        {
            new[] { ApproachId, ListenId },
            new[] { ApproachId, ListenId },
            new[] { ApproachId, ListenId },
            new[] { ApproachId, FreeId, FixationPrefix + "saudade" },
            new[] { ApproachId, FreeId, FixationPrefix + "culpa" },
            new[] { ApproachId, FreeId, FixationPrefix + "culpa" },
        });

        foreach (var memory in mode != "" ? System.Array.Empty<string>() : new[] { "noite_das_tochas", "agua_parada", "mesa_cheia", "maos_no_barro", "colo_da_avo" })
        {
            script.Enqueue(new[] { MemoryId, MemoryPrefix + memory });
        }

        while (IsInsideTree())
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (_menu.MenuVisible && _flow.Phase == BattlePhase.PlayerTurn)
            {
                await Wait(1f);
                // Presenciando, só mantém a atenção; fora disso segue o roteiro, depois absorve, usa o Eco ou ataca.
                var choices = _flow.IsWitnessing ? new[] { KeepWitnessingId }
                    : script.Count > 0 ? script.Dequeue()
                    : _menu.IsAvailable(AbsorbId) ? new[] { AbsorbId }
                    : _echoes.HasUsable ? new[] { EchoId, EchoPrefix + _echoes.Echoes[0].Id }
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
            else if (_stroke.IsRunning)
            {
                await PlayStroke();
            }
            else if (_dodge.IsRunning && _dodge.ExitCenter is { } exit)
            {
                // Fugindo: vai direto para a saída.
                var dir = (exit - _dodge.MarkerPosition).Normalized();
                SetAxis("move_left", "move_right", dir.X);
                SetAxis("move_up", "move_down", dir.Y);
            }
            else
            {
                // Na Esquiva, sobe e desce devagar: às vezes escapa, às vezes leva golpe. Fora dela, solta tudo.
                SetAxis("move_left", "move_right", 0f);
                var up = _dodge.IsRunning && Mathf.Sin(Time.GetTicksMsec() / DemoDodgeSwingMs) > 0f;
                var down = _dodge.IsRunning && !up;
                Input.ParseInputEvent(new InputEventAction { Action = "move_up", Pressed = up });
                Input.ParseInputEvent(new InputEventAction { Action = "move_down", Pressed = down });
            }
        }
    }

    /// <summary>Uma luta por via nova, nesta ordem.</summary>
    private static Queue<string[]> ViasScript() => new(new[]
    {
        new[] { ApproachId, OfferingId, OfferingPrefix + "agua" },
        new[] { ApproachId, WitnessId },
        new[] { BagId, ProvisionPrefix + "sementes_roma" },
        new[] { ApproachId, SpareId },
        new[] { FleeId },
        new[] { ApproachId, YieldId },
    });

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

    /// <summary>Conduz a luz no ritmo da guia: segura "para a frente" quando está atrás e solta quando passa dela.</summary>
    private async Task FollowPath()
    {
        while (_drag.IsRunning)
        {
            var push = _drag.Gap < 0f ? _drag.Tangent : Vector2.Zero;
            SetAxis("move_left", "move_right", push.X);
            SetAxis("move_up", "move_down", push.Y);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        SetAxis("move_left", "move_right", 0f);
        SetAxis("move_up", "move_down", 0f);
        while (_drag.Visible)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>Aperta cada direção do Traço no pulso (diagonal = as duas teclas juntas).</summary>
    private async Task PlayStroke()
    {
        var pattern = _stroke.Pattern;
        for (var i = 0; i < pattern.Steps.Count && _stroke.IsRunning; i++)
        {
            await Wait(Mathf.Max(0f, pattern.IdealTime(i) - _stroke.Elapsed));
            var actions = pattern.Steps[i] switch
            {
                StrokeDirection.Up => new[] { "move_up" },
                StrokeDirection.UpRight => new[] { "move_up", "move_right" },
                StrokeDirection.Right => new[] { "move_right" },
                StrokeDirection.DownRight => new[] { "move_down", "move_right" },
                StrokeDirection.Down => new[] { "move_down" },
                StrokeDirection.DownLeft => new[] { "move_down", "move_left" },
                StrokeDirection.Left => new[] { "move_left" },
                _ => new[] { "move_up", "move_left" },
            };
            foreach (var action in actions)
            {
                Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
            }

            await Wait(_stroke.ChordSeconds * 2f);
            foreach (var action in actions)
            {
                Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
            }
        }

        while (_stroke.Visible)
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
