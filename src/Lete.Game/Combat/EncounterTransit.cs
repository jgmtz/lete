using Godot;
using Lete.Core.World;

namespace Lete.Game.Combat;

/// <summary>
/// A passagem do mapa para a luta e de volta: o mapa (<c>Main</c>) guarda o encontro e troca para a cena da batalha; a
/// batalha lê o encontro daqui e, no fim, volta ao mapa (o <c>Main</c> recoloca o jogador onde estava).
/// </summary>
public static class EncounterTransit
{
    public const string BattleScene = "res://src/Lete.Game/Sandbox/BattleAttackTest.tscn";
    public const string MapScene = "res://src/Lete.Game/Main.tscn";

    /// <summary>O encontro da luta em andamento; nulo quando a batalha foi aberta direto (o teste).</summary>
    public static Encounter? Pending { get; private set; }

    public static void Begin(SceneTree tree, Encounter encounter)
    {
        Pending = encounter;
        tree.CallDeferred(SceneTree.MethodName.ChangeSceneToFile, BattleScene);
    }

    public static void Return(SceneTree tree)
    {
        Pending = null;
        tree.CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MapScene);
    }
}
