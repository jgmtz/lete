using Lete.Core.Combat.Gestures;

namespace Lete.Core.Combat;

/// <summary>Um valor por resultado de gesto (ex.: quanto de Firmeza o ataque tira em cada nível).</summary>
public sealed record GradeValues(float Perfect, float Good, float Failed)
{
    public GradeValues Scaled(float factor) => new(Perfect * factor, Good * factor, Failed * factor);

    public float For(GestureGrade grade) => grade switch
    {
        GestureGrade.Perfect => Perfect,
        GestureGrade.Good => Good,
        _ => Failed,
    };
}
