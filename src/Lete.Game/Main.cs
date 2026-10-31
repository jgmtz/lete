using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Lete.Core.Events;
using Lete.Core.Presentation;
using Lete.Core.State;
using Lete.Game.Debug;
using Lete.Game.UI;
using Lete.Game.World;

namespace Lete.Game;

/// <summary>
/// Cena principal do M0: o mundo 3D dentro de um SubViewport (para o teste de baixa resolução),
/// a densidade de pixel única, o HUD e o painel de debug.
/// </summary>
public partial class Main : Node
{
    /// <summary>Altura dos sprites de mapa (ARTE.md 7.8).</summary>
    [Export] public int SpriteHeightPx { get; set; } = 56;
    [Export] public float CharacterHeightMeters { get; set; } = 1.7f;

    /// <summary>Altura do personagem na tela em 1080p. Múltipla de 56 para o viewport de baixa resolução (168 = 3×).</summary>
    [Export] public int CharacterScreenHeightPx { get; set; } = 168;

    [Export] public bool LowResRender { get; set; } = true;

    /// <summary>Id desta região no save (por enquanto só existe a sala de teste).</summary>
    [Export] public string RegionId { get; set; } = "sala_teste";

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
        _world = _container.GetNode<Node3D>("SubViewport/TestRoom");
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
        // Começar numa região não é troca: só marca como visitada (sem salvar).
        GameRoot.Instance.Regions.Enter(RegionId);
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
        _markSubscription?.Dispose();
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

        if (args.TryGetValue("shot", out var shotPath))
        {
            await WaitFrames(args.TryGetValue("frames", out var frames) ? int.Parse(frames) : 30);
            var error = GetViewport().GetTexture().GetImage().SavePng(shotPath);
            GD.Print($"screenshot: {shotPath} ({error})");
            GetTree().Quit();
        }
    }

    private static ChoiceScreen Open(ChoiceScreen screen, Action open)
    {
        open();
        return screen;
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
