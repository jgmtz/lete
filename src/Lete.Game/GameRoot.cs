using System;
using System.Linq;
using Godot;
using Lete.Core.Conditions;
using Lete.Core.Data;
using Lete.Core.Dialogue;
using Lete.Core.Events;
using Lete.Core.Items;
using Lete.Core.World;
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
    private const string DialogueFolder = "res://content/dialogue";
    private const string OfferingRulesPath = "res://content/rules/oferendas.json";
    private const string HealingRulesPath = "res://content/rules/cura.json";
    public const string ProvisionsPath = "res://content/items/provisoes.json";
    public const string TestBagPath = "res://content/items/bolsa_teste.json";
    private const string ItemsFolder = "res://content/items";
    private const string ItemSetsFolder = "res://content/item_sets";
    private const string MerchantsFolder = "res://content/merchants";
    private const string TriviasFolder = "res://content/trivias";
    private const string TrialsFolder = "res://content/trials";
    private const string MerchantRulesPath = "res://content/rules/mercadores.json";
    private const string TribunalRulesPath = "res://content/rules/tribunal.json";
    private const string OriginsPath = "res://content/characters/origens.json";
    private const string OathRulesPath = "res://content/rules/juramentos.json";
    private const string CompanionsFolder = "res://content/companions";
    private const string CompanionRulesPath = "res://content/rules/companheiro.json";
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
    public Dialogues Dialogues { get; private set; } = null!;
    public Offerings Offerings { get; private set; } = null!;
    public ItemTracker Items { get; private set; } = null!;
    public Merchants Merchants { get; private set; } = null!;
    public Trivias Trivias { get; private set; } = null!;
    public Tribunal Tribunal { get; private set; } = null!;
    public Healing Healing { get; private set; } = null!;
    public Companions Companions { get; private set; } = null!;
    public ShadeRecord Shades { get; private set; } = null!;
    public Regions Regions { get; private set; } = null!;

    /// <summary>As provisões do jogo (definições; as quantidades estão na Bolsa do estado).</summary>
    public System.Collections.Generic.IReadOnlyList<Lete.Core.Combat.ProvisionDefinition> Provisions { get; private set; } = null!;

    /// <summary>Chave do nome de uma origem (para o rótulo <c>[Artesão]</c> das conversas).</summary>
    public string OriginNameKey(string originId) => _originNames.TryGetValue(originId, out var key) ? key : originId;

    /// <summary>Os efeitos dos nós de conversa, cada um no sistema certo.</summary>
    private sealed class GameDialogueEffects : IDialogueEffects
    {
        private readonly GameRoot _root;

        public GameDialogueEffects(GameRoot root) => _root = root;

        public void Apply(DialogueEffect effect)
        {
            switch (effect)
            {
                case AddJournalEntry e: _root.Journal.Add(e.EntryKey); break;
                case SetFlag e: _root.State.Flags.Add(e.Flag); break;
                case GainOriginFragment e: _root.OriginFragments.Gain(e.FragmentId); break;
                case HearRecollectionSource e: _root.Mosaic.Hear(e.RecollectionId, e.SourceId); break;
                case SwearOath e: _root.Oaths.Swear(e.OathId); break;
                case BreakOath e: _root.Oaths.Break(e.OathId); break;
                case OfferHospitality e: _root.Healing.Hospitality(e.ShadeId); break;
            }
        }
    }
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
    private System.Collections.Generic.Dictionary<string, DialogueGraph> _dialogues = null!;
    private OfferingRules _offeringRules = null!;
    private HealingRules _healingRules = null!;
    private System.Collections.Generic.Dictionary<string, Item> _items = null!;
    private System.Collections.Generic.Dictionary<string, ItemSet> _itemSets = null!;
    private System.Collections.Generic.Dictionary<string, Merchant> _merchants = null!;
    private System.Collections.Generic.Dictionary<string, Trivia> _trivias = null!;
    private System.Collections.Generic.Dictionary<string, Trial> _trials = null!;
    private MerchantRules _merchantRules = null!;
    private TribunalRules _tribunalRules = null!;
    private System.Collections.Generic.Dictionary<string, Companion> _companions = null!;
    private CompanionRules _companionRules = null!;
    private System.Collections.Generic.Dictionary<string, string> _originNames = null!;
    private System.Collections.Generic.IReadOnlyList<Origin> _origins = null!;

    /// <summary>As origens (com as pessoas ligadas e os fragmentos).</summary>
    public System.Collections.Generic.IReadOnlyList<Origin> Origins => _origins;

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
        _dialogues = LoadFolder(DialogueFolder, DialogueData.FromJson, d => d.Id);
        _offeringRules = BagData.OfferingRulesFromJson(FileAccess.GetFileAsString(OfferingRulesPath));
        _healingRules = BagData.HealingRulesFromJson(FileAccess.GetFileAsString(HealingRulesPath));
        Provisions = BagData.ProvisionsFromJson(FileAccess.GetFileAsString(ProvisionsPath));
        // A pasta de itens também tem a Bolsa de teste e as provisões: só os arquivos com 'category' são itens.
        _items = LoadFolder(ItemsFolder, ItemData.FromJson, i => i.Id, ItemData.IsItem);
        _itemSets = LoadFolder(ItemSetsFolder, ItemData.SetFromJson, s => s.Id);
        _merchants = LoadFolder(MerchantsFolder, WorldData.MerchantFromJson, m => m.Id);
        _trivias = LoadFolder(TriviasFolder, WorldData.TriviaFromJson, t => t.Id);
        _trials = LoadFolder(TrialsFolder, WorldData.TrialFromJson, t => t.Id);
        _merchantRules = WorldData.MerchantRulesFromJson(FileAccess.GetFileAsString(MerchantRulesPath));
        _tribunalRules = WorldData.TribunalRulesFromJson(FileAccess.GetFileAsString(TribunalRulesPath));
        _companions = LoadFolder(CompanionsFolder, CompanionData.FromJson, c => c.Id);
        _companionRules = CompanionData.RulesFromJson(FileAccess.GetFileAsString(CompanionRulesPath));
        _origins = OriginData.FromJson(FileAccess.GetFileAsString(OriginsPath));
        _originNames = _origins.ToDictionary(o => o.Id, o => o.NameKey, StringComparer.Ordinal);

        BuildSystems();
    }

    /// <summary>Um item por arquivo <c>.json</c> da pasta, pelo id (só os que <paramref name="accept"/> aceita, se houver).</summary>
    private static System.Collections.Generic.Dictionary<string, T> LoadFolder<T>(
        string folder, Func<string, T> read, Func<T, string> id, Func<string, bool>? accept = null)
    {
        var items = new System.Collections.Generic.Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var name in DirAccess.GetFilesAt(folder))
        {
            if (name.EndsWith(".json", StringComparison.Ordinal))
            {
                var json = FileAccess.GetFileAsString($"{folder}/{name}");
                if (accept is null || accept(json))
                {
                    var item = read(json);
                    items[id(item)] = item;
                }
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

    /// <summary>
    /// Troca de região (a cena chama ao passar para outra): é um dos dois momentos de salvar (o outro é a Trívia).
    /// Falso se já estava nela (nada a salvar).
    /// </summary>
    public bool ChangeRegion(string regionId)
    {
        if (!Regions.Enter(regionId))
        {
            return false;
        }

        var error = Save();
        GD.Print($"[save] troca de região para {regionId}: {error}");
        return true;
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
        Dialogues?.Dispose();
        Items?.Dispose();
        Companions?.Dispose();
        OriginFragments?.Dispose();
        Shades?.Dispose();
        Journal = new Journal(State, Events);
        Mark = new MarkTracker(State, Events, _markRules);
        Tide = new TideTracker(State, Events, _tideRules);
        Companions = new Companions(State, Events, _companions, _companionRules, Tide);
        Shades = new ShadeRecord(State, Events);
        Regions = new Regions(State, Events);
        Mosaic = new Mosaic(State, Events, _recollections, _mosaicRules);
        HeavyMemories = new HeavyMemories(State, Events, _recollections.Values);
        OriginFragments = new OriginFragments(State, Events, _origins,
            id => _dialogues.TryGetValue(id, out var graph) ? graph.ShadeId ?? id : id);
        Profile = new Profile(State, Events, _profileRules);
        Facts = new WorldFacts(State, _markRules, Mosaic, Profile);
        Oaths = new Oaths(State, Events, _oaths, _oathRules, () => Facts);
        Offerings = new Offerings(State, Events, _offeringRules);
        Healing = new Healing(State, Events, _healingRules, Offerings, () => Tide.Step);
        Dialogues = new Dialogues(State, Events, _dialogues, Facts, new GameDialogueEffects(this),
            id => _recollections.TryGetValue(id, out var recollection) ? recollection.Kind : null, Offerings);
        Items = new ItemTracker(State, Events, _items, _itemSets, () => Facts);
        Merchants = new Merchants(State, Events, _merchants, _merchantRules, Profile, Offerings, Items, () => Tide.Step);
        Trivias = new Trivias(State, Events, _trivias, Tide);
        Tribunal = new Tribunal(State, Events, _trials, _tribunalRules, Mosaic);
        PathKeys = new PathKeys(State, Events, _pathKeys, () => Facts);
        PathKeys.Refresh();
        Items.Refresh();
    }

    /// <summary>
    /// Confere o conteúdo inteiro ao abrir (DADOS.md 9): cada problema vira um erro com arquivo, campo e mensagem.
    /// Valores <c>_tbd</c> só contam como aviso em desenvolvimento; num export de release, viram erro.
    /// </summary>
    private void ValidateContent()
    {
        var files = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
        CollectJson(ContentRoot, files);
        foreach (var problem in LocaleValidator.Validate(Localizer.Strings))
        {
            GD.PushError($"Idioma: {problem}");
        }

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
