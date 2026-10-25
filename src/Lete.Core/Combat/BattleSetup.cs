using System.Numerics;

namespace Lete.Core.Combat;

/// <summary>
/// Posições da batalha no chão, relativas ao centro da câmera: x para a direita da tela, y para o fundo
/// (positivo = mais alto na tela). Grupo embaixo à esquerda, sombras em cima à direita (GDD 4.7).
/// <paramref name="DepthScale"/>: quanto menor fica quem está mais ao fundo (0 = todos iguais; no máximo
/// <see cref="MaxDepthScale"/>). Com 0, os pixels da arte continuam inteiros na tela.
/// </summary>
public sealed record BattleLayout(IReadOnlyList<Vector2> Party, IReadOnlyList<Vector2> Enemies, float DepthScale = 0f)
{
    public const float MaxDepthScale = 0.1f;

    /// <summary>Escala de quem está em <paramref name="ground"/>: 1 na posição mais à frente, 1 - DepthScale na mais ao fundo.</summary>
    public float ScaleAt(Vector2 ground)
    {
        var all = Party.Concat(Enemies).ToList();
        var front = all.Min(p => p.Y);
        var back = all.Max(p => p.Y);
        return back <= front ? 1f : 1f - DepthScale * (ground.Y - front) / (back - front);
    }

    /// <summary>Problemas de layout (lista vazia = válido).</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (DepthScale is < 0f or > MaxDepthScale)
        {
            errors.Add($"depth_scale precisa ficar entre 0 e {MaxDepthScale} (diferença sutil de tamanho).");
        }

        if (Party.Count == 0)
        {
            errors.Add("O grupo precisa de pelo menos uma posição.");
        }

        if (Enemies.Count == 0)
        {
            errors.Add("É preciso pelo menos uma posição de sombra.");
        }

        for (var i = 0; i < Party.Count; i++)
        {
            if (Party[i].X >= 0 || Party[i].Y >= 0)
            {
                errors.Add($"party[{i}] precisa ficar embaixo à esquerda (x < 0 e y < 0).");
            }
        }

        for (var i = 0; i < Enemies.Count; i++)
        {
            if (Enemies[i].X <= 0 || Party.Any(p => Enemies[i].Y <= p.Y))
            {
                errors.Add($"enemy[{i}] precisa ficar à direita e acima de todo o grupo.");
            }
        }

        return errors;
    }
}

/// <summary>
/// Arte de um participante da batalha e a altura dele no mundo (as versões de arte ficam todas com essa altura).
/// <paramref name="Portrait"/>: imagem do retrato pequeno (turno); <paramref name="NameKey"/>: chave do nome verdadeiro.
/// </summary>
public sealed record BattleActorArt(
    string Id, string Art, float Height, bool FlipH, string? Tint, string? Portrait = null, string? NameKey = null);

/// <summary>Uma versão de arte para comparar (liso, pixel_128, pixel_160).</summary>
public sealed record BattleArtStyle(string Id, string Folder, bool Smooth);

public sealed record BattleSetup(
    BattleLayout Layout,
    IReadOnlyList<BattleActorArt> Party,
    IReadOnlyList<BattleActorArt> Enemies,
    IReadOnlyList<BattleArtStyle> Styles)
{
    /// <summary>
    /// O protagonista fica sempre na posição do grupo mais à direita da tela (decisão do João, 25/out/2026).
    /// Devolve o problema, ou nulo se a posição estiver certa.
    /// </summary>
    public string? ProtagonistSlotError(string protagonistId)
    {
        var index = Party.ToList().FindIndex(p => p.Id == protagonistId);
        if (index < 0)
        {
            return $"Protagonista '{protagonistId}' não está no grupo.";
        }

        var rightmost = Enumerable.Range(0, Party.Count).MaxBy(i => Layout.Party[i].X);
        return index == rightmost
            ? null
            : $"O protagonista '{protagonistId}' precisa ficar na posição mais à direita do grupo (hoje é '{Party[rightmost].Id}').";
    }
}
