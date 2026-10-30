namespace Lete.Core.State;

/// <summary>Estado de um juramento jurado: ativo, quebrado (perjúrio, irreversível) ou cumprido (terminou).</summary>
public enum OathStatus
{
    Active,
    Broken,
    Kept,
}
