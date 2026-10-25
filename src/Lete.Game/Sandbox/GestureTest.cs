using System.Collections.Generic;
using Godot;
using Lete.Core.Combat.Gestures;
using Lete.Core.Data;
using Lete.Game.Combat.Gestures;
using Lete.Game.UI;

namespace Lete.Game.Sandbox;

/// <summary>
/// Cena de teste dos gestos (M1): repete o Anel sem parar para sentir o ritmo.
/// Mostra o resultado e, só aqui, o valor numérico e a contagem, para ajustar os números.
/// </summary>
public partial class GestureTest : Control
{
    private const string DifficultyPath = "res://content/difficulty/estige.json";
    private const string RingPath = "res://content/gestures/anel_basico.json";

    /// <summary>Pausa entre um anel e o próximo, para ler o resultado.</summary>
    [Export] public float PauseSeconds { get; set; } = 0.8f;

    private static readonly Color Gold = new("#C8A24A");
    private static readonly Color Ash = new("#6B6670");

    private readonly Dictionary<GestureGrade, int> _tally = new()
    {
        [GestureGrade.Perfect] = 0,
        [GestureGrade.Good] = 0,
        [GestureGrade.Failed] = 0,
    };

    private GestureWindows _windows = null!;
    private RingGesture _ring = null!;
    private Label _grade = null!;
    private Label _debug = null!;

    private static string T(string key) => GameRoot.Instance.Text(key);

    public override void _Ready()
    {
        _windows = GestureData.WindowsFromDifficulty(FileAccess.GetFileAsString(DifficultyPath));

        _ring = GetNode<RingGesture>("Ring");
        _ring.Timing = GestureData.RingFromJson(FileAccess.GetFileAsString(RingPath));
        _ring.Completed += OnCompleted;

        GetNode<Label>("Hint").Text = T("gesto.anel.dica");
        _grade = GetNode<Label>("Grade");
        _debug = GetNode<Label>("Debug");
        _grade.Text = "";
        UpdateDebug(null);

        StartNext();
    }

    private void StartNext()
    {
        _grade.Text = "";
        _ring.Begin();
    }

    private async void OnCompleted(float score)
    {
        var grade = _windows.Grade(score);
        _tally[grade]++;

        _grade.Text = T(grade switch
        {
            GestureGrade.Perfect => "gesto.perfeito",
            GestureGrade.Good => "gesto.bom",
            _ => "gesto.falho",
        });
        _ring.CircleColor = grade switch
        {
            GestureGrade.Perfect => Gold,
            GestureGrade.Good => UiStyle.Palette.Ivory,
            _ => Ash,
        };
        UpdateDebug(score);

        await ToSignal(GetTree().CreateTimer(PauseSeconds), SceneTreeTimer.SignalName.Timeout);
        StartNext();
    }

    // Texto técnico só desta cena de teste (não é interface do jogo).
    private void UpdateDebug(float? score) =>
        _debug.Text = $"valor {(score is { } s ? s.ToString("0.00") : "-")}   " +
                      $"perfeito {_tally[GestureGrade.Perfect]} · bom {_tally[GestureGrade.Good]} · falho {_tally[GestureGrade.Failed]}";
}
