using Godot;

namespace Lete.Game.Combat;

/// <summary>
/// Parâmetros das animações por código da batalha (respiração, dano, avanço, derrota).
/// Ficam num recurso editável no Inspector, não no código. Distâncias em pixels de tela (1080p) ou metros.
/// </summary>
[GlobalClass]
public partial class BattleAnimTuning : Resource
{
    [ExportGroup("Respiração")]
    [Export] public float BreathePixels { get; set; } = 1.5f;
    [Export] public float BreatheSeconds { get; set; } = 2.4f;

    [ExportGroup("Sombras (placeholder)")]
    /// <summary>Flutuação das sombras no lugar da respiração: subida e descida lenta, em metros.</summary>
    [Export] public float FloatMeters { get; set; } = 0.05f;
    [Export] public float FloatSeconds { get; set; } = 3.2f;

    [ExportGroup("Disco no chão")]
    [Export] public float BlobAlphaLiving { get; set; } = 0.45f;
    [Export] public float BlobAlphaSpirit { get; set; } = 0.18f;

    [ExportGroup("Anel de turno")]
    /// <summary>Raio do anel sob quem age, em alturas de personagem.</summary>
    [Export] public float TurnRingRadius { get; set; } = 0.3f;
    [Export] public float TurnRingThickness { get; set; } = 0.025f;
    [Export] public float TurnRingPulseSeconds { get; set; } = 1.2f;
    [Export] public float TurnRingPulseScale { get; set; } = 0.08f;

    [ExportGroup("Dano")]
    [Export] public float HitShakePixels { get; set; } = 8f;
    [Export] public int HitShakeCount { get; set; } = 4;
    [Export] public float HitShakeStepSeconds { get; set; } = 0.04f;
    [Export] public float HitFlashSeconds { get; set; } = 0.18f;

    /// <summary>Força da tremida e da piscada por resultado do gesto (0 a 1).</summary>
    [Export] public float HitStrengthPerfect { get; set; } = 1f;
    [Export] public float HitStrengthGood { get; set; } = 0.6f;
    [Export] public float HitStrengthFailed { get; set; } = 0.3f;

    [ExportGroup("Avanço de ataque")]
    /// <summary>Distância do avanço, em metros (o personagem tem 1,7 m de altura).</summary>
    [Export] public float AdvanceMeters { get; set; } = 1f;
    [Export] public float CharacterHeightMeters { get; set; } = 1.7f;
    [Export] public float AdvanceSeconds { get; set; } = 0.18f;
    [Export] public float AdvanceHoldSeconds { get; set; } = 0.12f;
    [Export] public float ReturnSeconds { get; set; } = 0.3f;

    [ExportGroup("Faísca do acerto")]
    [Export] public int SparkAmount { get; set; } = 14;
    [Export] public float SparkSeconds { get; set; } = 0.35f;

    /// <summary>Tamanho do grão em pixels da arte; altura do ponto de acerto (0 = pés, 1 = cabeça); velocidade em alturas/s.</summary>
    [Export] public float SparkPixels { get; set; } = 2f;
    [Export] public float SparkHeight { get; set; } = 0.55f;
    [Export] public float SparkSpeed { get; set; } = 0.9f;

    [ExportGroup("Derrota")]
    [Export] public float FallDegrees { get; set; } = 90f;
    [Export] public float FallSeconds { get; set; } = 0.5f;
    [Export] public float FadeSeconds { get; set; } = 0.7f;

    [ExportGroup("Partir em paz (sombra libertada)")]
    /// <summary>Quanto a sombra sobe enquanto some, em alturas de personagem, e em quanto tempo.</summary>
    [Export] public float DepartRise { get; set; } = 0.25f;
    [Export] public float DepartSeconds { get; set; } = 2.2f;

    [ExportGroup("Ser absorvida")]
    /// <summary>Quanto do caminho até quem absorve a sombra percorre (0 a 1) e em quanto tempo.</summary>
    [Export] public float AbsorbTravel { get; set; } = 0.7f;
    [Export] public float AbsorbSeconds { get; set; } = 1.2f;

    [ExportGroup("Desintegrar (sombras)")]
    [Export] public float DissolveSeconds { get; set; } = 1.4f;
    [Export] public int DustAmount { get; set; } = 220;
    [Export] public float DustLifetimeSeconds { get; set; } = 1.3f;

    /// <summary>Tamanho de cada grão de pó, em pixels da arte.</summary>
    [Export] public float DustPixels { get; set; } = 2f;

    /// <summary>Quanto o pó sobe e é levado para o lado, em alturas de personagem por segundo².</summary>
    [Export] public float DustRise { get; set; } = 0.35f;
    [Export] public float DustDrift { get; set; } = 0.12f;
    [Export] public Color DustColor { get; set; } = new(0.5f, 0.55f, 0.63f, 0.95f);
}
