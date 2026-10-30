using Godot;

namespace Lete.Game.UI;

/// <summary>Aba do Diário: as frases do que o jogador viu e ouviu, em ordem. Sem marcadores nem contagens.</summary>
public partial class JournalTab : ScrollContainer, IMenuTab
{
    private VBoxContainer _list = null!;

    public string TitleKey => "ui.diario.titulo";

    public string ShortcutAction => "journal";

    public Control View => this;

    public override void _Ready()
    {
        HorizontalScrollMode = ScrollMode.Disabled;
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 12);
        AddChild(_list);
    }

    public void Refresh()
    {
        foreach (var child in _list.GetChildren())
        {
            child.QueueFree();
        }

        var entries = GameRoot.Instance.Journal.Entries;
        if (entries.Count == 0)
        {
            _list.AddChild(UiStyle.MakeLabel(GameRoot.Instance.Text("ui.diario.vazio")));
            return;
        }

        foreach (var key in entries)
        {
            var line = UiStyle.MakeLabel("— " + GameRoot.Instance.Text(key));
            line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            line.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _list.AddChild(line);
        }
    }
}
