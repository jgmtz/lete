namespace Lete.Core.Combat;

/// <summary>
/// Ids das opções do menu da batalha (os mesmos que o conteúdo cita, por exemplo nos encontros do tutorial) e das
/// defesas na vez da sombra, que também podem fazer parte de uma lição.
/// </summary>
public static class BattleActions
{
    public const string Attack = "atacar";
    public const string Memory = "memoria";
    public const string Approach = "aproximar";
    public const string Absorb = "absorver";
    public const string Echo = "eco";
    public const string Bag = "bolsa";
    public const string Flee = "fugir";
    public const string Listen = "escutar";
    public const string Free = "libertar";
    public const string Offering = "oferenda";
    public const string Witness = "presenciar";
    public const string Spare = "poupar";
    public const string Yield = "ceder";

    /// <summary>Defesas (vez da sombra): aparar um golpe direto (Anel) e esquivar de um padrão (Esquiva).</summary>
    public const string Parry = "aparar";
    public const string Dodge = "esquivar";

    /// <summary>As opções que ficam dentro de Aproximar.</summary>
    public static readonly IReadOnlySet<string> ApproachGroup =
        new HashSet<string>(StringComparer.Ordinal) { Listen, Free, Offering, Witness, Spare, Yield };

    /// <summary>Todas as opções do menu (principal e Aproximar).</summary>
    public static readonly IReadOnlySet<string> Menu =
        new HashSet<string>(ApproachGroup.Concat(new[] { Attack, Memory, Approach, Absorb, Echo, Bag, Flee }), StringComparer.Ordinal);

    /// <summary>O que uma lição pode pedir: as opções do menu e as duas defesas.</summary>
    public static readonly IReadOnlySet<string> Learnable =
        new HashSet<string>(Menu.Concat(new[] { Parry, Dodge }), StringComparer.Ordinal);
}
