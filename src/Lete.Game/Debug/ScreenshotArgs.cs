using System.Threading.Tasks;
using Godot;

namespace Lete.Game.Debug;

/// <summary>
/// Conferir telas sem jogar à mão: com <c>-- --shot=caminho.png --shot-at=1.5,20</c> salva a janela real
/// (no tamanho e proporção dela, ao contrário do <c>--write-movie</c>) em cada segundo pedido e fecha o jogo
/// depois do último. Cada imagem ganha o sufixo do segundo (<c>caminho_20.png</c>).
/// </summary>
public static class ScreenshotArgs
{
    public static async void Run(Node node)
    {
        var args = OS.GetCmdlineUserArgs();
        var path = System.Array.Find(args, a => a.StartsWith("--shot="))?["--shot=".Length..];
        if (path is null)
        {
            return;
        }

        var at = System.Array.Find(args, a => a.StartsWith("--shot-at="))?["--shot-at=".Length..] ?? "1";
        var elapsed = 0f;
        foreach (var part in at.Split(','))
        {
            var seconds = float.Parse(part, System.Globalization.CultureInfo.InvariantCulture);
            await node.ToSignal(node.GetTree().CreateTimer(seconds - elapsed), SceneTreeTimer.SignalName.Timeout);
            elapsed = seconds;
            var file = path.Replace(".png", $"_{part}.png");
            var error = node.GetViewport().GetTexture().GetImage().SavePng(file);
            GD.Print($"screenshot: {file} ({error}, {node.GetViewport().GetVisibleRect().Size})");
        }

        node.GetTree().Quit();
    }
}
