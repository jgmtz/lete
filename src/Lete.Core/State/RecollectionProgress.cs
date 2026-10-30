namespace Lete.Core.State;

/// <summary>
/// O que o jogador já ouviu de uma Lembrança (só os fatos; estágio e confiança saem das regras do
/// <c>Recollections.Mosaic</c>).
/// </summary>
public sealed class RecollectionProgress
{
    /// <summary>Fontes ouvidas, sem as que contradizem.</summary>
    public HashSet<string> Sources { get; } = new(StringComparer.Ordinal);

    /// <summary>O ato que a tornou plena; nulo antes.</summary>
    public string? FullAct { get; set; }

    /// <summary>Contradita enquanto incerta: a pedra sumiu (o Tribunal ainda sabe que existiu).</summary>
    public bool False { get; set; }
}
