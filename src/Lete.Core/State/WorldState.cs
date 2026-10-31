namespace Lete.Core.State;

/// <summary>
/// Estado único e serializável do mundo (ARQUITETURA.md, 4.1). Salvar é gravar este objeto (<see cref="SaveGame"/>).
/// Os campos entram aos poucos, junto com os sistemas que os usam.
/// </summary>
public sealed class WorldState
{
    public Gender Gender { get; set; } = Gender.Masculine;

    /// <summary>
    /// Corpo escolhido no reflexo do rio (id do elenco: <c>sombra_m</c> ou <c>sombra_f</c>, protagonista-sombra,
    /// decisão de 26/out/2026); o gênero dos textos vem dele. Nulo antes da escolha.
    /// </summary>
    public string? ProtagonistId { get; set; }

    /// <summary>Origem escolhida logo depois do reflexo (id de <c>content/characters/origens.json</c>). Nula antes.</summary>
    public string? OriginId { get; set; }

    /// <summary>O companheiro da partida (um dos 6; irreversível). Nulo antes da escolha.</summary>
    public string? CompanionId { get; set; }

    /// <summary>Vínculo com o companheiro (só cresce; ver <c>Systems.Companions</c>).</summary>
    public int CompanionBond { get; set; }

    /// <summary>Quantas conversas de Trívia já teve com ele, e em que degrau da Maré foi a última.</summary>
    public int CompanionTalks { get; set; }

    public int? CompanionTalkStep { get; set; }

    /// <summary>Marca dos Mortos: contador interno, nunca exibido como número (só sobe; ver <c>Systems.MarkTracker</c>).</summary>
    public int Mark { get; set; }

    /// <summary>Etapa da Marca no corpo (0 a 3, só sobe), mostrada no inventário.</summary>
    public int MarkStage { get; set; }

    /// <summary>O rosto pela regra C; <see cref="State.FinalFace.None"/> até o fim.</summary>
    public FinalFace FinalFace { get; set; }

    /// <summary>Maré do Lete: só sobe; o degrau é a parte inteira (ver <c>Systems.TideTracker</c>).</summary>
    public decimal Tide { get; set; }

    /// <summary>Marcos da Maré já contados (cada um sobe a Maré uma vez só).</summary>
    public HashSet<string> TideMilestones { get; } = new(StringComparer.Ordinal);

    /// <summary>Mosaico: Lembranças que o jogador já começou a lembrar (id → fontes, ato pleno, falsa).</summary>
    public Dictionary<string, RecollectionProgress> Recollections { get; } = new(StringComparer.Ordinal);

    /// <summary>Memórias Pesadas já decididas (id → Encarar ou Deixar). As que faltam estão pendentes.</summary>
    public Dictionary<string, HeavyMemoryChoice> HeavyMemories { get; } = new(StringComparer.Ordinal);

    /// <summary>Perfil: vias e tipos de Memória contados, e os encontros recentes (ver <c>Systems.Profile</c>).</summary>
    public ProfileState Profile { get; } = new();

    /// <summary>Vendas por mercador e entrada (<c>mercador/entrada</c>), no degrau da Maré em que aconteceram.</summary>
    public Dictionary<string, MerchantSale> Merchants { get; } = new(StringComparer.Ordinal);

    /// <summary>Trívias acesas (ligadas à rede de viagem).</summary>
    public HashSet<string> LitTrivias { get; } = new(StringComparer.Ordinal);

    /// <summary>Julgamentos feitos (id → destino da sombra) e quantos perjúrios houve no Tribunal.</summary>
    public Dictionary<string, Lete.Core.World.ShadeDestination> Trials { get; } = new(StringComparer.Ordinal);

    public int Perjuries { get; set; }

    /// <summary>Itens únicos: os que tem (com o estágio), o equipado e os conjuntos reunidos.</summary>
    public ItemsState Items { get; } = new();

    /// <summary>A Bolsa: óbolos, oferendas e provisões.</summary>
    public InventoryState Inventory { get; } = new();

    /// <summary>Fôlego e Lucidez gastos desde o último descanso, e as fontes de cura já usadas.</summary>
    public VitalsState Vitals { get; } = new();

    /// <summary>Conversas: opções já escolhidas, comentários do companheiro feitos, sombras despertas.</summary>
    public DialogueState Dialogue { get; } = new();

    /// <summary>Juramentos já jurados (id → ativo, quebrado ou cumprido), ver <c>Systems.Oaths</c>.</summary>
    public Dictionary<string, OathStatus> Oaths { get; } = new(StringComparer.Ordinal);

    /// <summary>Chaves do Caminho obtidas (id da chave → forma que a deu); nunca se perdem.</summary>
    public Dictionary<string, string> PathKeys { get; } = new(StringComparer.Ordinal);

    /// <summary>Fragmentos de origem já ganhos, na ordem em que vieram.</summary>
    public List<string> OriginFragments { get; } = new();

    /// <summary>Região onde o jogo foi salvo (id da cena; por enquanto só a sala de teste). Nula antes de salvar.</summary>
    public string? RegionId { get; set; }

    /// <summary>Posição do protagonista na região, em metros. Nula antes de salvar.</summary>
    public System.Numerics.Vector3? Position { get; set; }

    public HashSet<string> Flags { get; } = new(StringComparer.Ordinal);

    /// <summary>Modo de dificuldade (id de <c>content/difficulty</c>), travado no começo da partida.</summary>
    public string Difficulty { get; set; } = "estige";

    /// <summary>Como terminou o último encontro com cada sombra (ver <c>Systems.ShadeRecord</c>).</summary>
    public Dictionary<string, Lete.Core.Conditions.ShadeState> Shades { get; } = new(StringComparer.Ordinal);

    /// <summary>Oferendas partilhadas, como <c>sombra/tipo</c> (condição <c>offering_shared</c>).</summary>
    public HashSet<string> SharedOfferings { get; } = new(StringComparer.Ordinal);

    /// <summary>Ecos guardados nos espaços (ids, na ordem em que foram absorvidos).</summary>
    public List<string> Echoes { get; } = new();

    /// <summary>O que cada sombra guarda de uma derrota do protagonista (volta se ela for vencida).</summary>
    public Dictionary<string, HoardState> Hoards { get; } = new(StringComparer.Ordinal);

    /// <summary>Regiões já visitadas (condição <c>region_visited</c>).</summary>
    public HashSet<string> VisitedRegions { get; } = new(StringComparer.Ordinal);

    /// <summary>Entradas do Diário, como chaves de localização, na ordem em que foram escritas.</summary>
    public List<string> JournalEntries { get; } = new();
}
