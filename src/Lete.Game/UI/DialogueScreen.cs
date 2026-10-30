using System;
using System.Linq;
using Godot;
using Lete.Core.Dialogue;

namespace Lete.Game.UI;

/// <summary>
/// Tela provisória de conversa (GDD 5.2), até haver arte própria: quem fala, a fala e as opções por postura. Opções já
/// escolhidas antes aparecem esmaecidas; bloqueadas, em cinza com a dica vaga no lugar do texto; o rótulo entre
/// colchetes vem do que a opção exige (origem, Memória, Marca, oferenda). O comentário do companheiro aparece antes das
/// opções, com Concordar, Discordar ou Ignorar. Nó sem opções: Continuar. Com uma sombra, "Oferenda" escolhe o tipo e
/// Dar ou Partilhar (decisão do João, 30/out/2026); depois a conversa recomeça, já com a lucidez nova.
/// </summary>
public partial class DialogueScreen : CanvasLayer
{
    [Export] public Vector2 PanelSize { get; set; } = new(1200, 520);

    private PanelContainer _panel = null!;
    private Label _speaker = null!;
    private Label _line = null!;
    private VBoxContainer _buttons = null!;
    private DialogueRunner? _runner;
    private string? _caption;

    public bool IsOpen => _panel.Visible;

    public event Action? OpenChanged;

    public override void _Ready()
    {
        Layer = 7;
        _panel = UiStyle.MakePanel();
        _panel.CustomMinimumSize = PanelSize;
        AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom, Control.LayoutPresetMode.KeepSize);
        _panel.OffsetTop -= 40;
        _panel.OffsetBottom -= 40;

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 20);
        _panel.AddChild(column);
        _speaker = UiStyle.MakeLabel("", UiStyle.TitleFontSize);
        column.AddChild(_speaker);
        _line = UiStyle.MakeLabel("");
        _line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_line);
        _buttons = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _buttons.AddThemeConstantOverride("separation", 8);
        column.AddChild(_buttons);

        _panel.Visible = false;
    }

    public void Open(string dialogueId)
    {
        _runner = GameRoot.Instance.Dialogues.Open(dialogueId);
        _speaker.Text = T(_runner.Graph.SpeakerKey);
        _panel.Visible = true;
        OpenChanged?.Invoke();
        ShowNode();
    }

    /// <summary>
    /// Avança como se o jogador apertasse a opção <paramref name="index"/> (conferência pela linha de comando): responde ao
    /// comentário, continua um nó sem opções ou escolhe entre as disponíveis.
    /// </summary>
    public void Press(int index) => _buttons.GetChildren().OfType<Button>().Where(b => !b.Disabled).ElementAt(index)
        .EmitSignal(BaseButton.SignalName.Pressed);

    private void ShowNode()
    {
        if (_runner is not { Current: { } node })
        {
            Close();
            return;
        }

        _line.Text = (_caption is { } caption ? T(caption) + "\n\n" : "") + T(node.LineKey);
        _caption = null;
        Clear();
        if (_runner.PendingCommentKey is { } comment)
        {
            _line.Text += "\n\n" + T(comment);
            foreach (var (key, response) in new[]
            {
                ("dialogo.comentario.concordar", CommentResponse.Agree),
                ("dialogo.comentario.discordar", CommentResponse.Disagree),
                ("dialogo.comentario.ignorar", CommentResponse.Ignore),
            })
            {
                AddButton(T(key), () => { _runner.AnswerComment(response); ShowNode(); });
            }
        }
        else if (_runner.Options.Count == 0)
        {
            AddButton(T("dialogo.continuar"), () => { _runner.Continue(); ShowNode(); });
        }
        else
        {
            foreach (var view in _runner.Options)
            {
                var text = view.Available ? T(view.Option.TextKey) : T(view.Option.HintKey ?? view.Option.TextKey);
                var button = AddButton(TagText(view.Tag) + text, () => { _runner.Choose(view); ShowNode(); });
                button.Disabled = !view.Available;
                var color = !view.Available ? UiStyle.Palette.ClayGray : view.Seen ? UiStyle.Palette.IvoryDim : UiStyle.Palette.Ivory;
                button.AddThemeColorOverride("font_color", color);
                button.AddThemeColorOverride("font_disabled_color", UiStyle.Palette.ClayGray);
            }

            if (_runner.Graph.Mode == DialogueMode.Shade && GameRoot.Instance.Offerings.Carried.Any())
            {
                AddButton(T("dialogo.oferenda"), ShowOfferingKinds);
            }
        }

        FocusFirst();
    }

    private void ShowOfferingKinds()
    {
        Clear();
        var offerings = GameRoot.Instance.Offerings;
        foreach (var kind in offerings.Carried)
        {
            AddButton(string.Format(T("dialogo.oferenda.tipo"), T($"oferenda.{kind}"), offerings.Count(kind)), () => ShowGiveOrShare(kind));
        }

        AddButton(T("combate.acao.voltar"), ShowNode);
        FocusFirst();
    }

    private void ShowGiveOrShare(string kind)
    {
        Clear();
        AddButton(T("dialogo.oferenda.dar"), () => Offer(kind, share: false));
        AddButton(T("dialogo.oferenda.partilhar"), () => Offer(kind, share: true));
        AddButton(T("combate.acao.voltar"), ShowOfferingKinds);
        FocusFirst();
    }

    private void Offer(string kind, bool share)
    {
        var dialogues = GameRoot.Instance.Dialogues;
        var id = _runner!.Graph.Id;
        var awakened = dialogues.Offer(id, kind, share);
        _caption = awakened ? share ? "dialogo.oferenda.partilhou" : "dialogo.oferenda.deu" : "dialogo.oferenda.nada";
        _runner = dialogues.Open(id);
        ShowNode();
    }

    private void FocusFirst()
    {
        var first = _buttons.GetChildren().OfType<Button>().FirstOrDefault(b => !b.Disabled);
        Callable.From(() =>
        {
            if (first is not null && IsInstanceValid(first) && first.IsInsideTree())
            {
                first.GrabFocus();
            }
        }).CallDeferred();
    }

    private Button AddButton(string text, Action action)
    {
        var button = new Button { Text = text, Alignment = HorizontalAlignment.Left };
        button.Pressed += action;
        _buttons.AddChild(button);
        return button;
    }

    private void Clear()
    {
        foreach (var child in _buttons.GetChildren())
        {
            _buttons.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void Close()
    {
        Clear();
        _runner = null;
        _panel.Visible = false;
        OpenChanged?.Invoke();
    }

    private static string TagText(DialogueTag? tag) => tag switch
    {
        null => "",
        { Kind: DialogueTagKind.Origin, Value: { } origin } => string.Format(T("dialogo.rotulo.origem"), T(GameRoot.Instance.OriginNameKey(origin))) + " ",
        { Kind: DialogueTagKind.Memory, Value: { } kind } => string.Format(T("dialogo.rotulo.memoria"), T($"dialogo.tipo.{kind}")) + " ",
        { Kind: DialogueTagKind.Mark } => T("dialogo.rotulo.marca") + " ",
        { Kind: DialogueTagKind.Offering } => T("dialogo.rotulo.oferenda") + " ",
        _ => "",
    };

    private static string T(string key) => GameRoot.Instance.Text(key);
}
