namespace Lete.Core.Presentation;

/// <summary>
/// Densidade de pixel única para sprites e texturas do cenário (ARTE.md, 2.1).
/// O enquadramento é fixo (o personagem ocupa sempre a mesma altura na tela);
/// o que muda com o tamanho do sprite é quantos pixels ele tem.
/// </summary>
/// <param name="SpriteHeightPx">Altura do sprite do personagem em pixels (teste: 32, 48 ou 64).</param>
/// <param name="CharacterHeightMeters">Altura do personagem no mundo 3D.</param>
/// <param name="CharacterScreenHeightPx">Altura do personagem na tela, na resolução de referência.</param>
/// <param name="ViewportHeightPx">Altura da resolução de referência (ex.: 1080).</param>
public sealed record PixelDensity(
    int SpriteHeightPx,
    float CharacterHeightMeters,
    int CharacterScreenHeightPx,
    int ViewportHeightPx)
{
    /// <summary>Pixels de arte por metro do mundo; vale para sprites e para texturas do cenário.</summary>
    public float PixelsPerMeter => SpriteHeightPx / CharacterHeightMeters;

    /// <summary>Tamanho de um pixel de arte em metros (o <c>pixel_size</c> do Sprite3D).</summary>
    public float PixelSize => CharacterHeightMeters / SpriteHeightPx;

    /// <summary>Altura visível da câmera ortográfica, em metros.</summary>
    public float CameraSize => CharacterHeightMeters * ViewportHeightPx / CharacterScreenHeightPx;

    /// <summary>
    /// Fator de redução do viewport de baixa resolução: renderizar em (tela / fator) faz
    /// 1 pixel de arte = 1 pixel renderizado. Precisa ser inteiro para a ampliação ficar limpa.
    /// </summary>
    public int LowResShrink
    {
        get
        {
            if (CharacterScreenHeightPx % SpriteHeightPx != 0)
            {
                throw new InvalidOperationException(
                    $"A altura na tela ({CharacterScreenHeightPx}px) precisa ser múltipla da altura do sprite ({SpriteHeightPx}px).");
            }

            return CharacterScreenHeightPx / SpriteHeightPx;
        }
    }

    /// <summary>Escala UV (triplanar no mundo) para uma textura de <paramref name="textureSizePx"/> pixels.</summary>
    public float TriplanarScale(int textureSizePx) => PixelsPerMeter / textureSizePx;
}
