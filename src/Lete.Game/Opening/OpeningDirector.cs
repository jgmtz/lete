using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Lete.Core.Combat;
using Lete.Core.Data;
using Lete.Core.State;
using Lete.Game.Combat;
using Lete.Game.UI;

namespace Lete.Game.Opening;

/// <summary>
/// Cutscene de abertura e escolha do personagem (proposta do João, 26/out/2026; ver PENDÊNCIAS): primeira pessoa,
/// acordando e caminhando desorientado até a margem de um rio escuro; ajoelha e vê o próprio reflexo como uma mancha
/// negra; escolhe um dos personagens (bustos borrados, um de cada vez); o reflexo se resolve no escolhido e o jogo
/// começa. Tudo em tempo real e com geometria provisória (margem, rio, juncos), sem vídeo.
/// Pular: qualquer tecla, a partir da segunda vez. Reduzir movimento: <c>reduce_motion_toggle</c> (F3), guardado.
/// </summary>
public partial class OpeningDirector : Node3D
{
    private const string MainScenePath = "res://src/Lete.Game/Main.tscn";
    private const string BustArtFolder = "res://art/sprites/batalha/pixel_160";
    private const string OptionsPath = "user://opcoes.cfg";
    private const string EyelidsShaderPath = "res://src/Lete.Game/Opening/eyelids.gdshader";
    private const string WaterShaderPath = "res://src/Lete.Game/Opening/water.gdshader";
    private const string ReflectionShaderPath = "res://src/Lete.Game/Opening/reflection.gdshader";

    /// <summary>Argumentos de conferência do Main: com eles, a abertura passa direto para a exploração.</summary>
    private static readonly string[] MainArgs = { "teleport", "ui", "shot", "examine", "render", "gender", "frames", "sem-abertura" };

    [ExportGroup("Tempo (segundos)")]
    // TODO decisão: duração da parte sem controle (proposta: 20 a 30 s, pulável).
    [Export] public float WalkSeconds { get; set; } = 13f;
    [Export] public float TurnSeconds { get; set; } = 3.5f;
    [Export] public float KneelSeconds { get; set; } = 4f;
    [Export] public float WakeSeconds { get; set; } = 2.5f;
    [Export] public float BlinkSeconds { get; set; } = 0.4f;
    [Export] public float[] BlinkAt { get; set; } = { 1.8f, 4.5f, 8f, 11.5f, 15f };
    [Export] public float[] HeadDropAt { get; set; } = { 6.5f, 10.5f };
    [Export] public float HeadDropSeconds { get; set; } = 1.3f;
    [Export] public float ResolveSeconds { get; set; } = 2.4f;
    [Export] public float HoldSeconds { get; set; } = 1.6f;
    [Export] public float CloseSeconds { get; set; } = 1.2f;
    [Export] public float CarouselSlideSeconds { get; set; } = 0.25f;

    [ExportGroup("Corpo e câmera (metros e graus)")]
    [Export] public float EyeHeight { get; set; } = 1.6f;
    [Export] public float KneelHeight { get; set; } = 0.8f;
    [Export] public float KneelPitchDegrees { get; set; } = -50f;
    [Export] public float TurnDegrees { get; set; } = 90f;
    [Export] public float BobMeters { get; set; } = 0.05f;
    [Export] public float StepsPerSecond { get; set; } = 1.6f;
    [Export] public float RollDegrees { get; set; } = 3f;
    [Export] public float HeadDropDegrees { get; set; } = 14f;

    /// <summary>Com "reduzir movimento", balanço, inclinação e cabeceadas ficam este tanto do normal; piscadas não fecham de todo.</summary>
    [Export] public float ReducedMotionFactor { get; set; } = 0.2f;
    [Export] public float ReducedBlinkMinOpen { get; set; } = 0.6f;

    [ExportGroup("Visão")]
    [Export] public float BlurStart { get; set; } = 3.5f;
    [Export] public float BlurEnd { get; set; } = 0.3f;

    /// <summary>Desfoque do reflexo enquanto é mancha negra e depois de resolvido (em fração da imagem).</summary>
    [Export] public float ReflectionBlur { get; set; } = 0.04f;
    [Export] public float ResolvedReflectionBlur { get; set; } = 0.002f;

    [ExportGroup("Luz (provisória: noite de lua, com névoa)")]
    [Export] public float AmbientEnergy { get; set; } = 0.9f;
    [Export] public float MoonEnergy { get; set; } = 1.0f;
    [Export] public float FogDensity { get; set; } = 0.045f;

    [ExportGroup("Lugar (metros)")]
    [Export] public Vector3 WalkStart { get; set; } = new(0f, 0f, 9f);
    [Export] public Vector3 TurnPoint { get; set; } = new(0f, 0f, 1.5f);
    /// <summary>Ajoelhado perto da beira: com a inclinação de -50°, a margem fica só no quarto de baixo da tela.</summary>
    [Export] public Vector3 KneelPoint { get; set; } = new(-2.07f, 0f, 1.2f);
    [Export] public float BankX { get; set; } = -2.4f;
    [Export] public float WaterY { get; set; } = -0.2f;
    [Export] public Vector2 ReflectionSize { get; set; } = new(1.3f, 1.6f);
    [Export] public int ReedCount { get; set; } = 40;

    /// <summary>Faixa sem juncos (metros para cada lado) onde ele se ajoelha.</summary>
    [Export] public float ReedGap { get; set; } = 1.8f;

    [ExportGroup("Escolha (pelo reflexo)")]
    /// <summary>Que fração do alto da arte de frente vira o busto refletido (cabeça e ombros).</summary>
    [Export] public float BustCrop { get; set; } = 0.42f;

    /// <summary>Quanto a água cobre o reflexo na escolha (turvo, mas com as cores) e depois de confirmar.</summary>
    [Export] public float ChoosingMurk { get; set; } = 0.45f;
    [Export] public float ResolvedMurk { get; set; } = 0.15f;
    [Export] public float ChoosingBlur { get; set; } = 0.012f;

    /// <summary>A mancha negra ganha cor ao começar a escolha, neste tempo.</summary>
    [Export] public float RevealSeconds { get; set; } = 1.6f;

    /// <summary>Ao trocar de personagem a água se agita: quanto a ondulação cresce e em quanto tempo assenta.</summary>
    [Export] public float StirRipple { get; set; } = 4f;
    [Export] public float StirSeconds { get; set; } = 0.6f;

    private enum Stage
    {
        Cinematic,
        Choosing,
        Resolving,
    }

    private readonly List<CastMember> _choices = new();
    private readonly Dictionary<string, Texture2D> _bustCache = new();
    private Stage _stage = Stage.Cinematic;
    private Tween? _stir;
    private float _time;
    private int _selected;
    private bool _seenBefore;
    private bool _reducedMotion;
    private Camera3D _camera = null!;
    private ShaderMaterial _eyes = null!;
    private ShaderMaterial _reflection = null!;
    private Control _carousel = null!;
    private Label _hint = null!;
    private Label _notice = null!;
    private float _eyesOpenOverride = -1f;

    private float CinematicSeconds => WalkSeconds + TurnSeconds + KneelSeconds;

    private static string T(string key) => GameRoot.Instance.Text(key);

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs().Select(a => a.TrimStart('-').Split('=', 2)[0]).ToHashSet();
        if (args.Overlaps(MainArgs))
        {
            CallDeferred(MethodName.GoToMain);
            return;
        }

        var options = new ConfigFile();
        options.Load(OptionsPath);
        _seenBefore = (bool)options.GetValue("abertura", "vista", false);
        _reducedMotion = (bool)options.GetValue("acessibilidade", "reduzir_movimento", false);

        // TODO decisão: ordem das origens no carrossel e qual vem selecionada. Provisório: a ordem do elenco.
        var cast = CastData.FromJson(FileAccess.GetFileAsString(BattleStage.CastPath));
        _choices.AddRange(cast.Values.Where(m => m.Selectable));

        BuildWorld();
        BuildEyes();
        BuildCarousel();
        UpdateReflection();

        if (OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--demo")) is { } demo)
        {
            RunDemo(demo);
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        if (_stage == Stage.Cinematic)
        {
            if (_time >= CinematicSeconds)
            {
                _time = CinematicSeconds;
                StartChoosing();
            }

            PoseCamera(_time);
        }

        var open = _eyesOpenOverride >= 0f ? _eyesOpenOverride : EyesOpen(_time);
        _eyes.SetShaderParameter("open", open);
        _eyes.SetShaderParameter("blur", Blur(_time));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("reduce_motion_toggle"))
        {
            _reducedMotion = !_reducedMotion;
            SaveOption("acessibilidade", "reduzir_movimento", _reducedMotion);
            ShowNotice(T(_reducedMotion ? "abertura.reduzir.ligado" : "abertura.reduzir.desligado"));
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_stage == Stage.Cinematic)
        {
            // Da segunda vez em diante, qualquer tecla pula a caminhada (a escolha continua).
            if (_seenBefore && @event is InputEventKey { Pressed: true } or InputEventJoypadButton { Pressed: true })
            {
                _time = CinematicSeconds;
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (_stage != Stage.Choosing)
        {
            return;
        }

        if (@event.IsActionPressed("ui_left") || @event.IsActionPressed("move_left"))
        {
            Select(_selected - 1);
        }
        else if (@event.IsActionPressed("ui_right") || @event.IsActionPressed("move_right"))
        {
            Select(_selected + 1);
        }
        else if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("gesture_confirm"))
        {
            Confirm();
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    // ---- Caminhada, piscadas e turvação -----------------------------------------------------------------

    private float Motion => _reducedMotion ? ReducedMotionFactor : 1f;

    private void PoseCamera(float t)
    {
        var eye = Vector3.Up * EyeHeight;
        Vector3 position;
        float yaw;
        float pitch = 0f;
        var walking = 1f;

        if (t < WalkSeconds)
        {
            position = WalkStart.Lerp(TurnPoint, Ease(t / WalkSeconds)) + eye;
            yaw = 0f;
        }
        else if (t < WalkSeconds + TurnSeconds)
        {
            var u = Ease((t - WalkSeconds) / TurnSeconds);
            position = TurnPoint.Lerp(KneelPoint, u) + eye;
            yaw = Mathf.DegToRad(TurnDegrees) * u;
        }
        else
        {
            var u = Ease((t - WalkSeconds - TurnSeconds) / KneelSeconds);
            position = KneelPoint + Vector3.Up * Mathf.Lerp(EyeHeight, KneelHeight, u);
            yaw = Mathf.DegToRad(TurnDegrees);
            pitch = Mathf.DegToRad(KneelPitchDegrees) * u;
            walking = 1f - u;
        }

        // Passo irregular: senos de frequências que não se repetem juntas, como quem anda zonzo.
        var step = t * StepsPerSecond * Mathf.Pi;
        var bob = Mathf.Abs(Mathf.Sin(step)) + 0.3f * Mathf.Sin(step * 0.37f + 1.3f);
        position += Vector3.Up * (bob * BobMeters * Motion * walking);
        var roll = Mathf.DegToRad(RollDegrees) * Motion * walking *
                   (Mathf.Sin(step * 0.5f) + 0.5f * Mathf.Sin(step * 0.23f + 0.7f));

        // A cabeça que cai um pouco e volta.
        foreach (var at in HeadDropAt)
        {
            pitch -= Mathf.DegToRad(HeadDropDegrees) * Motion * Bump(t - at, HeadDropSeconds);
        }

        _camera.Position = position;
        _camera.Rotation = new Vector3(pitch, yaw, roll);
    }

    private float EyesOpen(float t)
    {
        if (_stage != Stage.Cinematic)
        {
            return 1f;
        }

        // Abre devagar ao acordar; cada piscada fecha (com "reduzir movimento", só até a metade).
        var minOpen = _reducedMotion ? ReducedBlinkMinOpen : 0f;
        var open = Mathf.Clamp(t / WakeSeconds, 0f, 1f);
        foreach (var at in BlinkAt)
        {
            open = Mathf.Min(open, 1f - (1f - minOpen) * Bump(t - at, BlinkSeconds));
        }

        return open;
    }

    private float Blur(float t) =>
        Mathf.Lerp(BlurStart * (_reducedMotion ? ReducedMotionFactor : 1f), BlurEnd, Mathf.Clamp(t / (WalkSeconds + TurnSeconds), 0f, 1f));

    private static float Ease(float u) => u * u * (3f - 2f * Mathf.Clamp(u, 0f, 1f));

    /// <summary>Uma "corcova" de 0 a 1 e de volta a 0, centrada em 0, com a largura dada.</summary>
    private static float Bump(float x, float width) =>
        Mathf.Abs(x) >= width / 2f ? 0f : 0.5f + 0.5f * Mathf.Cos(x / (width / 2f) * Mathf.Pi);

    // ---- Escolha ---------------------------------------------------------------------------------------

    private void StartChoosing()
    {
        _stage = Stage.Choosing;
        if (!_seenBefore)
        {
            _seenBefore = true;
            SaveOption("abertura", "vista", true);
        }

        _carousel.Visible = true;
        var tween = CreateTween();
        tween.TweenProperty(_carousel, "modulate:a", 1f, CarouselSlideSeconds * 2f).From(0f);

        // A mancha negra ganha as cores do personagem, ainda turvo, coberto pela água.
        var reveal = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Sine);
        reveal.TweenMethod(Callable.From<float>(v => _reflection.SetShaderParameter("black", v)), 1f, 0f, RevealSeconds);
        reveal.TweenMethod(Callable.From<float>(v => _reflection.SetShaderParameter("blur", v)), ReflectionBlur, ChoosingBlur, RevealSeconds);
        reveal.TweenMethod(Callable.From<float>(v => _reflection.SetShaderParameter("murk", v)), 0f, ChoosingMurk, RevealSeconds);
    }

    /// <summary>
    /// Troca quem aparece no reflexo: a água se agita, a imagem troca no meio da agitação e a água assenta.
    /// A escolha é só pelo reflexo (pedido do João, 26/out/2026).
    /// </summary>
    private void Select(int index)
    {
        _selected = (index % _choices.Count + _choices.Count) % _choices.Count;
        _stir?.Kill();
        _stir = CreateTween().SetTrans(Tween.TransitionType.Sine);
        _stir.TweenMethod(Callable.From<float>(v => _reflection.SetShaderParameter("stir", v)), 0f, StirRipple, StirSeconds / 2f);
        _stir.TweenCallback(Callable.From(UpdateReflection));
        _stir.TweenMethod(Callable.From<float>(v => _reflection.SetShaderParameter("stir", v)), StirRipple, 0f, StirSeconds / 2f);
    }

    private async void Confirm()
    {
        _stage = Stage.Resolving;
        var chosen = _choices[_selected];
        var hide = CreateTween();
        hide.TweenProperty(_carousel, "modulate:a", 0f, CarouselSlideSeconds * 2f);

        // O reflexo se resolve no personagem escolhido.
        var resolve = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Sine);
        resolve.TweenMethod(Callable.From<float>(v => _reflection.SetShaderParameter("murk", v)), ChoosingMurk, ResolvedMurk, ResolveSeconds);
        resolve.TweenMethod(Callable.From<float>(v => _reflection.SetShaderParameter("blur", v)), ChoosingBlur, ResolvedReflectionBlur, ResolveSeconds);
        await ToSignal(resolve, Tween.SignalName.Finished);
        await ToSignal(GetTree().CreateTimer(HoldSeconds), SceneTreeTimer.SignalName.Timeout);

        // Fecha os olhos e o jogo começa.
        var close = CreateTween();
        close.TweenMethod(Callable.From<float>(v => _eyesOpenOverride = v), 1f, 0f, CloseSeconds);
        await ToSignal(close, Tween.SignalName.Finished);

        var state = GameRoot.Instance.State;
        state.ProtagonistId = chosen.Id;
        state.Gender = chosen.Gender == "f" ? Gender.Feminine : Gender.Masculine;
        GD.Print($"Abertura: escolhido {chosen.Id}");
        GoToMain();
    }

    private void GoToMain() => GetTree().ChangeSceneToFile(MainScenePath);

    private void UpdateReflection()
    {
        if (_choices.Count == 0)
        {
            return;
        }

        _reflection.SetShaderParameter("bust", BustTexture(_choices[_selected]));
    }

    /// <summary>
    /// Busto: o alto da arte de frente (cabeça e ombros), como imagem recortada de verdade (o shader do reflexo
    /// lê a textura inteira, então um recorte só de região não serviria). Todos olham para o mesmo lado no reflexo
    /// (para a direita da tela, pedido do João, 26/out/2026): quem tem a arte olhando para o outro lado é espelhado.
    /// </summary>
    private Texture2D BustTexture(CastMember member)
    {
        if (_bustCache.TryGetValue(member.Id, out var cached))
        {
            return cached;
        }

        var image = GD.Load<Texture2D>($"{BustArtFolder}/{member.FrontArt}.png").GetImage();
        var region = new Rect2I(0, 0, image.GetWidth(), Mathf.RoundToInt(image.GetHeight() * BustCrop));
        var cropped = image.GetRegion(region);
        if (member.FrontFacesRight)
        {
            cropped.FlipX();
        }

        var bust = ImageTexture.CreateFromImage(cropped);
        _bustCache[member.Id] = bust;
        return bust;
    }

    // ---- Construção da cena ----------------------------------------------------------------------------

    private void BuildWorld()
    {
        // TODO decisão: rio ou poça? Que rio? Antes de Tênaro ou dentro dele? Provisório: um rio sem nome, antes de Tênaro.
        AddChild(new WorldEnvironment
        {
            Environment = new Environment
            {
                BackgroundMode = Environment.BGMode.Color,
                BackgroundColor = new Color(0.02f, 0.025f, 0.035f),
                AmbientLightSource = Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.35f, 0.42f, 0.5f),
                AmbientLightEnergy = AmbientEnergy,
                FogEnabled = true,
                FogLightColor = new Color(0.12f, 0.16f, 0.19f),
                FogDensity = FogDensity,
            },
        });
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-35f, 60f, 0f),
            LightColor = new Color(0.6f, 0.7f, 0.85f),
            LightEnergy = MoonEnergy,
        });

        // Margem (terra escura) e rio, encostados na linha BankX.
        var bankWidth = 30f;
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh
            {
                Size = new Vector2(bankWidth, 40f),
                Material = new StandardMaterial3D { AlbedoColor = new Color(0.09f, 0.08f, 0.07f), Roughness = 1f },
            },
            Position = new Vector3(BankX + bankWidth / 2f, 0f, 0f),
        });
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh
            {
                Size = new Vector2(40f, 40f),
                Material = new ShaderMaterial { Shader = GD.Load<Shader>(WaterShaderPath) },
            },
            Position = new Vector3(BankX - 20f, WaterY, 0f),
        });

        // Juncos na beira, sempre no mesmo lugar (semente fixa).
        var rng = new RandomNumberGenerator { Seed = 7 };
        var reedMaterial = new StandardMaterial3D { AlbedoColor = new Color(0.07f, 0.09f, 0.07f), Roughness = 1f };
        for (var i = 0; i < ReedCount; i++)
        {
            var height = rng.RandfRange(0.5f, 1.1f);
            var z = rng.RandfRange(-8f, 10f);
            var x = BankX + rng.RandfRange(-0.1f, 0.5f);
            // Nenhum junco onde ele se ajoelha: a vista para o rio fica livre.
            if (Mathf.Abs(z - KneelPoint.Z) < ReedGap)
            {
                continue;
            }

            AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.03f, height, 0.03f), Material = reedMaterial },
                Position = new Vector3(x, height / 2f, z),
                RotationDegrees = new Vector3(rng.RandfRange(-8f, 8f), 0f, rng.RandfRange(-8f, 8f)),
            });
        }

        // O reflexo: deitado na água, logo à frente de onde ele se ajoelha, de pé para quem olha de cima.
        _reflection = new ShaderMaterial { Shader = GD.Load<Shader>(ReflectionShaderPath) };
        AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = ReflectionSize, Material = _reflection },
            Position = new Vector3(KneelPoint.X - LookDistance(), WaterY + 0.01f, KneelPoint.Z),
            RotationDegrees = new Vector3(-90f, 90f, 0f),
        });

        _camera = new Camera3D { Fov = 70f, Current = true };
        AddChild(_camera);
        PoseCamera(0f);
    }

    /// <summary>Distância no chão até onde o olhar da câmera ajoelhada encontra a água.</summary>
    private float LookDistance() =>
        (KneelHeight - WaterY) / Mathf.Tan(Mathf.DegToRad(-KneelPitchDegrees));

    private void BuildEyes()
    {
        var layer = new CanvasLayer { Layer = 1 };
        AddChild(layer);
        _eyes = new ShaderMaterial { Shader = GD.Load<Shader>(EyelidsShaderPath) };
        var cover = new ColorRect { Material = _eyes, MouseFilter = Control.MouseFilterEnum.Ignore };
        cover.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(cover);
    }

    private void BuildCarousel()
    {
        var layer = new CanvasLayer { Layer = 2 };
        AddChild(layer);
        _carousel = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore, Theme = UiStyle.BuildTheme(UiStyle.ExplorationScale) };
        _carousel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_carousel);

        // TODO decisão: mostrar o nome da origem e uma descrição? Provisório: só a dica de controle; a escolha é pelo reflexo.
        _hint = UiStyle.MakeLabel(T("abertura.dica.escolher"));
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        _hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _hint.GrowHorizontal = Control.GrowDirection.Both;
        _hint.OffsetTop = -UiStyle.BodyFontSize * 4;
        _carousel.AddChild(_hint);

        var noticeLayer = new CanvasLayer { Layer = 3 };
        AddChild(noticeLayer);
        _notice = UiStyle.MakeLabel("");
        _notice.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _notice.GrowHorizontal = Control.GrowDirection.Begin;
        _notice.Modulate = new Color(1, 1, 1, 0);
        noticeLayer.AddChild(_notice);
    }

    private void ShowNotice(string text)
    {
        _notice.Text = text;
        var tween = CreateTween();
        tween.TweenProperty(_notice, "modulate:a", 1f, 0.2f);
        tween.TweenInterval(1.5f);
        tween.TweenProperty(_notice, "modulate:a", 0f, 0.5f);
    }

    private static void SaveOption(string section, string key, Variant value)
    {
        var options = new ConfigFile();
        options.Load(OptionsPath);
        options.SetValue(section, key, value);
        options.Save(OptionsPath);
    }

    // ---- Modo demo -------------------------------------------------------------------------------------

    /// <summary>
    /// Com <c>-- --demo</c>: assiste e confirma o primeiro; <c>-- --demo=&lt;id&gt;</c> anda no carrossel até esse
    /// personagem e confirma. Sempre aperta uma tecla logo no começo, que só pula a caminhada se ela já foi vista.
    /// </summary>
    private async void RunDemo(string demo)
    {
        var target = demo.Contains('=') ? demo[(demo.IndexOf('=') + 1)..] : "";
        if (target == "pular")
        {
            target = "";
        }

        // Como o jogador apressado: aperta uma tecla logo no começo (só pula se a abertura já foi vista).
        await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = true });

        while (_stage != Stage.Choosing)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
        for (var tries = 0; target != "" && _choices[_selected].Id != target && tries < _choices.Count; tries++)
        {
            await Press("ui_right");
            await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
        }

        await Press("ui_accept");
    }

    private async Task Press(string action)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
    }
}
