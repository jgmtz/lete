using Godot;
using Lete.Game.Combat;

namespace Lete.Game.Sandbox;

/// <summary>
/// Teste de estilo da batalha (BATALHA_TESTE.md): grupo embaixo à esquerda, sombra em cima à direita,
/// alternando entre as versões de arte (liso, pixel_128, pixel_160) com a mesma altura na tela.
/// <c>switch_art</c> (Tab) e <c>art_style_1..3</c> (1/2/3) trocam a arte; <c>debug_hit</c> (H) testa as animações.
/// </summary>
public partial class BattleTest : BattleStage
{
    private int _hits;
    private bool _busy;

    public override void _Ready()
    {
        base._Ready();

        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--demo") >= 0)
        {
            RunDemo();
        }
    }

    /// <summary>Com <c>-- --demo</c>: repete o teste do H sozinho, para gravar vídeo sem jogar.</summary>
    private async void RunDemo()
    {
        while (IsInsideTree())
        {
            await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);
            if (!_busy)
            {
                PlayHitTest();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("switch_art"))
        {
            ApplyStyle((Style + 1) % Setup.Styles.Count);
        }
        else if (@event.IsActionPressed("art_style_1"))
        {
            ApplyStyle(0);
        }
        else if (@event.IsActionPressed("art_style_2"))
        {
            ApplyStyle(1);
        }
        else if (@event.IsActionPressed("art_style_3"))
        {
            ApplyStyle(2);
        }
        else if (@event.IsActionPressed("debug_hit"))
        {
            PlayHitTest();
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    /// <summary>H: o primeiro do grupo avança e a sombra sente o golpe; a cada terceiro golpe, ela é derrotada.</summary>
    private async void PlayHitTest()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        var attacker = Party[0].Node;
        var target = Enemies[0].Node;

        await attacker.PlayAdvance(target.Position);
        _hits++;
        await target.PlayHit();
        if (_hits % 3 == 0)
        {
            await target.PlayDefeat();
            await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
            target.Revive();
        }

        _busy = false;
    }
}
