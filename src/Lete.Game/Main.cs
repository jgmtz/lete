using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Lete.Core.Events;
using Lete.Core.Presentation;
using Lete.Core.State;
using Lete.Game.Combat;
using Lete.Game.Debug;
using Lete.Game.UI;
using Lete.Game.World;

namespace Lete.Game;

/// <summary>
/// Cena principal da exploração: a região atual (uma cena de <c>content/regions</c>, carregada pelo id) dentro de um
/// SubViewport (para o render em baixa resolução), a densidade de pixel única, o HUD, as telas e o painel de debug.
/// Passar por uma <see cref="RegionExit"/> recarrega esta cena com a região de destino e salva o jogo ao chegar.
/// </summary>
public partial class Main : Node
{
    /// <summary>Altura dos sprites de mapa (ARTE.md 7.8).</summary>
    [Export] public int SpriteHeightPx { get; set; } = 56;
    [Export] public float CharacterHeightMeters { get; set; } = 1.7f;

    /// <summary>Altura do personagem na tela em 1080p. Múltipla de 56 para o viewport de baixa resolução (168 = 3×).</summary>
    [Export] public int CharacterScreenHeightPx { get; set; } = 168;

    [Export] public bool LowResRender { get; set; } = true;

    /// <summary>Id da região carregada (definido ao abrir a cena).</summary>
    public string RegionId { get; private set; } = "";

    /// <summary>Onde o jogo começa sem save: o fim da abertura (decisão do João, 30/out/2026: a boca da gruta de Tênaro).</summary>
    [Export] public string StartRegion { get; set; } = "tenaro";

    /// <summary>Região das conferências pela linha de comando (os pontos de teleporte de teste ficam nela).</summary>
    public const string TestRegion = "sala_teste";

    /// <summary>
    /// Uma chegada pendente: a cena é recarregada e, ao abrir, vai para esta região (e salva, se for troca), no marcador
    /// <c>Entry</c> ou, voltando de uma luta, em <c>Position</c>.
    /// </summary>
    private sealed record Arrival(string Region, string? Entry, bool Save, Vector3? Position = null);

    private static Arrival? _arrival;

    /// <summary>Os argumentos de conferência rodam uma vez por execução (a cena recarrega ao trocar de região).</summary>
    private static bool _commandLineRan;

    /// <summary>Onde o jogador aparece ao chegar numa Trívia pela tocha (ao lado dela, não dentro).</summary>
    private static readonly Vector3 TriviaArrivalOffset = new(0f, 0f, 1.5f);

    private SubViewportContainer _container = null!;
    private Node3D _world = null!;
    private Player _player = null!;
    private CameraRig _cameraRig = null!;
    private Hud _hud = null!;
    private GameMenu _menu = null!;
    private HeavyMemoryScreen _heavyScreen = null!;
    private DialogueScreen _dialogueScreen = null!;
    private ShopScreen _shop = null!;
    private TriviaScreen _trivia = null!;
    private TrialScreen _trial = null!;
    private DebugPanel _debug = null!;
    private IDisposable? _markSubscription;

    public IReadOnlyList<Marker3D> TeleportPoints =>
        GetTree().GetNodesInGroup("teleport_points").OfType<Marker3D>().ToList();

    public override void _Ready()
    {
        _container = GetNode<SubViewportContainer>("ViewportContainer");
        var region = LoadRegion();
        _player = _world.GetNode<Player>("Player");
        if (GameRoot.Instance.State.ProtagonistId is { } chosen)
        {
            _player.SelectCharacter(chosen);
        }
        _cameraRig = _world.GetNode<CameraRig>("CameraRig");

        _hud = new Hud(_player);
        AddChild(_hud);
        _menu = new GameMenu();
        AddChild(_menu);
        _heavyScreen = new HeavyMemoryScreen();
        AddChild(_heavyScreen);
        _debug = new DebugPanel(this);
        AddChild(_debug);
        _menu.OpenChanged += RefreshPlayerInput;
        _debug.OpenChanged += RefreshPlayerInput;
        _heavyScreen.OpenChanged += RefreshPlayerInput;
        HeavyMemoryPlace.Reached += SurfaceHeavyMemory;
        _dialogueScreen = new DialogueScreen();
        AddChild(_dialogueScreen);
        _dialogueScreen.OpenChanged += RefreshPlayerInput;
        Talkable.Talked += OpenDialogue;
        _shop = new ShopScreen();
        _trivia = new TriviaScreen();
        _trial = new TrialScreen();
        foreach (var screen in new ChoiceScreen[] { _shop, _trivia, _trial })
        {
            AddChild(screen);
            screen.OpenChanged += RefreshPlayerInput;
        }

        _trivia.Traveled += ArriveAtTrivia;
        MerchantStall.Opened += OpenShop;
        TriviaRest.Reached += OpenTrivia;
        TribunalSeat.Opened += OpenTrial;
        GameRoot.Instance.StateReplaced += OnStateReplaced;
        GameRoot.Instance.Saving += OnSaving;
        RegionExit.Crossed += GoToRegion;
        ShadeEncounter.Started += GoToBattle;
        PlaceOnArrival(region);
        _player.CharacterChanged += ApplyMarkLook;
        _markSubscription = GameRoot.Instance.Events.Subscribe<MarkIncreased>(_ => ApplyMarkLook());
        ApplyMarkLook();

        ApplyPixelSettings();
        _ = RunCommandLineAsync();
    }

    public void SetLowResRender(bool lowRes)
    {
        LowResRender = lowRes;
        ApplyPixelSettings();
        _debug.Refresh();
    }

    public void TeleportPlayer(Marker3D point)
    {
        _player.TeleportTo(point.GlobalPosition);
        _cameraRig.SnapToTarget();
    }

    public override void _ExitTree()
    {
        GameRoot.Instance.StateReplaced -= OnStateReplaced;
        GameRoot.Instance.Saving -= OnSaving;
        HeavyMemoryPlace.Reached -= SurfaceHeavyMemory;
        Talkable.Talked -= OpenDialogue;
        MerchantStall.Opened -= OpenShop;
        TriviaRest.Reached -= OpenTrivia;
        TribunalSeat.Opened -= OpenTrial;
        RegionExit.Crossed -= GoToRegion;
        ShadeEncounter.Started -= GoToBattle;
        _markSubscription?.Dispose();
    }

    /// <summary>
    /// Escolhe e instancia a região: a da chegada pendente, a de <c>-- --regiao=id</c>, a do estado (save carregado ou
    /// região anterior), a de teste quando há argumentos de conferência, ou a do começo do jogo.
    /// </summary>
    private Lete.Core.World.RegionDefinition LoadRegion()
    {
        var regions = GameRoot.Instance.RegionDefinitions;
        var args = OS.GetCmdlineUserArgs();
        var id = _arrival?.Region
            ?? Array.Find(args, a => a.StartsWith("--regiao="))?["--regiao=".Length..]
            ?? GameRoot.Instance.State.RegionId
            ?? (args.Any(a => a != "--sem-abertura") ? TestRegion : StartRegion);
        if (!regions.TryGetValue(id, out var region))
        {
            GD.PushError($"Região '{id}' não existe em content/regions; abrindo '{StartRegion}'.");
            region = regions[StartRegion];
        }

        RegionId = region.Id;
        _world = GD.Load<PackedScene>(region.Scene).Instantiate<Node3D>();
        _container.GetNode("SubViewport").AddChild(_world);
        return region;
    }

    /// <summary>
    /// Põe o jogador no lugar: no marcador por onde chegou, onde salvou (save desta região) ou na chegada padrão. Chegar
    /// por uma passagem é trocar de região: salva. Começar numa região só a marca como visitada.
    /// </summary>
    private void PlaceOnArrival(Lete.Core.World.RegionDefinition region)
    {
        var arrival = _arrival;
        _arrival = null;
        var state = GameRoot.Instance.State;
        if (arrival is { Position: { } back })
        {
            // Volta de uma luta: onde estava, um pouco afastado da sombra.
            _player.TeleportTo(back);
            _cameraRig.SnapToTarget();
        }
        else if (arrival is { Entry: { } entry })
        {
            TeleportToMarker(entry);
        }
        else if (state.RegionId == RegionId && state.Position is { } position)
        {
            _player.TeleportTo(new Vector3(position.X, position.Y, position.Z));
            _cameraRig.SnapToTarget();
        }
        else
        {
            TeleportToMarker(region.Entry);
        }

        if (arrival is { Save: true })
        {
            GameRoot.Instance.ChangeRegion(RegionId);
        }
        else
        {
            GameRoot.Instance.Regions.Enter(RegionId);
        }
    }

    private void TeleportToMarker(string name)
    {
        if (TeleportPoints.FirstOrDefault(p => p.Name == name) is { } marker)
        {
            TeleportPlayer(marker);
        }
        else
        {
            GD.PushError($"Região '{RegionId}' não tem o marcador de chegada '{name}'.");
        }
    }

    /// <summary>Passou por uma passagem: recarrega a cena na região de destino (e salva ao chegar).</summary>
    private void GoToRegion(string regionId, string entry)
    {
        _arrival = new Arrival(regionId, entry, Save: true);
        GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
    }

    /// <summary>Chegou perto de uma sombra de encontro: vai para a luta e, no fim, volta a este lugar (sem salvar).</summary>
    private void GoToBattle(string encounterId, Vector3 returnPosition)
    {
        _arrival = new Arrival(RegionId, null, Save: false, returnPosition);
        EncounterTransit.Begin(GetTree(), GameRoot.Instance.Encounters.Definitions[encounterId]);
    }

    /// <summary>Uma Memória Pesada aflora: o aviso aparece (o jogador ainda pode adiar).</summary>
    public void SurfaceHeavyMemory(string memoryId)
    {
        if (!_heavyScreen.IsOpen)
        {
            _menu.SetOpen(false);
            _heavyScreen.Open(memoryId);
        }
    }

    public void OpenShop(string merchantId)
    {
        _menu.SetOpen(false);
        _shop.Open(merchantId);
    }

    public void OpenTrivia(string triviaId)
    {
        _menu.SetOpen(false);
        _trivia.Open(triviaId);
    }

    public void OpenTrial(string trialId)
    {
        _menu.SetOpen(false);
        _trial.Open(trialId);
    }

    /// <summary>Viagem pela tocha: leva o jogador à Trívia de destino, se ela estiver nesta região.</summary>
    private void ArriveAtTrivia(string triviaId)
    {
        if (GetTree().GetNodesInGroup(TriviaRest.Group).OfType<TriviaRest>().FirstOrDefault(t => t.TriviaId == triviaId) is { } target)
        {
            _player.TeleportTo(target.GlobalPosition + TriviaArrivalOffset);
            _cameraRig.SnapToTarget();
        }
    }

    public void OpenDialogue(string dialogueId)
    {
        if (!_dialogueScreen.IsOpen)
        {
            _menu.SetOpen(false);
            _dialogueScreen.Open(dialogueId);
        }
    }

    /// <summary>Chamado quando algo muda o texto exibido (por exemplo, o gênero).</summary>
    public void OnTextsChanged() => _menu.RefreshTexts();

    /// <summary>Um save foi carregado: o jogador volta a ser o corpo salvo, no lugar onde salvou.</summary>
    private void OnStateReplaced()
    {
        var state = GameRoot.Instance.State;
        if (state.RegionId is { } saved && saved != RegionId && GameRoot.Instance.RegionDefinitions.ContainsKey(saved))
        {
            // O save é de outra região: abre a cena dela, no lugar onde salvou (carregar não é trocar: não salva).
            _arrival = new Arrival(saved, null, Save: false);
            GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
            return;
        }

        if (state.ProtagonistId is { } chosen)
        {
            _player.SelectCharacter(chosen);
        }

        if (state.RegionId == RegionId && state.Position is { } position)
        {
            _player.TeleportTo(new Vector3(position.X, position.Y, position.Z));
            _cameraRig.SnapToTarget();
        }

        GameRoot.Instance.Regions.Enter(RegionId);

        ApplyMarkLook();
    }

    private void OnSaving()
    {
        var state = GameRoot.Instance.State;
        var position = _player.GlobalPosition;
        state.RegionId = RegionId;
        state.Position = new System.Numerics.Vector3(position.X, position.Y, position.Z);
    }

    /// <summary>A Marca só aparece no corpo do protagonista (os outros conjuntos de sprites são de teste).</summary>
    private void ApplyMarkLook()
    {
        var root = GameRoot.Instance;
        var isProtagonist = _player.CurrentPrefix == root.State.ProtagonistId;
        var band = root.Mark.Band;
        _player.SetMarkLook(
            isProtagonist ? root.MarkLook.DesaturationOf(band) : 0f,
            !isProtagonist || MarkLook.CastsShadow(band));
    }

    private void RefreshPlayerInput()
    {
        var talking = _dialogueScreen.IsOpen || _shop.IsOpen || _trivia.IsOpen || _trial.IsOpen;
        _hud.SetSuppressed(_menu.IsOpen || _heavyScreen.IsOpen || talking);
        _player.InputEnabled = !_menu.IsOpen && !_debug.IsOpen && !_heavyScreen.IsOpen && !talking;
    }

    /// <summary>Aplica a mesma densidade de pixel a sprites, texturas do cenário, câmera e viewport.</summary>
    private void ApplyPixelSettings()
    {
        var viewportHeight = UI.Screen.LogicalHeight(this);
        var density = new PixelDensity(SpriteHeightPx, CharacterHeightMeters, CharacterScreenHeightPx, viewportHeight);

        _player.PixelSize = density.PixelSize;
        foreach (var sprite in GetTree().GetNodesInGroup(MapSprite.Group).OfType<Sprite3D>())
        {
            sprite.PixelSize = density.PixelSize;
        }
        _cameraRig.Camera.Size = density.CameraSize;
        _container.StretchShrink = LowResRender ? density.LowResShrink : 1;

        // Superfícies do cenário usam triplanar no espaço do mundo; a escala define texels por metro.
        foreach (var node in GetTree().GetNodesInGroup("pixel_surface"))
        {
            if (node.Get("material").As<StandardMaterial3D>() is { AlbedoTexture: { } texture } material)
            {
                material.Uv1Scale = Vector3.One * density.TriplanarScale(texture.GetWidth());
            }
        }
    }

    /// <summary>
    /// Argumentos depois de "--" na linha de comando, para conferir a cena sem jogar à mão:
    /// --render=low|direct  --gender=m|f  --teleport=Nome  --examine
    /// --carregar  --ui=journal|mosaic|inventory|debug  --shot=caminho.png  --frames=N
    /// --pesada=encarar,deixar (decide as próximas Pesadas da fila pela cena; a última fica na tela, sem --ui)
    /// --regiao=tenaro|aqueronte|sala_teste (sem ela, as conferências abrem a sala de teste)
    /// --companheiro=aulo  --origem=artesao  --despertar=padeiro (oferenda certa)  --conversar=padeiro --escolher=0,1 (botões, em ordem)
    /// --loja=coveiro  --trivia=trivia_sala  --julgamento=julgamento_padeiro  --obolos=N  --apertar=0,1 (botões da tela aberta)
    /// </summary>
    private async Task RunCommandLineAsync()
    {
        var args = OS.GetCmdlineUserArgs()
            .Select(a => a.TrimStart('-').Split('=', 2))
            .ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "", StringComparer.Ordinal);

        if (args.Count == 0)
        {
            return;
        }

        if (_commandLineRan)
        {
            // Já rodou antes de uma troca de região: aqui só falta a foto (a instância anterior saiu da árvore).
            await Shot(args);
            return;
        }

        _commandLineRan = true;

        if (args.TryGetValue("render", out var render))
        {
            LowResRender = render != "direct";
        }

        if (args.ContainsKey("carregar"))
        {
            GD.Print($"carregar: {GameRoot.Instance.Load()}");
        }

        if (args.TryGetValue("gender", out var gender))
        {
            GameRoot.Instance.State.Gender = gender == "f" ? Gender.Feminine : Gender.Masculine;
        }

        if (args.TryGetValue("origem", out var origin))
        {
            // Teste: troca a origem direto no estado (no jogo é escolhida uma vez, na abertura).
            GameRoot.Instance.State.OriginId = origin;
        }

        ApplyPixelSettings();

        if (args.TryGetValue("encontros", out var finished))
        {
            // Teste: dá por terminados os encontros da lista, em ordem (ex.: os do Remador antes da câmara).
            foreach (var id in finished.Split(','))
            {
                GD.Print($"encontro {id}: {GameRoot.Instance.Encounters.Finish(id)}");
            }

            await WaitFrames(2);
        }

        if (args.TryGetValue("oferenda", out var offering))
        {
            // Teste: uma oferenda na Bolsa (ex.: a água que o Remador aceita, sem passar pela poça).
            GameRoot.Instance.Offerings.Pick(offering, 1);
        }

        if (args.TryGetValue("teleport", out var pointName) &&
            TeleportPoints.FirstOrDefault(p => p.Name == pointName) is { } point)
        {
            TeleportPlayer(point);
        }

        // Espera a física detectar as áreas de interação depois do teleporte.
        await WaitFrames(10);

        if (args.ContainsKey("examine"))
        {
            _player.CurrentInteractable?.Examine();
        }

        if (args.TryGetValue("companheiro", out var companion))
        {
            GameRoot.Instance.State.CompanionId = companion;
        }

        if (args.TryGetValue("despertar", out var shade) && GameRoot.Instance.Dialogues.Graphs.Values
                .FirstOrDefault(g => g.ShadeId == shade)?.Lucidity is { } lucidity)
        {
            GameRoot.Instance.Events.Publish(new OfferingShared(shade, lucidity.OfferingKind ?? "mel"));
        }

        if (args.TryGetValue("obolos", out var obols))
        {
            GameRoot.Instance.State.Inventory.Obols += int.Parse(obols);
        }

        ChoiceScreen? opened = args.TryGetValue("loja", out var shop) ? Open(_shop, () => OpenShop(shop))
            : args.TryGetValue("trivia", out var trivia) ? Open(_trivia, () => OpenTrivia(trivia))
            : args.TryGetValue("julgamento", out var trial) ? Open(_trial, () => OpenTrial(trial))
            : null;
        foreach (var step in opened is not null && args.TryGetValue("apertar", out var presses) ? presses.Split(',') : Array.Empty<string>())
        {
            opened!.Press(int.Parse(step));
        }

        if (args.TryGetValue("conversar", out var dialogue))
        {
            OpenDialogue(dialogue);
            foreach (var step in args.TryGetValue("escolher", out var steps) ? steps.Split(',') : Array.Empty<string>())
            {
                _dialogueScreen.Press(int.Parse(step));
            }
        }

        if (args.TryGetValue("pesada", out var choices))
        {
            var list = choices.Split(',');
            for (var i = 0; i < list.Length && GameRoot.Instance.HeavyMemories.Next is { } next; i++)
            {
                SurfaceHeavyMemory(next.Id);
                _heavyScreen.Press(0);
                _heavyScreen.Press(list[i] == "deixar" ? 1 : 0);
                if (i < list.Length - 1 || args.ContainsKey("ui"))
                {
                    _heavyScreen.Press(0);
                }
            }
        }

        if (args.TryGetValue("ui", out var ui))
        {
            if (ui == "debug")
            {
                _debug.SetOpen(true);
            }
            else
            {
                _menu.Open(ui);
            }
        }

        await Shot(args);
    }

    /// <summary>
    /// <c>--shot</c>: espera e salva a tela. Se a cena recarregou no meio (troca de região), a instância nova tira a foto.
    /// </summary>
    private async Task Shot(Dictionary<string, string> args)
    {
        if (!args.TryGetValue("shot", out var shotPath))
        {
            return;
        }

        await WaitFrames(args.TryGetValue("frames", out var frames) ? int.Parse(frames) : 30);
        if (!IsInsideTree())
        {
            return;
        }

        var error = GetViewport().GetTexture().GetImage().SavePng(shotPath);
        GD.Print($"screenshot: {shotPath} ({error})");
        GetTree().Quit();
    }

    private static ChoiceScreen Open(ChoiceScreen screen, Action open)
    {
        open();
        return screen;
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count && IsInsideTree(); i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
