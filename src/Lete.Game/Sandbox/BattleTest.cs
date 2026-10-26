using System.Collections.Generic;
using Godot;
using Lete.Core.Data;
using Lete.Game.Combat;

namespace Lete.Game.Sandbox;

/// <summary>
/// Teste de elenco da batalha: grupo embaixo à esquerda, sombra em cima à direita, na arte escolhida (pixel_160).
/// <c>switch_art</c> (Tab) passa pelo elenco (<c>cast_cycle</c> de <c>content/battle/teste_elenco.json</c>): quem tem
/// arte de costas entra no grupo, e a arte de frente aparece no lugar da sombra. <c>debug_hit</c> (H) testa as animações.
/// </summary>
public partial class BattleTest : BattleStage
{
    private int _hits;
    private bool _busy;
    private IReadOnlyList<string> _cycle = new List<string>();
    private int _current;

    public override void _Ready()
    {
        base._Ready();
        _cycle = CastData.CycleFromJson(FileAccess.GetFileAsString(SetupPath));

        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--demo") >= 0)
        {
            RunDemo();
        }
    }

    /// <summary>Com <c>-- --demo</c>: passa pelo elenco e repete o teste do H, para gravar vídeo sem jogar.</summary>
    private async void RunDemo()
    {
        while (IsInsideTree())
        {
            await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);
            if (!_busy)
            {
                ShowNext();
                PlayHitTest();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("switch_art"))
        {
            ShowNext();
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

    /// <summary>A próxima pessoa do elenco: de costas no grupo (se tiver essa arte) e de frente no lugar da sombra.</summary>
    private void ShowNext()
    {
        if (_cycle.Count == 0)
        {
            return;
        }

        _current = (_current + 1) % _cycle.Count;
        var member = Cast[_cycle[_current]];
        if (member.BackArt is not null)
        {
            Replace(enemy: false, 0, member.AsActor(back: true));
        }

        Replace(enemy: true, 0, member.AsActor(back: false));
        GD.Print($"Elenco: {member.Id}");
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
