using Godot;
using Lete.Core.Combat;
using Lete.Game.UI;

namespace Lete.Game.Combat;

/// <summary>
/// HUD de combate em pixel art, dentro do viewport de baixa resolução (GDD 4.7):
/// Fôlego e Lucidez no canto superior esquerdo; nome, Firmeza e Compreensão da sombra logo acima dela
/// (seguindo-a na tela); legenda do gesto numa faixa no alto; descrição da opção escolhida; retrato do
/// personagem ativo ao lado da caixa de ações. Sem números.
/// </summary>
public partial class BattleHud : Control
{
    private const string MeanderPath = "res://art/ui/meandro_fino.png";
    private const string PortraitFramePath = "res://art/ui/moldura_retrato.png";
    private const string TargetMarkPath = "res://art/ui/marca_alvo.png";
    private const string BreathIconPath = "res://art/ui/icone_folego.png";
    private const string LucidityIconPath = "res://art/ui/icone_lucidez.png";
    private const string ResolveIconPath = "res://art/ui/icone_firmeza.png";
    private const string LaurelPath = "res://art/ui/louro_turno.png";
    private const string TorchPath = "res://art/ui/tocha.png";

    [Export] public BattleHudStyle Style { get; set; } = null!;

    private VBoxContainer _heroBox = null!;
    private Panel _captionBand = null!;
    private PixelBar _breath = null!;
    private PixelBar _lucidity = null!;
    private Label _supportName = null!;
    private Label _supportStatus = null!;
    private ColorRect _fade = null!;
    private VBoxContainer _shadePanel = null!;
    private TextureRect _targetMark = null!;
    private Label _shadeName = null!;
    private PixelBar _resolve = null!;
    private PixelBar _peace = null!;
    private ObolRow _obols = null!;
    private Label _caption = null!;
    private PanelContainer _descriptionBox = null!;
    private Label _description = null!;
    private PanelContainer _portraitFrame = null!;
    private TextureRect _portrait = null!;
    private TextureRect _laurel = null!;
    private TextureRect _torch = null!;
    private Tween? _dangerPulse;
    private Camera3D? _camera;
    private BattleActor? _shade;
    private BattleActor? _activeHero;
    private bool _shadePanelFrozen;
    private Tween? _shadePanelFade;
    private Rect2 _gestureArea;
    private Rect2 _descriptionRect;
    private float? _portraitLeft;
    private bool _showGestureArea;

    private static string T(string key) => GameRoot.Instance.Text(key);

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Theme = UiStyle.BuildTheme(1);

        BuildHeroIndicators();
        BuildShadeIndicators();
        BuildCaption();
        BuildDescription();
        BuildPortrait();
        BuildTurnMarkers();
        BuildFade();

        // A legenda fica por cima do escurecer: na derrota, as falas (Trívia, Hécate) aparecem sobre a tela escura.
        MoveChild(_captionBand, GetChildCount() - 1);
    }

    public override void _Process(double delta)
    {
        if (_camera is null || _shade is null)
        {
            return;
        }

        if (_shadePanelFrozen)
        {
            return;
        }

        // Indicadores da sombra logo acima da cabeça dela, dentro da tela.
        var head = _camera.UnprojectPosition(_shade.PointAtHeight(1f));
        var size = _shadePanel.GetCombinedMinimumSize();
        var position = new Vector2(head.X - size.X / 2f, head.Y - size.Y - Style.ShadeIndicatorGap).Round();
        var screen = GetViewportRect().Size;
        _shadePanel.Position = position.Clamp(Vector2.Zero, screen - size);
        _shadePanel.Size = size;

        // Coroa de louro sobre o personagem que age.
        _laurel.Visible = _activeHero is not null;
        if (_activeHero is not null)
        {
            var top = _camera.UnprojectPosition(_activeHero.PointAtHeight(1f));
            var laurel = _laurel.Texture.GetSize();
            _laurel.Position = new Vector2(top.X - laurel.X / 2f, top.Y - laurel.Y - Style.LaurelGap).Round();
        }
    }

    /// <summary>
    /// Indicador de turno (proposta de UI, PENDENCIAS): louro sobre o personagem do grupo que age; na vez da
    /// sombra, uma tocha acesa ao lado do nome dela.
    /// </summary>
    public void SetTurn(BattleActor? hero, bool shadeTurn)
    {
        _activeHero = hero;
        _torch.Modulate = shadeTurn ? Colors.White : Colors.Transparent;
    }

    /// <summary>
    /// A sombra vai embora (absorvida): os indicadores dela param onde estão e somem aos poucos, em vez de
    /// acompanhá-la. Também tira o louro e a tocha do turno.
    /// </summary>
    public void FadeShadePanel(float seconds)
    {
        _shadePanelFrozen = true;
        _shadePanelFade?.Kill();
        _shadePanelFade = CreateTween();
        _shadePanelFade.TweenProperty(_shadePanel, "modulate:a", 0f, seconds);
    }

    /// <summary>Luta nova: os indicadores voltam a seguir a sombra, visíveis.</summary>
    public void ResetShadePanel()
    {
        _shadePanelFade?.Kill();
        _shadePanelFrozen = false;
        _shadePanel.Modulate = Colors.White;
    }

    public void Track(Camera3D camera, BattleActor shade)
    {
        _camera = camera;
        _shade = shade;
    }

    public void ApplyLayout(HudLayout layout, ScreenRect gestureArea)
    {
        var screen = GetViewportRect().Size;
        _heroBox.Position = (Style.HeroIndicatorsAt * screen).Round();
        var caption = Snap(new Rect2(Style.CaptionRect.Position * screen, Style.CaptionRect.Size * screen));
        _captionBand.Position = caption.Position;
        _captionBand.Size = caption.Size;

        Rect2 ToPixels(ScreenRect r) => Snap(new Rect2(r.X * screen.X, r.Y * screen.Y, r.Width * screen.X, r.Height * screen.Y));

        _descriptionRect = ToPixels(layout.Description);
        _description.CustomMinimumSize = new Vector2(_descriptionRect.Size.X, 0);
        PlaceDescription();

        _gestureArea = ToPixels(gestureArea);
        QueueRedraw();
    }

    /// <summary>Retrato ao lado (à esquerda e no alto) da caixa de ações.</summary>
    public void PlacePortrait(Rect2 menuRect)
    {
        var size = _portraitFrame.GetCombinedMinimumSize();
        _portraitFrame.Position = new Vector2(menuRect.Position.X - size.X - Style.PortraitGap, menuRect.End.Y - size.Y).Round();
        _portraitLeft = _portraitFrame.Position.X;
        PlaceDescription();
    }

    public void SetPortrait(Texture2D texture)
    {
        // Recorte quadrado do alto da imagem (o rosto).
        var side = texture.GetWidth();
        _portrait.Texture = new AtlasTexture { Atlas = texture, Region = new Rect2(0, 0, side, side) };
    }

    public void SetCaption(string text)
    {
        _caption.Text = text;
        CallDeferred(MethodName.WarnIfCaptionClipped);
    }

    /// <summary>Texto maior que a faixa some sem aviso; aqui ele aparece no console para encurtar o texto ou a faixa.</summary>
    private void WarnIfCaptionClipped()
    {
        if (_caption.GetLineCount() > _caption.GetVisibleLineCount())
        {
            GD.PushWarning($"Legenda cortada ({_caption.GetLineCount()} linhas, cabem {_caption.GetVisibleLineCount()}): \"{_caption.Text}\"");
        }
    }

    public void SetDescription(string text)
    {
        _description.Text = text;
        CallDeferred(MethodName.PlaceDescription);
    }

    /// <summary>
    /// A caixa da descrição cresce para cima conforme o texto: a base fica na linha do layout (alinhada com o menu),
    /// nunca sai da tela por baixo. A altura do layout é o mínimo.
    /// </summary>
    private void PlaceDescription()
    {
        _descriptionBox.Size = Vector2.Zero;
        var size = _descriptionBox.GetCombinedMinimumSize();
        var height = Mathf.Max(size.Y, _descriptionRect.Size.Y);
        _descriptionBox.Size = new Vector2(size.X, height);

        // Nunca por baixo do retrato: se o menu alargou (opções mais longas) e o retrato veio para a esquerda,
        // a caixa anda junto e fica sempre à esquerda dele.
        var x = _descriptionRect.Position.X;
        if (_portraitLeft is { } portraitLeft)
        {
            x = Mathf.Min(x, portraitLeft - Style.PortraitGap - size.X);
        }

        _descriptionBox.Position = new Vector2(x, _descriptionRect.End.Y - height).Round();
    }

    public void SetShadeName(string text) => _shadeName.Text = text;

    public void SetResolve(float fraction) => _resolve.TweenTo(fraction);

    /// <summary>Fôlego; abaixo de <see cref="BattleHudStyle.DangerFraction"/> a barra pulsa em terracota (perigo, nunca vermelho).</summary>
    public void SetBreath(float fraction)
    {
        _breath.TweenTo(fraction);
        var danger = fraction > 0f && fraction < Style.DangerFraction;
        _breath.Fill = danger ? Style.Palette.Terracotta : Style.Palette.Ivory;
        if (danger && _dangerPulse is null)
        {
            _dangerPulse = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine);
            _dangerPulse.TweenProperty(_breath, "self_modulate:a", 0.45f, Style.DangerPulseSeconds / 2f);
            _dangerPulse.TweenProperty(_breath, "self_modulate:a", 1f, Style.DangerPulseSeconds / 2f);
        }
        else if (!danger && _dangerPulse is not null)
        {
            _dangerPulse.Kill();
            _dangerPulse = null;
            _breath.SelfModulate = Colors.White;
        }
    }

    public void SetPeace(float fraction)
    {
        _peace.Visible = fraction > 0f;
        _peace.TweenTo(fraction);
    }

    public void SetLucidity(float fraction) => _lucidity.TweenTo(fraction);

    /// <summary>Selo do apoio do companheiro (nome e apoio) e o contador ("Apoio em 1 turno"), GDD 4.7.</summary>
    public void SetSupport(string name, string status)
    {
        _supportName.Text = name;
        _supportStatus.Text = status;
    }

    /// <summary>Escurece (ou clareia) a tela inteira, por cima do HUD.</summary>
    public SignalAwaiter Fade(bool toBlack, float seconds)
    {
        var tween = CreateTween();
        tween.TweenProperty(_fade, "color:a", toBlack ? 1f : 0f, seconds);
        return ToSignal(tween, Tween.SignalName.Finished);
    }

    public void SetUnderstanding(int points) => _obols.SetFilled(points);

    public void SetTargetMarked(bool marked) => _targetMark.Modulate = marked ? Colors.White : Colors.Transparent;

    /// <summary>
    /// Durante um gesto: some o que não for essencial (o retrato; o menu se esconde sozinho). A caixa de descrição
    /// só fica se houver <paramref name="hint"/>, a dica de como fazer o gesto.
    /// </summary>
    public void SetGestureActive(bool active, string? hint = null)
    {
        _portraitFrame.Visible = !active;
        _descriptionBox.Visible = !active || hint is not null;
        if (active && hint is not null)
        {
            SetDescription(hint);
        }
    }

    public void ToggleGestureAreaGuide()
    {
        _showGestureArea = !_showGestureArea;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_showGestureArea)
        {
            DrawRect(_gestureArea, new Color(Style.Palette.Gold, 0.8f), filled: false);
        }
    }

    private void BuildHeroIndicators()
    {
        // Cada barra com seu ícone na mesma linha (sopro = Fôlego, gota = Lucidez): o bloco fica baixo e
        // não chega na cabeça de quem está atrás no grupo.
        var box = _heroBox = new VBoxContainer();
        box.AddThemeConstantOverride("separation", Style.BarRowGap);
        box.AddChild(IconRow(BreathIconPath, _breath = new PixelBar { Style = Style, Fill = Style.Palette.Ivory }));
        box.AddChild(IconRow(LucidityIconPath, _lucidity = new PixelBar { Style = Style, Fill = Style.Palette.Lapis }));
        box.AddChild(new Control { CustomMinimumSize = new Vector2(0, Style.SupportGap) });
        box.AddChild(_supportName = MakeLabel(""));
        box.AddChild(_supportStatus = MakeLabel(""));
        _supportStatus.AddThemeColorOverride("font_color", Style.Palette.IvoryDim);
        AddChild(box);
    }

    private void BuildShadeIndicators()
    {
        // Compacto: [marca de alvo] nome, e embaixo a Firmeza com os óbolos de Compreensão ao lado.
        _shadePanel = new VBoxContainer();
        _shadePanel.AddThemeConstantOverride("separation", 0);
        var nameRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        nameRow.AddThemeConstantOverride("separation", 2);
        _targetMark = new TextureRect
        {
            Texture = GD.Load<Texture2D>(TargetMarkPath),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            Modulate = Colors.Transparent,
        };
        nameRow.AddChild(_targetMark);
        _shadeName = MakeLabel(T("combate.sombra.nome_oculto"));
        nameRow.AddChild(_shadeName);
        nameRow.AddChild(_torch = new TextureRect
        {
            Texture = GD.Load<Texture2D>(TorchPath),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            Modulate = Colors.Transparent,
        });
        _shadePanel.AddChild(nameRow);

        var barRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        barRow.AddThemeConstantOverride("separation", 2);
        barRow.AddChild(Icon(ResolveIconPath));
        barRow.AddChild(_resolve = new PixelBar { Style = Style, Fill = Style.Palette.ShadeFill, SizeFlagsVertical = SizeFlags.ShrinkCenter });
        barRow.AddChild(_obols = new ObolRow { Count = ShadeUnderstanding.MaxPoints, SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _shadePanel.AddChild(barRow);

        // Paz (Libertar): só aparece depois da primeira tentativa certa.
        _peace = new PixelBar { Style = Style, Fill = Style.Palette.PeaceFill, Value = 0f, Visible = false, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        _shadePanel.AddChild(_peace);
        AddChild(_shadePanel);

        var blink = CreateTween().SetLoops();
        blink.TweenProperty(_targetMark, "self_modulate:a", 0.35f, 0.5f);
        blink.TweenProperty(_targetMark, "self_modulate:a", 1f, 0.5f);
    }

    private void BuildCaption()
    {
        // Faixa de tamanho fixo (não é contêiner): o texto quebra linhas dentro dela sem esticá-la.
        _captionBand = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        _captionBand.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(Style.Palette.Night, Style.CaptionAlpha) });

        var top = MakeMeanderLine();
        top.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        top.OffsetBottom = top.CustomMinimumSize.Y;
        _captionBand.AddChild(top);

        var bottom = MakeMeanderLine();
        bottom.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        bottom.OffsetTop = -bottom.CustomMinimumSize.Y;
        _captionBand.AddChild(bottom);

        _caption = MakeLabel("");
        _caption.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _caption.OffsetTop = top.CustomMinimumSize.Y;
        _caption.OffsetBottom = -bottom.CustomMinimumSize.Y;
        _caption.HorizontalAlignment = HorizontalAlignment.Center;
        _caption.VerticalAlignment = VerticalAlignment.Center;
        _caption.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _caption.ClipText = true;
        _captionBand.AddChild(_caption);
        AddChild(_captionBand);
    }

    private void BuildDescription()
    {
        _descriptionBox = new PanelContainer();
        _description = MakeLabel("");
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.AddThemeColorOverride("font_color", Style.Palette.IvoryDim);
        _descriptionBox.AddChild(_description);
        AddChild(_descriptionBox);
    }

    private void BuildPortrait()
    {
        _portraitFrame = new PanelContainer();
        // Medalhão dourado do retrato.
        _portraitFrame.AddThemeStyleboxOverride("panel", new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>(PortraitFramePath),
            TextureMarginLeft = 5,
            TextureMarginTop = 5,
            TextureMarginRight = 5,
            TextureMarginBottom = 5,
            ContentMarginLeft = 4,
            ContentMarginTop = 4,
            ContentMarginRight = 4,
            ContentMarginBottom = 4,
        });
        _portrait = new TextureRect { StretchMode = TextureRect.StretchModeEnum.KeepCentered };
        _portraitFrame.AddChild(_portrait);
        AddChild(_portraitFrame);
    }

    private void BuildFade()
    {
        // Transições: fade curto na cor do fundo (azul-noite), não em preto puro.
        _fade = new ColorRect { Color = new Color(Style.Palette.Night, 0f), MouseFilter = MouseFilterEnum.Ignore };
        _fade.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_fade);
    }

    /// <summary>Arredonda para pixels inteiros (o HUD vive na grade de pixels da arte).</summary>
    public static Rect2 Snap(Rect2 rect) => new(rect.Position.Round(), rect.Size.Round());

    private TextureRect MakeMeanderLine()
    {
        var texture = GD.Load<Texture2D>(MeanderPath);
        return new TextureRect
        {
            Texture = texture,
            StretchMode = TextureRect.StretchModeEnum.Tile,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(0, texture.GetHeight()),
        };
    }

    private void BuildTurnMarkers()
    {
        AddChild(_laurel = new TextureRect { Texture = GD.Load<Texture2D>(LaurelPath), Visible = false, MouseFilter = MouseFilterEnum.Ignore });
    }

    private static TextureRect Icon(string path) => new()
    {
        Texture = GD.Load<Texture2D>(path),
        StretchMode = TextureRect.StretchModeEnum.KeepCentered,
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
    };

    private HBoxContainer IconRow(string iconPath, Control bar)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", Style.IconGap);
        bar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(Icon(iconPath));
        row.AddChild(bar);
        return row;
    }

    private Label MakeLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", Style.Palette.Ivory);
        return label;
    }
}
