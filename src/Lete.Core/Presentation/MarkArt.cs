using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Presentation;

/// <summary>
/// Qual imagem do corpo o inventário mostra (<c>docs/PROTAGONISTA_SOMBRA.md</c> 6): a da etapa da Marca e, depois
/// que a regra C decide, a do rosto. Os arquivos ficam na pasta <c>inventory_folder</c> do elenco.
/// </summary>
public static class MarkArt
{
    public static string ImageFor(string folder, int stage, FinalFace face) => face switch
    {
        FinalFace.Returned => $"{folder}/rosto_devolvido.png",
        FinalFace.Covered => $"{folder}/rosto_coberto.png",
        _ => $"{folder}/etapa{Math.Clamp(stage, 0, MarkTracker.HighStage)}.png",
    };

    /// <summary>Todas as imagens que a pasta precisa ter.</summary>
    public static IEnumerable<string> AllImages(string folder) =>
        Enumerable.Range(0, MarkTracker.HighStage + 1).Select(s => ImageFor(folder, s, FinalFace.None))
            .Append(ImageFor(folder, 0, FinalFace.Returned))
            .Append(ImageFor(folder, 0, FinalFace.Covered));
}
