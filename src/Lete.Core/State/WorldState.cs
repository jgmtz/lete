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

    /// <summary>Entradas do Diário, como chaves de localização, na ordem em que foram escritas.</summary>
    public List<string> JournalEntries { get; } = new();
}
