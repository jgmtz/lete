using System.Collections.Generic;
using Godot;

namespace Lete.Game.Sandbox;

/// <summary>
/// Roteiro de entrada para gravar o teste do andar sem jogar à mão: aperta as ações do Input Map
/// em sequência e fecha o jogo no fim. Ativado com <c>-- --demo</c> na linha de comando.
/// </summary>
public partial class WalkDemo : Node
{
    private readonly record struct Step(float Seconds, params string[] Actions);

    private static readonly Step[] Script =
    {
        new(0.8f),
        new(1.5f, "move_right"),
        new(1.5f, "move_down"),
        new(1.5f, "move_left"),
        new(1.5f, "move_up"),
        new(0.8f),
        new(1.5f, "move_right", "run"),
        new(1.5f, "move_down", "run"),
        new(1.5f, "move_left", "run"),
        new(1.5f, "move_up", "run"),
        new(1.0f),
    };

    private readonly HashSet<string> _pressed = new();
    private int _index = -1;
    private float _remaining;

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining > 0)
        {
            return;
        }

        foreach (var action in _pressed)
        {
            Input.ActionRelease(action);
        }

        _pressed.Clear();

        if (++_index >= Script.Length)
        {
            GetTree().Quit();
            return;
        }

        var step = Script[_index];
        _remaining = step.Seconds;
        foreach (var action in step.Actions)
        {
            Input.ActionPress(action);
            _pressed.Add(action);
        }
    }
}
