using System;
using Godot;
using Lete.Core.Conditions;
using Lete.Core.Data;
using Lete.Core.Events;
using Lete.Core.Localization;
using Lete.Core.Presentation;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Game;

/// <summary>
/// Autoload que guarda os serviços do Core (estado, eventos, sistemas, textos) durante o jogo inteiro.
/// É a ponte entre a camada Godot e a lógica pura de <c>Lete.Core</c>.
/// </summary>
public partial class GameRoot : Node
{
    private const string LocalePath = "res://content/locale/pt_BR.json";
    private const string MarkRulesPath = "res://content/rules/marca.json";
    private const string TideRulesPath = "res://content/rules/mare.json";
    private const string ProfileRulesPath = "res://content/rules/perfil.json";
    private const string PathFolder = "res://content/path";
    private const string OathsFolder = "res://content/oaths";
    private const string OathRulesPath = "res://content/rules/juramentos.json";
    private const string MosaicRulesPath = "res://content/rules/mosaico.json";
    private const string RecollectionsFolder = "res://content/recollections";
    private const string ContentRoot = "res://content";

    /// <summary>Um slot só por enquanto (ARQUITETURA 4.3 recomenda um por perfil mais o autosave de região).</summary>
    public static string SavePath => UserPath("save/jogo.json");

    /// <summary>
    /// Arquivo do jogador (save, opções). No jogo exportado, em <c>user://</c> (AppData). Rodando pelo editor do
    /// Godot (desenvolvimento), em <c>.dados_dev/</c> na raiz do projeto, para tudo do kit ficar dentro da pasta dele
    /// (pedido do João, 30/out/2026); o Godot não importa pastas que começam com ponto, e o git as ignora.
    /// </summary>
    public static string UserPath(string relative) => OS.HasFeature("editor")
        ? ProjectSettings.GlobalizePath($"res://.dados_dev/{relative}")
        : $"user://{relative}";

    public static GameRoot Instance { get; private set; } = null!;

    public EventBus Events { get; } = new();
    public WorldState State { get; private set; } = new();
    public Journal Journal { get; private set; } = null!;
    public MarkTracker Mark { get; private set; } = null!;
    public TideTracker Tide { get; private set; } = null!;
    public Mosaic Mosaic { get; private set; } = null!;
    public HeavyMemories HeavyMemories { get; private set; } = null!;
    public Profile Profile { get; private set; } = null!;
    public PathKeys PathKeys { get; private set; } = null!;
    public Oaths Oaths { get; private set; } = null!;
    public OriginFragments OriginFragments { get; private set; } = null!;

    /// <summary>Respostas do estado atual às condições do conteúdo (<see cref="ConditionEvaluator"/>).</summary>
    public WorldFacts Facts { get; private set; } = null!;
    public Localizer Localizer { get; private set; } = null!;

    /// <summary>Como a Marca aparece no sprite de mapa e de batalha (perda de cor por faixa).</summary>
    public MarkLook MarkLook { get; private set; } = null!;

    /// <summary>Logo antes de gravar: quem tem estado vivo fora do <see cref="State"/> (a posição do jogador) o escreve nele.</summary>
    public event Action? Saving;

    /// <summary>O estado inteiro foi trocado (carregar um save): telas e jogador devem se redesenhar.</summary>
    public event Action? StateReplaced;

    private MarkRules _markRules = null!;
    private TideRules _tideRules = null!;
    private ProfileRules _profileRules = null!;
    private MosaicRules _mosaicRules = null!;
    private System.Collections.Generic.Dictionary<string, Recollection> _recollections = null!;
    private System.Collections.Generic.Dictionary<string, PathKey> _pathKeys = null!;
    private System.Collections.Generic.Dictionary<string, Oath> _oaths = null!;
    private OathRules _oathRules = null!;

    public override void _EnterTree()
    {
        Instance = this;
        Localizer = Localizer.FromJson(FileAccess.GetFileAsString(LocalePath));
        ValidateContent();
        if (OS.IsDebugBuild())
        {
            Events.AnyPublished += e => GD.Print($"[evento] {e}");
        }
        var markJson = FileAccess.GetFileAsString(MarkRulesPath);
        _markRules = MarkData.FromJson(markJson);
        MarkLook = MarkData.LookFromJson(markJson);
        _tideRules = TideData.FromJson(FileAccess.GetFileAsString(TideRulesPath));
        _profileRules = ProfileData.FromJson(FileAccess.GetFileAsString(ProfileRulesPath));
        _mosaicRules = RecollectionData.RulesFromJson(FileAccess.GetFileAsString(MosaicRulesPath));
        _recollections = new System.Collections.Generic.Dictionary<string, Recollection>(StringComparer.Ordinal);
        foreach (var name in DirAccess.GetFilesAt(RecollectionsFolder))
        {
            if (name.EndsWith(".json", StringComparison.Ordinal))
            {
                var recollection = RecollectionData.FromJson(FileAccess.GetFileAsString($"{RecollectionsFolder}/{name}"));
                _recollections[recollection.Id] = recollection;
            }
        }
        _pathKeys = LoadFolder(PathFolder, PathData.FromJson, k => k.Id);
        _oaths = LoadFolder(OathsFolder, OathData.FromJson, o => o.Id);
        _oathRules = OathData.RulesFromJson(FileAccess.GetFileAsString(OathRulesPath));

        BuildSystems();
    }

    /// <summary>Um item por arquivo <c>.json</c> da pasta, pelo id.</summary>
    private static System.Collections.Generic.Dictionary<string, T> LoadFolder<T>(string folder, Func<string, T> read, Func<T, string> id)
    {
        var items = new System.Collections.Generic.Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var name in DirAccess.GetFilesAt(folder))
        {
            if (name.EndsWith(".json", StringComparison.Ordinal))
            {
                var item = read(FileAccess.GetFileAsString($"{folder}/{name}"));
                items[id(item)] = item;
            }
        }

        return items;
    }

    /// <summary>Texto da chave, já com o gênero do protagonista resolvido.</summary>
    public string Text(string key) => Localizer.Get(key, State.Gender);

    public bool HasSave => FileAccess.FileExists(SavePath);

    /// <summary>Grava o estado (nas Trívias e ao trocar de região; por enquanto só pelo painel de debug).</summary>
    public Error Save()
    {
        Saving?.Invoke();
        DirAccess.MakeDirRecursiveAbsolute(SavePath.GetBaseDir());
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file is null)
        {
            return FileAccess.GetOpenError();
        }

        file.StoreString(SaveGame.Write(State));
        return Error.Ok;
    }

    public Error Load()
    {
        if (!HasSave)
        {
            return Error.FileNotFound;
        }

        State = SaveGame.Read(FileAccess.GetFileAsString(SavePath));
        BuildSystems();
        StateReplaced?.Invoke();
        return Error.Ok;
    }

    /// <summary>Os sistemas apontam para o estado atual; ao trocá-lo, são refeitos (e os antigos param de escutar).</summary>
    private void BuildSystems()
    {
        Journal?.Dispose();
        Mark?.Dispose();
        Mosaic?.Dispose();
        Profile?.Dispose();
        PathKeys?.Dispose();
        Oaths?.Dispose();
        Journal = new Journal(State, Events);
        Mark = new MarkTracker(State, Events, _markRules);
        Tide = new TideTracker(State, Events, _tideRules);
        Mosaic = new Mosaic(State, Events, _recollections, _mosaicRules);
        HeavyMemories = new HeavyMemories(State, Events, _recollections.Values);
        OriginFragments = new OriginFragments(State, Events);
        Profile = new Profile(State, Events, _profileRules);
        Facts = new WorldFacts(State, _markRules, Mosaic, Profile);
        Oaths = new Oaths(State, Events, _oaths, _oathRules, () => Facts);
        PathKeys = new PathKeys(State, Events, _pathKeys, () => Facts);
        PathKeys.Refresh();
    }

    /// <summary>
    /// Confere o conteúdo inteiro ao abrir (DADOS.md 9): cada problema vira um erro com arquivo, campo e mensagem.
    /// Valores <c>_tbd</c> só contam como aviso em desenvolvimento; num export de release, viram erro.
    /// </summary>
    private void ValidateContent()
    {
        var files = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
        CollectJson(ContentRoot, files);
        var report = ContentValidator.Validate(files, new System.Collections.Generic.HashSet<string>(Localizer.Keys), release: !OS.IsDebugBuild());
        foreach (var error in report.Errors)
        {
            GD.PushError($"Conteúdo: {error}");
        }

        if (report.Warnings.Count > 0)
        {
            GD.Print($"Conteúdo: {files.Count} arquivos, {report.Errors.Count} erros, {report.Warnings.Count} valores provisórios (_tbd).");
        }
    }

    private static void CollectJson(string folder, System.Collections.Generic.Dictionary<string, string> files)
    {
        foreach (var name in DirAccess.GetFilesAt(folder))
        {
            if (name.EndsWith(".json", StringComparison.Ordinal))
            {
                var path = $"{folder}/{name}";
                files[path[(ContentRoot.Length + 1)..]] = FileAccess.GetFileAsString(path);
            }
        }

        foreach (var sub in DirAccess.GetDirectoriesAt(folder))
        {
            CollectJson($"{folder}/{sub}", files);
        }
    }
}
