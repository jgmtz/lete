using System;
using System.Threading.Tasks;
using Godot;

namespace Lete.Game.Combat;

/// <summary>
/// Participante da batalha: sprite virado para a câmera só no eixo Y (fica em pé no chão), sombra de blob
/// e animações por código. Hierarquia: este nó (giro Y + correção vertical) → Body (respira, treme, cai) → Sprite.
/// </summary>
public partial class BattleActor : Node3D
{
    private static readonly Shader FlashShader = GD.Load<Shader>("res://src/Lete.Game/Combat/flash.gdshader");
    private static readonly Texture2D BlobTexture = GD.Load<Texture2D>("res://art/sprites/placeholder/sombra_chao.png");

    /// <summary>Tamanho da sombra de blob no chão, em unidades do mundo.</summary>
    [Export] public Vector2 BlobSize { get; set; } = new(0.5f, 0.25f);

    private Node3D _body = null!;
    private Sprite3D _sprite = null!;
    private ShaderMaterial _material = null!;
    private Tween? _breathe;
    private float _height;
    private CpuParticles3D _dust = null!;
    private CpuParticles3D? _sparks;
    private Vector3? _home;
    private MeshInstance3D _blob = null!;

    public BattleAnimTuning Tuning { get; set; } = null!;

    /// <summary>Sombras flutuam em vez de respirar e têm o disco no chão mais fraco que o dos vivos.</summary>
    public bool IsSpirit { get; set; }

    private float _pitchCompensation = 1f;
    private float _depthScale = 1f;

    /// <summary>Metros do mundo por pixel de tela (1080p); converte os parâmetros em pixels de tela.</summary>
    public float WorldPerScreenPixel { get; set; }

    public override void _Ready()
    {
        _body = new Node3D { Name = "Body" };
        AddChild(_body);
        _material = new ShaderMaterial { Shader = FlashShader };
        _sprite = new Sprite3D
        {
            Name = "Sprite",
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = _material,
        };
        _body.AddChild(_sprite);

        _dust = new CpuParticles3D
        {
            Name = "Dust",
            Emitting = false,
            LocalCoords = false,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Box,
            Direction = Vector3.Up,
            Spread = 35f,
        };
        AddChild(_dust);

        AddChild(_blob = new MeshInstance3D
        {
            Name = "Blob",
            Position = new Vector3(0, 0.005f, 0),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Mesh = new PlaneMesh
            {
                Size = BlobSize,
                Material = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    AlbedoTexture = BlobTexture,
                },
            },
        });
    }

    /// <summary>
    /// Vira o plano para a câmera (giro Y) e estica na vertical para compensar a inclinação:
    /// assim a altura na tela é exatamente a altura no mundo e os pixels continuam inteiros.
    /// </summary>
    public void FaceCamera(float cameraYawRadians, float cameraPitchRadians)
    {
        Rotation = new Vector3(0, cameraYawRadians, 0);
        _pitchCompensation = 1f / Mathf.Cos(cameraPitchRadians);
        ApplyScale();
    }

    /// <summary>Escala sutil por profundidade (BattleLayout.DepthScale); 1 mantém os pixels inteiros.</summary>
    public void SetDepthScale(float scale)
    {
        _depthScale = scale;
        ApplyScale();
    }

    private void ApplyScale() => Scale = new Vector3(_depthScale, _depthScale * _pitchCompensation, _depthScale);

    /// <summary>Troca a textura mantendo a altura no mundo; o pixel_size sai da altura da textura.</summary>
    public void SetArt(Texture2D texture, float worldHeight, bool smooth, bool flipH, Color tint)
    {
        _height = worldHeight;
        _sprite.Texture = texture;
        _sprite.PixelSize = worldHeight / texture.GetHeight();
        _sprite.Offset = new Vector2(0, texture.GetHeight() / 2f);
        _sprite.FlipH = flipH;
        _material.SetShaderParameter(smooth ? "tex_linear" : "tex_nearest", texture);
        _material.SetShaderParameter("smooth_filter", smooth);
        _material.SetShaderParameter("tint", tint);
    }

    /// <summary>Ponto no meio do corpo (ex.: 0,55 = um pouco acima da cintura), em coordenadas do mundo.</summary>
    public Vector3 PointAtHeight(float fraction) => ToGlobal(new Vector3(0, _height * fraction, 0));

    /// <summary>Animação parada: os vivos respiram, as sombras flutuam devagar.</summary>
    public void StartIdle()
    {
        _breathe?.Kill();
        ((StandardMaterial3D)((PlaneMesh)_blob.Mesh).Material).AlbedoColor =
            new Color(1, 1, 1, IsSpirit ? Tuning.BlobAlphaSpirit : Tuning.BlobAlphaLiving);

        var amplitude = IsSpirit
            ? Tuning.FloatMeters / Tuning.CharacterHeightMeters / Scale.Y
            : Tuning.BreathePixels * WorldPerScreenPixel / Scale.Y;
        var period = IsSpirit ? Tuning.FloatSeconds : Tuning.BreatheSeconds;
        _breathe = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine);
        _breathe.TweenProperty(_body, "position:y", amplitude, period / 2f);
        _breathe.TweenProperty(_body, "position:y", 0f, period / 2f);
    }

    /// <summary>Tremida e piscada; <paramref name="strength"/> (0 a 1) diminui as duas para golpes mais fracos.</summary>
    public async Task PlayHit(float strength = 1f)
    {
        var shake = CreateTween();
        var amplitude = Tuning.HitShakePixels * strength * WorldPerScreenPixel;
        for (var i = 0; i < Tuning.HitShakeCount; i++)
        {
            shake.TweenProperty(_body, "position:x", i % 2 == 0 ? amplitude : -amplitude, Tuning.HitShakeStepSeconds);
        }
        shake.TweenProperty(_body, "position:x", 0f, Tuning.HitShakeStepSeconds);

        var flash = CreateTween();
        flash.TweenMethod(Callable.From<float>(v => _material.SetShaderParameter("flash", v)), strength, 0f, Tuning.HitFlashSeconds);
        Sparks().Restart();

        await ToSignal(shake, Tween.SignalName.Finished);
    }

    /// <summary>Desliza em direção ao alvo e volta; <paramref name="atImpact"/> roda no ponto mais à frente.</summary>
    public async Task PlayAdvance(Vector3 target, Func<Task>? atImpact = null)
    {
        var start = Position;
        var direction = (target - start) with { Y = 0 };
        // Na batalha a unidade do mundo é a altura do personagem (1,0), então metros viram unidades assim:
        var distance = Tuning.AdvanceMeters / Tuning.CharacterHeightMeters;

        var forward = CreateTween();
        forward.TweenProperty(this, "position", start + direction.Normalized() * distance, Tuning.AdvanceSeconds)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        await ToSignal(forward, Tween.SignalName.Finished);

        var impact = atImpact?.Invoke() ?? Task.CompletedTask;
        await ToSignal(GetTree().CreateTimer(Tuning.AdvanceHoldSeconds), SceneTreeTimer.SignalName.Timeout);

        var back = CreateTween();
        back.TweenProperty(this, "position", start, Tuning.ReturnSeconds)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        await ToSignal(back, Tween.SignalName.Finished);
        await impact;
    }

    /// <summary>Cai deitando (gira em Z em torno dos pés) e some.</summary>
    public async Task PlayDefeat()
    {
        _breathe?.Kill();
        // Cai para longe de quem olha: o sprite espelhado (sombra, olhando para a esquerda) tomba para a direita.
        var direction = _sprite.FlipH ? -1f : 1f;
        var tween = CreateTween();
        tween.TweenProperty(_body, "rotation:z", Mathf.DegToRad(Tuning.FallDegrees) * direction, Tuning.FallSeconds)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenMethod(Callable.From<float>(v => _material.SetShaderParameter("fade", v)), 1f, 0f, Tuning.FadeSeconds);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>Sombra libertada: sobe devagar e some, sem pó (ninguém a destruiu).</summary>
    public async Task PlayDepart()
    {
        _breathe?.Kill();
        var tween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_body, "position:y", _body.Position.Y + Tuning.DepartRise, Tuning.DepartSeconds);
        tween.TweenMethod(Callable.From<float>(v => _material.SetShaderParameter("fade", v)), 1f, 0f, Tuning.DepartSeconds);
        tween.TweenProperty(_blob, "transparency", 1f, Tuning.DepartSeconds);
        await ToSignal(tween, Tween.SignalName.Finished);
        _blob.Visible = false;
    }

    /// <summary>Sombra derrotada: desfaz-se pixel a pixel de cima para baixo enquanto solta pó que sobe.</summary>
    public async Task PlayDisintegrate()
    {
        _breathe?.Kill();
        ConfigureDust();
        _dust.Emitting = true;

        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v => _material.SetShaderParameter("dissolve", v)), 0f, 1f, Tuning.DissolveSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        await ToSignal(tween, Tween.SignalName.Finished);

        _blob.Visible = false;
        _dust.Emitting = false;
        await ToSignal(GetTree().CreateTimer(Tuning.DustLifetimeSeconds), SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>Faísca dourada (em pixels da arte, cor da paleta) criada na primeira vez que o ator apanha.</summary>
    private CpuParticles3D Sparks()
    {
        if (_sparks is not null)
        {
            return _sparks;
        }

        var grain = Tuning.SparkPixels * _sprite.PixelSize;
        _sparks = new CpuParticles3D
        {
            Emitting = false,
            OneShot = true,
            Explosiveness = 1f,
            Amount = Tuning.SparkAmount,
            Lifetime = Tuning.SparkSeconds,
            Position = new Vector3(0, _height * Tuning.SparkHeight, 0),
            Direction = Vector3.Up,
            Spread = 180f,
            InitialVelocityMin = _height * Tuning.SparkSpeed * 0.5f,
            InitialVelocityMax = _height * Tuning.SparkSpeed,
            Gravity = Vector3.Zero,
            Color = UI.UiStyle.Palette.Gold,
            ColorRamp = new Gradient { Offsets = new[] { 0f, 1f }, Colors = new[] { Colors.White, new Color(1, 1, 1, 0) } },
            Mesh = new QuadMesh
            {
                Size = new Vector2(grain, grain),
                Material = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    VertexColorUseAsAlbedo = true,
                },
            },
        };
        AddChild(_sparks);
        return _sparks;
    }

    private void ConfigureDust()
    {
        var grain = Tuning.DustPixels * _sprite.PixelSize;
        _dust.Amount = Tuning.DustAmount;
        _dust.Lifetime = Tuning.DustLifetimeSeconds;
        // A caixa cobre o corpo (o nó tem a correção vertical, então a altura local é a do mundo).
        _dust.Position = new Vector3(0, _height * 0.5f, 0);
        _dust.EmissionBoxExtents = new Vector3(_height * 0.16f, _height * 0.5f, 0.01f);
        _dust.InitialVelocityMin = _height * 0.05f;
        _dust.InitialVelocityMax = _height * 0.2f;
        _dust.Gravity = new Vector3(_height * Tuning.DustDrift, _height * Tuning.DustRise, 0);
        _dust.Color = Tuning.DustColor;
        _dust.ColorRamp = new Gradient
        {
            Offsets = new[] { 0f, 1f },
            Colors = new[] { Colors.White, new Color(1, 1, 1, 0) },
        };
        _dust.Mesh = new QuadMesh
        {
            Size = new Vector2(grain, grain),
            Material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                VertexColorUseAsAlbedo = true,
            },
        };
    }

    /// <summary>Teste: volta em pé e visível.</summary>
    /// <summary>Sombra absorvida: vai até quem a absorve e some nele, deixando um rastro de pó.</summary>
    public async Task PlayAbsorbInto(Vector3 target)
    {
        _breathe?.Kill();
        _home ??= Position;
        ConfigureDust();
        _dust.Emitting = true;
        var tween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "position", Position.Lerp(target with { Y = Position.Y }, Tuning.AbsorbTravel), Tuning.AbsorbSeconds);
        tween.TweenMethod(Callable.From<float>(v => _material.SetShaderParameter("fade", v)), 1f, 0f, Tuning.AbsorbSeconds);
        tween.TweenProperty(_blob, "transparency", 1f, Tuning.AbsorbSeconds);
        await ToSignal(tween, Tween.SignalName.Finished);
        _dust.Emitting = false;
        _blob.Visible = false;
    }

    public void Revive()
    {
        if (_home is { } home)
        {
            Position = home;
        }

        _material.SetShaderParameter("dissolve", 0f);
        _blob.Visible = true;
        _blob.Transparency = 0f;
        _body.Rotation = Vector3.Zero;
        _body.Position = Vector3.Zero;
        _material.SetShaderParameter("fade", 1f);
        StartIdle();
    }
}
